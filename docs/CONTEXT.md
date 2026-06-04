# PVV — Contexto para Claude Code

## Qué es este proyecto

**Portal de Ventas Virtual de Seguros Vehiculares (PVV)** — Trabajo Final Integrador,
Tecnicatura Universitaria en Programación, UTN FRC. Legajo 412299.

Es un ecosistema de 6 microservicios que digitaliza la venta de seguros vehiculares
en Argentina, soportando múltiples compañías aseguradoras desde una misma plataforma
(multi-tenancy). El documento completo de arquitectura está en `docs/arquitectura-pvv.md`.

---

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Backend | .NET 10 / ASP.NET Core 10 |
| Frontend | React 19 + TypeScript 5 strict + Vite 6 + Tailwind CSS v4 |
| Base de datos relacional | SQL Server 2022 (EF Core 9) |
| Base de datos documental | MongoDB 7 (MongoDB.Driver 3) |
| Caché | Redis 7 (StackExchange.Redis) |
| Mensajería | RabbitMQ 3 |
| Pagos | Mercado Pago SDK .NET + REST API |
| State management (React) | Zustand |
| CQRS | MediatR 12 |
| Logging | Serilog 4 |
| Testing .NET | xUnit + Moq |
| Testing React | Vitest |

---

## Los 6 microservicios

| Servicio | Tipo | Puerto | Descripción |
|---|---|---|---|
| pvv-soat | ASP.NET Core Web API | 5001 | Dominio central: vehículos, tomadores, presupuestos, pólizas |
| pvv-config | ASP.NET Core Web API + Worker | 5002 | Configuración multi-tenant + sync SQL→Redis |
| pvv-bff | ASP.NET Core Web API | 5003 | Gateway, sesiones, leads MongoDB, pagos MP, RabbitMQ |
| pvv-emission | .NET Worker Service | 5004 | Consume RabbitMQ y emite pólizas en pvv-soat |
| pvv-front | React 19 SPA | 5173 | Portal de compra — wizard 5 pasos |
| pvv-admin | React 19 SPA | 5174 | Panel de administración + dashboard de analytics |

---

## Estructura del monorepo

```
/pvv
  /pvv-soat/        → solución .NET (API, Application, Domain, Infrastructure)
  /pvv-config/      → solución .NET (API, Application, Domain, Infrastructure, Worker)
  /pvv-bff/         → solución .NET (API, Application, Domain, Infrastructure)
  /pvv-emission/    → solución .NET (Worker, Application, Domain, Infrastructure)
  /pvv-front/       → proyecto React 19 (FSD)
  /pvv-admin/       → proyecto React 19 (FSD)
  /infra/
    docker-compose.yml
    /scripts/
  /docs/
    arquitectura-pvv.md   ← leer antes de cualquier tarea
    sprint0-checklist.md
  .gitignore
  README.md
```

---

## Convenciones de código

### .NET
- Idioma del código: **inglés** (clases, métodos, variables, comentarios)
- Idioma de mensajes de log y excepciones: **inglés**
- Nomenclatura: PascalCase para clases/métodos, camelCase para variables locales
- Un archivo por clase, nombre del archivo = nombre de la clase
- Nunca lógica de negocio en controllers — solo llamadas a MediatR o services
- Siempre usar `CancellationToken ct` en métodos async
- Siempre usar `ILogger<T>` inyectado, nunca `Console.WriteLine`
- Retornar siempre DTOs desde la capa Application, nunca entidades de dominio
- Configuración siempre por `IOptions<T>`, nunca leer `IConfiguration` directamente en services

### React / TypeScript
- Idioma del código: **inglés**
- Nomenclatura: PascalCase para componentes, camelCase para funciones/variables
- Estructura Feature-Sliced Design (FSD): app / pages / widgets / features / entities / shared
- Nunca lógica de negocio en componentes — extraer a custom hooks o stores Zustand
- Siempre tipar explícitamente, nunca usar `any`
- Axios instance centralizada en `shared/api`, nunca fetch directo en componentes
- CSS solo con clases Tailwind, nunca estilos inline salvo valores dinámicos (CSS vars)

---

## Branching strategy

- `main` → estable, solo merge desde `develop` via PR
- `develop` → rama de integración
- `feature/pvv-XXX-descripcion` → una rama por HU

---

## Infra local (Docker)

| Servicio | Puerto | Credenciales |
|---|---|---|
| SQL Server 2022 | 1433 | sa / PvvLocal123! |
| MongoDB 7 | 27017 | pvv_user / pvv_pass |
| Redis 7 | 6379 | sin auth en local |
| Redis Commander | 8081 | — |
| RabbitMQ 3 | 5672 | pvv_user / pvv_pass |
| RabbitMQ UI | 15672 | pvv_user / pvv_pass |

RabbitMQ vhost: `pvv`
Colas: `pvv_emission_queue` (worker principal), `pvv_emission_dlq` (dead letter)

---

## Sprint actual y estado

> **Sprint actual: Sprint 2** (Sprint 0 y Sprint 1 completados).

### Sprint 0 — Setup e infraestructura (semana 1) ✅ COMPLETADO
**Objetivo:** Entorno 100% listo para escribir código de negocio desde el primer día del Sprint 1.

#### HU-01 — Setup de repositorio e infraestructura base
- [x] Estructura de monorepo creada
- [x] docker-compose.yml con SQL Server, MongoDB, Redis, RabbitMQ
- [x] .env.example por cada microservicio
- [x] README.md con instrucciones de setup
- [x] .gitignore raíz

