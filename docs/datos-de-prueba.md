# Datos de prueba

Referencia para levantar el sistema y recorrer los casos de prueba de punta a punta.

> ⚠️ **Todo lo que sigue es de desarrollo.** Las credenciales están sembradas por
> `ConfigDbSeeder` y solo existen en el entorno local. Nada de esto sirve —ni debe
> usarse— fuera de una máquina de desarrollo.

---

## 1. Servicios

| Servicio | Puerto | Cómo se levanta |
|---|---|---|
| pvv-soat | 5001 | `dotnet run` en `pvv-soat/PvvSoat.API` |
| pvv-config | 5002 | `dotnet run` en `pvv-config/PvvConfig.API` |
| pvv-bff | 5003 | `dotnet run` en `pvv-bff/PvvBff.API` |
| pvv-emission | — | `dotnet run` en `pvv-emission/PvvEmission.Worker` |
| pvv-front (portal) | 5173 | `npm run dev` en `pvv-front` |
| pvv-admin (panel) | 5174 | `npm run dev` en `pvv-admin` |

Infraestructura en Docker (`infra/docker-compose.yml`): SQL Server 1433 · MongoDB
27017 · Redis 6379 · RabbitMQ 5672 (UI 15672) · Redis Commander 8081.
Usuario/clave de Mongo y RabbitMQ: `pvv_user` / `pvv_pass`. SQL: `sa` / `PvvLocal123!`.

Para una compra completa hacen falta **los cinco**: config, soat, bff, worker y front.
Sin el worker el pago se confirma pero la póliza nunca se emite.

---

## 2. Cuentas del panel

| Usuario | Clave | Rol | Alcance |
|---|---|---|---|
| `superadmin@pvv.com` | `Super123!` | SystemAdmin | Alta de compañías y operadores. **No** ve leads ni config de inquilinos |
| `admin@segucor.com` | `Segu123!` | CompanyOperator | Solo SeguCor |
| `operador@rivadavia.com` | `Riva123!` | CompanyOperator | Solo Seguros Rivadavia |
| `admin@vehisegur.com` | `Vehi123!` | CompanyOperator | Solo VehiSegur |

El superadmin es administrador de plataforma: crea compañías y sus operadores, y
nada más. La configuración y los leads de cada compañía son de su propio operador.

---

## 3. Compañías y portales

El link del portal es `http://localhost:5173/?c=<hash>`. Ese hash es el `companyId`
encriptado con AES: **el token es la identidad del inquilino**, no hay tabla de links.
Se obtiene con el botón **Copiar portal** en la lista de compañías del superadmin.

### SeguCor — la única que vende

`companyId` `11111111-1111-1111-1111-111111111111`

```
http://localhost:5173/?c=cHZ2RGV2SXYxNkJ5dGVzIf_MeCvAuLi5PWH1u_DogZFZNq3_lkSFD3goOIzw6t738lyP2Gz2PiifbqNfJ4NT5A
```

Es la compañía del seed: tiene apariencia, productos y precios cargados. El hash es
**permanente** (id fijo + encriptación determinística), así que sobrevive a un reseteo
de la base.

### Seguros Rivadavia — inquilino sin catálogo

`companyId` `b5049c79-54a3-4971-bf82-21dfc4d52700`

```
http://localhost:5173/?c=cHZ2RGV2SXYxNkJ5dGVzIRe7XIv9E-SdzsecMHGIUGc5hu7DU82BqKZOA-wqeuroc-ShS5oatNmWnKp1ad1E9w
```

### VehiSegur — inquilino sin catálogo

`companyId` `ad37f2ad-ccb6-45d2-b818-d2c060be2843`

```
http://localhost:5173/?c=cHZ2RGV2SXYxNkJ5dGVzIakop4lRQo6fb22eDVISlhuBs0EjaGDSohUSrR8ML671uwANuv2uzWboagbn50gh1A
```

Rivadavia y VehiSegur se crearon desde el panel. Al darlas de alta se les sembró la
apariencia por defecto, pero **no** productos ni precios: sus portales cargan y
aplican su tema, y al llegar a coberturas avisan que no hay cobertura disponible.
Eso es correcto — un inquilino nuevo nace con la cara puesta y sin catálogo.

