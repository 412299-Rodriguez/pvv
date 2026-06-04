# Arquitectura del Ecosistema PVV — Portal de Ventas Virtual de Seguros Vehiculares

## 1. Vista General

PVV es un sistema distribuido de 6 microservicios que permite la venta online de seguros vehiculares en Argentina, soportando múltiples compañías aseguradoras desde una misma plataforma (multi-tenancy).

```
pvv-front (React 19) ──→ pvv-bff (.NET 10) ──→ pvv-soat (.NET 10) ──→ SQL Server
                                            ──→ pvv-config (.NET 10) ──→ SQL Server
                                            ──→ MongoDB (leads, event logs)
                                            ──→ Redis (config cache, sesiones, rutas ingress)
                                            ──→ RabbitMQ
                                            ──→ Mercado Pago API

pvv-emission (Worker .NET 10) ←── RabbitMQ ←── pvv-bff (post-pago confirmado)
pvv-admin (React 19) ──→ pvv-config (.NET 10)
pvv-admin (React 19) ──→ pvv-bff (.NET 10) (lectura de leads y analytics)
```

### Diferencias clave respecto al sistema de referencia (PVV2)

| Feature | PVV2 (referencia) | PVV (nuestro alcance) |
|---|---|---|
| Validación OTP (SMS/email) | ✅ | ❌ Fuera de alcance |
| Trust score / anti-fraude | ✅ SignalR en tiempo real | ❌ Fuera de alcance |
| Consulta registros oficiales (RUNT) | ✅ Bitsion API | ❌ Fuera de alcance |
| Cross-sell seguros adicionales | ✅ Insurcloud | ❌ Fuera de alcance |
| Pasarela de pagos | iRecaudo (PSE/tarjeta) | Mercado Pago SDK |
| Workers de emisión | 3 (Multicash, Insurcloud retry, Email) | 1 (RabbitMQ consumer) |
| Admin panel tabs | 16 tabs | 3 grupos funcionales |
| Pipeline de seguridad BFF | 8 middlewares | 4 middlewares (sin trust score/HMAC) |

---

## 2. pvv-config — API de Configuración Multi-tenant

### 2.0 Esencia del servicio

**pvv-config es el cerebro administrativo del sistema.** Su razón de ser es que cada compañía aseguradora que use el portal tenga su propia identidad, sus propios productos y sus propios precios, completamente aislados de las demás. Todo lo que un operador configura desde `pvv-admin` termina guardado acá.

**Qué guarda y gestiona:**
- **Compañías:** El alta, baja y modificación de las aseguradoras que usan el sistema. Cada compañía tiene un token encriptado (AES-256) que es el que viaja en la URL del portal del usuario (`?c=<token>`), de modo que nunca se expone el ID interno.
- **Productos:** Cada compañía define qué productos de seguro ofrece (nombre, tipo de cobertura, condiciones). Un producto puede activarse o desactivarse sin borrarse.
- **Precios:** Las reglas de precio de cada producto, segmentadas por tipo de vehículo y año. Así una compañía puede cobrar distinto por un auto nuevo que por uno viejo, o por un camión que por una moto.
- **Apariencia:** Los colores, logo y textos que el portal del usuario (`pvv-front`) va a mostrar para esa compañía. Theming dinámico sin tocar código.
- **Historial de cambios:** Cada vez que se edita cualquier configuración, se guarda un registro con el valor anterior y el nuevo. Auditoría completa.

**Qué hace además de guardar:**
- Emite JWT para que los operadores de cada aseguradora puedan autenticarse y solo vean los datos de su compañía.
- Mantiene Redis actualizado en todo momento a través de su worker interno (`CacheSyncWorker`), para que `pvv-bff` pueda leer la configuración en microsegundos sin tocar SQL Server en cada request del usuario.
- Cuando un operador edita algo, invalida el caché de esa compañía en Redis de forma inmediata (Pub/Sub), para que el cambio se refleje en el portal sin esperar al próximo ciclo del worker.

**Quién lo consume:**
- `pvv-admin` lo llama directamente para todas las operaciones de gestión (CRUD de compañías, productos, precios, apariencia).
- `pvv-bff` lo consume en modo lectura: primero busca en Redis (fast path), si no encuentra hace fallback HTTP a pvv-config.

