# Manual de instalación y puesta en marcha de SITRAC-ANCI

Este manual permite instalar el sistema completo en un equipo propio: base de datos, API, frontend, usuario administrador, usuarios adicionales y prueba del webhook.

La forma recomendada es **Docker Compose**, porque levanta SQL Server y la API con la configuración ya preparada. La alternativa es ejecutar la API con .NET contra un SQL Server local.

---

## 1. Qué se va a levantar

| Componente | Puerto | URL | Función |
|---|---:|---|---|
| API SITRAC | 5117 | `http://127.0.0.1:5117` | REST API y Swagger |
| Swagger | 5117 | `http://127.0.0.1:5117/swagger` | Explorador de endpoints, solo en Development |
| SQL Server | 1433 | `localhost,1433` | Base de datos `DbSitrac` |
| Frontend | 5500 | `http://127.0.0.1:5500/login.html` | Consola web de login e incidentes |

El puerto anterior quedó en desuso. Use siempre `5117` para la API.

---

## 2. Requisitos previos

### Opción recomendada: Docker Compose

- Docker Desktop 4.x o superior, con Compose v2 inclusivo.
- Comando disponible: `docker compose version`.
- Puertos libres: `1433`, `5117`, `5500`.
- No se necesita .NET SDK ni SQL Server instalados en el host.
- No se necesitan credenciales reales de INVGATE para la demo local.

### Opción alternativa: .NET local

- .NET SDK 10.0.400 o superior dentro de la línea 10.0 (`global.json` fija `10.0.400` con `rollForward: latestFeature`).
- SQL Server 2022/Express local o un contenedor SQL Server accesible en `localhost,1433`.
- SSMS, Azure Data Studio o `sqlcmd` para revisar la base.
- Un servidor estático para el frontend: `npx serve` o VS Code Live Server.
- Los mismos puertos libres indicados arriba.

---

## 3. Descargar o copiar el proyecto

Ubíquese en la raíz del repositorio. Debe ver estos archivos y carpetas:

```text
Sitrac.sln
.env.example
docker-compose.yml
Sitrac.dockerfile
global.json
Sitrac.Servidor/
Sitrac.Servicio/
Sitrac.Repositorio/
Sitrac.AccesoDatos/
Sitrac.MigracionBd/
Sitrac.Frontend/
README.md
ARCHITECTURE.md
TRANSACCIONES.md
```

Todos los comandos de este manual se ejecutan desde esa raíz.

---

## 4. Instalación con Docker Compose

### 4.1 Levantar los servicios

```bash
cp .env.example .env
# Completar secretos y URLs de UltraPort en .env
docker compose up --build -d
```

La primera ejecución puede demorar porque descarga imágenes, restaura paquetes y compila la API.

### 4.2 Revisar el estado

```bash
docker compose ps
```

Debe ver dos servicios activos o en proceso de arranque:

```text
sitrac_api
sitrac_sqlserver
```

### 4.3 Seguir los logs de la API

```bash
docker compose logs -f sitrac-api
```

Espere hasta ver mensajes de arranque de ASP.NET Core y las migraciones Evolve. No cierre los logs hasta confirmar que la API quedó escuchando.

### 4.4 Verificar que la API responde

Abra en el navegador:

```text
http://127.0.0.1:5117/swagger/index.html
```

También puede probar:

```bash
curl http://127.0.0.1:5117/swagger/index.html
```

### 4.5 Verificar SQL Server desde el host

En SSMS o Azure Data Studio:

```text
Server: localhost,1433
Authentication: SQL Server Authentication
User: sa
Password: <MSSQL_SA_PASSWORD>
Encrypt: opcional / Trust server certificate: sí
```

La base `DbSitrac` se crea automáticamente si no existe.

---

## 5. Instalación con .NET local

Use esta sección solo si no quiere ejecutar la API en Docker.

### 5.1 Verificar el SDK

```bash
dotnet --version
```

Debe mostrar `10.0.400` o una versión superior compatible dentro de .NET 10.

