# Implementación de PayPal Checkout en `eCommerce.Api`

## Nota importante

La solución actual está construida en `.NET 10` con `Carter`, `handlers`, `DTOs por feature` y arquitectura vertical.  
Por ese motivo, la implementación realizada es el **equivalente idiomático en .NET** del flujo que normalmente se haría en `Spring Boot`.

---

## Objetivo funcional

Se implementó el siguiente flujo completo:

1. El cliente crea una orden en el sistema.
2. El backend genera una orden de pago en PayPal.
3. El backend devuelve el `approval_url` al frontend.
4. El usuario aprueba el pago en PayPal.
5. PayPal redirige al frontend.
6. El frontend llama al backend para capturar el pago.
7. El backend actualiza el estado de la orden en base de datos.
8. Adicionalmente, PayPal puede notificar eventos mediante `webhooks` para reconciliación.

---

## Archivos agregados

### Configuración
- `src/eCommerce.Api/Options/PayPalOptions.cs`

### Servicios
- `src/eCommerce.Api/Services/Payments/PayPal/IPayPalService.cs`
- `src/eCommerce.Api/Services/Payments/PayPal/PayPalService.cs`
- `src/eCommerce.Api/Services/Payments/PayPal/IPayPalPaymentStore.cs`
- `src/eCommerce.Api/Services/Payments/PayPal/PayPalPaymentStore.cs`

### Features verticales
- `src/eCommerce.Api/Features/Payments/PayPal/CreatePayPalOrder.cs`
- `src/eCommerce.Api/Features/Payments/PayPal/CapturePayPalOrder.cs`
- `src/eCommerce.Api/Features/Payments/PayPal/PayPalWebhook.cs`

### Infraestructura
- `src/eCommerce.Api/Database/DatabaseInitializer.cs`

---

## Archivos modificados

- `src/eCommerce.Api/DependencyInjection.cs`
- `src/eCommerce.Api/Program.cs`
- `src/eCommerce.Api/Enums/OrderState.cs`
- `src/eCommerce.Api/Features/Orders/CreateOrder.cs`
- `src/eCommerce.Api/appsettings.json`
- `src/eCommerce.Api/appsettings.Development.json`

---

## Configuración de PayPal Checkout API

Se agregó una sección `PayPal` en configuración:

- `BaseUrl`
- `ClientId`
- `ClientSecret`
- `WebhookId`
- `ReturnUrl`
- `CancelUrl`
- `Currency`

### Cómo se usa

El backend:

- Obtiene `access_token` desde `/v1/oauth2/token`
- Crea la orden PayPal mediante `/v2/checkout/orders`
- Captura la orden con `/v2/checkout/orders/{paypalOrderId}/capture`
- Verifica firmas de webhook con `/v1/notifications/verify-webhook-signature`

---

## Registro de dependencias

En `DependencyInjection.cs` se registró:

- `PayPalOptions` con `IOptions<T>`
- `HttpClient` tipado para `IPayPalService`
- `IPayPalPaymentStore`

Esto mantiene la arquitectura desacoplada:

- **Feature**: orquesta el caso de uso
- **Service**: integración con PayPal
- **Store**: persistencia de metadata de pagos

---

## Inicialización de base de datos

Se agregó `DatabaseInitializer.cs` y se ejecuta al arrancar la aplicación desde `Program.cs`.

### Tablas creadas automáticamente

#### `OrderPayments`
Guarda la relación entre la orden local y la orden externa de PayPal.

Campos principales:
- `OrderPaymentId`
- `OrderId`
- `Provider`
- `ProviderOrderId`
- `ProviderCaptureId`
- `Currency`
- `Amount`
- `Status`
- `ApprovalUrl`
- `PayerEmail`
- `RawCreateResponse`
- `RawCaptureResponse`
- `CreateDate`
- `UpdateDate`

#### `PayPalWebhookEvents`
Guarda auditoría de webhooks recibidos.

Campos principales:
- `PayPalWebhookEventId`
- `EventId`
- `EventType`
- `VerificationStatus`
- `ProviderOrderId`
- `Payload`
- `Headers`
- `CreateDate`