### 2.1 Estructura de Capas (Clean Architecture)

| Proyecto | Capa | Responsabilidad |
|---|---|---|
| PVVConfig.API | Presentación | Controllers, middleware, DI, Program.cs |
| PVVConfig.Application | Lógica de negocio | Commands, Queries, Handlers (MediatR 12), Validators (FluentValidation) |
| PVVConfig.Domain | Dominio puro | Entidades: Company, Product, Pricing, AppearanceConfig, ConfigurationHistory |
| PVVConfig.Infrastructure | Acceso a datos | Repositories (EF Core 9 → SQL Server), Redis (StackExchange.Redis), servicios externos |
| PVVConfig.Worker | Background Service | Sincronización SQL → Redis (CacheSyncWorker) |

Dependencias: `API → Application → Domain ← Infrastructure`

### 2.2 CQRS con MediatR 12

Se separan lecturas (Queries) de escrituras (Commands). Cada una vive en su propio Handler.

**Query ejemplo:**
```csharp
public class GetProductsByCompanyQuery : IRequest<IEnumerable<ProductDto>>
{
    public Guid CompanyId { get; set; }
}

public class GetProductsByCompanyHandler : IRequestHandler<GetProductsByCompanyQuery, IEnumerable<ProductDto>>
{
    public async Task<IEnumerable<ProductDto>> Handle(GetProductsByCompanyQuery request, CancellationToken ct)
        => await _productRepository.GetByCompanyAsync(request.CompanyId, ct);
}
```

**Command ejemplo (con side effects):**
```csharp
public class CreateOrUpdateAppearanceCommand : IRequest<AppearanceConfigDto>
{
    public Guid CompanyId { get; set; }
    public string PrimaryColor { get; set; }
    public string LogoUrl { get; set; }
    public string CompanyName { get; set; }
}
```

El Handler de este command:
1. Busca si ya existe la config de apariencia para esa compañía en SQL
2. Crea un registro en `ConfigurationHistory` (auditoría: valor anterior / nuevo)
3. Persiste con transacción atómica (config + history)
4. Invalida el caché en Redis vía Pub/Sub
5. Retorna la config actualizada como DTO

Registro de MediatR:
```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
```

### 2.3 Entidades de Dominio

```
Company
  ├── CompanyId (GUID)
  ├── HashedCompanyId (AES-256, URL-safe) ← token que ve el frontend
  ├── Name, CUIT, IsActive
  └── AppearanceConfig (colores, logo, textos)

Product
  ├── ProductId, CompanyId
  ├── Name, CoverageType, Conditions
  └── IsActive

Pricing
  ├── PricingId, ProductId
  ├── VehicleType, YearFrom, YearTo
  └── Price

ConfigurationHistory (auditoría)
  ├── CompanyId, ConfigType
  ├── ValueBefore (JSON), ValueAfter (JSON)
  └── ModifiedAt, ModifiedBy
```

### 2.4 Worker de Sincronización SQL → Redis

El `CacheSyncWorker` (BackgroundService) asegura que Redis siempre tenga las configuraciones actualizadas:

**ExecuteAsync:**
1. Suscribirse al canal Pub/Sub `"pvv:cache:invalidate"` (best-effort)
2. Ejecutar sync inicial al arrancar
3. Loop con `PeriodicTimer` cada 5 minutos (fallback)

**Qué sincroniza:**
- **Apariencia por compañía:** Lee configs activas de SQL → serializa JSON → escribe en Redis con key `pvv:config:{hashedCompanyId}:appearance` y TTL 15 min
- **Productos activos por compañía:** key `pvv:config:{hashedCompanyId}:products`
- **Precios por producto:** key `pvv:pricing:{productId}`

### 2.5 Invalidación de Caché por Pub/Sub

**Flujo:**
1. Admin edita config desde `pvv-admin` → pvv-config API recibe el Command
2. Handler persiste en SQL → llama a `ICacheInvalidator`
3. `RedisCacheInvalidator` publica en canal `"pvv:cache:invalidate"` con el `hashedCompanyId`
4. `CacheSyncWorker` (suscrito) recibe la señal → ejecuta sync inmediato solo para esa compañía
5. Si Pub/Sub falla → el timer de 5 min actúa como fallback (consistencia eventual)
6. Adicionalmente, borra la key específica en Redis para que el BFF haga fallback HTTP si lee antes de que el Worker repopule

