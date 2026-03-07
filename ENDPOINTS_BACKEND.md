# Documentación de Endpoints - eCommerce API

## Información general

- **Proyecto:** `eCommerce.Api` (.NET 10)
- **Estilo de endpoints:** `Carter` (Minimal API)
- **Base URL local (según `eCommerce.Api.http`):** `https://localhost:7282`
- **Ruta base API:** `https://localhost:7282/api`

> Nota para Angular: la API devuelve una envoltura estándar `BaseResponse<T>` para los endpoints de negocio. En muchos casos el estado HTTP es `200 OK` incluso cuando `isSuccess = false`.

---

## Modelos compartidos

### `BaseResponse<T>`

```json
{
  "isSuccess": true,
  "data": {},
  "message": "string",
  "errors": [
    {
      "propertyName": "string",
      "errorMessage": "string"
    }
  ]
}
```

### `BaseError`

```json
{
  "propertyName": "string",
  "errorMessage": "string"
}
```

### Enums

- `UserType`
  - `1 = ADMIN`
  - `2 = USER`
- `OrderState`
  - `0 = CANCELLED`
  - `1 = CONFIRMED`

---

## Módulo `Users`

### 1) Obtener usuario por ID

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/users/{userId}`
- **Controller/Handler:** `GetUserByIdEndpoint` / `GetUserById.Handler`
- **Descripción:** Obtiene un usuario por identificador.
- **Request model:** No aplica (sin body).
- **Response model:** `BaseResponse<UserResponse>`
- **Route params:**
  - `userId` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": {
    "userId": 1,
    "username": "adrian",
    "password": "hashed-or-plain",
    "firstname": "Adrian",
    "lastname": "Valencia",
    "email": "adrian@example.com",
    "address": "Lima",
    "cellphone": "999999999",
    "userType": 2,
    "createDate": "2026-01-01T10:00:00Z",
    "updateDate": "2026-01-01T10:00:00Z"
  },
  "message": "Usuario obtenido exitosamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404` (ruta no coincide), `500`.

---

### 2) Obtener usuario por email

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/users/by-email/{email}`
- **Controller/Handler:** `GetUserByEmailEndpoint` / `GetUserByEmail.Handler`
- **Descripción:** Obtiene un usuario por correo electrónico.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<UserResponse>`
- **Route params:**
  - `email` (`string`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:** igual estructura al endpoint anterior.
- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

### 3) Crear usuario

- **Método HTTP:** `POST`
- **Ruta completa:** `https://localhost:7282/api/users`
- **Controller/Handler:** `CreateUserEndpoint` / `CreateUser.Handler`
- **Descripción:** Crea un nuevo usuario.
- **Request model:** `CreateUser.Command`
- **Response model:** `BaseResponse<bool>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "userId": 0,
  "username": "newuser",
  "password": "123456",
  "firstname": "Juan",
  "lastname": "Pérez",
  "email": "juan@example.com",
  "address": "Lima",
  "cellphone": "900000000",
  "userType": 2
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se registró correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400` (payload inválido), `415`, `500`.

---

## Módulo `Categories`

### 4) Listar categorías

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/categories`
- **Controller/Handler:** `GetAllCategoriesEndpoint` / `GetAllCategories.Handler`
- **Descripción:** Devuelve todas las categorías.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<IEnumerable<Category>>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": [
    {
      "categoryId": 1,
      "name": "Mouses",
      "createDate": "2026-01-01T00:00:00Z",
      "updateDate": "2026-01-01T00:00:00Z"
    }
  ],
  "message": "Categorías obtenidas correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `500`.

---

### 5) Obtener categoría por ID

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/categories/{id}`
- **Controller/Handler:** `GetByIdCategoryEndpoint` / `GetByIdCategory.Handler`
- **Descripción:** Devuelve una categoría por ID.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<Category?>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": {
    "categoryId": 2,
    "name": "Teclados",
    "createDate": "2026-01-02T00:00:00Z",
    "updateDate": "2026-01-02T00:00:00Z"
  },
  "message": "Categoría encontrada.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

### 6) Crear categoría

