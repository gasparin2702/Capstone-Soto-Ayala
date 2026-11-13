# GUÍA DE ESTUDIO — SITRAC (Sistema de Gestión de Incidentes · Ley N° 21.663)

> Esta guía está pensada para que una persona del equipo pueda **entender, ejecutar y
> modificar** SITRAC sin depender de conocimiento tribal. Se lee de arriba a abajo la
> primera vez y luego se usa como referencia.
>
> Si eres una IA trabajando en este repositorio, lee también `AGENTS.md`. Si algo en
> `ARCHITECTURE.md` contradice el código, **gana el código**.

## Índice

1. [¿Qué es SITRAC y para qué existe?](#1-qué-es-sitrac-y-para-qué-existe)
2. [Glosario del dominio](#2-glosario-del-dominio)
3. [Arquitectura general](#3-arquitectura-general)
4. [Estructura de carpetas y archivos clave](#4-estructura-de-carpetas-y-archivos-clave)
5. [Modelo de datos](#5-modelo-de-datos)
6. [Flujos principales, paso a paso](#6-flujos-principales-paso-a-paso)
7. [Puesta en marcha local](#7-puesta-en-marcha-local)
8. [Recorrido de estudio recomendado](#8-recorrido-de-estudio-recomendado)
9. [Conceptos y patrones que debes dominar](#9-conceptos-y-patrones-que-debes-dominar)
10. [Referencia rápida de la API](#10-referencia-rápida-de-la-api)
11. [Configuración y secretos](#11-configuración-y-secretos)
12. [Trampas, deuda técnica y puntos delicados](#12-trampas-deuda-técnica-y-puntos-delicados)
13. [Ejercicios para dominar el proyecto](#13-ejercicios-para-dominar-el-proyecto)
14. [Solución de problemas](#14-solución-de-problemas)
15. [Puntos de extensión](#15-puntos-de-extensión)

---

## 1. ¿Qué es SITRAC y para qué existe?

SITRAC es una **consola de gestión de incidentes de ciberseguridad** para ANCI / Ultraport,
construida alrededor de la **Ley N° 21.663 (Marco de Ciberseguridad de Chile)**. Su trabajo
es ayudar a cumplir los plazos legales de notificación y dejar trazabilidad de todo lo que
ocurrió.

En términos simples:

1. Entra un incidente (normalmente desde **InvGate** por webhook).
2. El sistema calcula sus **plazos legales** y crea **hitos regulatorios**:
   - **Aviso Temprano**: 3 horas desde la toma de conocimiento.
   - **Informe Preliminar**: 24 horas si la empresa es **OIV**, 72 horas si es **Esencial**.
   - **Informe Final**: 15 días.
3. Calcula una **prioridad** para decidir si alerta de inmediato o consolida la alerta.
4. Notifica por **correo electrónico** y **Microsoft Teams**.
5. Avanza el ciclo de vida solo cuando vence un plazo, con transacciones y registro de
   historial.
6. Escala por umbrales antes de que venza cada hito (por ejemplo, faltan 2 horas).
7. Un analista humano confirma el aviso, registra informes y cierra el incidente. Esas
   acciones exigen un comentario de auditoría.

La aplicación tiene dos caras:

- **Backend .NET 10** con API REST, workers en segundo plano y SQL Server.
- **Frontend estático** (HTML + CSS + JavaScript nativo) con pantalla de login y consola
  de operador.

---

## 2. Glosario del dominio

| Término | Significado |
|---|---|
| **Incidente** | Evento de ciberseguridad registrado en `TbSitracIncidente`. |
| **Hito** | Obligación regulatoria con fecha límite (`TbSitracHito`). |
| **Aviso Temprano** | Primer hito, 3 h desde la toma de conocimiento. |
| **Informe Preliminar** | Segundo hito, 24 h (OIV) o 72 h (Esencial). |
| **Informe Final** | Tercer hito, 15 días. |
| **SLA** | Acuerdo de nivel de servicio; aquí son los plazos legales. |
| **OIV** | Operador de Importancia Vital; clasificación con plazos más cortos. |
| **Esencial** | Empresa esencial; clasificación con 72 h para el preliminar. |
| **ANCI** | Agencia Nacional de Ciberseguridad de Chile. |
| **CSIRT** | Equipo de respuesta a incidentes; el informe preliminar alimenta al CSIRT. |
| **DPO** | Delegado de Protección de Datos; se registra si hubo datos personales. |
| **Outbox** | Tabla de salida (`TbSitracNotificacionLog`) para enviar notificaciones de forma confiable y consolidada. |
| **Escalamiento** | Alerta preventiva cuando faltan ciertos minutos para un vencimiento. |
| **Snapshot SLA** | Copia de los plazos calculados al momento de crear el incidente. No se recalcula después. |
| **Toma de conocimiento** | Fecha/hora en que la organización supo del incidente; es la base de todos los plazos. |

### Estados del incidente

```
1 En triage ──(confirmar aviso / vence aviso)──> 2 Aviso enviado
2 Aviso enviado ──(informe preliminar / vence preliminar)──> 3 Preliminar enviado
3 Preliminar enviado ──(informe final)──> sigue en 3 (solo se cierra el hito Final)
3 Preliminar enviado ──(cerrar / vence final sin informe)──> 4 Cerrado
4 Cerrado  (terminal: los motores ya no lo evalúan)
```

En el frontend los estados se muestran como `Detectado`, `Aviso Temprano`,
`Informe Preliminar` y `Cerrado`.

---

## 3. Arquitectura general

### 3.1 Capas

```
┌─────────────────────────────────────────────────────────────┐
│ Sitrac.Frontend (HTML/CSS/JS estático, puerto 5500)          │
│  login.html · index.html · js/api.js · js/app.js            │
└───────────────────────────────┬─────────────────────────────┘
                                │ HTTP + JWT (Bearer)
┌───────────────────────────────▼─────────────────────────────┐
│ Sitrac.Servidor (ASP.NET Core Web API, puerto 5116)          │
│  Controllers · Program.cs · JWT · RateLimit · CORS · Evolve │
└───────────────────────────────┬─────────────────────────────┘
┌───────────────────────────────▼─────────────────────────────┐
│ Sitrac.Servicio (lógica de negocio)                          │
│  IncidenteServicio · NotificacionServicio · Auth             │
│  AvanzadorCicloVida · Despachador · Escalamiento            │
└───────────────────────────────┬─────────────────────────────┘
┌───────────────────────────────▼─────────────────────────────┐
│ Sitrac.Repositorio (Dapper + SQL parametrizado)             │
│  Incidente · Hito · SLA · Notificación · Regla · Usuario    │
└───────────────────────────────┬─────────────────────────────┘
┌───────────────────────────────▼─────────────────────────────┐
│ Sitrac.AccesoDatos (FabricaConexionBd)                       │
└───────────────────────────────┬─────────────────────────────┘
┌───────────────────────────────▼─────────────────────────────┐
│ SQL Server — DbSitrac                                        │
│  Migraciones Evolve V0001…V0007                              │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Dependencias entre proyectos

```
Sitrac.Servidor ──> Sitrac.Servicio
                └─> Sitrac.Repositorio

Sitrac.Servicio ──> Sitrac.Repositorio

Sitrac.Repositorio ──> Sitrac.AccesoDatos

Sitrac.AccesoDatos ──> Microsoft.Data.SqlClient + IConfiguration
```

Regla mental: **las flechas solo apuntan hacia abajo**. Un controlador no ejecuta SQL; un
repositorio no envía correos; `Sitrac.AccesoDatos` no conoce a nadie más.

### 3.3 El punto de composición: `Program.cs`

`Sitrac.Servidor/Program.cs` es el archivo más importante para entender cómo se arma todo:

- Carga `appsettings.json`, `appsettings.{Environment}.json`, variables de entorno y
  user-secrets en Development.
- Registra opciones (`SmtpConfig`, `TeamsConfig`, `FrontendSettings`, `JwtSettings`).
- Registra la fábrica de conexiones como singleton, repositorios y servicios como scoped,
  y tres workers como hosted services.
- Configura JWT Bearer, rate limiting global (**100 req/min** por usuario o IP) y CORS
  estricto para `http://localhost:5500` y `http://127.0.0.1:5500`.
- Ejecuta las migraciones de Evolve al arrancar. Si fallan, la API no inicia.
- En Development habilita Swagger en `/swagger`.
- Orden del pipeline: **CORS → RateLimiter → Authentication → Authorization → Swagger
  (Dev) → HTTPS redirection → controllers**.

---

## 4. Estructura de carpetas y archivos clave

### 4.1 Raíz

| Archivo | Qué hace |
|---|---|
| `Sitrac.sln` | Solución con los 5 proyectos. |
| `global.json` | Fija el SDK .NET en `10.0.400`. |
| `docker-compose.yml` | Levanta SQL Server 2022, smtp4dev y la API. |
| `Sitrac.dockerfile` | Build multi-stage y copia de scripts de Evolve. |
| `AGENTS.md` | Contexto rápido para IAs. |
| `GUIA_DE_ESTUDIO.md` | Este documento. |
| `ARCHITECTURE.md` | Documento antiguo; **parcialmente desactualizado**. |

### 4.2 `Sitrac.Servidor` (presentación)

| Archivo | Qué hace |
|---|---|
| `Program.cs` | Composition root: configuración, DI, JWT, rate limit, CORS, Evolve. |
| `Controllers/IncidentesControlador.cs` | Endpoints de incidentes y webhook de InvGate. |
| `Controllers/AutenticacionControlador.cs` | `login` y `register` (bootstrap de desarrollo). |
| `appsettings.json` | Configuración base de desarrollo. |
| `appsettings.Development.json` | Sobrescribe SMTP para SMTP local en el puerto 25. |
| `Properties/launchSettings.json` | Perfil `http` en `http://localhost:5116`. |

### 4.3 `Sitrac.Servicio` (aplicación)

| Archivo | Qué hace |
|---|---|
| `IncidenteServicio.cs` | Casos de uso centrales: registrar incidente, priorizar, avanzar ciclo, confirmar aviso, registrar informe, cerrar. |
| `NotificacionServicio.cs` | Correos (MailKit), tarjetas de Teams, resumen consolidado y alertas de escalamiento. |
| `AutenticacionServicio.cs` | Login, generación de JWT, hash/verificación PBKDF2. |
| `AvanzadorCicloVidaServicio.cs` | Worker cada 1 minuto que avanza estados por vencimiento. |
| `DespachadorNotificacionServicio.cs` | Worker cada 15 segundos que consume el Outbox. |
| `EscalamientoSlaServicio.cs` | Worker cada 1 minuto que dispara alertas por umbral. |
| `Helpers/CalculadoraPrioridad.cs` | Fórmula de prioridad, etiquetas, colores y emojis. |
| `Configuration/Configuracion*.cs` | Clases de opciones con `SectionName`. |
| `Dtos/*.cs` | DTOs de entrada/salida y `ResultadoEnvio`. |
| `Interface/*.cs` | Contratos de servicios (namespace `Sitrac.Servicio.Interfaces`). |

### 4.4 `Sitrac.Repositorio` (datos)

| Archivo | Qué hace |
|---|---|
| `IncidenteRepositorio.cs` | Consultas de listado/detalle/historial y transacciones de cambio de estado, informes y cierre. |
| `HitoRepositorio.cs` | CRUD de hitos y **reserva idempotente** de notificaciones. |
| `SlaRepositorio.cs` | Lee la plantilla SLA y guarda el snapshot por incidente. |
| `RegistroNotificacionRepositorio.cs` | Inserta y procesa filas del Outbox. |
| `ReglaEscalamientoRepositorio.cs` | Lee reglas por tipo de hito. |
| `UsuarioRepositorio.cs` | Login/registro de usuarios. |
| `Models/*.cs` | Modelos que reflejan tablas o vistas de lectura. |
| `Interfaces/*.cs` | Contratos de repositorios. |

### 4.5 `Sitrac.AccesoDatos` y `Sitrac.MigracionBd`

| Archivo | Qué hace |
|---|---|
| `FabricaConexionBd.cs` | Lee `ConnectionStrings:SitracConnection` y crea `SqlConnection`. |
| `Scripts/V0001…V0007` | Esquema, catálogos, historial, hitos/outbox y usuarios. `Sitrac.MigracionBd` no tiene `Program.cs`; los scripts los aplica la API al iniciar. |

### 4.6 `Sitrac.Frontend` (UI)

| Archivo | Qué hace |
|---|---|
| `login.html` | Formulario de login con estilos inline. |
| `index.html` | Consola con tres vistas: Mi cola, Detalle del caso y Guía de diseño. |
| `css/Style.css` | Estilos de la consola. Ojo: el HTML enlaza `css/style.css` (minúscula). |
| `js/api.js` | Wrapper de `fetch`, agrega `Authorization`, mapea respuestas de la API. |
| `js/login.js` | Login, guarda `sitrac_token` y `sitrac_user` en `localStorage`. |
| `js/app.js` | Estado de la consola, contadores SLA, prioridad, modales y acciones. |

---

## 5. Modelo de datos

### 5.1 Tablas principales

| Tabla | Propósito |
|---|---|
| `TbSitracIncidente` | Incidente: fechas, canal, clasificación, gravedad y estado. |
| `TbSitracIncidentePlazoSla` | Snapshot 1:1 con las tres fechas límite del incidente. |
| `TbSitracTipoPlazoSla` | Plantilla de plazos por clasificación de empresa. |
| `TbSitracHito` | Hitos del incidente con estado, fecha límite y control de notificaciones. |
| `TbSitracTipoHito` | Catálogo: Aviso, Preliminar, Final y horas legales base. |
| `TbSitracEventoHistorial` | Bitácora de cambios de estado/acciones. |
| `TbSitracTipoAmbitoCambio` | Catálogo de ámbitos: automático, aviso, informe, cierre, creación. |
| `TbSitracNotificacionLog` | Outbox de notificaciones Email/Teams. |
| `TbSitracReglaEscalamiento` | Umbrales de minutos restantes y repetición. |
| `TbSitracInformePreliminar` | Formulario CSIRT/DPO del informe de 24/72 h. |
| `TbSitracInformeCierre` | Formulario CSIRT/DPO del informe final. |
| `TbSitracRevisionDatosPersonales` | Auditoría DPO (existe; la API aún pasa `null` en la transacción). |
| `TbSitracUsuario` | Usuarios para login JWT. |
| `TbSitracResponsable` | Personas/responsables de referencia. |
| `TbSitracComunicacion`, `TbSitracAprobacion` | Comunicaciones y aprobaciones (esquema; poco usadas por la API actual). |

Todas las tablas usan columnas de auditoría:

- `AuditNotDeleted` (soft delete; las consultas filtran `= 1`).
- `AuditCreateDate`.
- `AuditLastUpdateDate`.
- `AuditUserLastUpdate`.

### 5.2 Relaciones

```
TbSitracTipoClasificacionEmpresa 1 ── N TbSitracTipoPlazoSla
TbSitracTipoPlazoSla             1 ── N TbSitracIncidentePlazoSla
TbSitracIncidente                1 ── 1 TbSitracIncidentePlazoSla
TbSitracIncidente                1 ── N TbSitracHito
TbSitracTipoHito                 1 ── N TbSitracHito
TbSitracHito                     1 ── N TbSitracNotificacionLog
TbSitracIncidente                1 ── N TbSitracEventoHistorial
TbSitracIncidente                1 ── N TbSitracInformePreliminar / InformeCierre
TbSitracIncidente                1 ── 0/1 TbSitracRevisionDatosPersonales
```

### 5.3 Catálogos sembrados (¡los IDs importan!)

| Catálogo | IDs |
|---|---|
| Clasificación de empresa | `1 = Esencial`, `2 = OIV` |
| Gravedad | `1 = Bajo`, `2 = Medio`, `3 = Alto`, `4 = Crítico` |
| Estado del incidente | `1 = En triage`, `2 = Aviso enviado`, `3 = Preliminar enviado`, `4 = Cerrado` |
| Tipo de hito | `1 = Aviso Temprano`, `2 = Informe Preliminar`, `3 = Informe Final` |
| Canal de entrada | `1 = INVGATE`, `2 = Correo Regulador`, `3 = Manual` |
| Datos personales | `1 = Sí`, `2 = No`, `3 = Desconocido` |
| Ámbito de cambio | `1 = Automático SLA`, `2 = Confirmación aviso`, `3 = Envío informe`, `4 = Cierre manual`, `5 = Creación hito` |

Estos IDs están **hardcodeados** en servicios y SQL. No los cambies sin revisar todo el
código.

### 5.4 Plazos SLA

| Clasificación | Aviso | Preliminar | Final |
|---|---|---|---|
| Esencial (`1`) | 3 h | 72 h | 15 días |
| OIV (`2`) | 3 h | 24 h | 15 días |

El incidente guarda un **snapshot** de esos plazos. Si mañana cambia la plantilla, los
incidentes antiguos mantienen sus fechas originales.

### 5.5 Prioridad

Fórmula implementada en `Sitrac.Servicio/Helpers/CalculadoraPrioridad.cs` y replicada en
`Sitrac.Frontend/js/app.js`:

```
score = (gravedad × 3)
      + (clasificación × 2)      // OIV = 2, Esencial = 1
      + (urgencia × 40)
      + (pesoHito × 2)           // Aviso = 3, Preliminar = 2, Final = 1

urgencia = 1 − min(1, tiempoRestante / ventanaSLA)
```

Etiquetas:

| Score | Etiqueta |
|---|---|
| ≥ 30 | Máxima |
| ≥ 22 | Alta |
| ≥ 15 | Media |
| < 15 | Baja |

**Regla de enrutamiento:** score ≥ 30 → envío inmediato (Email + Teams). Score < 30 →
dos filas en el Outbox (Email y Teams) programadas a +30 segundos.

### 5.6 Reglas de escalamiento

Están en `TbSitracReglaEscalamiento`. Ejemplos sembrados:

- Aviso Temprano: avisa cuando faltan 120, 60, 30 y 10 minutos; al vencer, primera hora y
  luego cada 60 minutos.
- Informe Preliminar: 24 h, 8 h, 2 h, 30 min; al vencer, cada 30/60 minutos.
- Informe Final: 7 días, 5 días, 2 días, 1 día, 4 h; al vencer, cada 120 minutos.

El worker elige la regla **más urgente ya cruzada** (la de menor `TiempoRestanteMinutos`
que cumpla `restante <= umbral`).

---

## 6. Flujos principales, paso a paso

### 6.1 Entrada de un incidente desde InvGate

```
InvGate
  │ POST /api/Incidentes/invgate/webhook  + header X-Api-Key
  ▼
IncidentesControlador.RecepcionarTicketInvgate
  │ valida API key
  ▼
IncidenteServicio.RegistrarIncidenteInvgateAsync
  │ 1. INSERT TbSitracIncidente (estado 1, canal 1)
  │ 2. lee TbSitracTipoPlazoSla según clasificación
  │ 3. INSERT snapshot TbSitracIncidentePlazoSla
  │ 4. INSERT hito Aviso Temprano (Pendiente)
  │ 5. Calcula prioridad
  ├─ score >= 30 → envía Email + Teams inmediatamente
  └─ score < 30  → INSERT 2 filas en TbSitracNotificacionLog (+30 s)
  ▼
201 Created { Mensaje, Id }
```

Request esperado (JSON camelCase):

```json
{
  "codigoTicketInvgate": "INC-1001",
  "fechaTomaConocimiento": "2026-11-11T09:00:00",
  "codTipoClasificacionEmpresa": 2,
  "codTipoGravedad": 4,
  "descripcionSistemaAfectado": "Portal web"
}
```

Cosas importantes:

- El webhook es anónimo, pero exige `X-Api-Key`. La clave se lee de `WebhookApiKey` y por
  defecto es `SITRAC_INVGATE_SECRET_2026`.
- El controlador **no** devuelve `ex.Message` en errores 500.
- La prioridad usa la ventana del aviso (3 h) y la fecha de toma de conocimiento.
- Un incidente crítico puede generar un correo inmediato que, si SMTP falla, **no se
  reintenta** porque no pasa por el Outbox.

### 6.2 Ciclo de vida automático

`AvanzadorCicloVidaServicio` corre cada minuto y llama a
`IncidenteServicio.ProcesarCicloDeVidaAsync`:

1. Carga incidentes con estado `< 4` y snapshot SLA.
2. Evalúa:
   - Estado 1 y `ahora >= FechaLimiteAvisoTemprano` → pasa a 2.
   - Estado 2 y `ahora >= FechaLimiteInformePreliminar` → pasa a 3.
   - Estado 3 y `ahora >= FechaLimiteInformeFinal` **con hito Final pendiente** → pasa a 4.
     Si el informe final ya fue registrado, el sistema no cierra solo: debe hacerlo un
     humano con `/cerrar`.
3. Por cada transición, en una sola transacción:
   - cierra el hito anterior (`Cumplido`),
   - crea el hito siguiente si corresponde,
   - actualiza el estado del incidente,
   - inserta un evento de historial con ámbito automático (`1`).
4. Si el nuevo estado es `< 4`, intenta encolar notificaciones del hito siguiente.

> **Ojo:** el cierre automático marca el hito Final pendiente como `Cumplido` aunque no se
> haya cargado un informe. Revisa si esa semántica legal es la deseada antes de cambiarla.
>
> **Bug conocido:** después de avanzar, el código busca un hito pendiente de tipo
> `nuevoEstado + 1` en vez del hito recién creado (`hitoACrear.CodTipoHito`). En la
> práctica, los hitos creados por avance automático pueden quedar sin notificación
> encolada. Ver §12.

### 6.3 Outbox y consolidación

`DespachadorNotificacionServicio` corre cada 15 segundos:

1. `RegistroNotificacionRepositorio.ObtenerPendientesParaEnvioAsync(50)` busca filas
   `Pendiente`, no borradas y con `FechaProgramada <= GETUTCDATE()`.
2. `NotificacionServicio.EnviarResumenConsolidadoAsync`:
   - separa canales Email y Teams;
   - arma **un** correo resumen agrupado por gravedad (críticos, altos, medios/bajos);
   - arma **una** tarjeta Adaptive Card con el mismo agrupamiento.
3. Si el envío fue exitoso, marca cada fila como `Enviada`.
4. Si falla, las filas quedan pendientes para el próximo ciclo.

Esto evita "fatiga de alertas": en vez de 20 correos, llega un resumen.

### 6.4 Escalamiento SLA

`EscalamientoSlaServicio` corre cada minuto:

1. `HitoRepositorio.ObtenerPendientesParaEvaluarAsync()` carga hitos `Pendiente` no
   silenciados.
2. Carga reglas por tipo de hito y las cachea por ciclo.
3. Calcula `minutosRestantes = FechaLimite - DateTime.Now`.
4. Busca la regla más urgente ya cruzada.
5. Llama a `HitoRepositorio.ReservarNotificacionAsync(...)`, que hace un `UPDATE`
   condicional. Solo un worker puede "ganar" la reserva:
   - actualiza `UltimaReglaNotificada`,
   - actualiza `ProximaNotificacionAt` (o `DateTime.MaxValue` si la regla no se repite),
   - incrementa `ConteoNotificaciones`.
6. Si la reserva fue exitosa, envía correo + tarjeta de Teams.

La idempotencia está en la base de datos, no en memoria. Esa es la clave del diseño.

### 6.5 Acciones humanas

Todas están bajo `[Authorize]` y exigen un comentario de **15 a 500 caracteres**.

| Acción | Endpoint | Validaciones |
|---|---|---|
| Confirmar aviso | `POST /api/Incidentes/{id}/confirmar-aviso` | Incidente en estado 1; existe hito Aviso pendiente. Cierra aviso, crea Preliminar, pasa a estado 2, registra historial y encola un Teams. |
| Registrar informe preliminar | `POST /api/Incidentes/{id}/informe` con `tipo = "P"` | Incidente en estado 2; existe hito Preliminar. Inserta informe, cierra hito, crea hito Final, pasa a estado 3. |
| Registrar informe final | `POST /api/Incidentes/{id}/informe` con `tipo = "F"` | Incidente en estado 3; existe hito Final. Inserta informe de cierre y cierra el hito Final, sin cambiar de estado. |
| Cerrar incidente | `POST /api/Incidentes/{id}/cerrar` | Incidente en estado 3 y **sin** hito Final pendiente. Pasa a estado 4 y cancela hitos pendientes. |

Cada operación multi-tabla se ejecuta en una transacción de SQL Server dentro del
repositorio (`...TransaccionalAsync`).

### 6.6 Autenticación

1. `POST /api/Auth/login` busca el usuario y verifica PBKDF2-SHA256.
2. Si es válido, genera un JWT con:
   - `NameIdentifier` = ID de usuario,
   - `Name` = nombre de usuario,
   - `Role` = rol,
   - claim `NombreCompleto`.
3. El token dura `JwtSettings:ExpirationHours` (8 h por defecto).
4. El frontend guarda el token y lo envía como `Authorization: Bearer …`.
5. Si la API responde 401, `api.js` borra la sesión y redirige a `login.html`.

> `POST /api/Auth/register` existe como bootstrap de desarrollo: es **anónimo** y siempre
> crea un usuario `Admin`. No lo expongas en producción.

---

## 7. Puesta en marcha local

### Opción A — Docker (recomendada para empezar)

Requisitos: Docker + Docker Compose.

```bash
docker compose up --build
```

Servicios:

| Servicio | URL / puerto | Notas |
|---|---|---|
| API | `http://localhost:5116` | Swagger en `http://localhost:5116/swagger`. |
| SQL Server | `localhost:1433` | Usuario `sa`, contraseña `Sitrac2026!Strong`. |
| smtp4dev | `http://localhost:5000` | UI para ver correos capturados. SMTP interno en `smtp4dev:25`. |

El compose:

- Espera a que SQL Server esté sano.
- Sobrescribe la conexión, JWT, webhook, SMTP, Teams y `FrontendSettings` con variables de
  entorno.
- Ejecuta las migraciones Evolve al iniciar la API.
- Deja Teams deshabilitado.

El **frontend no está en el compose**. Levántalo aparte:

```bash
npx serve Sitrac.Frontend -l 5500
# Abrir http://localhost:5500/login.html
```

CORS solo permite el puerto 5500, así que respeta ese puerto.

> **Ojo con los deep links:** `FrontendSettings:BaseUrl` viene como
> `http://127.0.0.1:5500/Sitrac.Frontend` en `appsettings.json`. Si sirves la carpeta
> `Sitrac.Frontend` directamente en la raíz del puerto 5500, los enlaces de correos y
> tarjetas apuntarán a una ruta inexistente. Ajusta `FrontendSettings__BaseUrl` a
> `http://127.0.0.1:5500` o sirve el frontend desde la raíz del repositorio.

### Opción B — Local sin contenedores para la API

Requisitos:

- .NET SDK `10.0.400` o superior compatible (`dotnet --version`).
- SQL Server (local o contenedor) con base `DbSitrac` o permiso para crearla.
- Opcional: SMTP local (smtp4dev, MailHog, Mailtrap).

Pasos:

1. Ajusta la cadena de conexión. La de `appsettings.json` usa autenticación integrada de
   Windows (`Trusted_Connection=True`), que **no funciona en Linux**. Usa SQL auth o una
   variable de entorno:

   ```bash
   export ConnectionStrings__SitracConnection="Server=localhost,1433;Database=DbSitrac;User Id=sa;Password=Sitrac2026!Strong;TrustServerCertificate=True;"
   ```

2. Ejecuta la API desde el proyecto para que la ruta relativa de migraciones
   (`../Sitrac.MigracionBd/Scripts`) resuelva:

   ```bash
   cd Sitrac.Servidor
   dotnet restore
   dotnet run --launch-profile http
   ```

   API: `http://localhost:5116`, Swagger: `http://localhost:5116/swagger`.

3. Configura SMTP. `appsettings.Development.json` apunta a `localhost:25`. Si usas
   smtp4dev en el puerto 2525, cambia `SmtpConfig:Port` o usa variables de entorno:

   ```bash
   export SmtpConfig__Host=localhost
   export SmtpConfig__Port=2525
   export SmtpConfig__EnableSsl=false
   export SmtpConfig__UserName=demo@local.test
   export SmtpConfig__Password=no-reply
   export SmtpConfig__DestinatarioDemo=analista@local.test
   ```

   Si SMTP no está configurado, el sistema sigue funcionando, pero los envíos devuelven
   fallo y se registran en logs.

4. Levanta el frontend:

   ```bash
   npx serve Sitrac.Frontend -l 5500
   ```

5. Abre `http://localhost:5500/login.html`. Si quieres que los deep links de las
   notificaciones funcionen, ajusta `FrontendSettings:BaseUrl` como se explicó en la
   Opción A.

### Crear el primer usuario

El usuario `admin` sembrado por `V0007__AuthUsuarios.sql` tiene un hash de relleno que no
corresponde a una contraseña real. Para desarrollo, crea un usuario con el endpoint de
registro:

```bash
curl -s -X POST http://localhost:5116/api/Auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "analista",
    "password": "Admin123!",
    "nombreCompleto": "Analista SITRAC"
  }'
```

Luego inicia sesión:

```bash
curl -s -X POST http://localhost:5116/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"analista","password":"Admin123!"}'
```

Guarda el `token` que devuelve.

### Probar el webhook

La prioridad depende de la urgencia temporal. Para que los ejemplos sean
reproducibles, calcula fechas relativas al momento de ejecutar:

```bash
AHORA=$(date -u +%Y-%m-%dT%H:%M:%S)
HACE_2H=$(date -u -d '2 hours ago' +%Y-%m-%dT%H:%M:%S)
```

Incidente crítico (fecha de toma de conocimiento vencida → urgencia 1 → score ≥ 30,
envío inmediato):

```bash
curl -s -X POST http://localhost:5116/api/Incidentes/invgate/webhook \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: SITRAC_INVGATE_SECRET_2026" \
  -d "{
    \"codigoTicketInvgate\": \"INC-9001\",
    \"fechaTomaConocimiento\": \"$HACE_2H\",
    \"codTipoClasificacionEmpresa\": 2,
    \"codTipoGravedad\": 4,
    \"descripcionSistemaAfectado\": \"Portal de clientes\"
  }"
```

Incidente de baja prioridad (fecha actual, gravedad baja y Esencial → score < 30, va al
Outbox y se consolida ~30 s después):

```bash
curl -s -X POST http://localhost:5116/api/Incidentes/invgate/webhook \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: SITRAC_INVGATE_SECRET_2026" \
  -d "{
    \"codigoTicketInvgate\": \"INC-9002\",
    \"fechaTomaConocimiento\": \"$AHORA\",
    \"codTipoClasificacionEmpresa\": 1,
    \"codTipoGravedad\": 1,
    \"descripcionSistemaAfectado\": \"Equipo de laboratorio\"
  }"
```

Consultar incidentes:

```bash
TOKEN="pega-aqui-el-token"
curl -s http://localhost:5116/api/Incidentes \
  -H "Authorization: Bearer $TOKEN"
```

Confirmar aviso:

```bash
curl -s -X POST http://localhost:5116/api/Incidentes/1/confirmar-aviso \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "comentario": "Aviso enviado a la ANCI según protocolo interno.",
    "referenciaEnvio": "ANCI-2026-0001"
  }'
```

Registrar informe:

```bash
curl -s -X POST http://localhost:5116/api/Incidentes/1/informe \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "comentario": "Se registra informe preliminar con evidencia disponible.",
    "tipo": "P",
    "nombreArchivo": "informe-preliminar.pdf",
    "indiciosOcurrencia": "Accesos anómalos en el portal.",
    "activosAfectados": "Servidor web y base de datos.",
    "codTipoDatosPersonales": 3
  }'
```

Cerrar:

```bash
curl -s -X POST http://localhost:5116/api/Incidentes/1/cerrar \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "comentario": "Cierre validado con el equipo y la referencia del informe final.",
    "referenciaCierre": "CIERRE-2026-0001"
  }'
```

> Para que `/cerrar` funcione debe existir un informe final registrado (`tipo = "F"`) y no
> debe quedar hito Final pendiente.

### Verificar todo

1. `dotnet build Sitrac.sln` sin errores.
2. La API inicia y `/swagger` responde.
3. Las tablas `TbSitrac*` existen en `DbSitrac`.
4. Registro + login devuelven un JWT.
5. El webhook devuelve `201` con `{ Mensaje, Id }`.
6. `GET /api/Incidentes` devuelve el incidente.
7. El correo aparece en smtp4dev (si SMTP está configurado).
8. La consola web muestra el incidente y permite abrir el detalle.

---

## 8. Recorrido de estudio recomendado

Si tienes una semana, este orden funciona muy bien:

| Día | Objetivo | Archivos |
|---|---|---|
| 1 | Entender el dominio y el modelo | `AGENTS.md`, esta guía, `Scripts/V0001…V0006` |
| 2 | Entender el arranque y la API | `Program.cs`, `IncidentesControlador.cs`, `AutenticacionControlador.cs` |
| 3 | Entender el núcleo de negocio | `IncidenteServicio.cs`, `CalculadoraPrioridad.cs` |
| 4 | Entender datos y transacciones | `IncidenteRepositorio.cs`, `HitoRepositorio.cs`, `SlaRepositorio.cs` |
| 5 | Entender el asincronismo | `AvanzadorCicloVidaServicio.cs`, `DespachadorNotificacionServicio.cs`, `EscalamientoSlaServicio.cs` |
| 6 | Entender notificaciones | `NotificacionServicio.cs`, `ConfiguracionSmtp/Teams` |
| 7 | Entender la UI y probar de punta a punta | `api.js`, `app.js`, `index.html`, `login.js` |

Preguntas guía para cada archivo:

- ¿Qué responsabilidad tiene y qué **no** debería hacer?
- ¿Qué datos recibe y devuelve?
- ¿Qué pasa si falla a mitad de camino? ¿Hay transacción? ¿Hay reintento?
- ¿Dónde está el estado persistido?
- ¿Qué parte es legal/regulatoria y no se puede cambiar a la ligera?

---

## 9. Conceptos y patrones que debes dominar

### 9.1 Inyección de dependencias (DI)

Todo se registra en `Program.cs`. Los repositorios y servicios son `Scoped`; la fábrica de
conexión es `Singleton`. Los workers reciben `IServiceProvider` y crean un scope por ciclo
porque los servicios que usan son scoped.

### 9.2 Repository + Factory

- `IFabricaConexionBd` / `FabricaConexionBd` crean conexiones.
- Cada repositorio encapsula SQL y transacciones.
- Los servicios dependen de interfaces, no de implementaciones.

### 9.3 DTO

Los controladores no exponen modelos de base de datos directamente en las entradas. Usan
DTOs con DataAnnotations (`CrearIncidenteSolicitud`, `ConfirmarAvisoSolicitud`,
`RegistrarInformeSolicitud`, `CerrarIncidenteSolicitud`).

### 9.4 Transaction Script

Las operaciones que tocan varias tablas están en métodos transaccionales del repositorio:

- `AvanzarEstadoConHitosTransaccionalAsync`
- `ConfirmarAvisoTransaccionalAsync`
- `RegistrarInformeTransaccionalAsync`
- `CerrarIncidenteTransaccionalAsync`

Cada una abre conexión, `BeginTransaction`, ejecuta varios SQL y hace `Commit` o
`Rollback`.

### 9.5 BackgroundService + PeriodicTimer

Los tres workers heredan de `BackgroundService`. Usan `PeriodicTimer` y `WaitForNextTickAsync`,
con `try/catch` para que una excepción no mate el loop.

### 9.6 Outbox

En vez de enviar correos dentro de la transacción del incidente, se insertan filas en
`TbSitracNotificacionLog`. Un worker las consume y consolida. Ventajas:

- desacopla la creación del incidente del envío;
- permite reintentar;
- evita alert fatigue.

### 9.7 Smart routing

La prioridad decide el camino: envío inmediato para score ≥ 30, Outbox para el resto.

### 9.8 Idempotencia con reserva condicional

`ReservarNotificacionAsync` actualiza el hito solo si la regla cambió o ya pasó la próxima
notificación. El número de filas afectadas decide si este ciclo debe enviar. Esto evita
duplicados entre ciclos o instancias.

### 9.9 Reintentos con backoff y jitter

`NotificacionServicio.EnviarEmailConReintentosAsync` reintenta hasta `MaxReintentos` con
espera exponencial (`100 ms`, `200 ms`, …) más jitter, y usa un `SemaphoreSlim` estático
para espaciar envíos (`IntervaloEntreEnviosMs`).

### 9.10 Snapshot

Los plazos se copian al incidente al momento de crearlo. Así un cambio futuro de la
plantilla SLA no altera casos ya abiertos. Es un requisito de auditoría.

### 9.11 Soft delete y auditoría

Las consultas filtran `AuditNotDeleted = 1`. No se borran filas físicamente. Cada tabla
tiene columnas de auditoría. El historial (`TbSitracEventoHistorial`) es un registro legal:
no se edita ni se borra.

### 9.12 Máquina de estados

Los estados 1–4 son el corazón del flujo. Antes de cualquier cambio, valida el estado
actual y lanza `InvalidOperationException` si no corresponde. No confíes solo en la UI.

### 9.13 Adaptive Cards

`NotificacionServicio` construye JSON de Adaptive Cards para Teams: hechos, colores y un
botón `Action.OpenUrl` con deep link al incidente
(`{FrontendSettings:BaseUrl}/index.html?incidente={id}`).

### 9.14 JWT, PBKDF2 y rate limiting

- JWT firmado con HMAC-SHA256 y clave simétrica.
- Contraseñas con PBKDF2-SHA256, 100.000 iteraciones, salt de 16 bytes y hash de 32 bytes.
- Rate limiter global de ventana fija: 100 req/min por usuario o IP; devuelve 429.
- CORS restringido a los orígenes del frontend local.

### 9.15 Separación de responsabilidades

La regla más importante para no romper el proyecto: **controlador = orquestación;
servicio = negocio; repositorio = SQL; frontend = presentación**.

---

## 10. Referencia rápida de la API

Base: `http://localhost:5116/api`. El ruteo de ASP.NET Core no distingue mayúsculas.

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| `POST` | `/Auth/login` | No | Devuelve JWT, nombre y rol. |
| `POST` | `/Auth/register` | No | Crea un usuario `Admin` (solo desarrollo). |
| `POST` | `/Incidentes/invgate/webhook` | `X-Api-Key` | Registra incidente desde InvGate. |
| `GET` | `/Incidentes` | Bearer | Lista incidentes activos (`estado < 4`). |
| `GET` | `/Incidentes/{id}` | Bearer | Detalle + hitos. |
| `GET` | `/Incidentes/{id}/historial` | Bearer | Historial de eventos. |
| `POST` | `/Incidentes/{id}/confirmar-aviso` | Bearer | Confirma aviso temprano. |
| `POST` | `/Incidentes/{id}/informe` | Bearer | Registra informe `P` o `F`. |
| `POST` | `/Incidentes/{id}/cerrar` | Bearer | Cierra el incidente. |

Códigos de error esperados:

- `400`: ModelState inválido o transición de estado inválida.
- `401`: credenciales inválidas, falta token o API key incorrecta.
- `404`: incidente inexistente.
- `429`: rate limit excedido.
- `500`: error interno (mensaje genérico, sin detalles).

---

## 11. Configuración y secretos

### 11.1 Secciones de `appsettings.json`

| Sección | Claves relevantes |
|---|---|
| `ConnectionStrings` | `SitracConnection` |
| `JwtSettings` | `SecretKey`, `Issuer`, `Audience`, `ExpirationHours` |
| `WebhookApiKey` | Clave esperada en `X-Api-Key` |
| `SmtpConfig` | `Host`, `Port`, `EnableSsl`, `UserName`, `Password`, `DestinatarioDemo`, `NombreRemitente`, `TimeoutMs`, `MaxReintentos`, `IntervaloEntreEnviosMs` |
| `TeamsConfig` | `Habilitado`, `WebhookUrl`, `UmbralPrioridadMinima`, `TimeoutMs` |
| `FrontendSettings` | `BaseUrl` (usada en links de correos y tarjetas) |

### 11.2 Prioridad de configuración

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Variables de entorno (por ejemplo `SmtpConfig__Host`)
4. User-secrets en Development (`UserSecretsId = sitrac-anci-smtp-secrets`)

### 11.3 Buenas prácticas

- No subas secretos reales al repositorio.
- En local usa `dotnet user-secrets` o variables de entorno.
- En Docker usa las variables del compose o un archivo `.env` no versionado.
- Cambia la clave JWT por defecto y la API key del webhook antes de producción.
- El usuario sembrado `admin` no sirve para login real: crea uno nuevo o genera el hash.

---

## 12. Trampas, deuda técnica y puntos delicados

Estas son las cosas que más tiempo hacen perder. Léelas antes de depurar.

1. **`ARCHITECTURE.md` está desactualizado.** Usa nombres antiguos en inglés
   (`Sitrac.Server`, `Sitrac.Service`, `IncidenteService`) y dice que no hay autenticación.
   El código actual usa nombres en español y sí tiene JWT. **Confía en el código.**

2. **El hash del usuario `admin` sembrado es ficticio.** No corresponde a `Admin123!` ni a
   un PBKDF2 válido. El login con `admin` fallará. Crea un usuario por `/api/Auth/register`
   o calcula el hash con `AutenticacionServicio.HashPassword`.

3. **`/api/Auth/register` es anónimo y crea `Admin`.** Es un bootstrap de desarrollo.
   Protégelo o elimínalo antes de producción.

4. **Notificaciones automáticas del ciclo de vida.** En
   `ProcesarCicloDeVidaAsync`, después de avanzar se busca un hito de tipo
   `nuevoEstado + 1` en vez del hito recién creado. Por eso los hitos creados por avance
   automático pueden no quedar encolados. Si vas a tocar este flujo, usa
   `hitoACrear.CodTipoHito` o el ID devuelto por la creación.

5. **Cierre automático y semántica del hito Final.** El motor cierra el incidente cuando
   vence el plazo y el hito Final sigue pendiente, y en la misma transacción marca ese
   hito como `Cumplido`. Es decir, puede quedar registrado como cumplido sin que exista un
   informe final cargado. Verifica el requisito legal antes de cambiarlo.

6. **Los envíos inmediatos no se reintentan.** Si el score es ≥ 30, el correo/Teams se
   envía fuera del Outbox. Un fallo de SMTP o Teams queda en logs, no en una cola de
   reintento.

7. **La fórmula de prioridad está duplicada.** Backend (`CalculadoraPrioridad`) y frontend
   (`PRIORITY_WEIGHTS` en `app.js`). Si cambias una, cambia la otra o los ordenamientos
   diferirán.

8. **URLs del frontend hardcodeadas.** `js/api.js` y `js/login.js` apuntan a
   `http://localhost:5116/api`. CORS solo permite el puerto 5500. Para desplegar hay que
   cambiar ambos y la política CORS.

9. **CSS con mayúscula/minúscula.** `index.html` enlaza `css/style.css`, pero el archivo es
   `css/Style.css`. En Linux con servidor estático case-sensitive puede dar 404 y la
   consola se verá sin estilos.

10. **`Sitrac.MigracionBd` no es ejecutable.** No tiene `Program.cs`. Los scripts los corre
    la API con Evolve al iniciar. No intentes `dotnet run` en ese proyecto.

11. **Rutas de migración relativas.** `Program.cs` usa `../Sitrac.MigracionBd/Scripts`. Si
    ejecutas la API con un working directory distinto, Evolve no encontrará los scripts.
    Docker los copia a `/Sitrac.MigracionBd/Scripts`.

12. **Mezcla de zonas horarias.** Hay `GETDATE()` (hora local del servidor),
    `GETUTCDATE()` (UTC) y `DateTime.Now` en C#. El frontend formatea en
    `America/Santiago`. Evita introducir nuevas mezclas y, si tocas tiempos, documenta la
    convención.

13. **Campos y tablas parcialmente usados.** `RevisionDatosPersonales` existe, pero la
    transacción de informes recibe `null`. Las tablas de documentos/anexos existen, pero
    la API no las usa. `Sitrac.Servidor.http` sigue apuntando a `weatherforecast`.

14. **No hay tests ni CI.** Antes de dar algo por terminado: compila, arranca la API y
    prueba el endpoint con base de datos real. Si agregas tests, crea un proyecto de test
    y documenta el comando.

15. **El frontend no está en Docker Compose.** El compose levanta API, SQL Server y
    smtp4dev. El frontend se sirve aparte en el puerto 5500.

16. **`api.js` oculta errores de red.** Varias funciones devuelven `[]` o `null` cuando
    falla la API. Si la consola aparece vacía, revisa la consola del navegador y los logs
    de la API; no asumas que no hay incidentes.

17. **`FrontendSettings:BaseUrl` depende de cómo sirves el frontend.** El valor por
    defecto incluye `/Sitrac.Frontend`; si sirves la carpeta `Sitrac.Frontend` en la raíz
    del puerto 5500, los deep links de correos y Teams darán 404. Ajusta la URL o sirve
    desde la raíz del repositorio.

---

## 13. Ejercicios para dominar el proyecto

Hazlos en una rama local, de menor a mayor dificultad.

### Nivel 1 — Lectura y ejecución

1. Levanta el stack con Docker y crea un usuario por `/register`.
2. Registra un incidente crítico y uno de baja prioridad por webhook.
3. Observa la diferencia: el crítico se envía inmediato; el otro aparece en el Outbox y
   luego en un correo consolidado.
4. Revisa en SQL las filas de `TbSitracIncidente`, `TbSitracHito` y
   `TbSitracNotificacionLog`.
5. Abre la consola, confirma el aviso, registra un informe y cierra el incidente.

### Nivel 2 — Cambios pequeños

6. Agrega un campo nuevo a `CrearIncidenteSolicitud` y persístelo en una migración nueva.
7. Cambia el umbral de envío inmediato de 30 a 28 y actualiza el frontend.
8. Agrega una regla de escalamiento nueva directamente en SQL (sin tocar código).
9. Agrega un filtro por estado en `GET /api/Incidentes`.
10. Agrega un endpoint `GET /api/Incidentes/{id}/hitos`.

### Nivel 3 — Cambios de flujo

11. Corrige el encolado de notificaciones del ciclo de vida para que use el hito recién
    creado.
12. Haz que los envíos inmediatos también pasen por el Outbox (o que se persistan para
    reintento).
13. Agrega un canal de notificación nuevo (por ejemplo, Slack) implementando
    `INotificacionServicio` y una opción de configuración.
14. Agrega un proyecto de tests xUnit y prueba `CalculadoraPrioridad`,
    `AutenticacionServicio.HashPassword/VerifyPassword` y las validaciones de estado.
15. Agrega paginación o filtros al listado de incidentes.

---

## 14. Solución de problemas

| Síntoma | Causa probable | Qué revisar |
|---|---|---|
| La API no inicia y habla de migraciones | SQL Server no disponible, credenciales malas o scripts no encontrados | Cadena `SitracConnection`, healthcheck, ruta `../Sitrac.MigracionBd/Scripts`. |
| Error de login con `admin` | Hash sembrado inválido | Crea usuario por `/api/Auth/register`. |
| `401` en la consola | Token vencido o ausente | `localStorage` (`sitrac_token`), expiración de 8 h, `/login`. |
| `429` | Rate limit global | Espera 1 minuto o revisa la IP/usuario. |
| No llegan correos | SMTP deshabilitado o mal configurado | `SmtpConfig` y UI de smtp4dev. |
| No llegan alertas a Teams | `TeamsConfig:Habilitado = false` o webhook inválido | Config y logs `[Notificacion][Teams]`. |
| El frontend no carga estilos | `css/style.css` vs `css/Style.css` | Renombra el archivo o corrige el `<link>`. |
| CORS bloqueado | Frontend en otro puerto | Sirve en 5500 o ajusta `SitracPolicy`. |
| El incidente no avanza solo | Worker detenido o fechas futuras | Logs de `AvanzadorCicloVidaServicio`, snapshot SLA y estado. |
| El hito no escala | Regla no cruzada o reserva ya tomada | `TbSitracReglaEscalamiento`, `UltimaReglaNotificada`, `ProximaNotificacionAt`. |
| No se puede cerrar | Queda hito Final pendiente | Registra informe `F` primero. |
| `dotnet build` falla por SDK | SDK distinto al pinneado | Instala 10.0.400 o compatible; revisa `global.json`. |

---

## 15. Puntos de extensión

| Quieres… | Toca… |
|---|---|
| Nuevo endpoint | Controlador + interfaz/implementación de servicio + DI si aplica. |
| Nueva regla de negocio | `IncidenteServicio`, validando estado antes de llamar al repositorio. |
| Nueva consulta SQL | Interfaz + repositorio (parametrizada, con soft delete). |
| Nueva tabla/columna/catálogo | Nueva migración `V000N__*.sql`; modelos y repositorios. |
| Nuevo canal de notificación | `INotificacionServicio` (o extensión) + configuración + DI. |
| Cambiar prioridad | `CalculadoraPrioridad` y `PRIORITY_WEIGHTS`/`calcularPrioridad` del frontend. |
| Cambiar plazos SLA | Filas de `TbSitracTipoPlazoSla` (y fallbacks hardcodeados). |
| Cambiar umbrales de escalamiento | Filas de `TbSitracReglaEscalamiento`; no requiere código. |
| Cambiar autenticación | `AutenticacionServicio`, bloque JWT en `Program.cs`, `ConfiguracionJwt`. |
| Cambiar UI | `index.html`, `Style.css`, `app.js`, `api.js`. |
| Cambiar configuración | Clase `Configuracion*` + binding en `Program.cs` + `appsettings*.json` + docs. |

---

### Cierre

Si entiendes estos cinco puntos, entiendes SITRAC:

1. **El incidente tiene una máquina de estados 1→4 y un snapshot de plazos.**
2. **Cada plazo se materializa en un hito con su propia fecha límite.**
3. **La prioridad decide si se alerta inmediato o se consolida en el Outbox.**
4. **Tres workers mantienen el ciclo de vida, el envío y el escalamiento.**
5. **Todo cambio relevante deja rastro en `TbSitracEventoHistorial` y respeta las capas.**

Cuando dudes, vuelve al código y a este documento; y si cambias arquitectura o flujos,
actualiza `AGENTS.md` y esta guía.