#### HU-02 — Scaffolding de los 6 microservicios
- [x] pvv-soat: solución .NET, estructura de capas, Swagger, /health
- [x] pvv-config: solución .NET, Clean Architecture, MediatR, Swagger, /health
- [x] pvv-bff: solución .NET, Clean Architecture, MediatR, MongoDB, Redis, Swagger, /health
- [x] pvv-emission: solución .NET, BackgroundService base, /health
- [x] pvv-front: Vite + React 19 + TS strict + Tailwind v4 + FSD, levanta en 5173
- [x] pvv-admin: Vite + React 19 + TS strict + Tailwind v4 + FSD, levanta en 5174

### Sprint 1 — Dominio: pvv-soat + pvv-config (semanas 2-3) ✅ COMPLETADO
- [x] pvv-soat: entidades (Vehicle, Holder, Budget, Policy), EF Core, migración real, CQRS (vehículos/tomadores/presupuestos/pólizas), repositorios, controllers + ProblemDetails, BudgetExpirationJob, seeder
- [x] pvv-config: modelo EAV (Company, Configuration, ConfigurationHistory, Operator), migración real, Auth JWT + BCrypt, cifrado AES-256, CQRS (companies/configurations), endpoints + CompanyOwnershipFilter, CacheSyncWorker con Redis Pub/Sub, seeder

### Sprint 2 — Orquestación: pvv-bff + pvv-emission + pvv-front (semanas 4-5)
**Pendiente — sprint actual**

### Sprint 3 — Admin + Analytics + Testing (semanas 6-7)
**Pendiente**

---

## Notas técnicas resueltas (leer antes de tocar APIs / Redis)

Estas son trampas reales que aparecieron en el Sprint 1 con el stack .NET 10. Tenerlas
presentes para no perder tiempo en el Sprint 2.

### Swagger / Swashbuckle 10.x + Microsoft.OpenApi 2.x
- Swashbuckle.AspNetCore 10.x arrastra **Microsoft.OpenApi 2.x**, que **aplanó el namespace**:
  los tipos ya **no** están en `Microsoft.OpenApi.Models` sino directamente en **`Microsoft.OpenApi`**
  (`OpenApiSecurityScheme`, `SecuritySchemeType`, `ParameterLocation`, etc.).
- Para referenciar un security scheme en un requirement **ya no se usa** `new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }`.
  Se usa **`OpenApiSecuritySchemeReference("Bearer", document, null)`**.
- **`AddSecurityRequirement` ahora recibe un `Func<OpenApiDocument, OpenApiSecurityRequirement>`** (lambda), no un objeto directo.
- El valor del `OpenApiSecurityRequirement` es `List<string>` (usar `new List<string>()`, no `Array.Empty<string>()`).

Snippet que compila (config Bearer en `AddSwaggerGen`):
```csharp
using Microsoft.OpenApi;

options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header
});
options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
{
    { new OpenApiSecuritySchemeReference("Bearer", document, null), new List<string>() }
});
```

### Redis Pub/Sub con StackExchange.Redis (suscripción)
- La sobrecarga `ISubscriber.SubscribeAsync(channel, Action<RedisChannel, RedisValue>)` **fue removida**.
  Usar el patrón **`ChannelMessageQueue` + `OnMessage`**:
```csharp
var queue = await subscriber.SubscribeAsync(RedisChannel.Literal("pvv:cache:invalidate"));
queue.OnMessage(channelMessage => HandleAsync(channelMessage.Message, ct));
```
- Al deserializar el `RedisValue` con `System.Text.Json`, **convertir a string explícito** (`message.ToString()`):
  pasar el `RedisValue` directo es **ambiguo** entre las sobrecargas `string` y `ReadOnlySpan<byte>`.
- Los payloads se publican en camelCase, así que el deserializer necesita `PropertyNameCaseInsensitive = true`.

### Otras decisiones del Sprint 1 (válidas para todos los servicios)
- **EF Core 10** (no 9): empareja con el SDK .NET 10 y `dotnet-ef` 10.0.8.
- **Interfaces de repositorio en la capa Application** (no Infrastructure): con el grafo
  `Application → Domain` y `Infrastructure → Application`, los handlers no pueden ver Infrastructure.
- **`JsonStringEnumConverter`** en `AddControllers().AddJsonOptions(...)` para que los enums
  (ej. `VehicleType`) deserialicen desde string en el body.
- Para exponer XML comments en Swagger: `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
  + `<NoWarn>$(NoWarn);1591</NoWarn>` en el `.csproj` del API, e `IncludeXmlComments(...)` en SwaggerGen.
- En los Workers que además exponen `/health` (ej. pvv-emission), usar el **SDK Web** (minimal API)
  hospedando el `BackgroundService` como hosted service.

---

## Lo que está fuera de alcance (no implementar)

- Validación de identidad OTP (SMS / email)
- Trust score y detección de fraude en tiempo real
- Consulta a registros oficiales de vehículos (RUNT / RNPA)
- Venta cruzada (cross-sell) de seguros adicionales
- SignalR / WebSockets
- HMAC validation en BFF
- Múltiples workers de emisión (solo uno: EmissionWorker)

---

## Cómo usar este archivo

Al iniciar una sesión de Claude Code, pasá siempre estos dos archivos:
1. `docs/CONTEXT.md` (este archivo) — estado del proyecto y convenciones
2. `docs/arquitectura-pvv.md` — arquitectura técnica detallada

Luego pegá el prompt de la tarea que corresponde al sprint y HU actual.
