# PVV — Portal de Ventas Virtual de Seguros Vehiculares

Ecosistema de **6 microservicios** que digitaliza la venta online de seguros
vehiculares en Argentina, soportando múltiples compañías aseguradoras desde una
misma plataforma (multi-tenancy). Trabajo Final Integrador — Tecnicatura
Universitaria en Programación, UTN FRC.

El portal del usuario final es un wizard de 5 pasos (vehículo → tomador →
cotización → pago con Mercado Pago → emisión de póliza). Los operadores de cada
aseguradora gestionan productos, precios, apariencia y analytics desde un panel
de administración.

> 📚 Documentación de arquitectura completa en [`docs/arquitectura-pvv.md`](docs/arquitectura-pvv.md)
> y contexto del proyecto en [`docs/CONTEXT.md`](docs/CONTEXT.md).

## Microservicios

| Servicio | Tipo | Puerto | Descripción |
|---|---|---|---|
| `pvv-soat` | ASP.NET Core Web API | 5001 | Dominio central: vehículos, tomadores, presupuestos, pólizas |
| `pvv-config` | ASP.NET Core Web API + Worker | 5002 | Configuración multi-tenant + sync SQL→Redis |
| `pvv-bff` | ASP.NET Core Web API | 5003 | Gateway, sesiones, leads MongoDB, pagos MP, RabbitMQ |
| `pvv-emission` | .NET Worker Service | 5004 | Consume RabbitMQ y emite pólizas en pvv-soat |
| `pvv-front` | React 19 SPA | 5173 | Portal de compra — wizard 5 pasos |
| `pvv-admin` | React 19 SPA | 5174 | Panel de administración + dashboard de analytics |

## Requisitos previos

- **Docker Desktop** (con Docker Compose v2)
- **.NET 10 SDK**
- **Node.js 20+** (con npm)

## Setup local paso a paso

1. **Clonar el repositorio**

   ```bash
   git clone <url-del-repo>
   cd pvv
   ```

2. **Copiar el `.env.example` a `.env` en cada servicio**

   Cada microservicio trae un `.env.example` con las variables que necesita.
   Copialo a `.env` y completá los valores que correspondan:

   ```bash
   cp pvv-soat/.env.example     pvv-soat/.env
   cp pvv-config/.env.example   pvv-config/.env
   cp pvv-bff/.env.example      pvv-bff/.env
   cp pvv-emission/.env.example pvv-emission/.env
   cp pvv-front/.env.example    pvv-front/.env
   cp pvv-admin/.env.example    pvv-admin/.env
   ```

   > En PowerShell usá `Copy-Item pvv-soat/.env.example pvv-soat/.env`.

3. **Levantar la infraestructura (bases de datos, caché y mensajería)**

   ```bash
   cd infra
   docker-compose up -d
   ```

   Esto levanta SQL Server, MongoDB, Redis, Redis Commander y RabbitMQ. Al
   arrancar:
   - se crean automáticamente las bases `pvv_soat_db` y `pvv_config_db`,
   - se declaran el vhost `pvv`, el usuario `pvv_user` y las colas
     `pvv_emission_queue` y `pvv_emission_dlq`.

4. **Verificar que todos los servicios levantaron**

   ```bash
   docker-compose ps
   ```

   Todos los servicios de larga duración deben figurar como `running`/`healthy`.
   Los contenedores de init (`pvv-sqlserver-init`, `pvv-rabbitmq-init`) corren
   una sola vez y terminan en `exited (0)` — eso es lo esperado.

   Comprobaciones rápidas:
   - **RabbitMQ UI:** http://localhost:15672 (`pvv_user` / `pvv_pass`)
   - **Redis Commander:** http://localhost:8081

## Puertos

| Componente | Puerto |
|---|---|
| pvv-soat | 5001 |
| pvv-config | 5002 |
| pvv-bff | 5003 |
| pvv-emission | 5004 |
| pvv-front | 5173 |
| pvv-admin | 5174 |
| SQL Server | 1433 |
| MongoDB | 27017 |
| Redis | 6379 |
| RabbitMQ (AMQP) | 5672 |
| RabbitMQ UI | 15672 |
| Redis Commander | 8081 |

## Branching strategy

- **`main`** → rama estable. Solo se mergea desde `develop` vía Pull Request.
- **`develop`** → rama de integración. Acá convergen las features antes de pasar a `main`.
- **`feature/pvv-XXX-descripcion`** → una rama por Historia de Usuario
  (ej. `feature/pvv-012-vehicle-search`). Se mergea a `develop` vía PR.
