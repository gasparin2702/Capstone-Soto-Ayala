# Manual técnico — SITRAC

**Guía de instalación, configuración y despliegue**

Proyecto CAPSTONE 2026 (PTY4614) · DuocUC, sede Valparaíso · Empresa mandante: UltraPort
Versión 1.0 · Estado del sistema: MVP, Fase 2 en desarrollo

---

## 1. Propósito y alcance

Este manual explica cómo instalar, configurar, desplegar y verificar SITRAC. Está dirigido a quien deba levantar el sistema (equipo de desarrollo y, en una eventual migración, el equipo de sistemas de UltraPort). Para entender el diseño, ver `ARCHITECTURE.md`; para el modelo de datos, `Diccionario_de_Datos_SITRAC.xlsx`.

## 2. Resumen técnico

| Elemento | Detalle |
| --- | --- |
| Lenguajes | C# (.NET 10), T-SQL, HTML, CSS y JavaScript (módulos ES nativos) |
| Backend | ASP.NET Core Web API, JWT Bearer, rate limiting, CORS |
| Acceso a datos | Dapper + `Microsoft.Data.SqlClient` |
| Base de datos | SQL Server (base `DbSitrac`) |
| Migraciones | EvolveDB, scripts `V0001`–`V0007`, se ejecutan al iniciar la API |
| Notificaciones | MailKit/MimeKit (SMTP) y Microsoft Teams (Incoming Webhook) |
| Segundo plano | `BackgroundService` + `PeriodicTimer` |
| Frontend | Sitio estático (`Sitrac.Frontend`), sin bundler |
| Contenedores | Docker / Docker Compose (SQL Server 2022, smtp4dev, API) |

## 3. Requisitos

### 3.1 Para el despliegue con Docker (recomendado)

- Docker Desktop (o Docker Engine con Compose v2)
- Puertos libres: `5116` (API), `1433` (SQL Server), `5000` (smtp4dev) y `5500` (frontend)
- Node.js (opcional), solo para servir el frontend con `npx serve`

### 3.2 Para la ejecución local sin Docker

- SDK de .NET 10 (versión fijada en `global.json`: `10.0.400`, `rollForward: latestFeature`)
- SQL Server accesible (instalación local o contenedor)
- Servidor estático para el frontend en el puerto `5500` (el CORS solo permite `http://localhost:5500` y `http://127.0.0.1:5500`)

## 4. Instalación

### 4.1 Con Docker Compose

1. Clonar el repositorio y situarse en la raíz.
2. Crear el archivo de variables a partir del ejemplo:
   ```bash
   cp .env.example .env        # Windows (cmd): copy .env.example .env
   ```
3. Editar `.env` y reemplazar las claves de ejemplo (contraseña de SQL Server, `JwtSettings__SecretKey`, `WebhookApiKey`, etc.). El archivo `.env` no debe subirse al repositorio.
4. Levantar el stack:
   ```bash
   docker compose up --build
   ```
5. Servir el frontend:
   ```bash
   npx serve Sitrac.Frontend -l 5500
   ```

Servicios resultantes:

| Servicio | URL / puerto |
| --- | --- |
| API SITRAC | `http://localhost:5116` |
| SQL Server | `localhost:1433` |
| smtp4dev (captura de correos) | `http://localhost:5000` |
| Frontend (login) | `http://localhost:5500/login.html` |

Al iniciar, la API crea la base `DbSitrac` si no existe y aplica las migraciones Evolve.

### 4.2 Sin Docker (.NET local)

```bash
dotnet restore Sitrac.sln
dotnet build Sitrac.sln
dotnet run --project Sitrac.Servidor/Sitrac.Servidor.csproj --launch-profile http
```

Antes de ejecutar, configurar los valores de la sección 5 mediante `user-secrets` o variables de entorno. La API escucha en `http://localhost:5116`. Las migraciones se leen desde `../Sitrac.MigracionBd/Scripts`, relativo al proyecto servidor.

## 5. Configuración

Los valores se entregan como variables de entorno (el doble guion bajo `__` representa los niveles de `appsettings`) o mediante `user-secrets` en desarrollo. Ningún secreto debe quedar escrito en el repositorio.

| Variable | Descripción |
| --- | --- |
| `ConnectionStrings__SitracConnection` | Cadena de conexión a SQL Server |
| `JwtSettings__SecretKey` | Clave de firma de los JWT (larga y única por ambiente) |
| `JwtSettings__Issuer` / `JwtSettings__Audience` | Emisor y audiencia del token |
| `WebhookApiKey` | Clave esperada en el header `X-Api-Key` del webhook de INVGATE |
| `SmtpConfig__*` | Servidor, puerto y remitente del correo saliente |
| `TeamsConfig__Enabled` / `TeamsConfig__WebhookUrl` | Activa Teams (deshabilitado por defecto) y define el Incoming Webhook |
| `FrontendSettings__*` | Datos del frontend usados en las notificaciones |
| `ASPNETCORE_ENVIRONMENT` | `Development` habilita Swagger en `/swagger` |

Parámetros fijos en el código actual (requieren cambio para otros ambientes):