### 2.6 Multi-tenancy y Tokens

Cada compañía tiene:
- **CompanyId** (GUID interno, nunca expuesto)
- **HashedCompanyId** (GUID encriptado con AES-256-CBC, URL-safe) — es el token que viaja en la URL del portal (`?c=<HashedCompanyId>`)
- Auth JWT propio con `companyId` en claims para que los operadores de cada aseguradora solo vean sus datos

---

## 3. pvv-bff — Gateway e Inteligencia de Sesión

### 3.0 Esencia del servicio

**pvv-bff es el único punto de entrada del sistema para el usuario final.** `pvv-front` no sabe nada de pvv-soat, ni de pvv-config, ni de Mercado Pago — todo pasa por acá. Su trabajo es recibir los pedidos del wizard, enrutarlos al servicio correcto, y además ir construyendo en tiempo real el perfil de comportamiento del usuario (el "lead").

**Qué hace:**
- **Gateway dinámico (Ingress):** Recibe requests del frontend con un hash opaco (ej: `"PLATE_SEARCH"`) en lugar de una URL real. Busca en Redis a qué endpoint interno corresponde ese hash y hace el proxy. Esto desacopla completamente al frontend de la topología interna — si mañana pvv-soat cambia de puerto o de URL, solo se actualiza Redis, no el frontend.
- **Sesiones anónimas:** Crea y mantiene una sesión por cookie para cada visitante, sin requerir login. Así puede trackear el recorrido del usuario aunque no se haya identificado.
- **LeadProjection:** Por cada evento que el wizard dispara (buscó una patente, eligió un producto, abandonó en el pago), actualiza un documento `Lead` en MongoDB. Este documento es la "vista materializada" del embudo de conversión — es lo que después consume `pvv-admin` para el dashboard y la tabla de leads.
- **Gestión de pagos:** Crea la preferencia de pago en Mercado Pago, registra la transacción, recibe el webhook de confirmación y detecta cuándo un usuario inició el pago pero nunca volvió (abandono de pago).
- **Mensajería:** Una vez confirmado el pago, publica un mensaje en RabbitMQ para que `pvv-emission` emita la póliza de forma asíncrona.
- **Seguridad:** Pipeline de middlewares que aplica fingerprinting, Cloudflare Turnstile (anti-bot), sesión y rate limiting a cada request.

**Qué guarda en MongoDB:**
- `leads`: un documento por intento de compra, actualizado paso a paso. Es la fuente de verdad del embudo.
- `event_logs`: log crudo de todos los eventos del wizard. Append-only, TTL 90 días.

**Quién lo consume:**
- `pvv-front` lo llama para absolutamente todo: buscar vehículos, calcular presupuestos, pagar, consultar el estado de la emisión.
- `pvv-admin` lo consulta para leer leads y datos del dashboard (aggregation pipelines sobre MongoDB).

### 3.1 Estructura de Capas

| Proyecto | Contenido |
|---|---|
| Domain | Entidades: RouteConfig, Lead, EventLog |
| Application | Handlers MediatR, LeadProjectionService, interfaces |
| Infrastructure | RedisService, MongoRepositories, SoatApiClient, MercadoPagoClient, SessionService |
| API | Controllers, middlewares, Program.cs |

### 3.2 El Sistema de Ingress (routing dinámico por hash)

El BFF implementa un gateway dinámico basado en hashes Redis. El frontend **nunca** llama endpoints directamente — envía requests a `/api/ingress` con un hash de 12 caracteres.

**Flujo de un request:**
```
pvv-front: POST /api/ingress { hash: "KBIZN4UY2ZQJ", body: {...} }
    ↓
IngressController → MediatR → IngressHandler
    ↓
1. Redis lookup: key "route:KBIZN4UY2ZQJ:POST"
   → Retorna: { endpoint: "http://pvv-soat/api/vehicles/search", timeout: 15 }
    ↓
2. Agrega headers internos: X-BFF-Internal, X-Company-Id, X-Session-Id, X-Correlation-Id
    ↓
3. HTTP proxy call al servicio interno con timeout configurable
    ↓
4. Retorna { statusCode, data, error } al frontend
```

