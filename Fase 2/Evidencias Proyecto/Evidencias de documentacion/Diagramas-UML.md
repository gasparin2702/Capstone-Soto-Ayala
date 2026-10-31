# Diagramas UML — SITRAC

**Proyecto CAPSTONE 2026 PTY4614 · DuocUC sede Valparaíso · Empresa mandante: UltraPort**
**Autores:** Rodrigo Ayala López, Benjamín Soto Aguilar


##Diagrama de casos de uso

```mermaid
flowchart LR
    INV(["INVGATE<br/>sistema externo"])
    CIB(["Equipo de<br/>Ciberseguridad"])
    RESP(["Responsable de UltraPort<br/>Enzo · Gonzalo · Ángelo"])
    ADM(["Administrador<br/>del sistema"])
    TMP(["Temporizadores<br/>del sistema"])

    subgraph SIS["SITRAC-ANCI"]
        UC1(["CU-01 Recibir incidente<br/>por webhook"])
        UC2(["CU-02 Registrar incidente por<br/>correo, Teams o manual"])
        UC3(["CU-03 Calcular prioridad<br/>y plazos SLA"])
        UC4(["CU-04 Notificar a responsables<br/>Smart Routing y Outbox"])
        UC5(["CU-05 Alertar antes del<br/>vencimiento de un hito"])
        UC6(["CU-06 Confirmar aviso<br/>temprano a la ANCI"])
        UC7(["CU-07 Descartar como<br/>falso positivo"])
        UC8(["CU-08 Registrar comunicación<br/>y respuesta de la ANCI"])
        UC9(["CU-09 Registrar informe<br/>preliminar o final"])
        UC10(["CU-10 Consultar incidentes<br/>e historial"])
        UC11(["CU-11 Configurar plazos<br/>y reglas de escalamiento"])
        UC12(["CU-12 Revisar datos<br/>personales"])
        UC13(["CU-13 Cerrar incidente"])
        UC14(["CU-14 Avanzar ciclo de vida<br/>y crear hitos"])
    end

    INV --- UC1
    CIB --- UC2
    CIB --- UC10
    RESP --- UC6
    RESP --- UC7
    RESP --- UC8
    RESP --- UC9
    RESP --- UC10
    RESP --- UC12
    RESP --- UC13
    ADM --- UC11
    TMP --- UC5
    TMP --- UC14

    UC1 -.->|"«include»"| UC3
    UC2 -.->|"«include»"| UC3
    UC3 -.->|"«include»"| UC4
    UC5 -.->|"«include»"| UC4
    UC14 -.->|"«include»"| UC4
    UC9 -.->|"«include»"| UC8
```

| Caso de uso | Descripción | Requisitos | Estado según `ARCHITECTURE.md` |
| --- | --- | --- | --- |
| CU-01 | Recibe el ticket de INVGATE por `POST /api/incidentes/invgate/webhook` y lo registra. | RF-01 | Implementado |
| CU-02 | Registra incidentes que llegan por correo (ANCI o equipo de ciberseguridad), Teams o ingreso manual. Los canales existen en el catálogo. | RF-10, RF-11 | Pendiente: hoy solo existe el webhook |
| CU-03 | Calcula la prioridad y los tres plazos (aviso 3 h, preliminar 72 h o 24 h, final 15 días) según la clasificación de la empresa. | RF-12, RF-13 | Implementado |
| CU-04 | Envía el aviso por correo y Teams: inmediato si la prioridad es 30 o más, diferido y consolidado por Outbox en los demás casos. | RF-02 | Implementado |
| CU-05 | Envía alertas preventivas cuando faltan los minutos definidos en las reglas de escalamiento. | RF-02 | Implementado |
| CU-06 | Un responsable confirma el aviso (`POST .../confirmar-aviso`): cierra el hito de aviso y crea el del informe preliminar. | RF-03, RF-05 | Implementado |
| CU-07 | Descarta el incidente con justificación y detiene el flujo. La tabla existe. | RF-04 | Pendiente: sin endpoint |
| CU-08 | Registra la comunicación con la ANCI y su respuesta. | RF-06 | Parcial: tabla creada, sin endpoint propio |
| CU-09 | Registra el informe preliminar o final con datos CSIRT y DPO (`POST .../informe`). | RF-07, RF-08, RF-09 | Implementado |
| CU-10 | Lista incidentes activos, muestra el detalle y su historial. | RNF-06 | Implementado |
| CU-11 | Cambia plazos y reglas de escalamiento modificando los parámetros en base de datos. | RNF-05 | Parcial: sin interfaz |
| CU-12 | Registra la auditoría de datos personales de un incidente. | RF-14 | Pendiente: sin endpoint |
| CU-13 | Cierra definitivamente el incidente (`POST .../cerrar`). | RF-09 | Implementado |
| CU-14 | `LifecycleAdvancerService` avanza estados y crea hitos cada minuto. | RNF-01 | Implementado |