---

## 4. Patentes y qué valida cada una

| Patente | Vehículo | Valida |
|---|---|---|
| `MDJ345` | Auto 2020 | Compra completa |
| `LRP782` | Auto 2022 | Compra completa |
| `KQB910` | Auto 2018 | Compra completa |
| `MNO456` | Moto 2019 | Compra completa · **precio por tipo** (moto sale distinto que auto) |
| `TJK220` | Camioneta 2021 | Compra completa · precio por tipo |
| `VTR550` | Utilitario 2020 | Compra completa · precio por tipo |
| `AA001BB` | Auto 2021 | Compra completa |
| `AC123BD` | Camioneta 2023 | **Renovación** — tiene póliza vigente del seed, abre el modal |
| `VWX985` | Auto **1985** | **Sin cobertura** — el año queda fuera del rango de las reglas (2000–2030) |
| cualquiera inventada | — | **Vehículo no encontrado** — p. ej. `KKK222` |

> **Cuidado**: comprar una patente la deja asegurada, y a partir de ahí pasa a ser un
> caso de renovación. Para devolverla a "libre", borrá su póliza (ver §7).
> `AC123BD` es la única que **debe** quedar asegurada siempre: es el caso de renovación.

Los cuatro tipos de vehículo (Auto, Moto, Camioneta, Utilitario) existen a propósito:
sirven para comprobar que el precio cambia según el tipo.

---

## 5. Documentos

| DNI | Titular | Comportamiento |
|---|---|---|
| `30111222` | Juan Perez | Precarga nombre, mail y teléfono |
| `28999888` | Maria Gomez | Precarga |
| `27111333` | Máximo Agustín Rodríguez | Precarga |
| cualquier otro | — | Formulario vacío, se carga a mano y queda guardado |

El DNI se pide con mínimo 7 dígitos.

---

## 6. Productos y precios de SeguCor

Dos productos, con precio por tipo de vehículo y años 2000–2030:

| Producto | Auto | Moto | Camioneta | Utilitario |
|---|---|---|---|---|
| SOAT Básico | 15.000 | 8.000 | 22.000 | 18.000 |
| SOAT Full | 28.000 | 14.000 | 40.000 | 33.000 |

El portal marca la opción **más cara** como recomendada.

---

## 7. Tiempos de abandono

Un lead pasa a "abandonado" cuando deja de haber actividad. Los valores de desarrollo
están en `pvv-bff/PvvBff.API/appsettings.Development.json` y son cortos a propósito,
para poder ver el ciclo mientras se prueba:

| | Desarrollo | Producción |
|---|---|---|
| Lead sin actividad | **1 min** | 30 min |
| Checkout abierto sin pagar | **30 min** | 30 min |
| Frecuencia del barrido | **30 s** | 5 min |

Si el visitante vuelve y hace algo, el lead **deja de estar abandonado**: el abandono
es una inferencia por silencio, y un evento nuevo la desmiente.

Consecuencia práctica al probar: si te frenás más de un minuto en medio de una compra,
vas a ver el lead pasar a abandonado y volver a activo. Es el comportamiento correcto.

---

## 8. Casos borde

Los caminos que no son la compra feliz. Cada uno existe porque en algún momento se
comportó mal, y son los que conviene volver a recorrer después de tocar el flujo.

### El vehículo no se puede cotizar

Hay **dos maneras distintas** de llegar al mismo callejón, y el comprador ve el mismo
mensaje en las dos: *"No tenemos una cobertura para este vehículo"*, sin botones.

| Cómo llegar | Qué pasa por detrás |
|---|---|
| Patente `VWX985` en el portal de SeguCor | El auto es de 1985 y las reglas cubren 2000–2030. `QUOTE` responde **200 con lista vacía**: el vehículo existe, esta compañía no tiene precio para él |
| Cualquier patente en el portal de Rivadavia o VehiSegur | No tienen productos cargados. `QUOTE` responde **404** |