Las rutas (`ingress-routes.json`) se siembran en Redis al arrancar el BFF via `IngressRoutesSeederHostedService`. Permite modificar rutas sin redesplegar — solo actualizando Redis.

**Hashes de nuestro sistema (ejemplos):**

| Hash | Verbo | Destino |
|---|---|---|
| `PLATE_SEARCH` | GET | pvv-soat: búsqueda de vehículo por patente |
| `BUDGET_CALC` | POST | pvv-soat: calcular presupuesto |
| `PAYMENT_INIT` | POST | Mercado Pago: iniciar checkout |
| `CONFIG_LOAD` | GET | pvv-config (vía Redis): cargar config de compañía |
| `LEAD_EVENT` | POST | BFF interno: registrar evento de wizard |

### 3.3 LeadProjection — Vista Materializada de Leads

El `LeadProjectionService` transforma eventos del wizard del frontend en un documento `Lead` en MongoDB, usando upserts con operadores atómicos:

```
Evento "plate_validated" →
  $set: { "steps.step1.status": "completed", "steps.step1.plate": "ABC123" }
  $max: { "lastStep": 1 }   ← nunca retrocede

Evento "budget_calculated" →
  $set: { "steps.step3.status": "completed", "steps.step3.amount": 15000 }
  $max: { "lastStep": 3 }

Evento "payment_abandoned" →
  $set: { "steps.step4.status": "abandoned", "abandonedAt": now }
  ← este es el que alimenta el recupero de abandono en pvv-admin
```

**Operadores MongoDB usados:**
- `$set` con dot-notation (actualización parcial sin reemplazar documento)
- `$setOnInsert` (defaults solo al crear el documento)
- `$max` (lastStep solo sube, nunca baja)
- `$cond` (no sobreescribir "completed" con "abandoned")

**Estructura del Lead en MongoDB:**
```
Lead
  ├── companyId, sessionId, flowId
  ├── lastStep (1-5)
  ├── status: active | abandoned | completed
  ├── contactEmail, contactPhone (para recupero)
  └── steps
        ├── step1 (Vehículo): plate, vehicleData, status
        ├── step2 (Tomador): name, dni, email, phone, status
        ├── step3 (Cotización): productId, amount, status
        ├── step4 (Pago): paymentMethod, mpPreferenceId, status
        └── step5 (Emisión): policyNumber, status
```

### 3.4 MongoDB — Colecciones

| Colección | Propósito |
|---|---|
| `leads` | Vista materializada del embudo (upsert por companyId + flowId) |
| `event_logs` | Log crudo de eventos del wizard (append-only, TTL 90 días) |
| `logs` | Logs de Serilog (diagnóstico de app) |

> ⚠️ **Simplificación vs PVV2:** Eliminamos `bot_detections` porque no implementamos trust score.

### 3.5 Pipeline de Middleware

```
Request → ExceptionHandler → CORS → Auth
        → FingerprintMiddleware (SHA256 de IP + UA + ClientId)
        → TurnstileMiddleware (CAPTCHA Cloudflare anti-bot)
        → SessionMiddleware (cookie pvv-session, Redis-backed)
        → RateLimiter → Controllers
```

> ⚠️ **Simplificación vs PVV2:** Eliminamos `HmacValidation` (canal API) y `TrustScoreBlockingMiddleware`.

### 3.6 Gestión de Pagos con Mercado Pago

**Flujo completo:**
1. `pvv-front` envía datos del presupuesto seleccionado → BFF crea preferencia de pago en Mercado Pago API
2. BFF registra la transacción en MongoDB como `pending` con el `mp_preference_id`
3. Devuelve la `init_point` URL al frontend → usuario paga en Mercado Pago
4. Mercado Pago llama al webhook `POST /api/payments/webhook` → BFF confirma el pago
5. BFF actualiza el lead como `payment_confirmed` → publica evento a RabbitMQ para que pvv-emission emita la póliza
6. **Detección de abandono:** job periódico que busca transacciones `pending` con más de N minutos → las marca como `payment_abandoned` y actualiza el lead

