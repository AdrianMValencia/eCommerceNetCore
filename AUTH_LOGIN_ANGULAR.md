# Login JWT — Contratos y permisos para Angular

## Endpoint

- **Método:** `POST`
- **URL:** `/api/auth/login`
- **Autenticación:** anónimo — no requiere token

---

## Modelos TypeScript

### Respuesta base (aplica a todos los endpoints)

```typescript
export interface BaseError {
  propertyName?: string;
  errorMessage?: string;
}

export interface BaseResponse<T> {
  isSuccess: boolean;
  data?: T | null;
  message?: string;
  errors?: BaseError[] | null;
}
```

### Request de login

```typescript
export interface LoginRequest {
  email: string;    // "admin@demo.com"
  password: string; // "123456"
}
```

### Response de login

```typescript
export interface LoginResponse {
  token: string;            // JWT firmado listo para usar en Authorization header
  tokenType: string;        // siempre "Bearer"
  expiresInMinutes: number; // 120 (configurable en Jwt:ExpirationMinutes)
  userId: number;
  username: string;
  email: string;
  userType: string;         // "ADMIN" | "USER"
  roles: string[];          // ["ADMIN"] | ["USER"]
}
```

---

## Ejemplos JSON

### Request

```json
{
  "email": "admin@demo.com",
  "password": "123456"
}
```

### Response exitosa — HTTP 200

```json
{
  "isSuccess": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresInMinutes": 120,
    "userId": 1,
    "username": "admin",
    "email": "admin@demo.com",
    "userType": "ADMIN",
    "roles": ["ADMIN"]
  },
  "message": "Login exitoso.",
  "errors": null
}
```

### Response con credenciales inválidas — HTTP 400

```json
{
  "isSuccess": false,
  "data": null,
  "message": "Credenciales inválidas.",
  "errors": null
}
```

### Response con validaciones fallidas — HTTP 400

```json
{
  "isSuccess": false,
  "data": null,
  "message": "Validation failed.",
  "errors": [
    { "propertyName": "Email", "errorMessage": "Email is required." },
    { "propertyName": "Password", "errorMessage": "Password is required." }
  ]
}
```

---

## Firma del servicio Angular

```typescript
login(request: LoginRequest): Observable<BaseResponse<LoginResponse>> {
  return this.http.post<BaseResponse<LoginResponse>>(
    `${environment.apiUrl}/api/auth/login`,
    request
  );
}
```

---

## Header para endpoints protegidos

```typescript
Authorization: `Bearer ${loginResponse.data.token}`
```

---

## Lógica de permisos basada en `userType`

```typescript
export type UserRole = 'ADMIN' | 'USER';

export const ROLE_PERMISSIONS: Record<UserRole, string[]> = {
  ADMIN: [
    'orders.read',
    'orders.write',
    'payments.manage',
    'products.read',
    'products.write',
    'categories.read',
    'categories.write',
    'users.read'
  ],
  USER: [
    'products.read',
    'products.write',
    'categories.read',
    'categories.write',
    'users.read'
  ]
};
```

---

## Matriz de acceso por rol

| Endpoint | Método | USER | ADMIN | Anónimo |
|---|---|:---:|:---:|:---:|
| `/api/auth/login` | POST | ✅ | ✅ | ✅ |
| `/api/users` | POST | ✅ | ✅ | ✅ |
| `/api/payments/paypal/webhook` | POST | ✅ | ✅ | ✅ |
| `/api/users/{id}` | GET | ✅ | ✅ | ❌ |
| `/api/users/by-email/{email}` | GET | ✅ | ✅ | ❌ |
| `/api/categories` | GET | ✅ | ✅ | ❌ |
| `/api/categories/{id}` | GET | ✅ | ✅ | ❌ |
| `/api/categories` | POST | ✅ | ✅ | ❌ |
| `/api/categories/{id}` | PUT | ✅ | ✅ | ❌ |
| `/api/categories/{id}` | DELETE | ✅ | ✅ | ❌ |
| `/api/products` | GET | ✅ | ✅ | ❌ |
| `/api/products/{id}` | GET | ✅ | ✅ | ❌ |
| `/api/products` | POST | ✅ | ✅ | ❌ |
| `/api/products/{id}` | PUT | ✅ | ✅ | ❌ |
| `/api/products/{id}` | DELETE | ✅ | ✅ | ❌ |
| `/api/orders` | GET | ❌ | ✅ | ❌ |
| `/api/orders/{id}` | GET | ❌ | ✅ | ❌ |
| `/api/orders` | POST | ❌ | ✅ | ❌ |
| `/api/orders/{id}` | PUT | ❌ | ✅ | ❌ |
| `/api/orders/{id}/state` | PUT | ❌ | ✅ | ❌ |
| `/api/orders/{id}` | DELETE | ❌ | ✅ | ❌ |
| `/api/payments/paypal/orders` | POST | ❌ | ✅ | ❌ |
| `/api/payments/paypal/orders/{id}/capture` | POST | ❌ | ✅ | ❌ |

---

## Reglas sugeridas en Angular

- Guardar `token` en `localStorage` o `sessionStorage`.
- Guardar `userType` y `roles` junto al token.
- Usar un `AuthGuard` que lea `userType` para proteger rutas.
- Usar un `HttpInterceptor` que agregue el header `Authorization` automáticamente.
- Si `userType === 'ADMIN'`, habilitar rutas de órdenes y pagos.
- Si `userType === 'USER'`, redirigir a módulos de catálogo y perfil.
- Si `isSuccess === false` aunque el HTTP sea `200`, tratar como error de negocio.