- **Método HTTP:** `POST`
- **Ruta completa:** `https://localhost:7282/api/categories`
- **Controller/Handler:** `CreateCategoryEndpoint` / `CreateCategory.Handler`
- **Descripción:** Crea una categoría.
- **Request model:** `CreateCategory.Command`
- **Response model:** `BaseResponse<bool>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "name": "Teclados"
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se registró correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `415`, `500`.

---

### 7) Actualizar categoría

- **Método HTTP:** `PUT`
- **Ruta completa:** `https://localhost:7282/api/categories/{id}`
- **Controller/Handler:** `UpdateCategoryEndpoint` / `UpdateCategory.Handler`
- **Descripción:** Actualiza el nombre de una categoría.
- **Request model:** `UpdateCategory.Command` (el `id` se toma de la ruta)
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "name": "Accesorios"
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se actualizó correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `404`, `500`.

---

### 8) Eliminar categoría

- **Método HTTP:** `DELETE`
- **Ruta completa:** `https://localhost:7282/api/categories/{id}`
- **Controller/Handler:** `DeleteCategoryEndpoint` / `DeleteCategory.Handler`
- **Descripción:** Elimina una categoría por ID.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se eliminó correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

## Módulo `Products`

### 9) Listar productos

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/products`
- **Controller/Handler:** `GetAllProductsEndpoint` / `GetAllProducts.Handler`
- **Descripción:** Devuelve todos los productos.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<IEnumerable<Product>>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": [
    {
      "productId": 1,
      "name": "Mouse Gamer",
      "code": "MOU-001",
      "description": "RGB",
      "urlImage": "https://cdn/image.png",
      "price": 120.5,
      "createDate": "2026-01-02T00:00:00Z",
      "updateDate": "2026-01-02T00:00:00Z",
      "userId": 1,
      "categoryId": 2
    }
  ],
  "message": "Productos obtenidos correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `500`.

---

### 10) Obtener producto por ID

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/products/{id}`
- **Controller/Handler:** `GetByIdProductEndpoint` / `GetByIdProduct.Handler`
- **Descripción:** Devuelve un producto por ID.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<Product?>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": {
    "productId": 1,
    "name": "Mouse Gamer",
    "code": "MOU-001",
    "description": "RGB",
    "urlImage": "https://cdn/image.png",
    "price": 120.5,
    "createDate": "2026-01-02T00:00:00Z",
    "updateDate": "2026-01-02T00:00:00Z",
    "userId": 1,
    "categoryId": 2
  },
  "message": "Producto encontrado.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

### 11) Crear producto

- **Método HTTP:** `POST`
- **Ruta completa:** `https://localhost:7282/api/products`
- **Controller/Handler:** `CreateProductEndpoint` / `CreateProduct.Handler`
- **Descripción:** Crea un producto.
- **Request model:** `CreateProduct.Command`
- **Response model:** `BaseResponse<bool>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "name": "Mouse Gamer",
  "code": "MOU-001",
  "description": "RGB",
  "urlImage": "https://cdn/image.png",
  "price": 120.5,
  "userId": 1,
  "categoryId": 2
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se registró correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `415`, `500`.

---

### 12) Actualizar producto

- **Método HTTP:** `PUT`
- **Ruta completa:** `https://localhost:7282/api/products/{id}`
- **Controller/Handler:** `UpdateProductEndpoint` / `UpdateProduct.Handler`
- **Descripción:** Actualiza un producto por ID.
- **Request model:** `UpdateProduct.Command` (el `id` viene por ruta)
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "name": "Mouse Pro",
  "code": "MOU-001",
  "description": "RGB 2",
  "urlImage": "https://cdn/new-image.png",
  "price": 135,
  "userId": 1,
  "categoryId": 2
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se actualizó correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `404`, `500`.

---

### 13) Eliminar producto

- **Método HTTP:** `DELETE`
- **Ruta completa:** `https://localhost:7282/api/products/{id}`
- **Controller/Handler:** `DeleteProductEndpoint` / `DeleteProduct.Handler`
- **Descripción:** Elimina un producto por ID.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Se eliminó correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

## Módulo `Orders`