---

## 4. pvv-soat — Dominio Central de Pólizas

### 4.0 Esencia del servicio

**pvv-soat es el corazón del negocio.** Mientras que pvv-config sabe qué productos existen y a qué precio, y pvv-bff sabe qué está haciendo el usuario, pvv-soat es el único que sabe qué vehículos existen, quiénes son los dueños, cuánto costaría asegurarlos y cuáles pólizas están vigentes.

**Qué guarda y gestiona:**
- **Vehículos:** El registro de todos los vehículos que alguna vez pasaron por el portal. Se identifican por patente. Si un usuario ingresa una patente que ya existe en la base, se recuperan sus datos. Si es nueva, se crea el registro con la información que el usuario completó en el wizard.
- **Tomadores (Holders):** Los titulares del seguro. Se identifican por DNI. Al igual que los vehículos, si el DNI ya existe se recuperan los datos conocidos para pre-completar el formulario.
- **Presupuestos (Budgets):** Cuando un usuario selecciona un vehículo y quiere cotizar, pvv-soat genera un presupuesto: la combinación de vehículo + tomador + producto + compañía + precio calculado. El presupuesto tiene una vigencia corta (30 minutos) porque los precios pueden cambiar. Si el usuario paga dentro de ese tiempo, el presupuesto se "convierte" en póliza.
- **Pólizas (Policies):** El resultado final del proceso. Una póliza emitida tiene número único, fecha de inicio y fin, y un ciclo de vida completo: `Pending → Issued → Active → Expired / Cancelled`. Quien la emite no es el usuario directamente, sino `pvv-emission` llamando a pvv-soat después de que el pago fue confirmado.

**Quién lo consume:**
- `pvv-bff` lo llama (vía proxy ingress) para buscar vehículos, crear tomadores y generar presupuestos durante el wizard del usuario.
- `pvv-emission` lo llama directamente (llamada interna entre servicios) para emitir la póliza una vez confirmado el pago.

### 4.1 Estructura

Microservicio .NET 10 con arquitectura de capas simple (sin Clean Architecture completa):

| Capa | Responsabilidad |
|---|---|
| API | Controllers + Swagger |
| Application | Services de dominio |
| Domain | Entidades: Vehicle, Holder, Budget, Policy |
| Infrastructure | EF Core → SQL Server |

### 4.2 Entidades y Relaciones

```
Vehicle
  ├── VehicleId, Plate (unique)
  ├── Brand, Model, Year, VehicleType
  └── Holders (navigation)

Holder (Tomador)
  ├── HolderId, DNI (unique)
  ├── Name, Email, Phone
  └── Vehicles (navigation)

Budget (Presupuesto)
  ├── BudgetId, VehicleId, HolderId
  ├── CompanyId, ProductId
  ├── Price, ValidUntil
  └── Status: Active | Expired | Converted

Policy (Póliza)
  ├── PolicyId, PolicyNumber (único, legible)
  ├── BudgetId (origen)
  ├── VehicleId, HolderId, CompanyId, ProductId
  ├── StartDate, EndDate, Price
  └── Status: Pending | Issued | Active | Expired | Cancelled
```

### 4.3 Flujo de negocio principal

```
1. Búsqueda de vehículo por patente
   → Existe en BD: retorna datos
   → No existe: crea vehículo (con datos del formulario)

2. Búsqueda/creación de tomador por DNI

3. Generación de presupuesto
   → Consulta precios desde pvv-config (vía Redis o HTTP)
   → Crea Budget con ValidUntil = now + 30 minutos
   → Retorna opciones de precio por compañía

4. Emisión de póliza (llamada desde pvv-emission)
   → Valida que el Budget exista y esté Active
   → Valida que no haya póliza vigente para ese vehículo
   → Crea Policy con Status = Pending
   → Genera PolicyNumber único
   → Actualiza Status → Issued
   → Retorna póliza emitida
```

---

## 5. pvv-emission — Worker de Emisión

### 5.0 Esencia del servicio

**pvv-emission es el último eslabón de la cadena.** No tiene interfaz, no tiene Swagger, no recibe requests del usuario. Su único trabajo es escuchar la cola de RabbitMQ y, cuando llega un mensaje de "pago confirmado", hacer que pvv-soat emita la póliza.