### 5.2 Tener SQL Server disponible

Si quiere usar solo los contenedores de infraestructura y la API en el host:

```bash
docker compose up -d sqlserver
```

### 5.3 Configurar la cadena de conexión

El valor por defecto en `Sitrac.Servidor/appsettings.json` es:

```text
Server=localhost\SQLEXPRESS;Database=DbSitrac;Trusted_Connection=True;TrustServerCertificate=True;
```

Si su SQL Server es el contenedor de Docker, use:

```text
Server=localhost,1433;Database=DbSitrac;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;
```

La forma recomendada de configurarlo es con user-secrets, porque el perfil `http` ejecuta la API en entorno `Development`.

En Windows PowerShell:

```powershell
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "ConnectionStrings:SitracConnection" "Server=localhost,1433;Database=DbSitrac;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"
```

En macOS o Linux:

```bash
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'ConnectionStrings:SitracConnection' 'Server=localhost,1433;Database=DbSitrac;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;'
```

También puede usar una variable de entorno temporal.

PowerShell:

```powershell
$env:ConnectionStrings__SitracConnection = 'Server=localhost,1433;Database=DbSitrac;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;'
```

Bash:

```bash
export ConnectionStrings__SitracConnection='Server=localhost,1433;Database=DbSitrac;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;'
```

### 5.4 Compilar y ejecutar

```bash
dotnet build Sitrac.sln
cd Sitrac.Servidor
dotnet run --launch-profile http
```

Ejecute desde `Sitrac.Servidor/`: `Program.cs` busca `../Sitrac.MigracionBd/Scripts` relativo al directorio de trabajo. Si ejecuta desde la raíz, las migraciones no encuentran los scripts.

La API quedará en:

```text
http://127.0.0.1:5117
http://127.0.0.1:5117/swagger
```

Si prefiere HTTPS, use el perfil `https`; también expone el puerto `5117` además de `7265`.

Antes de iniciar la API configure `JwtSettings:SecretKey` y `WebhookApiKey` con user-secrets o variables de entorno. `JwtSettings:SecretKey` es obligatorio: si está vacío, la API falla en el arranque. El webhook responde `401` si `WebhookApiKey` está vacío o no coincide con el header `X-Api-Key`.

### 5.5 Configurar correo con Microsoft Graph

El correo se envía solo por Microsoft Graph App-Only (`POST /users/{RemitenteUpn}/sendMail`). No hay SMTP ni capturador local de correos.

Si ejecuta la API fuera de Docker, cargue `GraphConfig` con los user-secrets del proyecto (`UserSecretsId = sitrac-anci-secrets`). Reemplace los marcadores por valores reales de su tenant; no los versione:

Windows PowerShell:

```powershell
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:TenantId" "<TENANT_ID>"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:ClientId" "<CLIENT_ID>"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:ClientSecret" "<CLIENT_SECRET>"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:RemitenteUpn" "<REMITENTE_UPN>"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:DestinatarioCorreo" "<DESTINATARIO_CORREO>"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:GuardarEnEnviados" "true"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:TimeoutMs" "15000"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:MaxReintentos" "2"
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "GraphConfig:IntervaloEntreEnviosMs" "1200"
```

Bash:

```bash
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:TenantId' '<TENANT_ID>'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:ClientId' '<CLIENT_ID>'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:ClientSecret' '<CLIENT_SECRET>'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:RemitenteUpn' '<REMITENTE_UPN>'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:DestinatarioCorreo' '<DESTINATARIO_CORREO>'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:GuardarEnEnviados' 'true'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:TimeoutMs' '15000'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:MaxReintentos' '2'
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj 'GraphConfig:IntervaloEntreEnviosMs' '1200'
```