### 14) Listar órdenes

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/orders`
- **Controller/Handler:** `GetAllOrdersEndpoint` / `GetAllOrders.Handler`
- **Descripción:** Devuelve todas las órdenes.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<IEnumerable<Order>>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 1,
      "orderDate": "2026-01-10T12:00:00Z",
      "orderState": 1,
      "user": null,
      "orderDetails": null,
      "total": 250
    }
  ],
  "message": "Órdenes obtenidas correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `500`.

---

### 15) Obtener orden por ID

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/api/orders/{id}`
- **Controller/Handler:** `GetByIdOrderEndpoint` / `GetByIdOrder.Handler`
- **Descripción:** Devuelve una orden por ID.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<Order?>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": {
    "id": 1,
    "orderDate": "2026-01-10T12:00:00Z",
    "orderState": 1,
    "user": null,
    "orderDetails": null,
    "total": 250
  },
  "message": "Orden obtenida correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

### 16) Crear orden

- **Método HTTP:** `POST`
- **Ruta completa:** `https://localhost:7282/api/orders`
- **Controller/Handler:** `CreateOrderEndpoint` / `CreateOrder.Handler`
- **Descripción:** Crea una orden con sus detalles.
- **Request model:** `CreateOrder.Command`
- **Response model:** `BaseResponse<bool>`
- **Route params:** Ninguno.
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

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

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Orden creada correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `415`, `500`.

---

### 17) Actualizar orden

- **Método HTTP:** `PUT`
- **Ruta completa:** `https://localhost:7282/api/orders/{id}`
- **Controller/Handler:** `UpdateOrderEndpoint` / `UpdateOrder.Handler`
- **Descripción:** Actualiza una orden y sus detalles.
- **Request model:** `UpdateOrder.Command` (el `orderId` viene por ruta)
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "orderDetails": [
    {
      "orderDetailId": 1,
      "productId": 10,
      "quantity": 3,
      "price": 300
    }
  ]
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Orden actualizada correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `404`, `500`.

---

### 18) Actualizar estado de orden

- **Método HTTP:** `PUT`
- **Ruta completa:** `https://localhost:7282/api/orders/{id}/state`
- **Controller/Handler:** `UpdateOrderStateEndpoint` / `UpdateOrderState.Handler`
- **Descripción:** Actualiza el estado de una orden.
- **Request model:** `UpdateOrderState.Command`
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:**

```json
{
  "orderState": 0
}
```

- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Estado de la orden actualizado correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `400`, `404`, `500`.

---

### 19) Eliminar orden

- **Método HTTP:** `DELETE`
- **Ruta completa:** `https://localhost:7282/api/orders/{id}`
- **Controller/Handler:** `DeleteOrderEndpoint` / `DeleteOrder.Handler`
- **Descripción:** Elimina una orden por ID.
- **Request model:** No aplica.
- **Response model:** `BaseResponse<bool>`
- **Route params:**
  - `id` (`int`)
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:**

```json
{
  "isSuccess": true,
  "data": true,
  "message": "Orden eliminada correctamente.",
  "errors": null
}
```

- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

## Endpoint de infraestructura

### OpenAPI (solo `Development`)

- **Método HTTP:** `GET`
- **Ruta completa:** `https://localhost:7282/openapi/{documentName}.json`
- **Controller/Handler:** Mapeo global en `Program.cs` (`app.MapOpenApi()`)
- **Descripción:** Expone el documento OpenAPI para inspección.
- **Request model:** No aplica.
- **Response model:** Documento OpenAPI JSON.
- **Route params:**
  - `documentName` (`string`) normalmente `v1`
- **Query params:** Ninguno.
- **Ejemplo Request JSON:** No aplica.
- **Ejemplo Response JSON:** no fijo (spec OpenAPI).
- **Códigos HTTP posibles:** `200`, `404`, `500`.

---

## Sugerencia de consumo en Angular

1. Crear interfaces compartidas:
   - `BaseResponse<T>`
   - `BaseError`
   - `User`, `Category`, `Product`, `Order`, `OrderDetail`
2. Consumir siempre verificando `response.isSuccess` además del status HTTP.
3. En formularios, mapear `response.errors` por `propertyName`.
4. Centralizar `baseUrl` en `environment.ts`.
