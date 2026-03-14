# Implementación de Login con JWT y BCrypt en `eCommerce.Api`

## Objetivo

Se implementó autenticación con `JWT` y encriptación de contraseñas con `BCrypt.Net-Next`, siguiendo la arquitectura vertical existente del backend:

- `Carter`
- `Handlers`
- `DTOs por feature`
- documentación orientada a integración frontend

---

## Cambios realizados

### Paquetes agregados

- `BCrypt.Net-Next`
- `Microsoft.AspNetCore.Authentication.JwtBearer`

### Nuevos archivos

- `src/eCommerce.Api/Options/JwtOptions.cs`
- `src/eCommerce.Api/Services/Auth/IJwtTokenGenerator.cs`
- `src/eCommerce.Api/Services/Auth/JwtTokenGenerator.cs`
- `src/eCommerce.Api/Features/Auth/Login.cs`

### Archivos modificados

- `src/eCommerce.Api/eCommerce.Api.csproj`
- `src/eCommerce.Api/DependencyInjection.cs`
- `src/eCommerce.Api/Program.cs`
- `src/eCommerce.Api/Features/Users/CreateUser.cs`
- `src/eCommerce.Api/appsettings.json`
- `src/eCommerce.Api/appsettings.Development.json`

---

## 1. Configuración JWT

Se agregó una nueva sección `Jwt` en configuración.

### Ejemplo

```json
"Jwt": {
  "Issuer": "eCommerce.Api",
  "Audience": "eCommerce.Angular",
  "SecretKey": "JwtSuperSecretKeyForDevelopmentOnly_ChangeThisInProduction_123456789",
  "ExpirationMinutes": 120
}
```

### Significado

- `Issuer`: emisor del token
- `Audience`: consumidor esperado del token
- `SecretKey`: clave simétrica usada para firmar el JWT
- `ExpirationMinutes`: tiempo de vida del token

---

## 2. Generación del token JWT

Se creó el servicio:

- `IJwtTokenGenerator`
- `JwtTokenGenerator`

### Claims incluidos en el token

- `sub`: `UserId`
- `unique_name`: `Username`
- `email`: `Email`
- `role`: `UserType`
- `userType`: `UserType`

Esto permite que el frontend y futuros endpoints protegidos conozcan:

- identidad del usuario
- correo
- rol (`ADMIN` o `USER`)

---

## 3. Hash de contraseñas con BCrypt

En `CreateUser.cs` se modificó el registro para que la contraseña **no se guarde en texto plano**.

### Antes
Se almacenaba directamente `command.Password`.

### Ahora
Se usa:

```csharp
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(command.Password);
```

Y en el login se valida con:

```csharp
BCrypt.Net.BCrypt.Verify(command.Password, user.Password)
```

---

## 4. Endpoint de login implementado

### Ruta

`POST /api/auth/login`

### Request

```json
{
  "email": "admin@demo.com",
  "password": "123456"
}
```

### Response exitosa

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
    "roles": [
      "ADMIN"
    ]
  },
  "message": "Login exitoso.",
  "errors": null
}
```

### Response inválida

```json
{
  "isSuccess": false,
  "data": null,
  "message": "Credenciales inválidas.",
  "errors": null
}
```

---

## 5. Request y Response del login

### Request model

```csharp
public sealed class Command : ICommand<Response>
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
```

### Response model

```csharp
public sealed class Response
{
    public string Token { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresInMinutes { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string UserType { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
}
```

---

## 6. Middleware agregado al pipeline

En `Program.cs` se agregó:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Esto deja la API lista para proteger endpoints con JWT en pasos siguientes.

---

## 7. Validaciones agregadas

### `CreateUser`
- `Username` requerido
- `Password` requerida
- mínimo 6 caracteres
- `Firstname` requerido
- `Lastname` requerido
- `Email` válido

### `Login`
- `Email` requerido y válido
- `Password` requerida

---

## 8. Flujo completo de autenticación

### Registro
1. Frontend envía `POST /api/users`
2. Backend valida el request
3. Backend encripta la contraseña con `BCrypt`
4. Backend persiste el usuario

### Login
1. Frontend envía `POST /api/auth/login`
2. Backend busca usuario por `Email`
3. Backend compara la contraseña con el hash usando `BCrypt.Verify`
4. Si es válida, genera el JWT
5. Devuelve token y roles (`UserType`)

---

## 9. Cómo consumirlo desde Angular

### Login request

```json
{
  "email": "admin@demo.com",
  "password": "123456"
}
```

### Guardar token

El frontend debe guardar:

- `token`
- `tokenType`
- `roles`
- `userType`

### Header para llamadas autenticadas

```http
Authorization: Bearer {token}
```

---

## 10. Buenas prácticas para producción

1. Cambiar `Jwt:SecretKey` por una clave segura y larga.
2. No dejar secretos en `appsettings.json`.
3. Usar variables de entorno o secret stores.
4. No devolver nunca la contraseña del usuario.
5. Proteger endpoints sensibles con autorización.
6. Agregar refresh tokens si luego se requiere sesión prolongada.
7. Agregar expiración y revocación controlada de tokens.

---

## 11. Endpoints involucrados

### Crear usuario
- **POST** `/api/users`

### Login
- **POST** `/api/auth/login`

---

## 12. Resultado final

Con esta implementación el backend ya permite:

- registrar usuarios con contraseña cifrada
- autenticar usuarios con email y password
- devolver un JWT firmado
- devolver el rol del usuario basado en `UserType`
- dejar lista la infraestructura para proteger endpoints futuros con `[Authorize]` o equivalentes en Minimal APIs