En Docker, complete esas mismas variables en `.env`: `GraphConfig__TenantId`, `GraphConfig__ClientId`, `GraphConfig__ClientSecret`, `GraphConfig__RemitenteUpn`, `GraphConfig__DestinatarioCorreo`, `GraphConfig__GuardarEnEnviados`, `GraphConfig__TimeoutMs`, `GraphConfig__MaxReintentos` y `GraphConfig__IntervaloEntreEnviosMs`. `GraphConfig__DestinatarioCorreo` es opcional: si está vacío, se consultan los correos de `TbSitracResponsable` con `IndEsDelegadoCiberseguridad = 1` y `AuditNotDeleted = 1`. No use valores de ejemplo como `contoso.com`, `your_tenant` ni `XXX`: `ConfiguracionGraph.EstaConfigurado()` los rechaza.

Si faltan `TenantId`, `ClientId`, `ClientSecret` o `RemitenteUpn`, la API arranca normalmente pero cada envío de correo termina en `ResultadoEnvio.Fallo` con un mensaje en los logs. Si no hay destinatario configurado ni delegados activos, el envío también falla de forma controlada. Microsoft Teams queda deshabilitado por defecto.

---

## 6. Configuración y verificación de la base de datos

### 6.1 Qué hace la API al iniciar

La API ejecuta automáticamente las migraciones Evolve `V0001` a `V0011`:

- crea la base `DbSitrac` si no existe;
- crea catálogos, incidentes, hitos, SLA, informes, historial, escalamiento, outbox y usuarios;
- agrega a `TbSitracNotificacionLog` las columnas `DestinatarioCorreo`, `ReferenciaTicket`, `IdMensajeProveedor` e `IdempotenciaKey`, con índice único filtrado;
- elimina automáticamente el usuario semilla inválido de `V0007` (`admin`, `IdUsuario = 1`) si conserva el hash placeholder o el hash original sembrado (V0011);
- crea módulos, transacciones, perfiles y roles;
- migra usuarios antiguos con `Rol = 'Admin'` al rol `Administrador SITRAC`.

No edite migraciones ya aplicadas. Para cambios posteriores, agregue una nueva versión.

### 6.2 Verificar tablas

En SSMS o Azure Data Studio, con `DbSitrac` seleccionada:

```sql
SELECT name
FROM sys.tables
WHERE name LIKE 'TbSitrac%'
ORDER BY name;
```

Debe ver, entre otras:

```text
TbSitracUsuario
TbSitracModulo
TbSitracTransaccion
TbSitracPerfil
TbSitracPerfilTransaccion
TbSitracRol
TbSitracRolPerfil
TbSitracUsuarioRol
TbSitracIncidente
TbSitracHito
TbSitracEventoHistorial
TbSitracNotificacionLog
```

### 6.3 Verificar el seed de seguridad

```sql
SELECT r.NombreRol, COUNT(DISTINCT pt.IdTransaccion) AS Transacciones
FROM TbSitracRol r
JOIN TbSitracRolPerfil rp
  ON rp.IdRol = r.IdRol AND rp.AuditNotDeleted = 1
JOIN TbSitracPerfilTransaccion pt
  ON pt.IdPerfil = rp.IdPerfil AND pt.AuditNotDeleted = 1
WHERE r.AuditNotDeleted = 1
GROUP BY r.NombreRol
ORDER BY r.NombreRol;
```

Resultado esperado:

| Rol | Transacciones |
|---|---:|
| `Administrador SITRAC` | 9 |
| `Analista de Ciberseguridad` | 7 |
| `Auditor de Incidentes` | 4 |
| `Cuenta de Servicio INVGATE` | 1 |

### 6.4 Usuario semilla inválido

La migración `V0007` inserta un usuario `admin` con un hash que no sirve para el login PBKDF2 real. La migración `V0011` elimina sus asignaciones de rol y el usuario `IdUsuario = 1` automáticamente si conserva el hash placeholder o el hash original sembrado. No elimine este registro a mano. En bases ya migradas, verifique que ya no exista antes de ejecutar el bootstrap:

```sql
USE DbSitrac;
GO

SELECT IdUsuario, NombreUsuario, PasswordHash
FROM TbSitracUsuario
WHERE IdUsuario = 1 AND NombreUsuario = 'admin';
GO
```