El vehículo del primer caso no viene del seed: se insertó a mano para poder forzarlo
(§9 tiene el comando). Sin él no hay forma de provocar la lista vacía, porque todos los
vehículos sembrados caen dentro del rango de años configurado.

Antes esto dejaba la pantalla **en blanco**, sin mensaje ni salida, y el 404 mostraba
"intentá de nuevo en unos segundos" — un consejo que nunca podía funcionar. Vale la
pena verificar que ninguno de los dos vuelva a ese estado.

### Patente inexistente

Escribir cualquier patente inventada, por ejemplo `KKK222`. `PLATE_SEARCH` responde 404
y el portal avisa *"No encontramos ese vehículo"*. Deja lead en **paso 1** con un
`wizard_error`: el intento se registra aunque no haya vehículo.

### Renovación

`AC123BD` tiene póliza vigente del seed. Abre el modal de renovación, y si se confirma,
la póliza nueva arranca **cuando termina la vigente** (fecha futura), con número nuevo.
No devuelve la póliza vieja: quien paga una renovación tiene que recibir una póliza real.

### Pago rechazado

Llegar al checkout simulado y apretar **Rechazar**. El paso 4 queda en `rejected` y el
lead sigue **activo**, no abandonado: la persona puede reintentar el pago.

### Checkout abandonado

Llegar al checkout simulado y cerrar la pestaña sin responder. Pasados los minutos de
`Payments:AbandonmentTtlMinutes`, la transacción queda `Abandoned` y el lead abandonado
en el paso 4. Es el caso que alimenta el recupero por email.

### Un lead abandonado que vuelve

Dejar una compra quieta más de un minuto (queda abandonada) y después continuarla. El
lead vuelve a **activo** y se le borra la fecha de abandono. El abandono es una
inferencia por silencio; un evento nuevo la desmiente.

### Lead sin forma de contacto

En **Paso 1 / Abandonaron** el botón **Recuperar** no aparece: esos leads se fueron
antes de dejar un mail. Sí aparece de Paso 2 en adelante.

### Aislamiento entre compañías

Un operador solo alcanza lo suyo, y no puede ampliarlo desde la URL. El superadmin no
alcanza los datos de ningún inquilino. Comprobado en la API:

```
operador → sus leads / su config                200
operador → config de otra compañía              403
operador → listado de compañías                 403
superadmin → compañías y operadores             200
superadmin → leads, embudo, config de inquilino 403
```

La prueba más completa es de punta a punta: cargarle productos a VehiSegur, comprar en
**su** portal, y confirmar que ese lead aparece en su dashboard y **no** en el de SeguCor.

### Reglas de precio huérfanas

Borrar un producto que tenía precios cargados. Sus reglas siguen guardadas y aparecen
agrupadas bajo **"Sin producto asociado"**, en vez de desaparecer de la pantalla
mientras se siguen guardando.

### Emisión contada una sola vez

La pantalla de resultado consulta el estado en bucle, y en desarrollo React monta el
efecto dos veces, así que hay dos consultas en paralelo. La emisión tiene que quedar
registrada **una sola vez** en el historial del lead. Se verifica mirando que
`policy_issued` aparezca una vez sola en `event_logs`.

### Una compra deja un lead, no tres

Una compra son tres cargas de página — wizard, checkout, resultado. Solo la primera
cuenta como visita. Después de una compra completa, el total de leads sube en **uno**.

---

## 9. Comandos útiles

**Ver los últimos leads**

```bash
docker exec pvv-mongodb mongosh -u pvv_user -p pvv_pass --authenticationDatabase admin --quiet \
  --eval 'db.getSiblingDB("pvv_bff_db").leads.find().sort({CreatedAt:-1}).limit(3).pretty()'
```

**Borrar todos los leads y arrancar limpio**

```bash
docker exec pvv-mongodb mongosh -u pvv_user -p pvv_pass --authenticationDatabase admin --quiet \
  --eval 'const d=db.getSiblingDB("pvv_bff_db"); d.leads.deleteMany({}); d.event_logs.deleteMany({}); d.payment_transactions.deleteMany({});'
```

