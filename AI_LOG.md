# AI_LOG — Backend (Gestion-Siniestros-Api)

Registro de prompts usados con el asistente de IA, sugerencias aceptadas y rechazadas, y sus motivos.

Este archivo registra solo los prompts del backend. Los del frontend están en el `AI_LOG.md` de `Gestion-Siniestros-Frontend`.

## Etapa 1 — Estructura inicial de la API (2026-11-17)

### Prompt

> Lee el README.md y el contexto del proyecto antes de hacer cambios, el repositorio local esta vacío y será únicamente el backend.
> Revisa qué versión del SDK de .net está instalada y úsala para crear una API REST ASP:NET Core compatible, crea la solución y el proyecto de API con una estructura sencilla de capas de lógica dentro del mismo proyecto: Controllers, Interfaces, Services, Data, Models y Dtos.
> Configura Swagger/OpenAPI y CORS para desarrollo local con Angular.
> No agregues todavía funcionalidades del gestor ni base de datos en memoria.
> No agregues arquitectura que no te he solicitado ni agregues nada que no te haya pedido.
> Al terminar, ejecuta el build del proyecto y corrige errores de compilación, resume los archivos creados, los comandos para ejecutarlo y cualquier requisito local pendiente

### Decisiones

- SDK detectado: .NET 9.0.320 (único instalado). Se usa `net9.0`.
- Un solo proyecto `GestionSiniestros.Api` dentro de la solución `Gestion-Siniestros-Api.sln`, con carpetas `Controllers`, `Interfaces`, `Services`, `Data`, `Models` y `Dtos` (con `.gitkeep` para que git las conserve vacías).
- Swagger con `Swashbuckle.AspNetCore` (la plantilla de .NET 9 ya no incluye interfaz de Swagger), habilitado solo en `Development`.
- CORS con una política `AngularDev` cuyos orígenes se leen de `Cors:AllowedOrigins` (`http://localhost:4200` en `appsettings.Development.json`), aplicada solo en `Development`.

### Sugerencias rechazadas

- `WeatherForecast` de la plantilla: no forma parte del alcance.
- `UseAuthorization`: no hay autenticación ni autorización definidas todavía.
- Versionado, EF Core y SQL Server: quedan para etapas posteriores; no se solicitaron en esta etapa.

### Verificación

- El build no se ejecutó desde el asistente: no tuvo acceso a una terminal en el equipo. Queda pendiente ejecutar `dotnet build` localmente.

## Etapa 1.1 — Revisión del esqueleto (2026-11-17)

### Prompt

> Antes de implementar funcionalidades, revisa el esqueleto creado y detente hasta verificar la compilación. La versión Swashbuckle.AspNetCore 9.0.6 existe en NuGet, así que elimina la advertencia especulativa de AI_LOG.md.
> Dime qué SDK tienes disponible en el entorno donde se ejecutará el proyecto. Si no puedes ejecutar dotnet restore y dotnet build, indícalo claramente y no afirmes que está verificado. No continúes con endpoints ni base de datos hasta tener el resultado del build.

### Resultado

- `AI_LOG.md` no tenía advertencia sobre Swashbuckle (la nota solo se dio en el chat); no hubo nada que eliminar. Versión 9.0.6 confirmada por el usuario.
- Revisión estática: `appsettings.json`, `appsettings.Development.json` y `launchSettings.json` son JSON válido; el `.csproj` es XML válido; la `.sln` apunta a `GestionSiniestros.Api\GestionSiniestros.Api.csproj`.
- SDK en el equipo donde se ejecutará: .NET 9.0.320 (`C:\Program Files\dotnet\sdk`).
- `dotnet restore` y `dotnet build` NO se ejecutaron: el asistente no tiene terminal en el equipo. La compilación sigue sin verificar.
- Endpoints y base de datos en espera hasta tener el resultado del build.

## Etapa 2 — Backend funcional con EF Core y SQL Server (2026-11-17)

### Prompt

> El esqueleto ya compiló con `net9.0`, 0 advertencias y 0 errores.
> Usa el README.md como fuente de verdad para el modelo de datos, estados y contrato de la API. Implementa ahora el backend funcional con ASP.NET Core, EF Core y SQL Server.
> Incluye los endpoints acordados, validación del lado del servidor y persistencia real. Cada cambio de estado debe guardar el estado anterior, el nuevo y la fecha, al crear el siniestro también debe quedar registrado su estado inicial.
> Lee la cadena de conexión desde configuración local; no guardes credenciales en Git. Crea las migraciones de EF Core. Agrega un índice no único en PolicyNumber y revisa que la llave foránea del historial tenga índice sin duplicar el que EF Core genere automáticamente.
> Mantén la separación Controller → Interfaces → Service → DbContext. Si falta SQL Server o configuración local, explícame qué hace falta; no sustituyas la persistencia por una lista en memoria.
> Al terminar, ejecuta el build, resume los endpoints y dame los pasos para configurar la conexión, aplicar migraciones y revisar manualmente el flujo.