---

## 3. Diagramas de clases (modelo de dominio)

Reflejan las tablas creadas en `V0001` a `V0006`, sin el prefijo `TbSitrac`. Se omiten las columnas de auditoría (`AuditNotDeleted`, `AuditCreateDate`, `AuditLastUpdateDate`, `AuditUserLastUpdate`), presentes en todas las tablas.

### 3.1 Núcleo: incidente, plazos, hitos y notificaciones

```mermaid
classDiagram
    direction LR

    class Incidente {
        +long IdIncidente
        +datetime FechaTomaConocimiento
        +datetime FechaRegistroSistema
        +string CodigoTicketInvgate
        +bool IndEsIncidenteReal
        +string DescripcionSistemaAfectado
        +string DescripcionVectorAtaque
        +string DescripcionOrigenDeteccion
    }
    class TipoCanalEntrada {
        +string NombreTipoCanalEntrada
    }
    class TipoDatosPersonales {
        +string NombreTipoDatosPersonales
    }
    class TipoClasificacionEmpresa {
        +string NombreTipoClasificacionEmpresa
    }
    class TipoGravedad {
        +string NombreTipoGravedad
        +int OrdenTipoGravedad
    }
    class TipoEstadoIncidente {
        +string NombreTipoEstadoIncidente
        +int OrdenTipoEstadoIncidente
    }
    class TipoPlazoSla {
        +int HorasAvisoTemprano
        +int HorasInformePreliminar
        +int DiasInformeFinal
    }
    class IncidentePlazoSla {
        +datetime FechaTomaConocimiento
        +datetime FechaLimiteAvisoTemprano
        +datetime FechaLimiteInformePreliminar
        +datetime FechaLimiteInformeFinal
    }
    class TipoHito {
        +string NombreTipoHito
        +int HorasLegalesBase
    }
    class Hito {
        +long IdHito
        +int Secuencia
        +datetime FechaBase
        +datetime FechaLimite
        +string Estado
        +datetime FechaCumplimiento
        +string ReferenciaExterna
        +datetime ProximaNotificacionAt
        +int ConteoNotificaciones
        +datetime SilenciadoHasta
    }
    class ReglaEscalamiento {
        +int TiempoRestanteMinutos
        +int RepetirCadaMinutos
        +bool NotificarEmail
        +bool NotificarTeams
        +string Descripcion
    }
    class NotificacionLog {
        +int NumeroNotificacion
        +string Destinatario
        +string Canal
        +string Estado
        +int Intentos
        +datetime ProximoReintentoAt
        +datetime FechaProgramada
        +datetime FechaEnvio
        +string MensajeError
    }
    class Responsable {
        +string NombreResponsable
        +string EmailResponsable
        +string TelefonoResponsable
        +string DescripcionRolOrganizacional
        +bool IndEsDelegadoCiberseguridad
    }
    class EventoHistorial {
        +string CodigoTicketInvgate
        +datetime FechaCambio
        +string Comentario
    }
    class TipoAmbitoCambio {
        +string NombreAmbitoCambio
    }

    Incidente "*" --> "1" TipoCanalEntrada
    Incidente "*" --> "1" TipoDatosPersonales
    Incidente "*" --> "1" TipoClasificacionEmpresa
    Incidente "*" --> "1" TipoGravedad
    Incidente "*" --> "1" TipoEstadoIncidente
    TipoPlazoSla "*" --> "1" TipoClasificacionEmpresa
    Incidente "1" -- "1" IncidentePlazoSla : plazos calculados
    IncidentePlazoSla "*" --> "1" TipoPlazoSla : snapshot de
    Incidente "1" *-- "0..*" Hito
    Hito "*" --> "1" TipoHito
    ReglaEscalamiento "*" --> "1" TipoHito
    Hito "1" o-- "0..*" NotificacionLog : cola Outbox
    Incidente "1" o-- "0..*" EventoHistorial
    EventoHistorial "*" --> "1" TipoAmbitoCambio
    EventoHistorial "*" --> "1" Responsable : cambia
```

