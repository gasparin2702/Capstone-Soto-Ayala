# Product Vision — SITRAC-ANCI

**Sistema de Trazabilidad y Notificación Automática de Incidentes de Ciberseguridad a la ANCI**

Proyecto CAPSTONE 2026 (PTY4614) · DuocUC, sede Valparaíso · Empresa mandante: UltraPort
Product Owner / Gestión de Proyecto: Rodrigo Ayala López · Desarrollo Backend: Benjamín Soto Aguilar

---

## 1. Declaración de visión

| | |
| --- | --- |
| **Para** | el equipo de ciberseguridad y los responsables de UltraPort |
| **Que** | deben gestionar y aprobar las acciones del reporte de incidentes a la Agencia Nacional de Ciberseguridad (ANCI) dentro de los plazos estrictos de la Ley N° 21.663 |
| **SITRAC-ANCI es un** | sistema de trazabilidad y notificación automática de incidentes de ciberseguridad |
| **Que** | recibe los incidentes desde INVGATE, centraliza su trazabilidad en una base de datos propia y notifica a los responsables antes de que venzan los plazos de reporte |
| **A diferencia de** | llevar el seguimiento de plazos de forma manual sobre los tickets del service desk |
| **Nuestro producto** | calcula los plazos según la clasificación del incidente, registra cada hito y acción, y escala alertas preventivas, dejando un historial auditable del cumplimiento |

## 2. Necesidad que cubre

La Ley N° 21.663 obliga a notificar los incidentes de ciberseguridad en plazos estrictos:

| Hito | Plazo |
| --- | --- |
| Aviso temprano | Máximo 3 horas desde la toma de conocimiento |
| Informe preliminar | 72 horas (Esencial) o 24 horas (OIV), según clasificación |
| Informe final | 15 días |

Sin un mecanismo que controle estos plazos y avise a las personas correctas, UltraPort queda expuesto a incumplir la normativa y a las sanciones asociadas. SITRAC-ANCI cubre esa necesidad: nadie tiene que acordarse de los plazos, el sistema los calcula, los vigila y avisa.

## 3. Usuarios y partes interesadas

| Actor | Qué necesita del producto |
| --- | --- |
| Equipo de ciberseguridad de UltraPort | Ver la cola de incidentes activos, confirmar el aviso temprano y registrar informes preliminar y final |
| Responsables / aprobadores de UltraPort | Recibir notificaciones y alertas de escalamiento ante plazos por vencer |
| Mandante (UltraPort) | Evidencia trazable de que los reportes a la ANCI se gestionan dentro de plazo |
| Equipo SITRAC (DuocUC) | Un MVP funcional, documentado y demostrable dentro del periodo CAPSTONE |

## 4. Beneficio principal

Facilitar el cumplimiento de los plazos de reporte a la ANCI, reduciendo el riesgo de incumplimiento normativo mediante automatización y trazabilidad.

## 5. Qué lo diferencia

- **Plazos calculados por clasificación:** aplica el plazo que corresponde (Esencial u OIV) y guarda una copia (snapshot) de los plazos vigentes por incidente.
- **Ciclo de vida automático:** el sistema avanza estados e hitos según los vencimientos, sin intervención manual.
- **Notificaciones confiables:** correo (SMTP) y Microsoft Teams; los incidentes críticos se avisan de inmediato y el resto se consolida mediante una cola Outbox.
- **Escalamiento preventivo:** alerta antes del vencimiento, con reserva idempotente para no duplicar avisos.
- **Trazabilidad completa:** historial de acciones, hitos e informes (preliminar y final); un incidente solo se cierra con el informe final registrado.
- **Integración con el flujo actual:** recibe los incidentes desde INVGATE por webhook, sin pedir doble digitación.

## 6. Alcance del MVP

**Dentro del alcance (periodo CAPSTONE, 10 de agosto al 12 de diciembre de 2026):**
recepción de incidentes desde INVGATE, cálculo de prioridad y plazos, hitos e historial, avance automático del ciclo de vida, notificaciones SMTP/Teams, escalamiento SLA, autenticación JWT y consola web operativa.

**Fuera del alcance actual (trabajo futuro):**
integración completa con credenciales reales de INVGATE, gestión de falso positivo, revisión de datos personales, canales de notificación adicionales y hardening para producción.

## 7. Criterios de éxito

| Criterio | Cómo se verifica |
| --- | --- |
| Los incidentes recibidos quedan registrados con prioridad y plazos calculados | Prueba del webhook y revisión en base de datos |
| Los responsables reciben las notificaciones en los hitos definidos | Correos capturados (smtp4dev) y/o mensajes en Teams |
| Ningún incidente se cierra sin informe final | Prueba del endpoint de cierre |
| El mandante valida el flujo principal | Sprint Review con UltraPort |

## 8. Relación con otros documentos

- Objetivos del proyecto: Guía 1.5 (Documento de inicio de proyecto)
- Gestión Scrum y backlog: `Gestion_Scrum_SITRAC-ANCI.md`
- Arquitectura: `ARCHITECTURE.md`
- Requisitos no funcionales: `Requisitos-No-Funcionales.md`