**Por qué existe como servicio separado y no lo hace pvv-bff directamente:**
Porque la emisión de una póliza puede fallar (pvv-soat podría estar temporalmente caído, o haber un conflicto de datos). Si pvv-bff lo hiciera de forma sincrónica, el usuario quedaría esperando o recibiría un error aunque el pago ya fue acreditado. Al desacoplarlo con RabbitMQ, el pago se confirma al usuario de inmediato y la emisión se procesa de forma asíncrona con reintentos automáticos. El usuario ve el resultado unos segundos después por polling.

**Qué hace:**
- Escucha la cola `pvv_emission_queue` de forma continua (event-driven, no polling).
- Por cada mensaje: valida los datos, llama a pvv-soat para emitir la póliza, y registra el resultado.
- Si pvv-soat falla, reintenta con backoff exponencial (1 min → 2 min → 3 min).
- Si agota los reintentos, manda el mensaje a la dead-letter queue (`pvv_emission_dlq`) para revisión manual.
- Registra el estado de cada emisión en MongoDB: `pending → success / failed / retry-exhausted`.

**Quién lo activa:**
- Nadie lo llama directamente. Se activa solo cuando pvv-bff publica un mensaje en RabbitMQ al recibir el webhook de pago confirmado de Mercado Pago.

### 5.1 Estructura

Clean Architecture: Domain, Application, Infrastructure, Worker.

### 5.2 Un solo BackgroundService

A diferencia del PVV2 (que tiene 3 workers), nosotros tenemos uno:

| Worker | Mecanismo | Cola |
|---|---|---|
| `EmissionWorker` | RabbitMQ consumer (event-driven) | `pvv_emission_queue` |

> **Simplificación vs PVV2:** Eliminamos `InsurcloudRetryWorker` (no hay cross-sell) y `PepEmailDispatcherWorker` (no hay email outbox en este sprint).

### 5.3 Flujo de Emisión

Cuando Mercado Pago confirma el pago, pvv-bff publica un mensaje en RabbitMQ:

```json
{
  "budgetId": "...",
  "companyId": "...",
  "flowId": "...",
  "paidAt": "2026-05-19T10:00:00Z"
}
```

**EmissionWorker procesa el mensaje:**

1. **Validar** mensaje: deserializar, verificar campos requeridos
2. **Cargar** datos del Budget en pvv-soat (HTTP interno)
3. **Emitir** póliza: `POST pvv-soat/api/policies/emit` con el budgetId
4. **Confirmar** resultado: actualizar estado en MongoDB vía pvv-bff
5. **Acknowledge** el mensaje en RabbitMQ

**Resiliencia:**
- Reintento con backoff exponencial: 1 min → 2 min → 3 min
- Tras 3 fallos → dead-letter queue `pvv_emission_dlq`
- Estado en MongoDB: `pending` → `success` / `failed` / `retry-exhausted`

---

## 6. pvv-front — Portal de Compra (React 19)

### 6.0 Esencia del servicio

**pvv-front es lo que ve el usuario que quiere comprar su seguro.** Es una SPA de una sola página con un wizard de 5 pasos que guía al usuario desde "ingresar la patente" hasta "ver la póliza emitida". Todo el proceso es 100% digital y autoasistido — el usuario no necesita hablar con nadie.

**Qué hace el usuario acá:**
1. Ingresa la patente de su vehículo → el sistema recupera o crea el registro del vehículo.
2. Completa sus datos personales (DNI, nombre, email, teléfono) → el sistema recupera o crea el tomador.
3. Ve las opciones de cotización disponibles para su vehículo → elige la que más le conviene.
4. Paga con Mercado Pago → es redirigido al checkout de MP y vuelve al portal.
5. Ve el resultado: póliza emitida con su número, o un mensaje de error si algo falló.

**Qué hace el sistema por detrás (transparente para el usuario):**
- Al cargar, lee el token `?c=` de la URL para saber a qué compañía aseguradora pertenece ese portal, y aplica los colores, logo y textos de esa compañía (theming dinámico).
- Cada acción del usuario dispara un evento de tracking que pvv-bff convierte en un Lead en MongoDB. Así pvv-admin puede ver en tiempo real en qué paso están los usuarios y cuáles abandonaron.
- Nunca llama a pvv-soat ni a Mercado Pago directamente — todo pasa por pvv-bff usando el patrón Ingress (hashes en lugar de URLs).

