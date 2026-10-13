# Sitrac-Archi Architecture Reference

## Overview
Sitrac-Archi es un sistema de gestión de incidentes de seguridad informática basado en la Ley N° 21.663 (Marco de Ciberseguridad). El sistema registra, procesa y notifica incidentes de seguridad a través de múltiples canales (email y Microsoft Teams) según su prioridad y clasificación.

## Arquitectura General

El sistema sigue una arquitectura en capas típica de aplicaciones .NET:

```
┌─────────────────────────────────────┐
│          Presentación Layer         │
│  (Sitrac.Server - API REST)         │
└─────────────────────┬───────────────┘
                      │
┌─────────────────────▼───────────────┐
│        Aplicación Layer             │
│  (Sitrac.Service - Lógica de Negocio)│
└─────────────────────┬───────────────┘
                      │
┌─────────────────────▼───────────────┐
│        Acceso a Datos Layer         │
│  (Sitrac.Repository - Repositorios) │
└─────────────────────┬───────────────┘
                      │
┌─────────────────────▼───────────────┐
│         Capa de Datos               │
│  (Sitrac.DataAccess - Factory)      │
└─────────────────────┬───────────────┘
                      │
┌─────────────────────▼───────────────┐
│         Base de Datos               │
│  (SQL Server - DbSitrac)            │
└─────────────────────────────────────┘
```

## Componentes Principales

### 1. Sitrac.Server (Capa de Presentación)
- **Tipo**: API Web ASP.NET Core
- **Responsabilidad**: Exponer endpoints REST para interactuar con el sistema
- **Endpoints principales**:
  - `POST /api/incidentes/invgate/webhook` - Recibe incidentes desde InvGate
  - `GET /api/incidentes` - Obtiene lista de incidentes activos
  - `GET /api/incidentes/{id}` - Obtiene detalle de un incidente específico
  - `GET /api/incidentes/{id}/historial` - Obtiene historial de un incidente

### 2. Sitrac.Service (Capa de Lógica de Negocio)
- **Tipo**: Biblioteca de Clases .NET
- **Responsabilidad**: Contiene la lógica de negocio del sistema
- **Servicios principales**:
  - `IncidenteService`: Implementa `IIncidenteService` - Lógica principal de incidentes
  - `NotificacionService`: Implementa `INotificacionService` - Envío de notificaciones
  - `LifecycleAdvancerService`: Servicio background que ejecuta el ciclo de vida de incidentes cada minuto
  - `PrioridadCalculator`: Calculadora estática de prioridad de incidentes

### 3. Sitrac.Repository (Capa de Acceso a Datos)
- **Tipo**: Biblioteca de Clases .NET
- **Responsabilidad**: Interactuar con la base de datos usando Dapper
- **Repositorios principales**:
  - `IncidenteRepository`: Implementa `IIncidenteRepository` - Operaciones CRUD y consultas de incidentes
  - `SlaRepository`: Implementa `ISlaRepository` - Gestión de parámetros SLA

### 4. Sitrac.DataAccess (Factory de Conexiones)
- **Tipo**: Biblioteca de Clases .NET
- **Responsabilidad**: Crear conexiones a la base de datos
- **Componentes**:
  - `DbConnectionFactory`: Implementa `IDbConnectionFactory` - Factory para crear `SqlConnection`

### 5. Sitrac.DbMigration (Migraciones de Base de Datos)
- **Tipo**: Proyecto de Consola .NET
- **Responsabilidad**: Aplicar migraciones evolutivas a la base de datos usando EvolveDB

## Flujo de Trabajo Principal

### 1. Registro de Incidente (Desde InvGate)
```
InvGate Webhook 
        ↓ (HTTP POST)
IncidentesController.RecepcionarTicketInvgate()
        ↓
IncidenteService.RegistrarIncidenteInvgateAsync()
        ↓
IncidenteRepository.InsertarAsync() → TbAnciIncidente
        ↓
SlaRepository.ObtenerParametrosPorEmpresaAsync() → TbAnciTipoPlazoSla
        ↓
SlaRepository.InsertarPlazosAsync() → TbAnciIncidentePlazoSla
        ↓
NotificacionService.EnviarAlertaNuevoIncidenteAsync() → Email
        ↓
NotificacionService.EnviarAlertaTeamsAsync() → Microsoft Teams (si está configurado y supera umbral)
        ↓
Retorna ID del incidente creado
```

### 2. Procesamiento del Ciclo de Vida (Servicio Background)
```
LifecycleAdvancerService (Timer cada minuto)
        ↓
IncidenteService.ProcesarCicloDeVidaAsync()
        ↓
IncidenteRepository.ObtenerIncidentesParaEvaluacionSlaAsync()
        ↓
Para cada incidente que necesita avance de estado:
        ↓
IncidenteRepository.AvanzarEstadoConHistorialAsync() 
        ↓ 
(Transacción: UPDATE TbAnciIncidente + INSERT INTO TbAnciEventoHistorial)
        ↓
Si el nuevo estado < 4 (no cerrado):
        ↓
NotificacionService.EnviarAlertaTeamsAsync() → Microsoft Teams
```