Notas de lectura:
- Los tres plazos legales se guardan al registrar el incidente en `IncidentePlazoSla`. Los hitos (`Hito`) se crean de forma progresiva: primero el de aviso temprano y luego los siguientes al avanzar el ciclo de vida.
- `EventoHistorial` también referencia estados de incidente, estados de revisión de datos personales y gravedades anteriores y nuevas.
- `NotificacionLog` guarda el destinatario como texto y no se relaciona con `Responsable`.

### Comunicaciones, informes y revisión de datos personales

```mermaid
classDiagram
    direction LR

    class Incidente {
        +long IdIncidente
    }
    class Responsable {
        +string NombreResponsable
    }
    class TipoComunicacion {
        +string NombreTipoComunicacion
        +string CodigoSitrac
    }
    class Comunicacion {
        +datetime FechaHora
        +string ReferenciaArchivoAdjunto
    }
    class Aprobacion {
        +string DescripcionEtapa
        +datetime FechaHora
        +string Comentario
        +bool IndEsAprobacionValida
    }
    class FalsoPositivo {
        +string DescripcionJustificacion
        +datetime FechaRegistro
    }
    class RevisionDatosPersonales {
        +datetime FechaAuditoria
        +datetime FechaNotificaLegal
        +datetime FechaNotificaDpo
    }
    class TipoEstadoRevisionDp {
        +string NombreTipoEstadoRevisionDp
    }
    class RevisionDatosPersonalesHistorial {
        +datetime FechaCambio
    }
    class InformePreliminar {
        +datetime FechaEstimadaInicio
        +string IndiciosOcurrencia
        +string ActivosAfectados
        +string EvidenciaDisponible
        +string DescripcionIocs
        +string DescripcionMedidasMitigacion
        +string HayDatosPersonales
        +datetime FechaEnvio
    }
    class InformeCierre {
        +string DescripcionDetallada
        +string TipoAmenazaCausaPrincipal
        +string MedidasMitigacionAplicadas
        +string VulnerabilidadesExplotadas
        +string HayDatosPersonales
        +datetime FechaEnvio
    }
    class TipoClasificacionArt27 {
        +string NombreTipoClasificacionArt27
    }
    class TipoInformeCierre {
        +string NombreTipoInformeCierre
    }
    class TipoDocumento {
        +string NombreTipoDocumento
    }
    class InformePreliminarDocumento {
        +string NombreArchivo
        +string ReferenciaArchivo
        +datetime FechaCarga
    }
    class InformeCierreDocumento {
        +string NombreArchivo
        +string ReferenciaArchivo
        +datetime FechaCarga
    }

    Incidente "1" o-- "0..*" Comunicacion
    Comunicacion "*" --> "1" TipoComunicacion
    Comunicacion "*" --> "1" Responsable : envía
    Incidente "1" o-- "0..*" Aprobacion
    Aprobacion "*" --> "1" Responsable
    Aprobacion "*" --> "0..1" Comunicacion
    Incidente "1" -- "0..1" FalsoPositivo
    FalsoPositivo "*" --> "1" Responsable : registra
    Incidente "1" -- "0..1" RevisionDatosPersonales
    RevisionDatosPersonales "*" --> "1" TipoEstadoRevisionDp
    RevisionDatosPersonales "*" --> "0..1" Responsable : valida
    Incidente "1" o-- "0..*" RevisionDatosPersonalesHistorial
    Incidente "1" o-- "0..*" InformePreliminar
    Incidente "1" o-- "0..*" InformeCierre
    InformePreliminar "*" --> "0..1" Comunicacion
    InformePreliminar "*" --> "0..1" TipoClasificacionArt27
    InformeCierre "*" --> "0..1" Comunicacion
    InformeCierre "*" --> "1" TipoInformeCierre
    InformeCierre "*" --> "1" Responsable : confirma
    InformePreliminar "1" o-- "0..*" InformePreliminarDocumento
    InformeCierre "1" o-- "0..*" InformeCierreDocumento
    InformePreliminarDocumento "*" --> "1" TipoDocumento
    InformeCierreDocumento "*" --> "1" TipoDocumento
```