**Liberar una patente que quedó asegurada** (reemplazar la patente)

```bash
MSYS_NO_PATHCONV=1 docker exec pvv-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'PvvLocal123!' -C -Q \
  "DELETE p FROM pvv_soat_db.dbo.Policies p JOIN pvv_soat_db.dbo.Vehicles v ON v.VehicleId=p.VehicleId WHERE v.Plate='MDJ345'"
```

**Registrar un vehículo nuevo** (por ejemplo, para forzar otro caso sin cobertura)

```bash
curl -X POST http://localhost:5001/api/vehicles -H 'Content-Type: application/json' \
  -d '{"plate":"VWX985","brand":"Renault","model":"12 Break","year":1985,"vehicleType":"Car"}'
```

Tipos válidos: `Car`, `Motorcycle`, `Truck`, `Van`.

---

## 10. Notas

- El pago tiene **dos modos**, y los elige `Payments:Gateway` en la configuración del
  BFF. Con `Mock` se usa un checkout simulado propio (`/mock-checkout`), cuyos botones
  Aprobar y Rechazar llaman al mismo webhook que usaría el proveedor: sirve para
  demostrar la compra sin conexión ni credenciales. Con `MercadoPago` se va al Checkout
  Pro real — la receta completa está en **§11**.
- Turnstile corre con la clave de prueba de Cloudflare, que acepta cualquier token.
- Borrar una compañía se lleva en cascada su configuración y sus operadores, pero
  **no sus leads**: viven en MongoDB, en otro servicio, sin borrado en cascada entre
  bases. Es una limitación conocida.

---

## 11. Probar con Mercado Pago real

Todo lo de acá es con **credenciales de prueba**: no se mueve plata real en ningún
momento. Aun así, el armado tiene varias piezas que se enredan entre sí, y una trampa
que cuesta horas si no se sabe de antemano.

### 11.1 Credenciales

Van en **`dotnet user-secrets`**, nunca en `appsettings.Development.json`, que **sí**
está versionado. El proyecto ya tiene su `UserSecretsId`.

```bash
cd pvv-bff/PvvBff.API
dotnet user-secrets set "MercadoPago:AccessToken"   "<token del vendedor de prueba>"
dotnet user-secrets set "MercadoPago:WebhookSecret" "<clave secreta del webhook>"
```

### 11.2 Las cuentas: dos partes distintas, las dos de prueba

