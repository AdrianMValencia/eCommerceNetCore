# Prompt profesional para integrar PayPal desde Angular

Copia y pega el siguiente prompt directamente a tu equipo frontend o a una herramienta de IA para generar la integración Angular.

---

## Prompt listo para usar

Actúa como un arquitecto frontend senior especializado en `Angular 20+`, `TypeScript`, `RxJS`, `HttpClient`, `Signals` y aplicaciones eCommerce.

Necesito que implementes el flujo completo de pagos con PayPal para un frontend Angular que consume una API backend `.NET 10`.

Quiero código real, listo para copiar y pegar, con `standalone components`, `inject()`, tipado fuerte y separación por `models`, `services`, `state`, `pages` y `routing`.

---

## Base URL del backend

Usa:

```ts
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7282'
};
```

---

## Contratos exactos del backend

### Respuesta común

```ts
export interface BaseError {
  propertyName?: string;
  errorMessage?: string;
}

export interface BaseResponse<T> {
  isSuccess: boolean;
  data?: T;
  message?: string;
  errors?: BaseError[] | null;
}
```

---

## Endpoint 1: crear orden local

### HTTP
- **Método:** `POST`
- **URL:** `/api/orders`

### Descripción
Crea la orden local y la deja en estado `PENDING_PAYMENT`.

### Request exacto

```ts
export interface CreateOrderDetailRequest {
  productId: number;
  quantity: number;
  price: number;
}

export interface CreateOrderRequest {
  userId: number;
  orderDetails: CreateOrderDetailRequest[];
}
```

### JSON ejemplo

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

### Response exacto

```ts
export interface OrderCreatedResponse {
  orderId: number;
  total: number;
  orderState: string;
}
```

### JSON ejemplo

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

### Firma exacta del cliente Angular

```ts
createOrder(request: CreateOrderRequest): Observable<BaseResponse<OrderCreatedResponse>>;
```

---

## Endpoint 2: crear orden PayPal

### HTTP
- **Método:** `POST`
- **URL:** `/api/payments/paypal/orders`

### Descripción
Genera la orden de pago en PayPal usando el `orderId` local y devuelve el `approvalUrl` para redirigir al usuario.

### Request exacto

```ts
export interface CreatePayPalOrderRequest {
  orderId: number;
}
```

### JSON ejemplo

```json
{
  "orderId": 123
}
```

### Response exacto

```ts
export interface CreatePayPalOrderResponse {
  orderId: number;
  payPalOrderId: string;
  status: string;
  approvalUrl: string;
  currency: string;
  amount: number;
}
```

### JSON ejemplo

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

### Firma exacta del cliente Angular

```ts
createPayPalOrder(request: CreatePayPalOrderRequest): Observable<BaseResponse<CreatePayPalOrderResponse>>;
```

---

## Endpoint 3: capturar pago PayPal

### HTTP
- **Método:** `POST`
- **URL:** `/api/payments/paypal/orders/{paypalOrderId}/capture`

### Descripción
Captura el pago aprobado en PayPal y actualiza la orden local a `PAID`.

### Route param exacto

```ts
paypalOrderId: string
```

### Request exacto
Sin body relevante. En Angular envía `{}`.

### Response exacto

```ts
export interface CapturePayPalOrderResponse {
  orderId: number;
  payPalOrderId: string;
  captureId: string;
  status: string;
  orderState: string;
  payerEmail?: string | null;
}
```

### JSON ejemplo

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

### Firma exacta del cliente Angular

```ts
capturePayPalOrder(payPalOrderId: string): Observable<BaseResponse<CapturePayPalOrderResponse>>;
```

---

## Firmas exactas de servicios Angular

Quiero que generes estos servicios exactamente con estas firmas:

### `orders.service.ts`

```ts
export abstract class IOrdersService {
  abstract createOrder(request: CreateOrderRequest): Observable<BaseResponse<OrderCreatedResponse>>;
}
```

### `paypal-checkout.service.ts`

```ts
export abstract class IPayPalCheckoutService {
  abstract createPayPalOrder(request: CreatePayPalOrderRequest): Observable<BaseResponse<CreatePayPalOrderResponse>>;
  abstract capturePayPalOrder(payPalOrderId: string): Observable<BaseResponse<CapturePayPalOrderResponse>>;
}
```

### Implementación concreta esperada