No ejecute sentencias destructivas en una base con datos reales.

---

## 7. Crear el usuario administrador inicial

El sistema incluye un endpoint de bootstrap de un solo uso. Crea un usuario con contraseña válida PBKDF2 y le asigna el rol `Administrador SITRAC`, que tiene las 9 transacciones.

### 7.1 Ejecutar el bootstrap

Los ejemplos de usuario y contraseña de este manual son solo para entorno local; en UltraPort reemplace `admin.demo` / `Demo2026!Strong` por credenciales definidas por TI.

El endpoint responde `403` si `Seguridad:HabilitarBootstrap = false`. Habilítelo solo para la creación inicial del administrador y desactívelo después.

Bash:

```bash
curl -X POST http://127.0.0.1:5117/api/Auth/bootstrap \
  -H "Content-Type: application/json" \
  -d '{"username":"admin.demo","password":"Demo2026!Strong","nombreCompleto":"Administrador Demo"}'
```

PowerShell:

```powershell
Invoke-RestMethod -Method Post `
  -Uri 'http://127.0.0.1:5117/api/Auth/bootstrap' `
  -ContentType 'application/json' `
  -Body '{"username":"admin.demo","password":"Demo2026!Strong","nombreCompleto":"Administrador Demo"}'
```

Respuesta esperada:

```json
{"mensaje":"Administrador inicial creado."}
```

Si responde `409`, ya existe un usuario con rol `Administrador SITRAC`. Use esas credenciales o reinicie el entorno local desde cero.

Después del bootstrap, bloquee el endpoint. En Docker edite `.env`:

```text
Seguridad__HabilitarBootstrap=false
```

Luego reinicie la API:

```bash
docker compose up -d --force-recreate sitrac-api
```

Si ejecuta fuera de Docker:

```bash
dotnet user-secrets set --project Sitrac.Servidor/Sitrac.Servidor.csproj "Seguridad:HabilitarBootstrap" "false"
```

Con el flag en `false`, una nueva llamada a `/api/Auth/bootstrap` responde `403`.

### 7.2 Iniciar sesión contra la API

Bash:

```bash
curl -X POST http://127.0.0.1:5117/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin.demo","password":"Demo2026!Strong"}'
```

PowerShell:

```powershell
$login = Invoke-RestMethod -Method Post `
  -Uri 'http://127.0.0.1:5117/api/Auth/login' `
  -ContentType 'application/json' `
  -Body '{"username":"admin.demo","password":"Demo2026!Strong"}'
$login
```

La respuesta contiene `token`, `nombreCompleto` y `rol`.

### 7.3 Probar un endpoint protegido

Bash, con `jq` instalado:

```bash
TOKEN=$(curl -s -X POST http://127.0.0.1:5117/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin.demo","password":"Demo2026!Strong"}' | jq -r .token)

curl http://127.0.0.1:5117/api/Incidentes \
  -H "Authorization: Bearer $TOKEN"
```

PowerShell:

```powershell
$login = Invoke-RestMethod -Method Post `
  -Uri 'http://127.0.0.1:5117/api/Auth/login' `
  -ContentType 'application/json' `
  -Body '{"username":"admin.demo","password":"Demo2026!Strong"}'

