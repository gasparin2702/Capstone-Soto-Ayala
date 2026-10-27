# SITRAC – Referencia de Arquitectura

## Índice

1. [Visión general](#1-visión-general)
2. [Arquitectura en capas](#2-arquitectura-en-capas)
3. [Componentes principales](#3-componentes-principales)
4. [Flujos de trabajo](#4-flujos-de-trabajo)
5. [Dependencias entre proyectos](#5-dependencias-entre-proyectos)
6. [Modelo de datos](#6-modelo-de-datos)
7. [Configuración](#7-configuración)
8. [Mecanismos de notificación](#8-mecanismos-de-notificación)
9. [Servicios en segundo plano](#9-servicios-en-segundo-plano)
10. [Manejo de errores y consistencia de datos](#10-manejo-de-errores-y-consistencia-de-datos)
11. [Tecnologías utilizadas](#11-tecnologías-utilizadas)
12. [Patrones de diseño](#12-patrones-de-diseño)
13. [Puntos de extensión](#13-puntos-de-extensión)

---

## 1. Visión general

SITRAC es un sistema de gestión de incidentes de seguridad informática basado en la Ley N° 21.663 (Marco de Ciberseguridad). Registra, procesa y notifica incidentes a través de múltiples canales (email y Microsoft Teams) según su prioridad y clasificación.

Patrones y mecanismos principales:

- **Outbox Pattern** para la cola de notificaciones.
- **Smart Routing** (envío inmediato o diferido según prioridad).
- **Consolidación de alertas** en resúmenes agrupados por severidad.
- **Escalamiento de SLA** mediante alertas preventivas antes del vencimiento de hitos.

---

## 2. Arquitectura en capas

El sistema sigue una arquitectura en capas típica de aplicaciones .NET:

```
┌──────────────────────────────────────────┐
│           Capa de Presentación           │
│        (Sitrac.Server – API REST)        │
└────────────────────┬─────────────────────┘
                     │
┌────────────────────▼─────────────────────┐
│           Capa de Aplicación             │
│   (Sitrac.Service – Lógica de negocio)   │
└────────────────────┬─────────────────────┘
                     │
┌────────────────────▼─────────────────────┐
│         Capa de Acceso a Datos           │
│   (Sitrac.Repository – Repositorios)     │
└────────────────────┬─────────────────────┘
                     │
┌────────────────────▼─────────────────────┐
│             Capa de Datos                │
│     (Sitrac.DataAccess – Factory)        │
└────────────────────┬─────────────────────┘
                     │
┌────────────────────▼─────────────────────┐
│             Base de Datos                │
│          (SQL Server – DbSitrac)         │
└──────────────────────────────────────────┘
```

---

## 3. Componentes principales

### 3.1 Sitrac.Server (Presentación)

- **Tipo**: API Web ASP.NET Core.
- **Responsabilidad**: exponer endpoints REST para interactuar con el sistema.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/incidentes/invgate/webhook` | Recibe incidentes desde InvGate |
| `GET`  | `/api/incidentes` | Obtiene la lista de incidentes activos |
| `GET`  | `/api/incidentes/{id}` | Obtiene el detalle de un incidente |
| `GET`  | `/api/incidentes/{id}/historial` | Obtiene el historial de un incidente |
| `POST` | `/api/incidentes/{id}/confirmar-aviso` | Cierra el hito de aviso y crea el de informe preliminar |
| `POST` | `/api/incidentes/{id}/informe` | Registra el informe (Preliminar o Final) y datos CSIRT/DPO |
| `POST` | `/api/incidentes/{id}/cerrar` | Cierra definitivamente el incidente |

### 3.2 Sitrac.Service (Lógica de negocio)

- **Tipo**: biblioteca de clases .NET.
- **Responsabilidad**: contiene la lógica de negocio del sistema.

| Servicio | Interfaz | Descripción |
|----------|----------|-------------|
| `IncidenteService` | `IIncidenteService` | Lógica principal de incidentes, transacciones y Smart Routing |
| `NotificacionService` | `INotificacionService` | Envío de notificaciones (individuales, consolidadas y de escalamiento) |
| `LifecycleAdvancerService` | – (`BackgroundService`) | Ejecuta el ciclo de vida de incidentes y avanza hitos automáticamente cada minuto |
| `NotificationDispatcherService` | – (`BackgroundService`) | Evalúa la cola Outbox cada 15 segundos para consolidar notificaciones |
| `SlaEscalationService` | – (`BackgroundService`) | Evalúa umbrales de tiempo y envía alertas antes del vencimiento |
| `PrioridadCalculator` | – | Calculadora estática de prioridad de incidentes |

### 3.3 Sitrac.Repository (Acceso a datos)

- **Tipo**: biblioteca de clases .NET.
- **Responsabilidad**: interactuar con la base de datos usando Dapper.

| Repositorio | Interfaz | Descripción |
|-------------|----------|-------------|
| `IncidenteRepository` | `IIncidenteRepository` | Operaciones CRUD, consultas de incidentes e inserciones transaccionales de informes |
| `HitoRepository` | `IHitoRepository` | Gestión del ciclo de vida de hitos regulatorios |
| `SlaRepository` | `ISlaRepository` | Gestión de parámetros SLA |
| `NotificacionLogRepository` | `INotificacionLogRepository` | Gestión de la cola de salida (Outbox) de notificaciones |
| `ReglaEscalamientoRepository` | `IReglaEscalamientoRepository` | Lectura de las reglas de escalamiento por umbral |

### 3.4 Sitrac.DataAccess (Factory de conexiones)

- **Tipo**: biblioteca de clases .NET.
- **Responsabilidad**: crear conexiones a la base de datos.
- **Componente**: `DbConnectionFactory` (implementa `IDbConnectionFactory`), que crea instancias de `SqlConnection`.

### 3.5 Sitrac.DbMigration (Migraciones)

- **Tipo**: proyecto de consola .NET.
- **Responsabilidad**: aplicar migraciones evolutivas a la base de datos usando EvolveDB.

---

## 4. Flujos de trabajo

### 4.1 Registro de incidente (desde InvGate)

```
InvGate Webhook
        ↓ (HTTP POST)
IncidentesController.RecepcionarTicketInvgate()
        ↓
IncidenteService.RegistrarIncidenteInvgateAsync()
        ↓
IncidenteRepository.InsertarAsync()                    → TbSitracIncidente
        ↓
SlaRepository.ObtenerParametrosPorEmpresaAsync()       → TbSitracTipoPlazoSla
        ↓
SlaRepository.InsertarPlazosAsync()                    → TbSitracIncidentePlazoSla
        ↓
HitoRepository.CrearAsync()                            → TbSitracHito (Aviso Temprano)
        ↓
Cálculo de prioridad (Smart Routing)
        ↓
Prioridad >= 30 (Crítico): envío inmediato (Email + Teams)
Prioridad <  30 (Normal):  inserción en TbSitracNotificacionLog (Outbox)
```

### 4.2 Ciclo de vida (LifecycleAdvancerService)

```
LifecycleAdvancerService (timer cada minuto)
        ↓
IncidenteService.ProcesarCicloDeVidaAsync()
        ↓
IncidenteRepository.ObtenerIncidentesParaEvaluacionSlaAsync()
        ↓
Para cada incidente que necesita avance de estado:
        ↓
Verifica si el hito Final ya fue enviado (evita el auto-cierre sobre una acción humana)
        ↓
IncidenteRepository.AvanzarEstadoConHitosTransaccionalAsync()
        ↓
Transacción: cierra hito anterior + crea hito siguiente
             + UPDATE TbSitracIncidente + INSERT en historial
        ↓
Si el nuevo estado < 4: encola notificación en el Outbox
```

### 4.3 Consolidación de notificaciones (NotificationDispatcherService)

```
NotificationDispatcherService (timer cada 15 segundos)
        ↓
NotificacionLogRepository.ObtenerPendientesParaEnvioAsync()  → TbSitracNotificacionLog
        ↓
NotificacionService.EnviarResumenConsolidadoAsync()
        ↓
Agrupa por severidad (Críticos, Altos, Medios/Bajos)
        ↓
Envía un único correo/tarjeta con el resumen
        ↓
NotificacionLogRepository.MarcarEnviadaAsync()
```

### 4.4 Escalamiento de SLA (SlaEscalationService)

```
SlaEscalationService (timer cada minuto)
        ↓
HitoRepository.ObtenerPendientesParaEvaluarAsync()          → TbSitracHito
        ↓
ReglaEscalamientoRepository.ObtenerPorTipoHitoAsync()       → TbSitracReglaEscalamiento
        ↓
Calcula minutos restantes vs. umbrales configurados
        ↓
HitoRepository.ReservarNotificacionAsync()  (idempotencia)
        ↓
NotificacionService.EnviarAlertaEscalamientoAsync()  (Email + Teams)
```

### 4.5 Acciones humanas (cierre de ciclo)

```
Cliente HTTP
        ↓ (HTTP POST)
IncidentesController.ConfirmarAviso() / RegistrarInforme() / CerrarIncidente()
        ↓
Métodos correspondientes de IncidenteService
        ↓
Valida el estado actual del incidente
        ↓
IncidenteRepository.*TransaccionalAsync()
        ↓
Transacción: actualiza hito + estado + inserta informe + historial
        ↓
Retorna respuesta exitosa
```

---

## 5. Dependencias entre proyectos

```
Sitrac.Server
    ├──→ Sitrac.Service
    └──→ Sitrac.Repository

Sitrac.Service
    └──→ Sitrac.Repository

Sitrac.Repository
    └──→ Sitrac.DataAccess

Sitrac.DataAccess
    └──→ (ninguna; solo usa System.Data y Microsoft.Data.SqlClient)
```

---

## 6. Modelo de datos

### 6.1 Entidades de base de datos

| Tabla | Descripción |
|-------|-------------|
| `TbSitracIncidente` | Tabla principal de incidentes |
| `TbSitracIncidentePlazoSla` | Plazos SLA asociados a incidentes (snapshot) |
| `TbSitracTipoPlazoSla` | Plantillas de plazos SLA por clasificación de empresa |
| `TbSitracHito` | Hitos regulatorios operativos (Aviso, Preliminar, Final) |
| `TbSitracTipoHito` | Catálogo de tipos de hitos y sus plazos legales |
| `TbSitracInformePreliminar` | Formulario CSIRT y datos DPO (reporte 24/72 h) |
| `TbSitracInformeCierre` | Formulario CSIRT y datos DPO (informe final) |
| `TbSitracRevisionDatosPersonales` | Auditoría de datos personales (DPO) |
| `TbSitracNotificacionLog` | Cola de salida (Outbox) para consolidación |
| `TbSitracReglaEscalamiento` | Umbrales de tiempo para alertas preventivas |
| `TbSitracEventoHistorial` | Historial de cambios de estado de incidentes |

Además existen tablas de referencia (tipos de canal, clasificación, gravedad, estado, etc.).

### 6.2 Modelos C#

| Modelo | Mapea a / Propósito |
|--------|---------------------|
| `SitracIncidente` | `TbSitracIncidente` |
| `SitracIncidentePlazoSla` | `TbSitracIncidentePlazoSla` |
| `Hito` / `HitoResumen` | `TbSitracHito` |
| `InformePreliminar` / `InformeCierre` | `TbSitracInformePreliminar` / `TbSitracInformeCierre` |
| `NotificacionLog` | `TbSitracNotificacionLog` |
| `ReglaEscalamiento` | `TbSitracReglaEscalamiento` |
| `IncidenteSlaInfo` | Vista combinada para evaluación de SLA |
| `IncidenteResumen` | Vista resumida para listados |
| `IncidenteDetalle` | Vista detallada con joins a tablas de referencia |
| `EventoHistorialResumen` | Vista del historial de incidentes |

**DTOs**: `CrearIncidenteRequest`, `ConfirmarAvisoRequest`, `RegistrarInformeRequest`, `CerrarIncidenteRequest`, `ResultadoEnvio`.

---

## 7. Configuración

El sistema se configura mediante `appsettings.json`.

| Sección | Claves |
|---------|--------|
| `ConnectionStrings` | `SitracConnection` (conexión a SQL Server) |
| `SmtpConfig` (email) | `Host`, `Port`, `EnableSsl`, `UserName`, `Password`, `DestinatarioDemo`, `NombreRemitente`, `TimeoutMs`, `MaxReintentos`, `IntervaloEntreEnviosMs` |
| `TeamsConfig` | `Habilitado` (bool), `WebhookUrl`, `UmbralPrioridadMinima` (puntuación mínima para notificar), `TimeoutMs` |
| `FrontendSettings` | `BaseUrl` (URL base del frontend para generar enlaces directos / deep linking) |

---

## 8. Mecanismos de notificación

### Email (MailKit)

- Usa SMTP para enviar alertas individuales, resúmenes consolidados y alertas de escalamiento.
- Implementa reintentos con espaciado configurable (`MaxReintentos`, `IntervaloEntreEnviosMs`).

### Microsoft Teams (HttpClient)

- Envía tarjetas adaptativas (Adaptive Cards) a un webhook configurado.
- Formatea visualmente los resúmenes agrupando incidentes por severidad (colores y emojis).
- Incluye botones de acción (`Action.OpenUrl`) para redirección directa (deep linking).

---

## 9. Servicios en segundo plano

Los tres heredan de `BackgroundService` de .NET y usan `PeriodicTimer`.

| Servicio | Frecuencia | Función |
|----------|-----------|---------|
| `LifecycleAdvancerService` | 1 minuto | Evalúa SLA y avanza estados creando y cerrando hitos de forma transaccional |
| `NotificationDispatcherService` | 15 segundos | Consume la tabla Outbox y consolida envíos para evitar fatiga de alertas |
| `SlaEscalationService` | 1 minuto | Evalúa umbrales de tiempo y dispara alertas preventivas, con idempotencia en base de datos |

---

## 10. Manejo de errores y consistencia de datos

- Las capas implementan manejo de excepciones.
- Los controladores devuelven códigos HTTP adecuados (200, 400, 404, 500).
- Los servicios de notificación retornan objetos `ResultadoEnvio` con éxito/fallo y mensaje de error.
- Las operaciones críticas usan transacciones (`BEGIN TRANSACTION`, `COMMIT`, `ROLLBACK`) para garantizar atomicidad.
- El logging usa `Microsoft.Extensions.Logging` con niveles apropiados y formato estructurado.
- Los DTO de la API usan DataAnnotations (`[Required]`, `[MinLength]`, etc.) para validar los datos regulatorios.

> **Nota**: la autenticación/autorización de los endpoints aún no está implementada (ver [Puntos de extensión](#13-puntos-de-extensión)).

---

## 11. Tecnologías utilizadas

| Tecnología | Uso |
|-----------|-----|
| .NET 10 (LTS) | Framework principal |
| ASP.NET Core Web API | Capa de presentación |
| Dapper | Micro-ORM para acceso a datos |
| MailKit | Envío de emails vía SMTP |
| Microsoft Teams Webhook API | Notificaciones a Teams |
| EvolveDB | Migraciones de base de datos versionadas |
| Microsoft.Extensions.Logging | Logging estructurado |
| Microsoft.Extensions.Options | Gestión de configuración y validación temprana |
| Microsoft.Data.SqlClient | Conexiones a SQL Server |
| System.Security.Cryptography.Pkcs | Firma digital de notificaciones |

---

## 12. Patrones de diseño

| Patrón | Dónde / Cómo |
|--------|--------------|
| Dependency Injection | Uso extensivo mediante `IServiceCollection` |
| Repository | `Sitrac.Repository`, abstrae Dapper |
| Factory | `DbConnectionFactory` para crear conexiones |
| Background Service | Los tres motores asíncronos (Lifecycle, Dispatcher, Escalation) |
| Outbox | Entrega confiable de notificaciones y consolidación |
| Smart Routing | `IncidenteService`: envío inmediato o diferido según prioridad |
| DTO | Transferencia de datos entre capas |
| Unit of Work (implícito) | Transacciones manuales en repositorios (creación de informes, avance de hitos) |
| Strategy | Distintas estrategias de notificación (individual, consolidada, escalamiento) |

---

## 13. Puntos de extensión

1. **Canales de notificación**: agregar nuevos canales implementando `INotificacionService`.
2. **Fuentes de incidentes**: agregar endpoints en `IncidentesController` para otros sistemas.
3. **Lógica de prioridad**: modificar `PrioridadCalculator` para cambiar el algoritmo de puntuación.
4. **Generación de documentos**: integrar librerías como QuestPDF en la capa de servicio para generar reportes oficiales en PDF.
5. **Autenticación**: implementar JWT o integración con Entra ID (Azure AD) para proteger los endpoints.
6. **Esquema de base de datos**: añadir tablas y actualizar repositorios mediante un nuevo script de EvolveDB.
