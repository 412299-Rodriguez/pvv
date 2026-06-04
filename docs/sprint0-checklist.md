# Sprint 0 — Checklist de verificación

**Proyecto:** PVV — Portal de Ventas Virtual de Seguros Vehiculares
**Fecha de completado:** 2026-06-04
**Rama:** `feature/pvv-002-scaffolding-microservicios`

Entorno verificado: Docker Desktop 28.3.2 · .NET SDK 10.0.300 · Node.js v22.18.0 / npm 11.16.0

---

## HU-01 — Setup de repositorio e infraestructura base

| Ítem | Estado |
|---|:---:|
| Estructura de monorepo creada (6 servicios + infra + docs) | ✅ |
| `docker-compose.yml` con SQL Server, MongoDB, Redis, Redis Commander, RabbitMQ | ✅ |
| `.env.example` por cada microservicio (6) | ✅ |
| `README.md` con instrucciones de setup | ✅ |
| `.gitignore` raíz (+ `.gitattributes` para EOL de scripts) | ✅ |

## HU-02 — Scaffolding de los 6 microservicios

| Servicio | Estructura / requisitos | Estado |
|---|---|:---:|
| pvv-soat | Solución .NET (API/Application/Domain/Infrastructure), Swagger, `/health`, EF Core | ✅ |
| pvv-config | Solución .NET + Worker, Clean Architecture, MediatR, FluentValidation, JWT, Swagger, `/health` | ✅ |
| pvv-bff | Solución .NET, Clean Architecture, MediatR, MongoDB, Redis, JWT, Swagger, `/health` | ✅ |
| pvv-emission | Solución .NET, `EmissionWorker : BackgroundService`, `/health` (sin Swagger) | ✅ |
| pvv-front | Vite + React 19 + TS strict + Tailwind v4 + FSD, levanta en 5173 | ✅ |
| pvv-admin | Vite + React 19 + TS strict + Tailwind v4 + FSD, levanta en 5174 | ✅ |

---

## Verificación final (Prompt 2C)

### 1. Infraestructura Docker — `docker compose ps`

| Servicio | Estado |
|---|:---:|
| pvv-sqlserver | ✅ Up (healthy) |
| pvv-mongodb | ✅ Up (healthy) |
| pvv-redis | ✅ Up (healthy) |
| pvv-redis-commander | ✅ Up (healthy) |
| pvv-rabbitmq | ✅ Up (healthy) |

> Los contenedores de init (`pvv-sqlserver-init`, `pvv-rabbitmq-init`) corrieron una sola vez y terminaron en `Exited (0)` (esperado).

### 2. Servicios .NET — `GET /health`

| Servicio | Puerto | Respuesta | Estado |
|---|:---:|---|:---:|
| pvv-soat | 5001 | `{"status":"healthy","service":"pvv-soat"}` | ✅ |
| pvv-config | 5002 | `{"status":"healthy","service":"pvv-config"}` | ✅ |
| pvv-bff | 5003 | `{"status":"healthy","service":"pvv-bff"}` | ✅ |
| pvv-emission | 5004 | `{"status":"healthy","service":"pvv-emission"}` | ✅ |

### 3. Frontends — `npm run dev`

| Servicio | Puerto | HTTP | Estado |
|---|:---:|:---:|:---:|
| pvv-front | 5173 | 200 | ✅ |
| pvv-admin | 5174 | 200 | ✅ |

> Ambos también pasan `npm run build` (tsc en modo strict + vite build) sin errores.

### 4. Conectividad de bases de datos

| Verificación | Resultado | Estado |
|---|---|:---:|
| pvv-soat → `pvv_soat_db` + migration `InitialCreate` | `20260604122726_InitialCreate` en `__EFMigrationsHistory` | ✅ |
| pvv-config → `pvv_config_db` + migration `InitialCreate` | `20260604124117_InitialCreate` en `__EFMigrationsHistory` | ✅ |
| pvv-bff → MongoDB (`pvv_bff_db`) | `runCommand({ping:1})` → `{ ok: 1 }` (auth `pvv_user`) | ✅ |
| pvv-bff → Redis | `redis-cli ping` → `PONG`; el BFF conecta (eager) al arrancar | ✅ |
| pvv-emission → MongoDB | mismo cluster/credenciales, `ConnectionFactory` configurado | ✅ |
| pvv-emission → RabbitMQ | aliveness AMQP en vhost `pvv` → `{"status":"ok"}` | ✅ |

> `pvv_bff_db` se materializa en la primera escritura (comportamiento estándar de MongoDB); la conexión y las credenciales quedan verificadas contra ese namespace.

### 5. RabbitMQ — colas en vhost `pvv`

| Verificación | Resultado | Estado |
|---|---|:---:|
| Cola `pvv_emission_queue` | presente (durable, DLX → `pvv_emission_dlq`) | ✅ |
| Cola `pvv_emission_dlq` | presente (durable) | ✅ |
| Aliveness test (publish/consume real) | `{"status":"ok"}` | ✅ |

---

## Notas / desvíos respecto del documento de contexto

- **EF Core 10** en lugar de EF Core 9: es la versión que empareja con el SDK .NET 10 y con `dotnet-ef` 10.0.8. No afecta el modelo de dominio (las migraciones `InitialCreate` están vacías).
- **Tailwind CSS v4 estable** vía el plugin `@tailwindcss/vite` (el prompt mencionaba `@next`; v4 ya es estable).
- **pvv-emission** se hospeda sobre el SDK Web (minimal API) para poder exponer `GET /health` en 5004, además de correr el `EmissionWorker` como hosted service. Sin Swagger.
- El wiring de RabbitMQ consumer (pvv-emission), Ingress/Leads/Session (pvv-bff) y los handlers de CQRS (pvv-config) queda como `// TODO Sprint 1`.

## Resultado

**Sprint 0 completado ✅** — entorno 100% operativo para comenzar el Sprint 1.
