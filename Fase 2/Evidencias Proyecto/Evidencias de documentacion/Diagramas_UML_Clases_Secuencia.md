# Diagramas UML — SITRAC-ANCI (clases y secuencia)

Complementan los diagramas de casos de uso y de componentes que ya existen en el repositorio. Los diagramas están en Mermaid, por lo que GitHub los renderiza directamente.

> **Nota de validación:** los nombres de clases de dominio y sus atributos son una primera versión basada en el README y en la arquitectura. Antes de entregar, contrastarlos con los modelos reales de `Sitrac.Repositorio/` y con `Diccionario_de_Datos_SITRAC.xlsx`.

---

## 1. Diagrama de clases — dominio

```mermaid
classDiagram
    class Incidente {
        +int Id
        +string IdExterno
        +string Titulo
        +string Clasificacion
        +string Prioridad
        +string Estado
        +DateTime FechaConocimiento
    }
    class PlazoSla {
        +string TipoHito
        +int HorasPlazo
    }
    class Hito {
        +string Tipo
        +DateTime FechaLimite
        +DateTime FechaCumplimiento
        +string Estado
    }
    class Informe {
        +char Tipo
        +DateTime FechaRegistro
        +string Contenido
    }
    class RevisionDatosPersonales {
        +bool ContieneDatosPersonales
        +string Observacion
    }
    class EventoHistorial {
        +DateTime Fecha
        +string Accion
        +string Detalle
    }
    class NotificacionLog {
        +string Canal
        +string Estado
        +DateTime FechaEnvio
    }
    class ReglaEscalamiento {
        +string TipoHito
        +int MinutosAntes
    }
    class Usuario {
        +int Id
        +string NombreUsuario
        +string HashContrasena
        +string Perfil
    }

    Incidente "1" --> "*" PlazoSla : snapshot de plazos
    Incidente "1" --> "*" Hito : tiene
    Incidente "1" --> "*" Informe : registra
    Incidente "1" --> "*" EventoHistorial : genera
    Incidente "1" --> "*" NotificacionLog : encola
    Informe "1" --> "0..*" RevisionDatosPersonales : se revisa
    Hito "*" --> "0..1" ReglaEscalamiento : evalúa
    EventoHistorial "*" --> "0..1" Usuario : ejecutado por
```

## 2. Diagrama de clases — servicios y capas

```mermaid
classDiagram
    class IncidentesController {
        +RecibirWebhook()
        +Listar()
        +ObtenerDetalle()
        +ObtenerHistorial()
        +ConfirmarAviso()
        +RegistrarInforme()
        +Cerrar()
    }
    class AuthController {
        +Login()
        +Register()
    }
    class BackgroundService {
        <<abstract>>
    }
    class AvanzadorCicloVidaServicio {
        +Frecuencia 1 minuto
    }
    class DespachadorNotificacionServicio {
        +Frecuencia 15 segundos
    }
    class EscalamientoSlaServicio {
        +Frecuencia 1 minuto
    }
    class Servicio {
        <<capa Sitrac.Servicio>>
        reglas de negocio
        calculo de prioridad
        hitos y SLA
        notificaciones
    }
    class Repositorio {
        <<capa Sitrac.Repositorio - Dapper>>
        incidentes
        hitos
        SLA
        outbox
        escalamiento
        usuarios
    }
    class FabricaConexion {
        <<Sitrac.AccesoDatos>>
        +CrearConexion()
    }
    class Notificador {
        <<SMTP MailKit / Teams Webhook>>
    }

    IncidentesController --> Servicio
    AuthController --> Servicio
    BackgroundService <|-- AvanzadorCicloVidaServicio
    BackgroundService <|-- DespachadorNotificacionServicio
    BackgroundService <|-- EscalamientoSlaServicio
    AvanzadorCicloVidaServicio --> Servicio
    DespachadorNotificacionServicio --> Servicio
    EscalamientoSlaServicio --> Servicio
    Servicio --> Repositorio
    Servicio --> Notificador
    Repositorio --> FabricaConexion
```

---

## 3. Diagrama de secuencia — recepción de un incidente desde INVGATE

```mermaid
sequenceDiagram
    autonumber
    participant INV as INVGATE
    participant API as IncidentesController
    participant SRV as Servicio
    participant REP as Repositorio (Dapper)
    participant BD as SQL Server (DbSitrac)
    participant NOT as Notificador (SMTP/Teams)

    INV->>API: POST /api/Incidentes/invgate/webhook (X-Api-Key)
    alt API key inválida
        API-->>INV: 401 No autorizado
    else API key válida
        API->>SRV: Procesar incidente recibido
        SRV->>SRV: Calcular prioridad y plazos SLA
        SRV->>REP: Registrar incidente, hitos e historial
        REP->>BD: INSERT en transacción
        BD-->>REP: OK
        REP-->>SRV: Incidente registrado
        alt Incidente crítico
            SRV->>NOT: Notificar de inmediato a responsables
        else Incidente no crítico
            SRV->>REP: Encolar notificación en Outbox
        end
        SRV-->>API: Resultado
        API-->>INV: 200 OK
    end
```

## 4. Diagrama de secuencia — gestión del incidente por el usuario

```mermaid
sequenceDiagram
    autonumber
    actor USR as Usuario (ciberseguridad)
    participant FE as Sitrac.Frontend
    participant API as API (Controllers)
    participant SRV as Servicio
    participant BD as SQL Server

    USR->>FE: Ingresa credenciales
    FE->>API: POST /api/Auth/login
    API-->>FE: JWT
    USR->>FE: Abre la cola de incidentes
    FE->>API: GET /api/Incidentes (JWT)
    API-->>FE: Incidentes activos

    USR->>FE: Confirma aviso temprano
    FE->>API: POST /api/Incidentes/{id}/confirmar-aviso
    API->>SRV: Confirmar aviso
    SRV->>BD: Registrar evento y crear hito de informe preliminar
    API-->>FE: OK

    USR->>FE: Registra informe preliminar (P) y luego final (F)
    FE->>API: POST /api/Incidentes/{id}/informe
    API->>SRV: Registrar informe
    SRV->>BD: Guardar informe e historial
    API-->>FE: OK

    USR->>FE: Cierra el incidente
    FE->>API: POST /api/Incidentes/{id}/cerrar
    API->>SRV: Validar informe final
    alt Falta el informe final
        API-->>FE: Error: no se puede cerrar
    else Informe final registrado
        SRV->>BD: Cambiar estado a cerrado
        API-->>FE: OK
    end
```

## 5. Diagrama de secuencia — procesos en segundo plano (ciclo de vida, Outbox y escalamiento)

```mermaid
sequenceDiagram
    autonumber
    participant CV as AvanzadorCicloVidaServicio (1 min)
    participant ES as EscalamientoSlaServicio (1 min)
    participant DS as DespachadorNotificacionServicio (15 s)
    participant BD as SQL Server
    participant NOT as SMTP / Teams
    actor RESP as Responsable

    loop Cada minuto
        CV->>BD: Buscar incidentes activos con hitos vencidos
        BD-->>CV: Incidentes
        CV->>BD: Avanzar estado e hito (transacción)
    end

    loop Cada minuto
        ES->>BD: Evaluar reglas de escalamiento
        ES->>BD: Reservar alerta (idempotente) y encolar en Outbox
    end

    loop Cada 15 segundos
        DS->>BD: Leer Outbox (TbSitracNotificacionLog)
        DS->>NOT: Enviar resumen consolidado
        NOT-->>RESP: Correo / mensaje en Teams
        DS->>BD: Marcar notificación como enviada
    end
```
