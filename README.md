# Gestion-Siniestros-Api
 Prueba técnica Zurich, backend

API REST para registrar y dar seguimiento a siniestros. ASP.NET Core (.NET 9), Entity Framework Core 9 (Code First con migraciones) y SQL Server.

## Requisitos previos

- .NET SDK 9 (probado con 9.0.320).
- SQL Server (LocalDB, Express, Developer o una instancia accesible).
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef --version 9.0.10`
  (o `dotnet tool update --global dotnet-ef --version 9.0.10` si ya está instalada).

## Estructura

```
GestionSiniestros.Api/
  Controllers/   ClaimsController (endpoints HTTP)
  Interfaces/    IClaimService
  Services/      ClaimService (reglas y operaciones)
  Data/          ClaimsDbContext y Migrations/
  Models/        Claim, ClaimStatus, ClaimStatusHistory
  Dtos/          Contratos JSON de entrada y salida
```

## Configuración de SQL Server

La cadena de conexión **no** se guarda en el repositorio. Se lee de `ConnectionStrings:ClaimsDb`
y en desarrollo se configura con secretos de usuario (desde la carpeta `GestionSiniestros.Api`):

```powershell
cd GestionSiniestros.Api

# LocalDB
dotnet user-secrets set "ConnectionStrings:ClaimsDb" "Server=(localdb)\MSSQLLocalDB;Database=GestionSiniestros;Trusted_Connection=True;TrustServerCertificate=True"

# o una instancia con usuario y contraseña
dotnet user-secrets set "ConnectionStrings:ClaimsDb" "Server=localhost;Database=GestionSiniestros;User Id=<usuario>;Password=<contraseña>;TrustServerCertificate=True"
```

Si falta la cadena, la API no arranca y muestra un mensaje indicando cómo configurarla.

## Migraciones

```powershell
cd GestionSiniestros.Api
dotnet ef migrations has-pending-model-changes   # debe indicar que no hay cambios pendientes
dotnet ef database update                        # crea la base y las tablas
```

Para futuras migraciones: `dotnet ef migrations add <Nombre> --output-dir Data/Migrations`.

Esquema:

- `Claims`: índice no único `IX_Claims_PolicyNumber`.
- `ClaimStatusHistory`: FK `ClaimId` hacia `Claims` con borrado en cascada e índice `IX_ClaimStatusHistory_ClaimId`
  (es el mismo índice que EF Core genera por convención para la FK; no hay duplicado).

## Ejecución

```powershell
dotnet restore
dotnet build
dotnet run --project GestionSiniestros.Api --launch-profile https
```

- Swagger: `https://localhost:7080/swagger` (o `http://localhost:5080/swagger` con el perfil `http`).
- CORS de desarrollo: `http://localhost:4200` (`appsettings.Development.json`, `Cors:AllowedOrigins`).

## Endpoints

Prefijo `/api/v1`, configurado en `Program.cs`.

| Método | Ruta | Respuestas |
| ------ | ---- | ---------- |
| GET | `/api/v1/claims?policyNumber={prefijo}&status={estado}` | 200 |
| GET | `/api/v1/claims/{id}` | 200, 404 |
| POST | `/api/v1/claims` | 201, 400 |
| PUT | `/api/v1/claims/{id}` | 200, 400, 404 |
| PATCH | `/api/v1/claims/{id}/status` | 200, 400, 404 |
| DELETE | `/api/v1/claims/{id}` | 204, 404 |
| GET | `/api/v1/claims/{id}/status-history` | 200, 404 |

- `status` en JSON es string: `Open`, `InReview`, `Closed`. Se rechazan valores numéricos en el cuerpo.
- Fechas en UTC, ISO 8601 (`2026-11-17T19:00:00Z`).
- POST/PUT reciben `policyNumber`, `insuredName`, `claimType` y `estimatedAmount` (mayor que cero, máximo dos decimales).
- Al crear, el estado es `Open` y se registra el evento inicial del historial (`previousStatus: null`).
- PATCH recibe `{ "status": "InReview" }`; registra estado anterior, nuevo y fecha. Si el estado es igual al actual responde 400.

## Manejo de errores

Todas las respuestas de error usan el formato ProblemDetails (`application/problem+json`) e incluyen `traceId`.
Se configura en `Program.cs` con `AddProblemDetails`, `UseExceptionHandler` y `UseStatusCodePages`; los
controladores no tienen `try/catch`.

- **400 de validación y 404:** sin cambios; las genera MVC (`[ApiController]`).
- **500 por excepción no controlada:** respuesta genérica en español, sin stack trace, mensajes SQL ni detalles internos:

  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
    "title": "Error interno del servidor.",
    "status": 500,
    "detail": "Ocurrió un error inesperado. Si el problema persiste, comunícate con soporte e indica el traceId.",
    "traceId": "00-<trace-id>-<span-id>-00"
  }
  ```

- **Errores sin cuerpo** (ruta inexistente, método no permitido, etc.): `UseStatusCodePages` los devuelve como ProblemDetails.
  No modifica las respuestas que ya tienen cuerpo.

**Correlación con logs:** el `ExceptionHandlerMiddleware` registra la excepción completa en el log con nivel Error.
Con `Logging:Console:IncludeScopes` activo (`appsettings.json`), la línea del log incluye `TraceId:<trace-id>`, que es
el segundo segmento del `traceId` devuelto al cliente.

**Prueba manual del 500:** con la API detenida, cambia temporalmente la cadena de conexión a un servidor inexistente
(`dotnet user-secrets set "ConnectionStrings:ClaimsDb" "Server=noexiste;Database=GestionSiniestros;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3"`),
arranca la API y llama a `GET /api/v1/claims`. Debe responder el 500 genérico y la consola debe mostrar la excepción
con el mismo TraceId. Después restaura la cadena original.