---

## Estados de orden

Se extendió `OrderState` para soportar pagos:

- `CANCELLED = 0`
- `CONFIRMED = 1`
- `PENDING_PAYMENT = 2`
- `PAID = 3`
- `PAYMENT_FAILED = 4`

### Cambio importante

`CreateOrder` ahora crea la orden en estado:

- `PENDING_PAYMENT`

Esto es correcto para un checkout con proveedor externo, ya que la orden local existe antes de que el pago quede confirmado.

---

## Endpoints implementados

## 1) Crear orden local

### `POST /api/orders`

Crea la orden local y devuelve el `orderId`.

### Request

```json
{
  "userId": 1,
  "orderDetails": [
    {
      "productId": 10,
      "quantity": 2,
      "price": 100
    },
    {
      "productId": 11,
      "quantity": 1,
      "price": 50
    }
  ]
}
```

### Response

```json
{
  "isSuccess": true,
  "data": {
    "orderId": 123,
    "total": 250,
    "orderState": "PENDING_PAYMENT"
  },
  "message": "Orden creada correctamente y pendiente de pago.",
  "errors": null
}
```

---

## 2) Crear orden de pago en PayPal

### `POST /api/payments/paypal/orders`

Genera la orden externa en PayPal usando el total de la orden local.

### Request

```json
{
  "orderId": 123
}
```

### Response

```json
{
  "isSuccess": true,
  "data": {
    "orderId": 123,
    "payPalOrderId": "6M123456AB123456C",
    "status": "CREATED",
    "approvalUrl": "https://www.sandbox.paypal.com/checkoutnow?token=6M123456AB123456C",
    "currency": "USD",
    "amount": 250
  },
  "message": "Orden de pago PayPal creada correctamente.",
  "errors": null
}
```

### Uso frontend

El frontend debe redirigir al usuario a `approvalUrl`.

---

## 3) Capturar pago aprobado

### `POST /api/payments/paypal/orders/{paypalOrderId}/capture`

Captura el pago luego de que el usuario vuelve desde PayPal.  
Normalmente el frontend obtiene el `token` desde la URL de retorno y lo envía como `paypalOrderId`.

### Route Param

- `paypalOrderId`

### Response

```json
{
  "isSuccess": true,
  "data": {
    "orderId": 123,
    "payPalOrderId": "6M123456AB123456C",
    "captureId": "4D7654321X9876543",
    "status": "COMPLETED",
    "orderState": "PAID",
    "payerEmail": "buyer@example.com"
  },
  "message": "Pago PayPal capturado correctamente.",
  "errors": null
}
```

### Efecto de negocio

- Actualiza `OrderPayments`
- Cambia el estado de la orden local a `PAID`

---

## 4) Webhook de PayPal

### `POST /api/payments/paypal/webhook`

Recibe eventos asíncronos desde PayPal y verifica la firma antes de procesarlos.

### Headers requeridos por PayPal

- `PayPal-Transmission-Id`
- `PayPal-Transmission-Time`
- `PayPal-Transmission-Sig`
- `PayPal-Cert-Url`
- `PayPal-Auth-Algo`

### Response ejemplo

```json
{
  "isSuccess": true,
  "data": {
    "verificationStatus": "SUCCESS",
    "eventId": "WH-1234567890",
    "eventType": "PAYMENT.CAPTURE.COMPLETED",
    "payPalOrderId": "6M123456AB123456C",
    "orderUpdated": true
  },
  "message": "Webhook PayPal procesado correctamente.",
  "errors": null
}
```

### Eventos contemplados

- `CHECKOUT.ORDER.APPROVED`
- `PAYMENT.CAPTURE.COMPLETED`
- `PAYMENT.CAPTURE.DENIED`
- `PAYMENT.CAPTURE.DECLINED`

### Comportamiento

- Si la verificación falla, el webhook no se procesa.
- Se guarda auditoría en `PayPalWebhookEvents`.
- Si llega `PAYMENT.CAPTURE.COMPLETED`, se marca la orden como `PAID`.

---

## Flujo completo recomendado para frontend

