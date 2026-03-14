using eCommerce.Api.Options;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace eCommerce.Api.Services.Payments.PayPal;

/// <summary>
/// Servicio de integración con la API de PayPal Checkout.
/// Encapsula toda la comunicación HTTP con los endpoints de PayPal:
/// autenticación OAuth, creación de órdenes, captura de pagos y verificación de webhooks.
/// </summary>
/// <remarks>
/// Se registra como un HttpClient tipado, por lo que <see cref="HttpClient"/>
/// ya tiene configurado el <c>BaseAddress</c> con la URL base de PayPal
/// (sandbox o live) desde <see cref="PayPalOptions.BaseUrl"/>.
/// </remarks>
public sealed class PayPalService(
    HttpClient httpClient,
    IOptions<PayPalOptions> options,
    ILogger<PayPalService> logger) : IPayPalService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly PayPalOptions _options = options.Value;
    private readonly ILogger<PayPalService> _logger = logger;

    /// <summary>
    /// Crea una orden de pago en PayPal a partir de una orden local del sistema.
    /// </summary>
    /// <param name="request">
    /// Datos de la orden local: <c>OrderId</c>, <c>Amount</c>, <c>Currency</c> y <c>Description</c>.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación del request HTTP entrante.</param>
    /// <returns>
    /// <see cref="PayPalCreateOrderResult"/> con el <c>PayPalOrderId</c> y el <c>ApprovalUrl</c>
    /// al que debe redirigirse el usuario para aprobar el pago.
    /// </returns>
    /// <remarks>
    /// Flujo interno:
    /// 1. Valida que la configuración tenga todos los valores obligatorios.
    /// 2. Obtiene un <c>access_token</c> OAuth de PayPal.
    /// 3. Envía el payload de la orden a <c>POST /v2/checkout/orders</c>.
    /// 4. Extrae el <c>approval_url</c> de la colección de links de la respuesta.
    /// </remarks>
    public async Task<PayPalCreateOrderResult> CreateOrderAsync(PayPalCreateOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Verifica que todas las claves de configuración necesarias estén presentes.
            ValidateConfiguration();

            var accessToken = await GetAccessTokenAsync(cancellationToken);

            // Se construye el payload según la especificación de PayPal Orders v2.
            // intent=CAPTURE indica que el pago se capturará inmediatamente al ser aprobado.
            var payload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        // reference_id y custom_id permiten correlacionar la orden PayPal
                        // con la orden interna del sistema.
                        reference_id = request.OrderId.ToString(),
                        custom_id = request.OrderId.ToString(),
                        description = request.Description,
                        amount = new
                        {
                            currency_code = request.Currency,
                            // Se formatea con InvariantCulture para evitar problemas de decimales
                            // según el locale del servidor (ej. "250,00" en vez de "250.00").
                            value = request.Amount.ToString("0.00", CultureInfo.InvariantCulture)
                        }
                    }
                },
                application_context = new
                {
                    // PayPal redirigirá al usuario a estas URLs tras aprobar o cancelar el pago.
                    return_url = _options.ReturnUrl,
                    cancel_url = _options.CancelUrl,
                    // PAY_NOW muestra el botón de pago directo sin pantalla intermedia de revisión.
                    user_action = "PAY_NOW",
                    // NO_SHIPPING evita que PayPal solicite dirección de envío al comprador.
                    shipping_preference = "NO_SHIPPING"
                }
            };

            var response = await SendAuthorizedAsync(HttpMethod.Post, "/v2/checkout/orders", accessToken, payload, cancellationToken);
            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Se extrae el mensaje de error real devuelto por PayPal para diagnóstico preciso.
                var errorMessage = ExtractPayPalErrorMessage(rawResponse) ?? "No fue posible crear la orden en PayPal.";
                _logger.LogError("PayPal create order failed with status {StatusCode}. Body: {Body}", response.StatusCode, rawResponse);
                return new PayPalCreateOrderResult(false, null, null, null, rawResponse, errorMessage);
            }

            using var document = JsonDocument.Parse(rawResponse);
            var root = document.RootElement;

            // PayPal devuelve una colección de links; se busca el de rel="approve" o "payer-action".
            var approvalUrl = GetApprovalUrl(root);

            if (string.IsNullOrWhiteSpace(approvalUrl))
            {
                // Si no viene el link de aprobación, no se puede continuar el checkout.
                _logger.LogError("PayPal create order response does not contain an approval link. Body: {Body}", rawResponse);
                return new PayPalCreateOrderResult(false, null, root.GetProperty("status").GetString(), null, rawResponse, "PayPal no devolvió el approval_url para continuar el checkout.");
            }

            return new PayPalCreateOrderResult(
                true,
                root.GetProperty("id").GetString(),
                root.GetProperty("status").GetString(),
                approvalUrl,
                rawResponse,
                null);
        }
        catch (Exception ex)
        {
            // Se devuelve el detalle real de la excepción para diagnóstico en tiempo de desarrollo.
            _logger.LogError(ex, "Error creating PayPal order for local order {OrderId}", request.OrderId);
            return new PayPalCreateOrderResult(false, null, null, null, null, $"Ocurrió un error al comunicarse con PayPal. Detalle: {ex.Message}");
        }
    }

    /// <summary>
    /// Captura el pago de una orden PayPal previamente aprobada por el comprador.
    /// </summary>
    /// <param name="paypalOrderId">
    /// Identificador de la orden en PayPal, obtenido del parámetro <c>token</c>
    /// que PayPal envía en la URL de retorno al frontend.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación del request HTTP entrante.</param>
    /// <returns>
    /// <see cref="PayPalCaptureOrderResult"/> con el <c>CaptureId</c>, el <c>Status</c>
    /// y el email del comprador si está disponible.
    /// </returns>
    /// <remarks>
    /// Este método debe llamarse solo después de que el usuario haya aprobado el pago en PayPal.
    /// Si el status del capture es <c>COMPLETED</c>, el pago fue exitoso y la orden local
    /// debe marcarse como <c>PAID</c>.
    /// </remarks>
    public async Task<PayPalCaptureOrderResult> CaptureOrderAsync(string paypalOrderId, CancellationToken cancellationToken)
    {
        try
        {
            ValidateConfiguration();

            var accessToken = await GetAccessTokenAsync(cancellationToken);

            // El endpoint de captura no requiere body; PayPal identifica la orden por la URL.
            var response = await SendAuthorizedAsync(HttpMethod.Post, $"/v2/checkout/orders/{paypalOrderId}/capture", accessToken, new { }, cancellationToken);
            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = ExtractPayPalErrorMessage(rawResponse) ?? "No fue posible capturar la orden en PayPal.";
                _logger.LogError("PayPal capture failed for order {PayPalOrderId} with status {StatusCode}. Body: {Body}", paypalOrderId, response.StatusCode, rawResponse);
                return new PayPalCaptureOrderResult(false, paypalOrderId, null, null, null, rawResponse, errorMessage);
            }

            using var document = JsonDocument.Parse(rawResponse);
            var root = document.RootElement;

            // PayPal retorna los detalles del capture dentro de purchase_units[0].payments.captures[0].
            var purchaseUnit = root.GetProperty("purchase_units")[0];
            var capture = purchaseUnit.GetProperty("payments").GetProperty("captures")[0];

            // El email del comprador puede no estar disponible en todos los escenarios.
            var payerEmail = root.TryGetProperty("payer", out var payer) && payer.TryGetProperty("email_address", out var email)
                ? email.GetString()
                : null;

            return new PayPalCaptureOrderResult(
                true,
                root.GetProperty("id").GetString(),
                capture.GetProperty("id").GetString(),
                capture.GetProperty("status").GetString(),
                payerEmail,
                rawResponse,
                null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal order {PayPalOrderId}", paypalOrderId);
            return new PayPalCaptureOrderResult(false, paypalOrderId, null, null, null, null, $"Ocurrió un error al capturar el pago en PayPal. Detalle: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica la autenticidad de una notificación webhook recibida desde PayPal.
    /// </summary>
    /// <param name="request">
    /// Headers y evento recibidos del webhook de PayPal:
    /// <c>TransmissionId</c>, <c>TransmissionTime</c>, <c>TransmissionSignature</c>,
    /// <c>CertUrl</c>, <c>AuthAlgorithm</c> y el <c>WebhookEvent</c> en bruto.
    /// </param>
    /// <param name="cancellationToken">Token de cancelación del request HTTP entrante.</param>
    /// <returns>
    /// <see cref="PayPalWebhookVerificationResult"/> indicando si la verificación fue
    /// exitosa (<c>SUCCESS</c>) o fallida (<c>FAILED</c>).
    /// </returns>
    /// <remarks>
    /// PayPal firma cada evento con los headers HTTP que llegan al endpoint webhook.
    /// Este método envía esos mismos headers junto con el <c>webhook_id</c> configurado
    /// a <c>POST /v1/notifications/verify-webhook-signature</c> para confirmar la firma.
    /// Si la verificación falla, el evento no debe procesarse para evitar fraudes.
    /// </remarks>
    public async Task<PayPalWebhookVerificationResult> VerifyWebhookAsync(PayPalWebhookVerificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            ValidateConfiguration();

            var accessToken = await GetAccessTokenAsync(cancellationToken);

            // PayPal verifica que los headers y el evento coincidan con la firma enviada.
            var payload = new
            {
                transmission_id = request.TransmissionId,
                transmission_time = request.TransmissionTime,
                cert_url = request.CertUrl,
                auth_algo = request.AuthAlgorithm,
                transmission_sig = request.TransmissionSignature,
                // webhook_id identifica de forma unívoca el webhook registrado en PayPal Developer.
                webhook_id = _options.WebhookId,
                webhook_event = request.WebhookEvent
            };

            var response = await SendAuthorizedAsync(HttpMethod.Post, "/v1/notifications/verify-webhook-signature", accessToken, payload, cancellationToken);
            var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = ExtractPayPalErrorMessage(rawResponse) ?? "No fue posible verificar el webhook con PayPal.";
                _logger.LogError("PayPal webhook verification failed with status {StatusCode}. Body: {Body}", response.StatusCode, rawResponse);
                return new PayPalWebhookVerificationResult(false, "FAILED", errorMessage);
            }

            using var document = JsonDocument.Parse(rawResponse);
            // verification_status puede ser "SUCCESS" o "FAILURE".
            var verificationStatus = document.RootElement.GetProperty("verification_status").GetString() ?? "FAILED";
            return new PayPalWebhookVerificationResult(verificationStatus == "SUCCESS", verificationStatus, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayPal webhook signature");
            return new PayPalWebhookVerificationResult(false, "FAILED", $"Ocurrió un error al verificar el webhook de PayPal. Detalle: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene un token de acceso OAuth 2.0 de PayPal usando el flujo
    /// <c>client_credentials</c>.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación del request HTTP entrante.</param>
    /// <returns>El <c>access_token</c> listo para incluir como Bearer en los requests.</returns>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si PayPal responde con error o no devuelve el token en la respuesta.
    /// </exception>
    /// <remarks>
    /// Las credenciales se codifican en Base64 como <c>clientId:clientSecret</c>
    /// según el estándar HTTP Basic Auth (RFC 7617).
    /// Este token tiene una vida útil limitada (normalmente ~9 horas en sandbox).
    /// En producción de alta concurrencia conviene cachear el token para evitar
    /// solicitar uno nuevo en cada petición.
    /// </remarks>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/oauth2/token");

        // Se codifican las credenciales en Base64 usando UTF8 tal como exige PayPal.
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // El body debe ser application/x-www-form-urlencoded con grant_type=client_credentials.
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Se extrae el mensaje real de PayPal para distinguir entre credenciales
            // inválidas, cuenta suspendida u otros errores de autenticación.
            var errorMessage = ExtractPayPalErrorMessage(rawResponse) ?? "No fue posible obtener el token de PayPal.";
            _logger.LogError("PayPal token request failed with status {StatusCode}. Body: {Body}", response.StatusCode, rawResponse);
            throw new InvalidOperationException(errorMessage);
        }

        using var document = JsonDocument.Parse(rawResponse);
        return document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayPal no devolvió access_token.");
    }

    /// <summary>
    /// Envía un request HTTP autenticado con Bearer token a la API de PayPal.
    /// </summary>
    /// <param name="method">Método HTTP a usar (normalmente POST).</param>
    /// <param name="uri">Ruta relativa al BaseAddress del HttpClient.</param>
    /// <param name="accessToken">Token OAuth obtenido desde <see cref="GetAccessTokenAsync"/>.</param>
    /// <param name="payload">Objeto que se serializa como JSON en el body del request.</param>
    /// <param name="cancellationToken">Token de cancelación del request HTTP entrante.</param>
    /// <returns>El <see cref="HttpResponseMessage"/> crudo devuelto por PayPal.</returns>
    private async Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string uri, string accessToken, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);

        // Se agrega el token como Bearer para autenticar el request frente a PayPal.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // PayPal requiere Content-Type: application/json en todos sus endpoints.
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        return await _httpClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Valida que todos los valores obligatorios de la configuración de PayPal estén presentes.
    /// Se llama al inicio de cada operación para detectar problemas de configuración
    /// de forma temprana y con mensajes claros.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si alguna propiedad obligatoria está vacía o nula en la configuración.
    /// </exception>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("La configuración PayPal:BaseUrl es obligatoria.");

        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("La configuración PayPal:ClientId es obligatoria.");

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("La configuración PayPal:ClientSecret es obligatoria.");

        if (string.IsNullOrWhiteSpace(_options.ReturnUrl))
            throw new InvalidOperationException("La configuración PayPal:ReturnUrl es obligatoria.");

        if (string.IsNullOrWhiteSpace(_options.CancelUrl))
            throw new InvalidOperationException("La configuración PayPal:CancelUrl es obligatoria.");
    }

    /// <summary>
    /// Busca el link de aprobación del comprador dentro del array <c>links</c>
    /// de la respuesta de creación de orden de PayPal.
    /// </summary>
    /// <param name="root">Elemento raíz del JSON de respuesta de PayPal.</param>
    /// <returns>
    /// La URL de aprobación si existe, o <c>null</c> si no se encuentra ningún
    /// link con <c>rel</c> igual a <c>approve</c> o <c>payer-action</c>.
    /// </returns>
    /// <remarks>
    /// PayPal puede devolver el link con rel="approve" o rel="payer-action"
    /// dependiendo de la versión de la API y el tipo de integración.
    /// Se soportan ambos para mayor compatibilidad.
    /// </remarks>
    private static string? GetApprovalUrl(JsonElement root)
    {
        if (!root.TryGetProperty("links", out var links))
            return null;

        foreach (var link in links.EnumerateArray())
        {
            if (!link.TryGetProperty("rel", out var rel))
                continue;

            var relValue = rel.GetString();

            // Se aceptan "approve" (standard) y "payer-action" (usado en ACDC/advanced flows).
            if (relValue is not ("approve" or "payer-action"))
                continue;

            if (link.TryGetProperty("href", out var href))
                return href.GetString();
        }

        return null;
    }

    /// <summary>
    /// Extrae el mensaje de error legible de una respuesta JSON de error de PayPal.
    /// </summary>
    /// <param name="rawResponse">Cuerpo de respuesta en formato JSON de PayPal.</param>
    /// <returns>
    /// El mensaje de error extraído, o el <c>rawResponse</c> completo si no se puede parsear,
    /// o <c>null</c> si el string está vacío.
    /// </returns>
    /// <remarks>
    /// PayPal devuelve errores en distintos formatos según el endpoint:
    /// - <c>error_description</c>: errores OAuth (ej. invalid_client).
    /// - <c>message</c>: errores de la API REST (ej. UNPROCESSABLE_ENTITY).
    /// - <c>details[].description</c>: validaciones más detalladas (ej. monto inválido).
    /// </remarks>
    private static string? ExtractPayPalErrorMessage(string? rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
            return null;

        try
        {
            using var document = JsonDocument.Parse(rawResponse);
            var root = document.RootElement;

            // Prioridad 1: errores OAuth como "Client Authentication failed".
            if (root.TryGetProperty("error_description", out var errorDescription))
                return errorDescription.GetString();

            // Prioridad 2: mensaje general de error de la Orders API.
            if (root.TryGetProperty("message", out var message))
                return message.GetString();

            // Prioridad 3: detalles individuales de validación concatenados.
            if (root.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
            {
                var detailMessages = details.EnumerateArray()
                    .Select(x => x.TryGetProperty("description", out var description) ? description.GetString() : null)
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                var joined = string.Join(" | ", detailMessages!);
                if (!string.IsNullOrWhiteSpace(joined))
                    return joined;
            }
        }
        catch
        {
            // Si el parsing falla, se devuelve el raw como fallback para diagnóstico.
        }

        return rawResponse;
    }
}