Es la regla que explica casi todos los errores. En
**[Tus integraciones](https://www.mercadopago.com.ar/developers/panel/app)** → tu app →
**Cuentas de prueba**, crear una **Vendedor** y una **Comprador** (las dos de
Argentina). El `AccessToken` sale del vendedor de prueba; con el comprador se paga.

Para saber de quién es un token que ya tenés:

```bash
curl -s -H "Authorization: Bearer $TOKEN" https://api.mercadopago.com/users/me
```

Un usuario de prueba responde con `tags: ["test_user"]` y un nickname `TESTUSER…`.
**El prefijo no sirve para distinguirlos**: un usuario de prueba también tiene
credenciales `APP_USR-`.

Al pagar, **ventana de incógnito** y logueado con la cuenta compradora. Con tu sesión
normal de Mercado Pago te estarías comprando a vos mismo y lo rechaza.

### 11.3 ⚠️ La trampa: sandbox rompe los pagos con tarjeta

**`UseSandbox` tiene que estar en `false`.** Con credenciales de un usuario de prueba,
el checkout correcto es el **`init_point` común**, no el `sandbox_init_point`.

Mandar un cobrador de prueba al host de sandbox falla con *"una de las partes con la
que intentás hacer el pago es de prueba"* **solo al pagar con tarjeta**. Con dinero en
cuenta funciona igual, porque ese pago no sale de Mercado Pago, y esa asimetría es
exactamente lo que despista: parece un problema de la tarjeta o del comprador, y es de
la URL del checkout.

Que el checkout abra en `www.mercadopago.com.ar` y no en `sandbox.` es lo esperado y
correcto: no hay plata real porque el cobrador es un usuario de prueba.

### 11.4 Los dos túneles

Mercado Pago no acepta un `back_url` en localhost y tampoco puede alcanzar tu máquina
para el webhook, así que hacen falta **dos túneles de cloudflared**:

```bash
cloudflared tunnel --url http://localhost:5173   # portal
cloudflared tunnel --url http://localhost:5003   # BFF (webhook)
```

Los nombres son **distintos en cada arranque**, y hay cuatro valores que actualizar
juntos:

| Dónde | Qué | Valor |
|---|---|---|
| `pvv-front/.env` | `VITE_BFF_BASE_URL` | túnel del **BFF** |
| user-secrets | `MercadoPago:BackUrlBase` | túnel del **front** |
| user-secrets | `Cors:AllowedOrigins:2` | túnel del **front** |
| user-secrets | `MercadoPago:NotificationUrl` | túnel del **BFF** + `/api/payments/mp/webhook` |

`VITE_BFF_BASE_URL` apunta al túnel y no a `http://localhost` porque el portal queda
servido por HTTPS, y una página HTTPS llamando a HTTP es contenido mixto: el navegador
lo bloquea.

**Entrar al portal por la URL del túnel, nunca por `localhost:5173`.** `localStorage`
es por origen, y Mercado Pago devuelve al comprador al host del túnel: si arrancás en
localhost, al volver caés en otro origen con la sesión vacía.

### 11.5 Tarjetas de prueba

| Tarjeta | Número | CVV | Vence |
|---|---|---|---|
| Visa crédito | `4509 9535 6623 3704` | 123 | 11/30 |
| Mastercard crédito | `5031 7557 3453 0604` | 123 | 11/30 |
| Visa débito | `4002 7686 9439 5619` | 123 | 11/30 |

Lo que decide el resultado **no es el número, es el nombre del titular**: `APRO`
aprueba · `OTHE` error genérico · `FUND` sin fondos · `CONT` deja el pago pendiente ·
`SECU` código inválido · `EXPI` vencida. Documento: **DNI `12345678`**.

Un usuario de prueba **no puede pagar con una tarjeta real** — da el mismo mensaje de
"una de las partes es de prueba". La cuenta compradora también tiene saldo ficticio,
así que se puede pagar con **Dinero en cuenta** y saltear la tarjeta.

> Mercado Pago rota estos valores. Si algo no coincide, la fuente es
> [su documentación de tarjetas de prueba](https://www.mercadopago.com.ar/developers/es/docs/checkout-pro/additional-content/your-integrations/test/cards).

### 11.6 Los cuatro finales que conviene recorrer

| Caso | Cómo | Qué tiene que pasar |
|---|---|---|
| Compra feliz | Titular `APRO`, o Dinero en cuenta | Póliza emitida y ticket en pantalla |
| Pago rechazado | Titular `OTHE` | "Hubo un problema", el lead sigue **activo** |
| Volver sin pagar | Llegar al checkout y volver al sitio | ~12 s y después **"No se completó el pago"**. Antes se quedaba colgado en "Emitiendo tu póliza…" para siempre |
| Pago pendiente | Titular `CONT`, o un cupón | "Falta que pagues tu cupón", con fecha límite |

Comprar deja la patente asegurada, así que la siguiente compra sobre esa patente pasa a
ser una **renovación** y la póliza nueva arranca cuando termina la vigente. Para
recorrer varios casos seguidos, usá una patente distinta cada vez (§4) o liberá la
anterior (§9).

### 11.7 Verificar contra la base

```bash
docker exec pvv-mongodb mongosh -u pvv_user -p pvv_pass --authenticationDatabase admin --quiet \
  --eval 'db.getSiblingDB("pvv_bff_db").payment_transactions.find({},{Status:1,ProviderPaymentId:1,EmissionStatus:1,PolicyNumber:1}).sort({CreatedAt:-1}).limit(3).toArray()'
```

`Status`: 0 Pending · 1 Confirmed · 2 Failed · 3 Abandoned. Una compra exitosa tiene
`Status: 1`, un `ProviderPaymentId` con el id real del pago en Mercado Pago,
`EmissionStatus: "success"` y su número de póliza.

---

## 12. Probar el recupero de leads por email

### 12.1 Configuración

Las credenciales SMTP van en **user-secrets**, nunca en el appsettings versionado:

```bash
cd pvv-bff/PvvBff.API
dotnet user-secrets set "Email:Username"    "<login SMTP>"
dotnet user-secrets set "Email:Password"    "<clave SMTP>"
dotnet user-secrets set "Email:FromAddress" "<remitente verificado>"
```

El host, el puerto y el proveedor están en `appsettings.Development.json`
(`Email:Provider = Smtp`, `smtp-relay.brevo.com`, puerto 587, STARTTLS). Con
`Email:Provider = Mock` no se manda nada: el mensaje se registra en el log y alcanza
para recorrer el flujo sin credenciales.

⚠️ **`Email:PortalBaseUrl`** tiene que ser una URL alcanzable desde afuera — es el link
del botón del correo. Con el valor por defecto (`localhost:5173`) quien reciba el mail
aterriza en su propia computadora. Para una prueba real, apuntalo al túnel del front:

```bash
dotnet user-secrets set "Email:PortalBaseUrl" "https://<tunel>.trycloudflare.com"
```

**El remitente tiene que estar verificado con el proveedor.** En Brevo se verifica una
dirección suelta (no hace falta un dominio) en https://app.brevo.com/senders/list.

> Mandar *desde* una dirección `@gmail.com` a través de Brevo suele caer en **spam**,
> porque el SPF/DKIM del proveedor no alinea con `gmail.com`. Para la demo alcanza con
> mirar la carpeta de spam; la solución real es un dominio propio, fuera de alcance.

⚠️ **Si el envío falla con `525 5.7.1 Unauthorized IP address`**, no es el código: Brevo
bloquea las IPs desconocidas para las claves SMTP, y viene **activado por defecto** en
las cuentas creadas después de mayo de 2024. Se arregla en
https://app.brevo.com/security/authorised_ips, agregando la IP pública o —mejor en una
conexión hogareña, donde la IP cambia— desactivando la restricción. No hace falta
reiniciar nada: el cliente SMTP se conecta de cero en cada envío.

Este error aparece **al final**, después de que todo lo demás funcionó, así que es fácil
leerlo como si el problema fuera otro. El log del BFF lo dice con todas las letras.

### 12.2 Preparar un lead recuperable

Hace falta un lead **abandonado y con correo**, o sea que llegó al paso 2. Empezá una
compra en el portal, cargá patente y DNI (los datos de contacto se guardan ahí), y
abandonala. En desarrollo un lead se marca abandonado al minuto de silencio (§7).

### 12.3 Enviar

En el panel: **Leads** → tab del paso donde quedó → **Abandonaron** → botón
**Recuperar** → *Enviar correo*.

| Qué esperar | Cuándo |
|---|---|
| Se envía y la fila pasa a decir **Contactado** | Camino feliz |
| El botón no aparece | El lead no dejó correo, o ya fue contactado |
| "A este lead ya se le envió el correo" | Se forzó un segundo envío |
| "No pudimos enviar el correo" | El proveedor rechazó — mirar el log del BFF |

El texto sale de **Recupero** en el panel del operador. Con los campos vacíos se usan
los textos por defecto del sistema, que son los que se ven en gris.

### 12.4 Verificar contra la base

```bash
docker exec pvv-mongodb mongosh -u pvv_user -p pvv_pass --authenticationDatabase admin --quiet \
  --eval 'db.getSiblingDB("pvv_bff_db").leads.find({RecoveredAt:{$ne:null}},{ContactEmail:1,RecoveredAt:1,RecoveredBy:1}).toArray()'
```

Para poder reenviarle a un lead que ya fue contactado (útil al probar), borrale la marca:

```bash
docker exec pvv-mongodb mongosh -u pvv_user -p pvv_pass --authenticationDatabase admin --quiet \
  --eval 'db.getSiblingDB("pvv_bff_db").leads.updateMany({},{$set:{RecoveredAt:null,RecoveredBy:null}})'
```
