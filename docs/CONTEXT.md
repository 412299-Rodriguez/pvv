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
| Pagos | Mercado Pago Checkout Pro — REST API con `HttpClient` tipado, **sin el SDK** |
| Correo | SMTP con MailKit (Brevo en desarrollo) |
| State management (React) | Zustand |
| CQRS | MediatR 12 |
| Logging | Serilog 4 |
| Testing .NET | xUnit + Moq (unitario, sin infraestructura) |
| Testing React | — (no cubierto, ver §Testing) |

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
    datos-de-prueba.md    ← credenciales, portales, patentes y casos borde
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
- Los dos frontends **no comparten sistema de estilos**, y es a propósito:

  **`pvv-front` (portal) → CSS Modules + design tokens.** Un `*.module.css` por
  componente; colores, radios y sombras como CSS custom properties en
  `app/styles/tokens.css`. Nunca estilos inline salvo valores dinámicos. _Decisión
  Sprint 2 / HU-05: el mockup reusado ya estaba hecho con CSS Modules y es mobile-first,
  y las CSS vars son lo que permite inyectar los colores de cada compañía en runtime.
  Reemplaza la regla original de "solo Tailwind" para este proyecto._

  **`pvv-admin` (panel) → Tailwind v4, monocromo.** Neutros cálidos (`stone`, nunca
  `slate`), tinta (`stone-900`) como color interactivo — botón primario, tab activa,
  segmento seleccionado — y **el color reservado para estado**: verde vendido, ámbar
  abandonado, rojo rechazado. Nada más lleva color. Superficies con hairline y sin
  sombra, salvo lo que flota de verdad (modales, tooltips). _Razón de producto: el
  portal de cada compañía es el lugar donde va una marca; el panel que configura a
  todas no debe competir con ninguna._

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

> **Sprint actual: Sprint 3** (Sprints 0, 1 y 2 completados). Del Sprint 3 queda un solo
> entregable: la sección de infraestructura para el superadmin. Después de eso, el PR de
> `develop` a `main`.

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

### Sprint 2 — Orquestación: pvv-bff + pvv-emission + pvv-front (semanas 4-5) ✅ COMPLETADO
- [x] **HU-05 pvv-front** — wizard de 5 pasos (se reusó el mockup FSD ya validado)
- [x] **HU-06 pvv-bff gateway** — ingress por hash (proxy + handlers internos), pipeline
      Fingerprint → Turnstile → Session → RateLimiter, CORS, ProblemDetails
- [x] **HU-08 pvv-bff pagos** — `IPaymentGateway` con implementación **mock**, webhook,
      publicación a RabbitMQ, job de abandono. Mercado Pago real queda para HU-11
- [x] **HU-09 pvv-emission** — consumer con backoff exponencial y DLQ
- [x] **HU-10 integración e2e** — cliente ingress real, theming y textos por compañía,
      compra completa funcionando de punta a punta
- [x] **HU-07 leads** — captura de eventos del wizard y proyección del embudo en MongoDB

### Sprint 3 — Admin + Analytics + Testing (semanas 6-7)
- [x] **pvv-admin configuración** — login por rol, apariencia, productos y precios,
      ABM de compañías y operadores
- [x] **pvv-admin analytics** — embudo de conversión, tabla de leads con filtros,
      exportación y recupero de abandonos
- [x] **HU-11 Mercado Pago real** — Checkout Pro por redirección: preferencia real,
      webhook firmado, confirmación preguntándole al proveedor (nunca creyéndole al
      navegador) y conciliación periódica. El mock sigue vivo detrás de
      `Payments:Gateway = Mock | MercadoPago` para poder demostrar la compra sin
      conexión. Ver §3.6 de `docs/arquitectura-pvv.md`
- [x] **HU-12 Recuperación de leads por email** — el botón **Recuperar** envía de verdad.
      **Solo por email y solo a los leads que dejaron sus datos**: sin dirección no hay
      recupero y la acción no se ofrece. El mail es HTML, sale con la marca del
      inquilino y **invita a rehacer la compra**, no reanuda el wizard donde quedó.
      Plantilla configurable por compañía (`RECOVERY_EMAIL_CONFIG`), envío por SMTP
      desde el BFF, y registro en el lead de cuándo y quién contactó — se manda una
      sola vez. Detalle en `docs/arquitectura-pvv.md` §11.2