### 3. Consulta de Información
```
Cliente HTTP
        ↓ (HTTP GET)
IncidentesController.ObtenerIncidentesActivos() / ObtenerDetalleIncidente() / ObtenerHistorialIncidente()
        ↓
IncidenteService.ObtenerIncidentesActivosAsync() / ObtenerDetalleIncidenteAsync() / ObtenerHistorialIncidenteAsync()
        ↓
IncidenteRepository.* métodos correspondientes
        ↓
Retorna datos al controlador
        ↓
Controlador devuelve JSON al cliente
```

## Dependencias Entre Proyectos

```
Sitrac.Server
    ├──→ Sitrac.Service
    └──→ Sitrac.Repository

Sitrac.Service
    └──→ Sitrac.Repository

Sitrac.Repository
    └──→ Sitrac.DataAccess

Sitrac.DataAccess
    └──→ (Ninguna - solo usa System.Data y Microsoft.Data.SqlClient)
```

## Modelos de Datos Principales

### Entidades de Base de Datos
- `TbAnciIncidente` - Tabla principal de incidentes
- `TbAnciIncidentePlazoSla` - Plazos SLA asociados a incidentes
- `TbAnciTipoPlazoSla` - Plantillas de plazos SLA por clasificación de empresa
- `TbAnciEventoHistorial` - Historial de cambios de estado de incidentes
- Tablas de referencia (Tipos de Canal, Clasificación, Gravedad, Estado, etc.)

### Modelos C# Correspondientes
- `AnciIncidente` - Mapea a TbAnciIncidente
- `AnciIncidentePlazoSla` - Mapea a TbAnciIncidentePlazoSla
- `IncidenteSlaInfo` - Vista combinada para evaluación SLA
- `IncidenteResumen` - Vista resumida para listas
- `IncidenteDetalle` - Vista detallada con joins a tablas de referencia
- `EventoHistorialResumen` - Vista del historial de incidentes
- DTOs: `CrearIncidenteRequest`, `ResultadoEnvio`

## Configuración

El sistema se configura mediante `appsettings.json`:

### ConnectionStrings
- `SitracConnection`: Conexión a SQL Server

### SmtpConfig (Para notificaciones por email)
- Host, Port, EnableSsl, UserName, Password
- DestinatarioDemo, NombreRemitente
- TimeoutMs, MaxReintentos, IntervaloEntreEnviosMs

### TeamsConfig (Para notificaciones por Microsoft Teams)
- Habilitado (boolean)
- WebhookUrl (URL del webhook de Teams)
- UmbralPrioridadMinima (puntuación mínima para enviar notificación)
- TimeoutMs

## Mecanismos de Notificación

### Email (MailKit)
- Usa SMTP para enviar alertas de nuevos incidentes
- Implementa reintentos con espaciado configurables
- Formato HTML con tabla de información esencial

### Microsoft Teams (HttpClient)
- Envía tarjetas adaptativas Adaptive Card a webhook configurado
- Solo envía si la prioridad supera el umbral configurado
- Incluye hechos como ticket, prioridad, gravedad, clasificación, estado, próximo hito y sistema

## Servicios Background

### LifecycleAdvancerService
- Hereda de `BackgroundService` de .NET
- Usa `PeriodicTimer` para ejecutar cada minuto
- Llama a `IncidenteService.ProcesarCicloDeVidaAsync()`
- Registra logs de ejecución y errores

## Seguridad y Manejo de Errores

- Todas las capas implementan manejo de excepciones apropiado
- Los controladores devuelven códigos de estado HTTP adecuados (200, 404, 500)
- Los servicios de notificación retornan objetos `ResultadoEnvio` con éxito/fallo y mensaje de error
- Las transacciones de base de datos usan `BEGIN TRANSACTION`, `COMMIT` y `ROLLBACK` apropiados
- Los registros de log usan el framework de logging de Microsoft con niveles apropiados

## Tecnologías Utilizadas

- **.NET 8.0** - Framework principal
- **ASP.NET Core Web API** - Para la capa de presentación
- **Dapper** - ORM micro para acceso a datos
- **MailKit** - Para envío de emails vía SMTP
- **Microsoft Teams Webhook API** - Para notificaciones a Teams
- **EvolveDB** - Para migraciones de base de datos
- **Microsoft.Extensions.Logging** - Para logging estructurado
- **Microsoft.Extensions.Options** - Para gestión de configuración
- **System.Data.SqlClient** - Para conexiones a SQL Server

## Patrones de Diseño Observados

- **Dependency Injection** - Usado extensivamente a través de `IServiceCollection`
- **Repository Pattern** - Implementado en Sitrac.Repository
- **Factory Pattern** - `DbConnectionFactory` para creación de conexiones
- **Background Service** - `LifecycleAdvancerService` para tareas periódicas
- **DTO Pattern** - Objetos de transferencia de datos entre capas
- **Strategy Pattern** implícito - Diferentes estrategias de notificación (email vs Teams)
- **Decorator Pattern** implícito - Servicios que agregan funcionalidad (reintentos, logging)

## Puntos de Extensión

1. **Tipos de Notificación**: Agregar nuevos canales de notificación implementando `INotificacionService`
2. **Fuentes de Incidentes**: Agregar nuevos endpoints en `IncidentesController` para otros sistemas
3. **Lógica de Prioridad**: Modificar `PrioridadCalculator` para cambiar el algoritmo de puntuación
4. **Reglas de Negocio**: Extender `IncidenteService` para nuevas reglas de procesamiento
5. **Esquema de Base de Datos**: Añadir nuevas tablas y actualizar los repositorios correspondiente