### 6.1 Stack y Estructura

React 19, TypeScript 5 (strict), Vite 6, Tailwind CSS v4, Zustand, React Router.

Estructura Feature-Sliced Design (FSD):
```
src/
├── app/        → Providers, routing, inicialización
├── pages/      → PurchasePage (única página real)
├── widgets/    → purchase-wizard (wizard principal)
├── features/   → vehicle-search, holder-form, quotation, checkout, emission-result
├── entities/   → Vehicle, Holder, Budget, Policy, Company
└── shared/     → ingress client, BI tracking, UI components, hooks
```

### 6.2 Inicialización y Multi-compañía

URL: `/soat?c=<HASHED_COMPANY_ID>`

**Flujo de arranque:**
1. Extrae `?c=` de la URL → token AES-256 que identifica la compañía
2. Token inválido → pantalla `InvalidCompanyScreen`
3. Carga paralela: config UI (colores, logo, textos), masters (tipos doc, ciudades)
4. Aplica theming dinámico: inyecta CSS custom properties (`--color-primary`, `--color-secondary`, etc.)
5. Trackea evento `session_start`

### 6.3 Wizard de Compra (5 pasos)

| Paso | Componente | Acción Principal |
|---|---|---|
| 1. Vehículo | `VehicleSearchForm` | Ingresar patente → buscar/crear vehículo en pvv-soat |
| 2. Tomador | `HolderForm` | Ingresar DNI + datos personales → buscar/crear tomador |
| 3. Cotización | `QuotationForm` | Ver opciones de precio por compañía → seleccionar producto |
| 4. Checkout | `CheckoutModal` | Iniciar pago con Mercado Pago → redirigir al checkout |
| 5. Resultado | `EmissionResultModal` | Mostrar estado: emitiendo / póliza emitida / error con derivación |

### 6.4 Comunicación con BFF (Patrón Ingress)

Todas las llamadas van por `ingress()`:
```typescript
async function ingress<T>(hash: string, verb: 'GET' | 'POST' | 'PUT', body?: unknown): Promise<T>
```

Headers automáticos: `X-Company-Token`, `X-Session-Id`, `X-Turnstile-Token`.
Auto-retry en 5xx con backoff (300ms, 800ms).

### 6.5 BI Tracking (eventos del wizard)

Eventos rastreados para alimentar el dashboard de pvv-admin:

| Evento | Cuándo se dispara |
|---|---|
| `session_start` | Al cargar el portal |
| `plate_entered` | Al tipear la patente |
| `plate_validated` | Al encontrar el vehículo |
| `holder_completed` | Al completar datos del tomador |
| `budget_calculated` | Al ver las cotizaciones |
| `product_selected` | Al elegir una opción |
| `payment_initiated` | Al ir al checkout de MP |
| `payment_confirmed` | Webhook confirmó el pago |
| `payment_abandoned` | Usuario salió sin pagar |
| `policy_issued` | Póliza emitida exitosamente |
| `wizard_error` | Error en cualquier paso |

### 6.6 State Management (Zustand)

Stores por feature (no un store global):
- **useWizardStore** — paso actual, vehículo, tomador, presupuesto seleccionado
- **useBIStore** — sessionId, flowId, timestamps
- **useCompanyStore** — token y config de la compañía
- **usePvvConfigStore** — theming dinámico (colores, textos, logo)

---

## 7. pvv-admin — Panel de Administración (React 19)

### 7.0 Esencia del servicio

**pvv-admin es lo que ven los operadores de cada compañía aseguradora.** Tiene dos funciones muy distintas: configurar el portal y medir su rendimiento.

**Qué hace un operador acá:**

**Configuración (gestión):**
- Da de alta su compañía en el sistema y obtiene el token que va en la URL del portal.
- Crea y gestiona sus productos de seguro: qué coberturas ofrece, a qué precio, para qué tipos de vehículo.
- Personaliza la apariencia de su portal: colores de la marca, logo, textos de cada pantalla del wizard.