$headers = @{ Authorization = "Bearer $($login.token)" }
Invoke-RestMethod -Uri 'http://127.0.0.1:5117/api/Incidentes' -Headers $headers
```

Resultado esperado: `200 OK` con una lista, vacía al principio.

---

## 8. Crear usuarios adicionales

### 8.1 Registrar el usuario con la API

Use el token del administrador. El endpoint exige la transacción `SITR_AUTE_REGISTRAR_USUARIO`. Envíe `nombreRol` para que el backend valide que el rol exista y esté activo y lo asigne automáticamente; así no necesita ejecutar `INSERT` SQL manual. Los valores `analista.demo` / `Analista2026!Strong` son ejemplos solo locales: reemplácelos por las credenciales corporativas. Roles vigentes: `Administrador SITRAC`, `Analista de Ciberseguridad`, `Auditor de Incidentes` y `Cuenta de Servicio INVGATE`.

Bash:

```bash
curl -X POST http://127.0.0.1:5117/api/Auth/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"username":"analista.demo","password":"Analista2026!Strong","nombreCompleto":"Analista Demo","nombreRol":"Analista de Ciberseguridad"}'
```

PowerShell:

```powershell
Invoke-RestMethod -Method Post `
  -Uri 'http://127.0.0.1:5117/api/Auth/register' `
  -ContentType 'application/json' `
  -Headers @{ Authorization = "Bearer $($login.token)" } `
  -Body '{"username":"analista.demo","password":"Analista2026!Strong","nombreCompleto":"Analista Demo","nombreRol":"Analista de Ciberseguridad"}'
```

La respuesta incluye `RolAsignado`. Si `nombreRol` no existe o no está activo, la API responde `400`.

### 8.2 Asignar un rol de seguridad manualmente

Si se omite `nombreRol`, `/api/Auth/register` crea el usuario con `Rol = "Admin"` legacy pero **no le asigna un rol de seguridad**. El usuario podrá hacer login, pero recibirá `403` en los endpoints protegidos hasta que se asigne el rol y vuelva a iniciar sesión.

Ejecute en SQL solo cuando `nombreRol` no haya sido informado:

```sql
USE DbSitrac;
GO

DECLARE @Usuario VARCHAR(50) = 'analista.demo';
DECLARE @Rol VARCHAR(50) = 'Analista de Ciberseguridad';

INSERT INTO TbSitracUsuarioRol (IdUsuario, IdRol)
SELECT u.IdUsuario, r.IdRol
FROM TbSitracUsuario u
CROSS JOIN TbSitracRol r
WHERE u.NombreUsuario = @Usuario
  AND r.NombreRol = @Rol
  AND u.AuditNotDeleted = 1
  AND r.AuditNotDeleted = 1
  AND NOT EXISTS (
      SELECT 1
      FROM TbSitracUsuarioRol ur
      WHERE ur.IdUsuario = u.IdUsuario
        AND ur.IdRol = r.IdRol
  );
GO
```

Roles disponibles:

| Rol | Permisos |
|---|---|
| `Administrador SITRAC` | Acceso total, 9 transacciones |
| `Analista de Ciberseguridad` | Acceder, consultar y gestionar el ciclo, 7 transacciones |
| `Auditor de Incidentes` | Acceder y solo lectura, 4 transacciones |
| `Cuenta de Servicio INVGATE` | Solo ingesta mediante webhook, 1 transacción |

Para dar permisos totales, cambie `@Rol` por `Administrador SITRAC`.

### 8.3 Verificar las transacciones del usuario

```sql
USE DbSitrac;
GO

DECLARE @Usuario VARCHAR(50) = 'analista.demo';

SELECT DISTINCT t.CodigoTransaccion
FROM TbSitracUsuario u
JOIN TbSitracUsuarioRol ur
  ON ur.IdUsuario = u.IdUsuario AND ur.AuditNotDeleted = 1
JOIN TbSitracRol r
  ON r.IdRol = ur.IdRol AND r.AuditNotDeleted = 1
JOIN TbSitracRolPerfil rp
  ON rp.IdRol = r.IdRol AND rp.AuditNotDeleted = 1
JOIN TbSitracPerfil p
  ON p.IdPerfil = rp.IdPerfil AND p.AuditNotDeleted = 1
JOIN TbSitracPerfilTransaccion pt
  ON pt.IdPerfil = p.IdPerfil AND pt.AuditNotDeleted = 1
JOIN TbSitracTransaccion t
  ON t.IdTransaccion = pt.IdTransaccion AND t.AuditNotDeleted = 1
WHERE u.NombreUsuario = @Usuario
  AND u.AuditNotDeleted = 1