```ts
@Injectable({ providedIn: 'root' })
export class OrdersService implements IOrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  createOrder(request: CreateOrderRequest): Observable<BaseResponse<OrderCreatedResponse>> {
    return this.http.post<BaseResponse<OrderCreatedResponse>>(`${this.baseUrl}/api/orders`, request);
  }
}

@Injectable({ providedIn: 'root' })
export class PayPalCheckoutService implements IPayPalCheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  createPayPalOrder(request: CreatePayPalOrderRequest): Observable<BaseResponse<CreatePayPalOrderResponse>> {
    return this.http.post<BaseResponse<CreatePayPalOrderResponse>>(`${this.baseUrl}/api/payments/paypal/orders`, request);
  }

  capturePayPalOrder(payPalOrderId: string): Observable<BaseResponse<CapturePayPalOrderResponse>> {
    return this.http.post<BaseResponse<CapturePayPalOrderResponse>>(
      `${this.baseUrl}/api/payments/paypal/orders/${payPalOrderId}/capture`,
      {}
    );
  }
}
```

---

## Flujo exacto que debe implementar el frontend

Implementa este flujo exacto:

1. El usuario confirma el checkout.
2. Angular llama `createOrder(request)`.
3. Si `isSuccess === true`, toma `orderId`.
4. Angular llama `createPayPalOrder({ orderId })`.
5. Si `isSuccess === true`, redirige con `window.location.href = approvalUrl`.
6. PayPal regresa al frontend en la ruta `/checkout/paypal/success?token=PAYPAL_ORDER_ID`.
7. En esa página, Angular obtiene `token` desde `ActivatedRoute`.
8. Angular llama `capturePayPalOrder(token)`.
9. Si `response.data.status === 'COMPLETED'` y `response.data.orderState === 'PAID'`, mostrar éxito.
10. Si falla cualquier paso, mostrar error amigable y opción de reintento.

---

## Ejemplo exacto de orquestación RxJS

Quiero que generes una implementación equivalente a esta:

```ts
this.ordersService.createOrder(orderRequest).pipe(
  switchMap(orderResponse => {
    if (!orderResponse.isSuccess || !orderResponse.data) {
      throw new Error(orderResponse.message ?? 'No se pudo crear la orden');
    }

    return this.paypalCheckoutService.createPayPalOrder({
      orderId: orderResponse.data.orderId
    });
  })
).subscribe({
  next: payPalResponse => {
    if (!payPalResponse.isSuccess || !payPalResponse.data) {
      throw new Error(payPalResponse.message ?? 'No se pudo crear la orden PayPal');
    }

    window.location.href = payPalResponse.data.approvalUrl;
  },
  error: error => {
    console.error(error);
  }
});
```

Y para la pantalla de retorno:

```ts
const token = this.route.snapshot.queryParamMap.get('token');

if (!token) {
  throw new Error('PayPal token no encontrado en la URL');
}

this.paypalCheckoutService.capturePayPalOrder(token).subscribe({
  next: response => {
    if (!response.isSuccess || !response.data) {
      throw new Error(response.message ?? 'No se pudo capturar el pago');
    }

    if (response.data.status === 'COMPLETED' && response.data.orderState === 'PAID') {
      // mostrar éxito
    }
  }
});
```

---

## Estructura de archivos esperada

Quiero que organices el frontend así:

```text
src/app/core/models/
  base-response.model.ts
  order.model.ts
  paypal.model.ts

src/app/core/services/
  orders.service.ts
  paypal-checkout.service.ts

src/app/features/checkout/pages/
  checkout-page.component.ts
  paypal-success-page.component.ts
  paypal-cancel-page.component.ts
  payment-error-page.component.ts

src/app/features/checkout/state/
  checkout.state.ts

src/app/features/checkout/
  checkout.routes.ts
```

---

## Reglas técnicas obligatorias

- Usa `standalone components`.
- Usa `inject()` en vez de constructor cuando aplique.
- Usa `takeUntilDestroyed()`.
- Usa `signals` para estado local de UI.
- Tipa completamente requests y responses.
- Maneja tanto errores HTTP como respuestas con `isSuccess = false`.
- No uses código ficticio ni placeholders ambiguos.
- Todo debe quedar listo para producción básica.

---

## Extras que debes incluir

Además del código, incluye:

1. una explicación breve del flujo
2. manejo de loading y disabled states
3. helper para convertir `BaseResponse.errors` en mensajes de formulario
4. ejemplo de botón `Pagar con PayPal`
5. ejemplo de pantalla de éxito y cancelación
6. rutas configuradas para:
   - `/checkout`
   - `/checkout/paypal/success`
   - `/checkout/paypal/cancel`

---

## Formato de salida esperado

Devuélveme la respuesta en Markdown, organizada por archivo, con código completo y listo para usar en Angular.