- [x] **Testing unitario del camino crítico** — 139 pruebas xUnit repartidas en cuatro
      proyectos (`PvvBff.Tests`, `PvvSoat.Tests`, `PvvEmission.Tests`, `PvvConfig.Tests`),
      una por solución. Cubren emisión de pólizas, clasificación de fallas de emisión,
      firma del webhook de Mercado Pago, idempotencia del cobro, proyección del embudo,
      recupero de leads y cifrado/JWT. **No necesitan Docker**: cada dependencia externa
      está mockeada, así que la suite entera corre en menos de un segundo.
      Alcance y lo que queda afuera en la sección *Testing* de este documento
- [ ] **Sección de infraestructura para el superadmin** — rutas de ingress y punteros de
      servicios en Redis, separada de la configuración por inquilino

> Para levantar todo y recorrer los casos de prueba: **`docs/datos-de-prueba.md`**
> (credenciales, portales de cada compañía, patentes y qué valida cada una, casos borde).

---

## Testing

Cuatro proyectos xUnit, uno por solución, agregados a su `.sln`:

| Proyecto | Qué cubre |
|---|---|
| `pvv-soat/PvvSoat.Tests` | Emisión: presupuesto inexistente, vencido o ya convertido; idempotencia ante mensajes repetidos; numeración `PVV-{año}-{000000}`; renovación futuro-fechada |
| `pvv-emission/PvvEmission.Tests` | Clasificación de la respuesta de soat en reintento / DLQ / éxito; soat caído o con timeout; el binder de configuración que **agrega** en vez de reemplazar |
| `pvv-bff/PvvBff.Tests` | Firma HMAC del webhook de Mercado Pago (falsificación, replay, ventana temporal); idempotencia del cobro y el tri-estado aprobado/rechazado/pendiente; proyección del embudo de leads; recupero por email (reclamo previo al envío, escapado de HTML, sanitización de color) |
| `pvv-config/PvvConfig.Tests` | Cifrado determinístico del token del portal; claims del JWT, incluido `companyToken` |

```bash
dotnet test pvv-bff/PvvBff.sln       # y lo mismo para las otras tres
```

**Son pruebas unitarias: no levantan nada.** Cada dependencia externa —repositorios,
Mongo, Redis, RabbitMQ, SMTP, la API de Mercado Pago, pvv-soat— está mockeada con Moq o
con un `HttpMessageHandler` de prueba, así que la suite corre sin Docker y sin red.

**Lo que deliberadamente NO cubren**, para no dar una falsa sensación de red:

- **Lo que sólo se puede verificar contra la base.** El `$max` que impide que un lead
  retroceda de paso, el compare-and-set que evita el doble `policy_issued`, la
  numeración de pólizas bajo transacción serializable con `UPDLOCK/HOLDLOCK`: acá se
  verifica que la capa de aplicación *pida* lo correcto, no que Mongo o SQL lo cumplan.
  Eso requiere tests de integración con contenedores efímeros.
- **El cableado HTTP**: controllers, middlewares del ingress (fingerprint, Turnstile,
  sesión, rate limiting), CORS y autorización por rol.
- **Los dos frontends.** No hay Vitest configurado; el wizard y el panel se siguen
  verificando a mano con `docs/datos-de-prueba.md`.

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

### RabbitMQ.Client 7.x — API asíncrona (HU-08)
- El cliente 7.x es **async**: `ConnectionFactory.CreateConnectionAsync(ct)`,
  `connection.CreateChannelAsync(cancellationToken: ct)`, `channel.QueueDeclareAsync(...)`,
  `channel.BasicPublishAsync(exchange, routingKey, mandatory, basicProperties, body, ct)`.
  (Las sobrecargas síncronas de versiones viejas ya no aplican.)
- Propiedades del mensaje: `new BasicProperties { Persistent = true, ContentType = "application/json" }`.
- Publicar directo a una cola = exchange `""` (default) + `routingKey` = nombre de la cola.
- DLQ por argumentos al declarar la cola principal: `x-dead-letter-exchange = ""` +
  `x-dead-letter-routing-key = "pvv_emission_dlq"`. **El publisher (BFF) y el consumer
  (pvv-emission, HU-09) deben declarar la cola con LOS MISMOS argumentos** o RabbitMQ
  tira `PRECONDITION_FAILED`.