ORDER BY t.CodigoTransaccion;
GO
```

### 8.4 Volver a iniciar sesión

El JWT contiene las transacciones vigentes al momento del login. Después de asignar un rol, el usuario debe cerrar sesión y volver a ingresar para obtener un token actualizado.

---

## 9. Levantar el frontend

El frontend es estático y no requiere compilación.

```bash
npx serve Sitrac.Frontend -l 5500
```

También puede usar VS Code Live Server apuntando a la carpeta `Sitrac.Frontend` en el puerto `5500`.

Abra:

```text
http://127.0.0.1:5500/login.html
```

Ingrese:

```text
Usuario: admin.demo
Contraseña: Demo2026!Strong
```

Después del login verá la consola en `index.html`.

### 9.1 URL base de la API

La URL de la API está definida una sola vez en `Sitrac.Frontend/js/api.js`:

```text
http://127.0.0.1:5117/api
```

Para UltraPort, copie `Sitrac.Frontend/js/config.example.js` a `js/config.js` y ajuste la URL:

```js
window.SITRAC_API_BASE_URL = 'http://TU_HOST:5117/api';
```

`login.html` e `index.html` ya incluyen comentado el include opcional de `js/config.js` antes de los módulos; descoméntelo cuando use el archivo.

CORS se configura con `FrontendSettings:AllowedOrigins` (variables `FrontendSettings__AllowedOrigins__0`, `FrontendSettings__AllowedOrigins__1`, etc.). Cuando el arreglo está vacío, se usan como fallback `http://localhost:5500` y `http://127.0.0.1:5500`.

Servir el frontend desde un origen no permitido produce error de red o CORS, no un 401.

---

## 10. Probar el webhook de INVGATE

El webhook usa API key, no JWT.

API key local:

```text
<WEBHOOK_API_KEY>
```

Bash:

```bash
curl -X POST http://127.0.0.1:5117/api/Incidentes/invgate/webhook \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <WEBHOOK_API_KEY>" \
  -d '{"codigoTicketInvgate":"INV-DEMO-001","fechaTomaConocimiento":"2026-11-20T10:00:00Z","codTipoClasificacionEmpresa":1,"codTipoGravedad":4,"descripcionSistemaAfectado":"Servidor web DEMO"}'
```

PowerShell:

```powershell
Invoke-RestMethod -Method Post `
  -Uri 'http://127.0.0.1:5117/api/Incidentes/invgate/webhook' `
  -ContentType 'application/json' `
  -Headers @{ 'X-Api-Key' = '<WEBHOOK_API_KEY>' } `
  -Body '{"codigoTicketInvgate":"INV-DEMO-001","fechaTomaConocimiento":"2026-11-20T10:00:00Z","codTipoClasificacionEmpresa":1,"codTipoGravedad":4,"descripcionSistemaAfectado":"Servidor web DEMO"}'
```

Valores válidos:

| Campo | Valores |
|---|---|
| `codTipoClasificacionEmpresa` | `1` = Esencial, `2` = OIV |
| `codTipoGravedad` | `1` = Bajo, `2` = Medio, `3` = Alto, `4` = Crítico |

Respuesta esperada: `201 Created` con el nuevo `IdIncidente`.

Luego consulte la lista con el token del administrador y debe aparecer `INV-DEMO-001`.

---

## 11. Revisar correos enviados

Los correos salen por Microsoft Graph App-Only desde el buzón configurado en `GraphConfig:RemitenteUpn`. Revise ese buzón y el destinatario resuelto para confirmar la entrega: si `GraphConfig:DestinatarioCorreo` está vacío, se usan los delegados activos de `TbSitracResponsable`. Los incidentes críticos pueden generar notificación inmediata; el resto se consolida por el outbox en segundo plano.

Para revisar la trazabilidad de los correos:

```sql
SELECT IdNotificacion, IdHito, Canal, Estado, Intentos, FechaProgramada, FechaEnvio,
       DestinatarioCorreo, ReferenciaTicket, IdMensajeProveedor, IdempotenciaKey, MensajeError
FROM TbSitracNotificacionLog
ORDER BY IdNotificacion DESC;
```

Si no hay correos:

- confirme que `TenantId`, `ClientId`, `ClientSecret` y `RemitenteUpn` estén presentes en user-secrets o variables de entorno;
- confirme que `GraphConfig:DestinatarioCorreo` tenga un valor válido o que exista al menos un delegado de ciberseguridad activo con correo;
- confirme que la aplicación Azure tenga el permiso de aplicación `Mail.Send` con consentimiento administrativo;
- confirme que `RemitenteUpn` sea un buzón válido y con licencia;
- revise los logs de `sitrac-api`, el estado del outbox en `TbSitracNotificacionLog` y los reintentos registrados en `Intentos`/`ProximoReintentoAt`;
- recuerde que Teams está deshabilitado por defecto y sus filas pendientes se cancelan en el despachador.

---

## 12. Checklist de verificación final

| Prueba | Resultado esperado |
|---|---|
| `http://127.0.0.1:5117/swagger/index.html` | `200 OK` |
| Login con el usuario del bootstrap | `200` con token |
| `GET /api/Incidentes` con token | `200` |
| `GET /api/Incidentes` sin token | `401` |
| Login con contraseña incorrecta | `401` con `Credenciales inválidas.` |
| Bootstrap repetido | `409` |
| Bootstrap con `Seguridad__HabilitarBootstrap=false` | `403` |
| `POST /api/Auth/register` con `nombreRol` válido | `200`, respuesta con `RolAsignado` |
| `POST /api/Auth/register` con `nombreRol` inválido/inactivo | `400` |
| Webhook sin `X-Api-Key` | `401` |
| Webhook con API key válida | `201` |
| Frontend `login.html` en puerto `5500` | Login correcto y redirección a `index.html` |
| Usuario recién registrado sin rol asignado | Login `200`, endpoints protegidos `403` |
| Usuario con rol asignado y nuevo login | Endpoints protegidos según transacciones |
| Ticket crítico con Graph configurado | Correo enviado o fila `Pendiente`/error registrada en `TbSitracNotificacionLog` |
| `DestinatarioCorreo` vacío | Destinatarios resueltos desde `TbSitracResponsable` |
| Teams deshabilitado | Sin envíos Teams; filas pendientes canceladas por el despachador |

---

## 13. Solución de problemas

