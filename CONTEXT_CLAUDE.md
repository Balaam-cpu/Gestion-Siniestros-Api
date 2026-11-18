# Gestor de Siniestros

## Contexto del proyecto

Aplicación fullstack para registrar y dar seguimiento a siniestros de seguros. El proyecto se entrega en dos repositorios independientes:

- `Gestion-Siniestros-Frontend`: frontend Angular.
- `Gestion-Siniestros-Api`: API REST con ASP.NET Core, Entity Framework Core y SQL Server.

La interfaz debe estar en español. El código y el contrato JSON deben usar nombres en inglés para mantener consistencia técnica.

Este documento resume el alcance y las decisiones técnicas para dar contexto al asistente. No reemplaza el README técnico de cada repositorio ni implica por sí solo que una función esté implementada.

El código y la configuración actuales son la referencia de lo que está implementado. Antes de modificar algo, inspecciona el repositorio y señala cualquier diferencia entre el código y este contexto; no supongas que una función existe solo porque aparece aquí.

## Alcance funcional

La aplicación permite:

- Crear, consultar, editar y eliminar siniestros.
- Listar siniestros con búsqueda por número de póliza y filtro por estado.
- Consultar el detalle de un siniestro y su historial de cambios de estado.
- Validar los datos requeridos y que el monto estimado sea mayor que cero.
- Persistir la información en SQL Server.

### Datos de un siniestro

- `id`: identificador.
- `policyNumber`: número de póliza.
- `insuredName`: nombre del asegurado.
- `claimType`: tipo de siniestro.
- `estimatedAmount`: monto estimado, decimal de dos posiciones.
- `status`: estado actual.
- `createdAt`: fecha de creación.
- `updatedAt`: fecha de última actualización.

Los estados internos son:

| Valor API | Etiqueta en la interfaz |
| --------- | ----------------------- |
| `Open` | Abierto |
| `InReview` | En revisión |
| `Closed` | Cerrado |

El estado inicial es `Open`. Cada cambio de estado guarda el estado anterior, el nuevo estado y la fecha del cambio. También se registra el estado inicial al crear el siniestro.

## Contrato inicial de la API

La API usa el prefijo `/api/v1`. El versionado y las reglas de seguridad que se acuerden deben configurarse centralmente, no repetirse en cada endpoint. No agregues medidas de seguridad nuevas fuera del alcance sin consultarlo.

- `GET /api/v1/claims?policyNumber={texto}&status={estado}`: lista y filtra siniestros.
- `GET /api/v1/claims/{id}`: detalle del siniestro.
- `POST /api/v1/claims`: crea un siniestro.
- `PUT /api/v1/claims/{id}`: actualiza los datos editables del siniestro.
- `PATCH /api/v1/claims/{id}/status`: cambia el estado y agrega un evento al historial.
- `DELETE /api/v1/claims/{id}`: elimina el siniestro.
- `GET /api/v1/claims/{id}/status-history`: obtiene su historial de estados.

El filtro por número de póliza acepta coincidencias por prefijo. El filtro por estado se puede combinar con la búsqueda por póliza.

Los valores `status` del JSON son strings (`Open`, `InReview`, `Closed`), no números del enum. Las fechas se manejan en UTC y se envían en formato ISO 8601.

## Decisiones técnicas

### Backend

- ASP.NET Core Web API sobre .NET 9.
- Entity Framework Core con SQL Server y enfoque Code First mediante migraciones.
- Arquitectura en capas sencilla dentro de un solo proyecto:
  - `Controllers`: endpoints HTTP.
  - `Interfaces`: contratos que definen las operaciones que deben implementar los servicios. `IClaimService` declara las operaciones que implementa `ClaimService`.
  - `Services`: reglas y operaciones de la aplicación.
  - `Data`: `DbContext` y persistencia.
  - `Models` y `Dtos`: entidades y contratos JSON.
- Inyección de dependencias para conectar `IClaimService` con `ClaimService`.
- Índice no único en `PolicyNumber`.
- Índice `IX_ClaimStatusHistory_ClaimId` para la llave foránea del historial. EF Core lo genera por convención y el modelo lo declara explícitamente sin duplicarlo.
- Manejo global de excepciones con middleware integrado de ASP.NET Core y respuestas `ProblemDetails`. El error 500 genérico también está documentado en Swagger.
- CORS para el origen local de Angular, habilitado solo en Development.

No agregar CQRS, MediatR, repositorios genéricos ni proyectos separados por capa sin una razón concreta para este alcance.

### Decisiones que no deben cambiarse sin un requisito

- Los números de póliza no son únicos: no agregar validación ni restricción única.
- No crear una clase o carpeta `Middleware` para las excepciones; el manejo actual usa las piezas integradas de ASP.NET Core.

La cadena de conexión y cualquier secreto deben configurarse localmente mediante secretos de usuario. No guardar credenciales en el repositorio.

### Frontend

- Angular con componentes standalone y routing.
- Servicio Angular para centralizar las llamadas HTTP.
- Formularios con validación visible para el usuario.
- URL de la API en configuración de entorno, no repetida en los componentes.
- Pantallas para listado, formulario y detalle con historial.
- Mostrar estados de carga, error, resultado vacío y confirmación de acciones.
- Consumir la API real; no dejar datos mock como implementación final.

## Instrucciones para trabajar con un asistente de IA

Antes de modificar archivos:

1. Inspecciona el contenido del repositorio y las versiones instaladas.
2. Resume qué existe y qué falta.
3. Confirma cualquier decisión que cambie el contrato de la API o el alcance.

Durante el desarrollo:

- Implementa una parte pequeña y funcional a la vez.
- Conserva las convenciones existentes.
- No agregues dependencias o arquitectura innecesarias.
- No inventes resultados de ejecución ni afirmes que algo fue verificado si no lo fue.
- Al terminar cada etapa, resume los archivos modificados, decisiones tomadas y pasos manuales para revisar el resultado.
- Mantén actualizado `AI_LOG.md` con prompts, decisiones y verificaciones reales.


## Ejecución local

- Requisitos previos: completar en el README del repositorio correspondiente.
- Configuración de SQL Server y comando para ejecutar la API: documentar en `Gestion-Siniestros-Api/README.md`.
- Comando para ejecutar Angular y URL local de la API: documentar en `Gestion-Siniestros-Frontend/README.md`.