**Medición (analytics):**
- Ve el embudo de conversión: de todos los usuarios que llegaron al portal, cuántos buscaron un vehículo, cuántos llegaron a cotizar, cuántos pagaron, cuántos tienen la póliza emitida. Con las tasas de conversión entre cada paso.
- Accede a la tabla de leads: el listado de todos los intentos de compra con sus datos de contacto, filtrable por paso del embudo, fecha, estado y método de pago.
- Exporta leads a Excel para trabajarlos externamente.
- Identifica los leads que abandonaron en el paso de pago y les manda un email de recupero directamente desde la tabla.

**Quién puede acceder:**
- Solo operadores autenticados con JWT emitido por pvv-config. Cada operador solo ve los datos de su propia compañía (el `companyId` viene en el token).

### 7.1 Stack

React 19, TypeScript 5 (strict), Vite 6, Tailwind CSS v4, Zustand, React Router. Auth JWT con refresh automático.

### 7.2 Módulos

**Gestión (CRUD)**
- ABM de compañías aseguradoras
- ABM de productos por compañía
- ABM de reglas de precio por producto
- Configuración de apariencia del portal (colores, logo, textos)

**Dashboard de Analytics**
- Embudo de conversión de 5 pasos (trapezoides) con tasas por etapa
- KPIs: total leads, tasa de conversión global, pólizas emitidas
- Actualización periódica (auto-refresh cada 5 minutos)

**Tabla de Leads**
- Paginada, filtros por: paso del embudo, rango de fechas, estado, método de pago
- 4 tabs según paso donde quedó el lead
- Exportación a Excel (hasta 10.000 filas con paginación automática)

**Recupero de Abandono**
- Leads con `status = abandoned` y `steps.step4.status = payment_abandoned` son identificados automáticamente
- Acción de contacto por email directo desde la tabla (abre modal con template pre-cargado)

---

## 8. Patrones Arquitectónicos Aplicados

| Patrón | Dónde se usa |
|---|---|
| Clean Architecture | pvv-config, pvv-bff, pvv-emission |
| CQRS + MediatR | pvv-config (Commands/Queries), pvv-bff (Ingress/Events) |
| Event Sourcing (parcial) | LeadProjection: eventos del wizard → vista materializada en MongoDB |
| BFF Pattern | pvv-bff como gateway unificado del frontend |
| Redis Pub/Sub | Invalidación de caché entre pvv-config API y CacheSyncWorker |
| Feature-Sliced Design | pvv-front, pvv-admin |
| Multi-tenancy | Token AES-256 en URL, config y JWT por compañía |
| Ingress Hash Routing | Desacoplamiento frontend ↔ API topology (frontend no conoce URLs internas) |
| Worker Pattern | pvv-emission: RabbitMQ consumer con backoff exponencial |
| Outbox / Webhook | pvv-bff: recibe webhook de Mercado Pago y publica a RabbitMQ |

---

## 9. Diagrama de Flujo Completo — Compra Exitosa

```
Usuario ingresa patente
        ↓
pvv-front → ingress(PLATE_SEARCH) → pvv-bff → pvv-soat
        ↓ vehículo encontrado/creado
Usuario completa datos del tomador
        ↓
pvv-front → ingress(BUDGET_CALC) → pvv-bff → pvv-soat
                                           → pvv-config (Redis: precios)
        ↓ opciones de cotización
Usuario selecciona producto → inicia pago
        ↓
pvv-front → ingress(PAYMENT_INIT) → pvv-bff → Mercado Pago API
        ↓ init_point URL
Usuario paga en Mercado Pago
        ↓
Mercado Pago → webhook → pvv-bff (POST /api/payments/webhook)
        ↓ pago confirmado
pvv-bff → actualiza Lead en MongoDB (step4 = completed)
pvv-bff → publica en RabbitMQ: { budgetId, companyId, flowId }
        ↓
pvv-emission → consume mensaje → llama pvv-soat → emite póliza
pvv-emission → actualiza estado en MongoDB: success
        ↓
pvv-front polling → ingress(EMISSION_STATUS) → pvv-bff → MongoDB
        ↓ status = success
Usuario ve póliza emitida ✅
```