- La URL base de la API del frontend está en `Sitrac.Frontend/js/api.js` y `Sitrac.Frontend/js/login.js` (`http://localhost:5116/api`).
- Los orígenes CORS permitidos son solo `http://localhost:5500` y `http://127.0.0.1:5500`.
- Rate limiting global: 100 solicitudes por minuto por usuario o IP (responde 429 al exceder).

## 6. Despliegue

### 6.1 Construcción de la imagen

La API se empaqueta con el Dockerfile multi-stage `Sitrac.dockerfile`:

```bash
docker build -f Sitrac.dockerfile -t sitrac-api:1.0 .
```

### 6.2 Orden de arranque

1. SQL Server disponible.
2. API: al iniciar, ejecuta las migraciones `V0001`–`V0007`. No se deben editar migraciones ya aplicadas; cualquier cambio de esquema va en una nueva versión (`V0008`, …).
3. Servicios en segundo plano (se inician con la API): ciclo de vida (cada 1 minuto), despachador de notificaciones Outbox (cada 15 segundos) y escalamiento SLA (cada 1 minuto).
4. Frontend estático publicado en un servidor web.

### 6.3 Lista de verificación antes de pasar a un ambiente productivo

El MVP funciona en ambiente de desarrollo. Antes de un uso real se debe resolver lo siguiente:

- [ ] Restringir o eliminar `POST /api/Auth/register` (hoy es un endpoint de bootstrap sin protección que crea usuarios `Admin`).
- [ ] Reemplazar el hash sembrado del usuario `admin` en `V0007` (es un placeholder no válido para el login PBKDF2).
- [ ] Parametrizar la URL de la API del frontend y los orígenes CORS.
- [ ] Mover todos los secretos a variables de entorno o a un gestor de secretos; rotar las claves de ejemplo.
- [ ] Publicar la API detrás de HTTPS.
- [ ] Configurar el SMTP real y, si corresponde, habilitar Teams.
- [ ] Obtener de UltraPort las credenciales y la configuración reales de INVGATE.
- [ ] Definir copias de seguridad de la base `DbSitrac`.

## 7. Verificación de la instalación

1. **API arriba:** abrir `http://localhost:5116/swagger` (solo en `Development`).
2. **Base de datos:** comprobar que existe `DbSitrac` y que las migraciones `V0001`–`V0007` quedaron aplicadas.
3. **Autenticación:** crear un usuario desde Swagger (`/api/Auth/register`, solo en desarrollo) y probar `/api/Auth/login`; debe devolver un JWT.
4. **Webhook:** enviar un incidente de prueba con el header de seguridad:
   ```bash
   curl -X POST http://localhost:5116/api/Incidentes/invgate/webhook \
     -H "Content-Type: application/json" \
     -H "X-Api-Key: <valor de WebhookApiKey>" \
     -d @incidente-ejemplo.json
   ```
   Sin el header, o con una clave incorrecta, la API debe rechazar la solicitud.
5. **Consola web:** iniciar sesión en `http://localhost:5500/login.html` y revisar la cola de incidentes.
6. **Notificaciones:** abrir smtp4dev en `http://localhost:5000` y confirmar que llegan los correos.

## 8. Operación y mantenimiento

- **Actualizar el esquema:** agregar un nuevo script versionado en `Sitrac.MigracionBd/Scripts/`; se aplica en el siguiente arranque de la API.
- **Reiniciar servicios:** `docker compose restart` (o `docker compose down` / `up --build` para reconstruir).
- **Revisar notificaciones:** la cola Outbox está en la tabla `TbSitracNotificacionLog`.
- **Pruebas:** ver `Plan_de_Pruebas_Fase_3_SITRAC.docx`.

## 9. Solución de problemas

| Síntoma | Causa probable | Solución |
| --- | --- | --- |
| El frontend no puede llamar a la API (error CORS) | El frontend no corre en el puerto 5500 | Servir el frontend en `5500` |
| La API responde 429 | Se superó el límite de 100 solicitudes por minuto | Esperar un minuto o reducir la frecuencia |
| La API no inicia o no encuentra migraciones | Ruta de `Sitrac.MigracionBd/Scripts` incorrecta, o SQL Server aún no disponible | Verificar la ruta relativa y que SQL Server esté arriba |
| `dotnet build` falla por versión del SDK | No está instalado .NET 10 (`10.0.400`) | Instalar el SDK indicado en `global.json` |
| El webhook devuelve 401 | `X-Api-Key` ausente o distinta de `WebhookApiKey` | Enviar el header con el valor configurado |
| No llegan correos | smtp4dev detenido o `SmtpConfig` mal configurado | Revisar el contenedor y las variables `SmtpConfig__*` |
| No se puede iniciar sesión con `admin` | El hash sembrado en `V0007` es un placeholder | Crear un usuario con `/api/Auth/register` |

## 10. Referencias

- `README.md` · `ARCHITECTURE.md` · `Requisitos-No-Funcionales.md`
- Ley N° 21.663, Ministerio del Interior y Seguridad Pública (2024). https://www.bcn.cl/leychile/navegar?idNorma=1202434