### Decisiones aceptadas

- Code First con migraciones.
- Paquetes: `Microsoft.EntityFrameworkCore.SqlServer` y `Microsoft.EntityFrameworkCore.Design` 9.0.10.
- Tablas `Claims` y `ClaimStatusHistory`. Estados guardados como texto (`nvarchar(20)`); `PreviousStatus` es null en el evento inicial.
- `EstimatedAmount` como `decimal(18,2)`; validación: obligatorio, mayor que cero y máximo dos decimales.
- Fechas `DateTime` UTC en `datetime2`; un `ValueConverter` marca `DateTimeKind.Utc` al leer para que el JSON salga con `Z`.
- Índice no único `IX_Claims_PolicyNumber`. Para la FK, `HasIndex(ClaimId)` explícito: reutiliza el índice de convención `IX_ClaimStatusHistory_ClaimId`, no lo duplica.
- Borrado en cascada del historial al eliminar el siniestro.
- Prefijo `/api/v1` con una convención de rutas registrada en `Program.cs` (los controladores solo declaran `claims`).
- Enum como string en JSON con `JsonStringEnumConverter(allowIntegerValues: false)`.
- Un mismo DTO (`SaveClaimRequest`) para POST y PUT, porque los campos editables son los mismos. El estado solo se cambia con PATCH.
- PATCH con el mismo estado actual responde 400 y no agrega evento al historial.
- Cadena de conexión `ConnectionStrings:ClaimsDb` desde user-secrets; si falta, la API no arranca y muestra un mensaje claro.


### Verificación

- Las migraciones (`InitialCreate`, su Designer y el ModelSnapshot) se escribieron a mano porque el asistente no puede ejecutar `dotnet ef`. Verificar con `dotnet ef migrations has-pending-model-changes`.
- `dotnet build` NO se ejecutó desde el asistente. La compilación de esta etapa sigue sin verificar.

## Etapa 3 — Manejo global de excepciones (2026-11-17)

### Prompt

> Implementa manejo global de excepciones para esta API usando AddProblemDetails y UseExceptionHandler. Devuelve un 500 genérico en español con un traceId para correlacionar el error con los logs; nunca expongas stack traces, mensajes SQL ni detalles internos al cliente.
> Conserva las respuestas actuales de validación, 400 y 404. No agregues try/catch repetidos en cada controller y no cambies reglas de negocio. Agrega UseStatusCodePages solo si hace falta para que errores sin cuerpo también sigan el formato ProblemDetails. Actualiza el README y ejecuta dotnet build e informa el resultado real.

### Decisiones aceptadas

- `AddProblemDetails` con `CustomizeProblemDetails` agrega `traceId` a los ProblemDetails que produce el framework, incluidos errores sin cuerpo.
- `traceId` = `Activity.Current?.Id ?? HttpContext.TraceIdentifier`; el handler inline lo agrega al 500 y `CustomizeProblemDetails` lo agrega con `TryAdd` a las demás respuestas ProblemDetails.
- `UseExceptionHandler` al inicio del pipeline usa un handler inline para escribir el 500 genérico como `application/problem+json`, sin negociar con `Accept`. `UseStatusCodePages()` genera ProblemDetails para errores sin cuerpo cuando el cliente acepta un formato soportado por el writer registrado.
- Corrección de la revisión del 2026-11-17: la frase anterior que afirmaba que `UseExceptionHandler()` evitaba exponer detalles incluso con la página de desarrollo era demasiado amplia. `AddProblemDetails` puede no escribir si `Accept` no admite JSON; en Development, una excepción que escape puede llegar a la página de desarrollo. Se configuró un handler inline en `Program.cs` que escribe el 500 genérico como `application/problem+json` sin negociar con `Accept`, evitando ese fallback para excepciones normales de la aplicación.
- `Logging:Console:IncludeScopes = true` para que el log incluya el TraceId y se pueda correlacionar.

### Sugerencias rechazadas

- Clase `IExceptionHandler` propia: no hace falta para una respuesta genérica única; bastan `AddProblemDetails` y `UseExceptionHandler`.
- `try/catch` en controladores o servicios: lo prohibió el usuario y duplica el manejo global.

### Verificación

- `dotnet build` NO se ejecutó desde el asistente (sin terminal en el equipo por falta de espacio en disco). Resultado pendiente del usuario.