---

## Diagramas de secuencia

### Registro de un incidente desde INVGATE

```mermaid
sequenceDiagram
    autonumber
    participant INV as INVGATE
    participant CTL as IncidentesController
    participant SVC as IncidenteService
    participant REP as Repositorios
    participant DB as SQL Server
    participant NOT as NotificacionService
    participant EXT as SMTP y Teams

    INV->>CTL: POST /api/incidentes/invgate/webhook
    CTL->>SVC: RegistrarIncidenteInvgateAsync(request)
    SVC->>REP: IncidenteRepository.InsertarAsync
    REP->>DB: INSERT Incidente
    SVC->>REP: SlaRepository.ObtenerParametrosPorEmpresaAsync
    REP->>DB: SELECT TipoPlazoSla
    SVC->>REP: SlaRepository.InsertarPlazosAsync
    REP->>DB: INSERT IncidentePlazoSla
    SVC->>REP: HitoRepository.CrearAsync (Aviso Temprano)
    REP->>DB: INSERT Hito
    SVC->>SVC: PrioridadCalculator calcula la prioridad

    alt Prioridad 30 o más (crítico)
        SVC->>NOT: Envío inmediato
        NOT->>EXT: Correo y tarjeta de Teams
    else Prioridad menor a 30
        SVC->>REP: NotificacionLogRepository (cola Outbox)
        REP->>DB: INSERT NotificacionLog (Pendiente)
    end

    CTL-->>INV: Respuesta HTTP
```

### Servicios en segundo plano: ciclo de vida, escalamiento y Outbox

```mermaid
sequenceDiagram
    autonumber
    participant LIF as LifecycleAdvancerService
    participant ESC as SlaEscalationService
    participant DIS as NotificationDispatcherService
    participant SVC as IncidenteService
    participant NOT as NotificacionService
    participant REP as Repositorios
    participant DB as SQL Server
    participant EXT as SMTP y Teams

    loop Cada 1 minuto
        LIF->>SVC: ProcesarCicloDeVidaAsync
        SVC->>REP: Obtener incidentes para evaluación SLA
        SVC->>REP: AvanzarEstadoConHitosTransaccionalAsync
        REP->>DB: Cierra hito, crea hito siguiente, actualiza estado, inserta historial
        SVC->>REP: Encolar notificación en Outbox
    end

    loop Cada 1 minuto
        ESC->>REP: HitoRepository.ObtenerPendientesParaEvaluarAsync
        ESC->>REP: ReglaEscalamientoRepository.ObtenerPorTipoHitoAsync
        ESC->>ESC: Compara minutos restantes con los umbrales
        ESC->>REP: HitoRepository.ReservarNotificacionAsync (idempotencia)
        ESC->>NOT: EnviarAlertaEscalamientoAsync
        NOT->>EXT: Correo y Teams
    end

    loop Cada 15 segundos
        DIS->>REP: NotificacionLogRepository.ObtenerPendientesParaEnvioAsync
        DIS->>NOT: EnviarResumenConsolidadoAsync (agrupa por severidad)
        NOT->>EXT: Un solo correo o tarjeta con el resumen
        DIS->>REP: MarcarEnviadaAsync
    end
```

###  Confirmación del aviso temprano por un responsable