### Paso 1: crear orden local
Frontend llama:

`POST /api/orders`

Guarda `orderId`.

### Paso 2: crear orden PayPal
Frontend llama:

`POST /api/payments/paypal/orders`

con:

```json
{ "orderId": 123 }
```

### Paso 3: redirigir a PayPal
Frontend usa `approvalUrl` para enviar al usuario a PayPal.

### Paso 4: PayPal redirige al frontend
PayPal redirige a:

- `ReturnUrl` cuando aprueba
- `CancelUrl` cuando cancela

En la URL de éxito, PayPal suele devolver `token`.

### Paso 5: capturar pago
Frontend toma `token` y llama:

`POST /api/payments/paypal/orders/{token}/capture`

### Paso 6: confirmar UI
Si la respuesta viene con:

- `status = COMPLETED`
- `orderState = PAID`

entonces el checkout quedó cerrado correctamente.

---

## Manejo de errores implementado

## Validaciones

Se agregaron validaciones con `FluentValidation` para:

- `CreateOrder`
- `CreatePayPalOrder`
- `CapturePayPalOrder`
- `PayPalWebhook`

## Errores de integración externa

`PayPalService`:

- registra errores con `ILogger`
- devuelve mensajes de negocio estandarizados
- encapsula fallos HTTP y errores de serialización

## Errores de negocio

Casos manejados:

- orden local inexistente
- orden ya pagada
- `paypalOrderId` no asociado localmente
- captura no completada
- webhook sin firma válida

---

## Equivalencia conceptual con Spring Boot

Aunque la implementación es `.NET`, la equivalencia arquitectónica respecto a `Spring Boot` es:

- `Controller` -> `ICarterModule` por feature
- `Service` -> `PayPalService`
- `Repository`/`Store` -> `PayPalPaymentStore`
- `DTOs` -> `Command`, `Response`, records y modelos por feature
- `ConfigurationProperties` -> `PayPalOptions`

---

## Buenas prácticas para producción

## 1. No guardar secretos en `appsettings.json`
Usar preferentemente:

- variables de entorno
- Azure Key Vault
- User Secrets para desarrollo

## 2. Restringir webhook
- validar firma siempre
- permitir solo tráfico esperado
- registrar auditoría de todos los eventos

## 3. Idempotencia
- no recrear órdenes locales duplicadas
- tolerar reintentos del webhook
- evitar doble captura del mismo `paypalOrderId`

## 4. Observabilidad
- agregar correlación entre `orderId`, `paypalOrderId` y `captureId`
- monitorear eventos fallidos
- alertar cuando existan pagos aprobados no capturados

## 5. Conciliación
- usar webhook como fuente de reconciliación
- crear tareas programadas para revisar pagos inconsistentes

## 6. Seguridad
- nunca exponer `clientSecret` al frontend
- no capturar desde Angular directo contra PayPal
- todo acceso a PayPal debe pasar por backend

## 7. Estados de negocio claros
Sugerido:

- `PENDING_PAYMENT`
- `PAID`
- `PAYMENT_FAILED`
- `CANCELLED`

---

## Prueba manual rápida

## 1. Crear orden local
Llamar `POST /api/orders`

## 2. Crear pago PayPal
Llamar `POST /api/payments/paypal/orders`

## 3. Abrir `approvalUrl`
Aprobar el pago en sandbox.

## 4. Capturar
Tomar el `token` de la URL de retorno y llamar:

`POST /api/payments/paypal/orders/{token}/capture`

## 5. Verificar base de datos
- `Orders.OrderState = PAID`
- registro en `OrderPayments`
- auditoría en `PayPalWebhookEvents` si configuraste el webhook en PayPal Developer Dashboard

---

## Recomendación siguiente

Como siguiente paso conviene:

1. agregar pruebas de integración para los endpoints de pago
2. actualizar `ENDPOINTS_BACKEND.md` con los nuevos endpoints
3. crear servicios Angular (`OrdersService`, `PayPalCheckoutService`)
4. implementar la pantalla `checkout/paypal/success` consumiendo el endpoint de captura