## Auditoría manual del código (2026-11-17)

### Prompt

> Listo termine la auditoria manual del código, igual déjalo registrado en el AI log, y lo que duro la auditoria que le hice, hice algunos ajustes dentro del código y realice pruebas y debugueo

### Duración

- Aproximadamente 2 horas: de las 14:20 a las 16:23 (hora de México), según la hora de los mensajes y el historial de git (commits entre 14:41 y 16:20).

### Alcance

- Revisión manual de todo el código generado por el asistente, ajustes, pruebas y depuración, hechos por el usuario.

### Ajustes hechos por el usuario

- `Program.cs`: el mensaje cuando falta la cadena de conexión ya no incluye el comando de `dotnet user-secrets`.
- `ClaimsController.cs`: se quitaron `[Produces("application/json")]` y `[Consumes("application/json")]`.
- `SaveClaimRequest.cs`: en `[Range]` se quitó `ConvertValueInInvariantCulture = true`; se conserva `ParseLimitsInInvariantCulture = true`.
- Se eliminaron los comentarios y la documentación XML que había agregado el asistente, y se ajustó el formato (saltos de línea y espacios) en `Program.cs`, controlador, interfaz, servicio, DTOs, modelos, `ClaimsDbContext` y la migración.
- Sin cambios: `ClaimsDbContextModelSnapshot.cs` (contenido igual), `.csproj`, `appsettings*.json`, `launchSettings.json` y reglas de negocio.

### Verificación

- El usuario compiló, probó y depuró la API en su equipo. El asistente no ejecutó build ni pruebas.

## Revisión integral del backend y documentación (2026-11-17)

- Revisión estática de Program.cs, controlador, interfaz, servicio, modelos, DTOs, DbContext, migración y configuración.
- La API conserva capas sencillas dentro de un solo proyecto; no se agregó una carpeta Middleware personalizada porque el framework ya provee el middleware de excepciones.
- Se revisó el README para que explique cómo configurar SQL Server, aplicar migraciones, ejecutar la API y revisar el flujo funcional.
- Compilación de la revisión actual: correcta, 0 advertencias y 0 errores. Se usó una carpeta temporal porque Visual Studio tenía bloqueados los binarios de salida normales.
- Prueba HTTP en Development con una cadena de SQL inválida y `Accept: text/html`: `GET /api/v1/claims` respondió 500, `Content-Type: application/problem+json` y cuerpo genérico con `traceId`; la respuesta no incluyó la excepción SQL ni el stack trace. La consola sí registró la excepción y su TraceId coincide con el devuelto en la respuesta. La prueba no usó ni modificó la base de datos real.

## Revisión del manejo de excepciones en Program.cs (2026-11-17)

### Prompt

> revisa el api y revisa mis cambios en codigo y coumentalo fue un ajuste que se hizo en el program.cs

### Cambio del usuario

- `app.UseExceptionHandler()` se reemplazó por un handler inline que escribe siempre el 500 genérico (`ProblemDetails` con título y detalle en español y `traceId`), sin depender del `Accept` del cliente. El `ExceptionHandlerMiddleware` sigue registrando la excepción completa en el log.

### Observaciones de la revisión del asistente

- `WriteAsJsonAsync` se configuró con `contentType: "application/problem+json"` para conservar el tipo de contenido esperado. El build actual terminó con 0 advertencias y 0 errores.
- Se comprobó por HTTP que el handler devuelve el 500 genérico aun con `Accept: text/html`; la excepción SQL queda en el log y no aparece en el cuerpo de respuesta.
- Se retiró la rama de excepciones redundante de `CustomizeProblemDetails`; esa personalización conserva `traceId` en los ProblemDetails que genera el servicio para errores sin cuerpo.

## Respuestas 500 documentadas en Swagger (2026-11-17)

### Observación del usuario

> Revisa los ajustes actuales y documenta el código que modifiqué.

## Verificación
- En su revisión, el usuario detectó que Swagger no documentaba el 500 global que la API puede devolver, aunque el README sí lo describía.

### Ajuste

- Se añadió `[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]` a cada una de las siete acciones de `ClaimsController`.
- Con esto, Swagger documenta que cualquier operación puede responder con `ProblemDetails` y estado 500 por una excepción no controlada, en concordancia con el manejo global configurado en `Program.cs` y lo descrito en el README.

### Verificación

- `dotnet build` terminó correctamente con 0 advertencias y 0 errores.
- Se inició una instancia temporal en Development con una cadena SQL no válida y se leyó `/swagger/v1/swagger.json`. Las siete operaciones incluyen ahora la respuesta 500; la prueba no modificó la base de datos real.