```mermaid
sequenceDiagram
    autonumber
    actor RESP as Responsable
    participant FE as Sitrac.Frontend
    participant CTL as IncidentesController
    participant SVC as IncidenteService
    participant REP as IncidenteRepository
    participant DB as SQL Server

    RESP->>FE: Abre el enlace directo del aviso (Teams o correo)
    FE->>CTL: GET /api/incidentes/{id}
    CTL-->>FE: Detalle del incidente
    RESP->>FE: Confirma el aviso
    FE->>CTL: POST /api/incidentes/{id}/confirmar-aviso
    CTL->>SVC: ConfirmarAviso(id, request)
    SVC->>SVC: Valida el estado actual del incidente
    SVC->>REP: Operación transaccional
    REP->>DB: BEGIN TRANSACTION
    REP->>DB: Cierra hito de aviso y crea hito de informe preliminar
    REP->>DB: UPDATE estado a Aviso enviado e INSERT historial
    REP->>DB: COMMIT
    CTL-->>FE: 200 OK
```

---

##Diagrama de componentes

```mermaid
flowchart LR
    subgraph EXT["Sistemas externos"]
        INV["INVGATE<br/>Service Desk"]
        SMTP["Servidor SMTP<br/>MailKit"]
        TEAMS["Microsoft Teams<br/>webhook"]
    end

    subgraph UP["Servidores UltraPort · sin exposición a internet"]
        FE["Sitrac.Frontend<br/>HTML · CSS · JS"]

        subgraph SRV["Sitrac.Server · API REST"]
            CTL["IncidentesController"]
        end

        subgraph SVC["Sitrac.Service"]
            ISV["IncidenteService"]
            NSV["NotificacionService"]
            PRI["PrioridadCalculator"]
            BG["Servicios en segundo plano<br/>Lifecycle · SlaEscalation · Dispatcher"]
        end

        REP["Sitrac.Repository<br/>Dapper"]
        DA["Sitrac.DataAccess<br/>DbConnectionFactory"]
        DB[("SQL Server")]
        MIG["Sitrac.DbMigration<br/>Evolve V0001 a V0006"]
    end

    INV -->|"webhook"| CTL
    FE --> CTL
    CTL --> ISV
    CTL --> REP
    ISV --> PRI
    ISV --> NSV
    ISV --> REP
    BG --> ISV
    BG --> NSV
    BG --> REP
    NSV --> SMTP
    NSV --> TEAMS
    REP --> DA
    DA --> DB
    MIG -->|"esquema y catálogos"| DB
```

| Componente | Responsabilidad | Tecnología |
| --- | --- | --- |
| `Sitrac.Frontend` | Registro, seguimiento y pantallas de acción. | HTML, CSS, JavaScript |
| `Sitrac.Server` | Endpoints REST: webhook, consulta, confirmar aviso, informe y cierre. | ASP.NET Core |
| `Sitrac.Service` | Reglas de negocio, prioridad, ciclo de vida, escalamiento y notificaciones. | C#, `BackgroundService` |
| `Sitrac.Repository` | Repositorios de incidentes, hitos, SLA, Outbox y reglas de escalamiento. | Dapper |
| `Sitrac.DataAccess` | Fábrica de conexiones a SQL Server. | Microsoft.Data.SqlClient |
| `Sitrac.DbMigration` | Scripts versionados de esquema y catálogos. | Evolve, T-SQL |

---

## Trazabilidad entre diagramas y requisitos

| Diagrama | Requisitos que cubre |
| --- | --- |
| Casos de uso | RF-01 a RF-14, RNF-05, RNF-06 |
| Clases 3.1 | RF-12, RF-13, RNF-01, RNF-05, RNF-06 |
| Clases 3.2 | RF-04, RF-05 a RF-09, RF-14 |
| Secuencia 4.1 | RF-01, RF-02, RF-13 |
| Secuencia 4.2 | RF-02, RNF-01, RNF-03 |
| Secuencia 4.3 | RF-03, RF-05 |
| Componentes | RNF-04, RNF-07, restricción de despliegue sin exposición a internet |

