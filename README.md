# Gestion-Siniestros-Api

API REST para registrar y dar seguimiento a siniestros de seguros. Está construida con ASP.NET Core 9, Entity Framework Core 9 y SQL Server.

## Requisitos

- .NET SDK 9 (el entorno local usa 9.0.320).
- SQL Server: LocalDB, Express, Developer o una instancia accesible.
- Herramienta de EF Core 9: `dotnet tool install --global dotnet-ef --version 9.0.10`.

## Configurar SQL Server

La API lee `ConnectionStrings:ClaimsDb` desde la configuración. Guarda la cadena en secretos de usuario; no la agregues a los archivos del repositorio.

Desde la carpeta `GestionSiniestros.Api`, configura LocalDB:

```powershell
dotnet user-secrets set "ConnectionStrings:ClaimsDb" "Server=(localdb)\MSSQLLocalDB;Database=GestionSiniestros;Trusted_Connection=True;TrustServerCertificate=True"
```

Para usar otra instancia, reemplaza `Server` por el nombre correspondiente y configura las credenciales según la instalación.

### Aplicar las migraciones

Desde la carpeta `GestionSiniestros.Api`, con la cadena ya configurada:

```powershell
dotnet ef migrations has-pending-model-changes
dotnet ef database update
```

`dotnet ef database update` crea la base de datos `GestionSiniestros` si aún no existe y aplica las migraciones pendientes. La cuenta de SQL Server debe tener permisos para crear la base.

Para crear una migración nueva:

```powershell
dotnet ef migrations add <Nombre> --output-dir Data/Migrations
```

La base contiene las tablas `Claims` y `ClaimStatusHistory`. `Claims.PolicyNumber` tiene un índice no único; el historial tiene una llave foránea hacia `Claims` con borrado en cascada.

## Ejecutar la API

Desde la raíz del repositorio:

```powershell
dotnet restore
dotnet build
dotnet run --project GestionSiniestros.Api --launch-profile https
```

En Visual Studio puedes seleccionar GestionSiniestros.Api como proyecto de inicio y ejecutar el perfil https.

Si Swagger no confía en el certificado HTTPS de desarrollo, ejecuta `dotnet dev-certs https --trust`.

En desarrollo, CORS permite el origen `http://localhost:4200`; se configura en `appsettings.Development.json`.

## Estructura del proyecto

- Program.cs: configura inyección de dependencias, Entity Framework, CORS, prefijo de rutas, Swagger y errores globales.
- Controllers: adaptan las solicitudes HTTP y delegan en IClaimService.
- Interfaces y Services: definen e implementan las operaciones del negocio.
- Data: ClaimsDbContext y migraciones Code First.
- Models y Dtos: entidades de persistencia y contratos JSON.

La solución mantiene las capas dentro de un solo proyecto. El manejo de errores usa AddProblemDetails, UseExceptionHandler y UseStatusCodePages, integrados en ASP.NET Core y configurados centralmente en Program.cs; no hay una clase de middleware personalizada. El handler de excepciones escribe la respuesta genérica como `application/problem+json` aun cuando el cliente envíe un `Accept` que no admita JSON.

## Documentación interactiva

Con la API en ejecución, abre Swagger:

- HTTPS: `https://localhost:7080/swagger`
- HTTP: `http://localhost:5080/swagger`

Swagger muestra los endpoints, parámetros, respuestas y modelos de solicitud.

## Endpoints

Todas las rutas usan el prefijo `/api/v1`.

| Método | Ruta | Respuestas principales |
| ------ | ---- | ---------------------- |
| GET | `/api/v1/claims?policyNumber={prefijo}&status={estado}` | 200 |
| GET | `/api/v1/claims/{id}` | 200, 404 |
| POST | `/api/v1/claims` | 201, 400 |
| PUT | `/api/v1/claims/{id}` | 200, 400, 404 |
| PATCH | `/api/v1/claims/{id}/status` | 200, 400, 404 |
| DELETE | `/api/v1/claims/{id}` | 204, 404 |
| GET | `/api/v1/claims/{id}/status-history` | 200, 404 |

Ejemplo de cuerpo para crear o actualizar un siniestro:

```json
{
  "policyNumber": "POL-2026-001",
  "insuredName": "Ana López",
  "claimType": "Daño por granizo",
  "estimatedAmount": 24500.50
}
```

Para cambiar el estado, envía un `PATCH` a `/api/v1/claims/{id}/status`:

```json
{
  "status": "InReview"
}
```

Los estados se envían como strings: `Open`, `InReview` o `Closed`. Se rechazan valores numéricos. Las fechas se manejan en UTC y se envían en formato ISO 8601.

Los campos `policyNumber`, `insuredName` y `claimType` son obligatorios, se recortan espacios externos y aceptan como máximo 50, 200 y 100 caracteres, respectivamente. `estimatedAmount` debe estar entre `0.01` y `9999999999999999.99` y admite hasta dos decimales.

Al crear un siniestro, su estado inicial es `Open` y se registra el evento correspondiente en el historial. Si se solicita el estado actual, la API responde 400 y no agrega otro evento.

## Errores

Las respuestas de error usan `ProblemDetails` (`application/problem+json`) e incluyen un `traceId` para correlacionar la respuesta con los logs.

- Los errores de validación responden 400.
- Un siniestro inexistente responde 404.
- Una excepción no controlada responde 500 con un mensaje genérico en español, sin exponer detalles internos.
- Los errores sin cuerpo, como rutas inexistentes o métodos no permitidos, se devuelven como `ProblemDetails` cuando el `Accept` del cliente admite JSON (por ejemplo, `application/json` o `*/*`).

El manejo se configura globalmente en `Program.cs`; los controladores no necesitan bloques `try/catch`.

## Revisión manual en Swagger

Con SQL Server iniciado, migraciones aplicadas y la API en ejecución, abre https://localhost:7080/swagger:

1. Crea un siniestro válido con POST. La respuesta debe ser 201 y el estado inicial Open.
2. Consulta su historial. Debe incluir el evento inicial con previousStatus: null.
3. Cambia el estado con PATCH a InReview y después a Closed. El historial debe registrar cada cambio.
4. Repite el PATCH con el estado actual. Debe responder 400 y no agregar otro evento.
5. Filtra el listado por un prefijo de póliza y combina el filtro con un estado.
6. Actualiza los datos con PUT y confirma que updatedAt cambió.
7. Prueba un monto 0, uno con más de dos decimales (`10.555`) y uno por encima del máximo (`10000000000000000.00`); deben responder 400. El máximo `9999999999999999.99` debe aceptarse. Prueba también un texto que exceda el límite de su campo; debe responder 400.
8. Elimina el siniestro con DELETE (204) y vuelve a consultar su detalle; debe responder 404.