- `IConnection` es caro: reusar uno (singleton, lazy con `SemaphoreSlim`) y abrir un
  `IChannel` por publish (los channels no son thread-safe para publish concurrente).

### Mercado Pago Checkout Pro (HU-11)

- **REST con `HttpClient` tipado, no el SDK oficial.** Hacen falta tres endpoints en
  total, y el SDK toma las credenciales de un estático global (`MercadoPagoConfig
  .AccessToken`), que pelea con la inyección de dependencias y cerraría la puerta a un
  token por inquilino más adelante.
- **`sandbox_init_point` rompe los pagos con tarjeta.** Con credenciales de un *usuario
  de prueba*, el checkout correcto es el **`init_point` común** (`UseSandbox: false`).
  Mandar un cobrador de prueba al host de sandbox da *"una de las partes con la que
  intentás hacer el pago es de prueba"* **solo con tarjeta**: pagar con dinero en cuenta
  funciona igual, porque no sale de Mercado Pago, y esa asimetría es lo que despista.
- **El prefijo del token no dice si es de prueba.** Un usuario de prueba también tiene
  credenciales `APP_USR-`. Para saber de quién es un token:
  `GET https://api.mercadopago.com/users/me` — un usuario de prueba trae
  `tags: ["test_user"]` y un nickname `TESTUSER…`.
- **Comprador y vendedor tienen que ser dos partes distintas y las dos de prueba.** Hay
  que crear una cuenta de prueba **Comprador** (panel → la app → *Cuentas de prueba*) y
  pagar con ella en una ventana de **incógnito**, o se termina comprándose a uno mismo.
  Un usuario de prueba tampoco puede pagar con una tarjeta real: solo con las de prueba.
- **En desarrollo hacen falta DOS túneles de cloudflared**: uno al **front** (Mercado
  Pago no acepta un `back_url` en localhost) y otro al **BFF** (el webhook). Como el
  portal queda servido por HTTPS, `VITE_BFF_BASE_URL` tiene que apuntar al túnel del
  BFF y no a `http://localhost`, que sería contenido mixto y el navegador lo bloquea.
  Los nombres cambian en cada arranque: hay que actualizar juntos el `.env` del front y
  los secretos `MercadoPago:BackUrlBase`, `MercadoPago:NotificationUrl` y
  `Cors:AllowedOrigins:2`. **Entrar al portal por la URL del túnel, nunca por
  localhost** — `localStorage` es por origen y Mercado Pago devuelve al comprador al
  host del túnel.
- **Las credenciales van en `dotnet user-secrets`**, nunca en `appsettings.Development
  .json`, que **sí** está versionado (de ahí el `UserSecretsId` en `PvvBff.API.csproj`).
- La fecha de `expiration_date_to` necesita ISO 8601 **con milisegundos y offset
  explícito**; un `Z` de UTC pelado lo rechaza.

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
- Recupero de leads por cualquier canal que no sea email (nada de SMS ni llamadas),
  y recupero de leads que no dejaron datos de contacto

---

## Limitación conocida — borrar una compañía no borra sus leads

Al eliminar una compañía se borran en cascada su configuración, su historial y sus
operadores: todo eso vive en la misma base SQL. **Sus leads sobreviven**, porque
viven en el MongoDB de pvv-bff — otro servicio, otro motor, y ninguna transacción
cruza esa frontera.

No es un olvido: es la consecuencia directa de que cada servicio sea dueño de sus
datos. Ese aislamiento es lo que permite desplegarlos por separado, y el precio es
que un borrado que abarca a más de uno deja de ser atómico. La salida idiomática
sería publicar un evento `CompanyDeleted` y que pvv-bff limpie lo suyo al consumirlo
(consistencia eventual); la alternativa es dejarlos como registro histórico. Se
documenta en vez de resolverse porque la decisión depende de si los leads son dato
operativo o histórico, que es una pregunta de negocio. Desarrollado en
`docs/arquitectura-pvv.md` §11.1.

---

## Cómo usar este archivo

Al iniciar una sesión de Claude Code, pasá siempre estos dos archivos:
1. `docs/CONTEXT.md` (este archivo) — estado del proyecto y convenciones
2. `docs/arquitectura-pvv.md` — arquitectura técnica detallada

Luego pegá el prompt de la tarea que corresponde al sprint y HU actual.