| Síntoma | Causa probable | Acción |
|---|---|---|
| `docker compose: command not found` | Docker Desktop o Compose v2 no instalado | Instalar Docker Desktop y verificar con `docker compose version` |
| SQL Server no pasa healthcheck | Contraseña inválida o arranque lento | Esperar hasta 60 s; verificar `MSSQL_SA_PASSWORD` en `docker-compose.yml` |
| El puerto `1433` está en uso | Otro SQL Server escuchando | Detener el otro servicio o cambiar el mapeo en `docker-compose.yml` |
| La API no arranca y muestra error de conexión | Cadena de conexión incorrecta | Revisar `ConnectionStrings__SitracConnection` o user-secrets |
| No se crea `DbSitrac` | El usuario SQL no puede crear bases | Usar `sa` en local o crear la base previamente y otorgar permisos |
| Login muestra `Credenciales inválidas.` | Usuario o contraseña incorrectos, o se está usando el seed `admin` inválido | Usar `admin.demo` creado por bootstrap |
| Bootstrap responde `409` | Ya existe `Administrador SITRAC` | Ingresar con el usuario existente o reiniciar el entorno local |
| Frontend muestra `Error HTTP: 401. Verifique sus credenciales.` | API en puerto equivocado o usuario inválido | Confirmar `127.0.0.1:5117` y credenciales del bootstrap |
| Frontend queda en blanco o falla la red | Frontend servido fuera del puerto `5500` | Servir en `5500` o agregar el origen a CORS |
| Usuario hace login pero todos los endpoints dan `403` | No tiene rol de seguridad asignado | Ejecutar el SQL de asignación de rol y volver a hacer login |
| Webhook devuelve `401` | Falta o es incorrecta la API key | Enviar `X-Api-Key: <WEBHOOK_API_KEY>` |
| No llegan correos | `GraphConfig` incompleto, permiso `Mail.Send` faltante, sin destinatarios o incidente no crítico | Revisar secretos/variables, delegados activos, permisos de la app y logs de `sitrac-api` |
| Graph devuelve `401` | `TenantId`/`ClientId` incorrectos o `ClientSecret` expirado/rotado | Verificar credenciales en secret store y renovar el secreto en Entra ID |
| Graph devuelve `403` | Falta consentimiento administrativo de `Mail.Send`, Application Access Policy restrictiva o buzón no autorizado | Aplicar Admin Consent, revisar `RemitenteUpn` y la política de acceso de la aplicación en Exchange Online |
| Graph devuelve `404` | `RemitenteUpn` no existe, no es buzón o no tiene licencia | Corregir el UPN del buzón emisor y reiniciar la API |
| Graph devuelve `429` | Throttling de Microsoft Graph | El servicio aplica backoff y `Retry-After`; revisar `Intentos` y `ProximoReintentoAt` antes de reenviar manualmente |
| Los links de correo apuntan a `/Sitrac.Frontend` | `FrontendSettings__BaseUrl` incluye ese sufijo | Cambiarlo a `http://127.0.0.1:5500` y reiniciar la API |
| `dotnet --version` no coincide | SDK incorrecto | Instalar .NET SDK 10.0.400 o compatible |
| `localhost` no conecta pero `127.0.0.1` sí | Resolución IPv6 `::1` | Usar siempre `127.0.0.1:5117` |

---

## 14. Reinicio local completo

Solo si quiere empezar desde cero y acepta perder los datos locales.

Con Docker Compose:

```bash
docker compose down -v
docker compose up --build -d
```

Con SQL Server local:

```sql
USE master;
GO

DROP DATABASE DbSitrac;
GO
```

Después de reiniciar, repita:

1. Verificación de base de datos.
2. Verificación de la limpieza del usuario semilla por V0011, si aplica.
3. Bootstrap.
4. Login.
5. Levantamiento del frontend.

---

## 15. Seguridad para entornos compartidos

Los secretos no están versionados. Cópiese `.env.example` a `.env` y complete:

- `MSSQL_SA_PASSWORD` y `SITRAC_DB_PASSWORD` (deben coincidir);
- `JwtSettings__SecretKey`;
- `WebhookApiKey`;
- `GraphConfig__*` y `TeamsConfig__*`;
- `FrontendSettings__*` y `Seguridad__HabilitarBootstrap`.

Antes de exponer el sistema:

- rotar todos los secretos y verificar que `.env` no esté versionado;
- deshabilitar `/api/Auth/bootstrap` con `Seguridad__HabilitarBootstrap=false` después del bootstrap inicial;
- usar `nombreRol` en `/api/Auth/register` o restringir el endpoint;
- usar HTTPS;
- configurar `FrontendSettings__AllowedOrigins__*` solo con orígenes reales de UltraPort;
- no commitear user-secrets ni contraseñas.

Si cambia `JwtSettings:SecretKey`, todos los tokens anteriores dejan de ser válidos; vuelva a iniciar sesión.

---

## 16. Referencias

- `TRASPASO_ULTRAPORT.md`: guía ejecutiva para TI/Ciberseguridad de UltraPort.
- `README.md`: resumen general, ejecución local y estado del proyecto.
- `ARCHITECTURE.md`: arquitectura, capas, seguridad y decisiones de diseño.
- `TRANSACCIONES.md`: catálogo completo de transacciones, credenciales y guía de verificación.
- `test.md`: matriz T01–T22 y plantillas de prueba del webhook y ciclo operativo.
