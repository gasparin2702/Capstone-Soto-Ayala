# SITRAC-ANCI · Gestión del proyecto bajo Scrum

> Product Backlog, Sprints, bloqueos, decisiones y retrospectiva · **Estado al 9 de octubre de 2026**
>
> Proyecto CAPSTONE (PTY4614) · Duoc UC, sede Valparaíso · Empresa mandante: UltraPort

| Dato | Detalle |
| --- | --- |
| Equipo | Rodrigo Ayala López · Benjamín Soto Aguilar |
| Docente guía | María Ignacia Cobo |
| Metodología | Scrum, con sprints alineados a las semanas de CAPSTONE |
| Herramienta de gestión | Trello, tablero «SITRAC-ANCI — Product Backlog» |
| Período del proyecto | 10 de agosto al 12 de diciembre de 2026 (18 semanas) |
| Documentos relacionados | [Flujo 1](Flujo1.png) · [Flujo 2](Flujo2.png) · [Flujo 3](Flujo3.png) · [Diccionario de datos](Diccionario_de_Datos_SITRAC.xlsx) |

---

## 1. Visión del producto

Automatizar la trazabilidad y notificación de incidentes de ciberseguridad de UltraPort a la ANCI, garantizando el cumplimiento de los plazos establecidos por la Ley N.° 21.663 (3 horas, 72 horas y 15 días).

El sistema recibe los tickets de incidentes de seguridad, centraliza la trazabilidad en una base de datos propia y notifica al responsable de turno dentro del plazo legal.

## 2. Roles

| Integrante | Rol | Responsabilidades |
| --- | --- | --- |
| Rodrigo Ayala López | Gestión de proyecto y cumplimiento normativo | Product Backlog, análisis de la Ley N.° 21.663, diseño del modelo de datos y de los flujos, coordinación con UltraPort, facilitación de sprints, apoyo técnico a mockups y backend |
| Benjamín Soto Aguilar | Desarrollo backend | Integración con INVGATE, temporizadores y notificaciones, arquitectura y pruebas |

Ambos participan en el diseño de flujos, la arquitectura, las pruebas, las retrospectivas y las presentaciones.

## 3. Definiciones de trabajo

### Definition of Ready (DoR)

Una historia entra a un sprint solo si cumple:

- Tiene una descripción «Como / quiero / para» clara.
- Tiene al menos un criterio de aceptación en borrador.
- No depende de información 100 % pendiente de UltraPort (si depende, se mantiene en la lista de bloqueos).
- Fue estimada por el equipo (story points o talla).
- El responsable (Rodrigo, Benjamín o ambos) está identificado.

### Definition of Done (DoD)

Una historia se considera terminada solo si cumple:

- Código funcionando e integrado, sin romper funcionalidad previa.
- Pruebas ejecutadas (unitarias o de integración, según corresponda).
- Documentación técnica actualizada en el repositorio.
- Revisión por el otro integrante del equipo.
- Cumple el requisito no funcional asociado, si aplica (plazo legal, seguridad, disponibilidad).
- Subida a GitHub con un commit descriptivo.

> **Nota de estado:** las pruebas formales (unitarias y de integración) están planificadas para el sprint final, por lo que las historias del Sprint 2 se cierran hasta ahora con revisión entre pares.

## 4. Product Backlog

Las 11 historias de usuario derivan de la Especificación de Requisitos de apoyo (16 requisitos funcionales y 9 no funcionales).

| ID | Historia | Prioridad | RF | Sprint | Estado al 09-10-2026 |
| --- | --- | --- | --- | --- | --- |
| HU-01 | Consumo de tickets desde INVGATE | Alta | RF-01 | 2 | **Parcial.** Recepción de tickets por webhook implementada; la consulta directa a la API está bloqueada por falta de credenciales |
| HU-02 | Notificación automática al responsable de turno | Alta | RF-02 | 2 | **Pendiente.** Las notificaciones aún no están implementadas |
| HU-03 | Aprobación y envío del aviso a la ANCI | Alta | RF-03, RF-04 | 2 | **En curso.** Formulario de registro manual y pantalla de aprobación en desarrollo |
| HU-04 | Registro de evidencia del aviso enviado | Media | RF-05 | 2–3 | **Por hacer.** Modelo de datos definido |
| HU-05 | Registro de la respuesta de la ANCI | Media | RF-06 | 2–3 | **Por hacer.** Modelo de datos definido; flujo definido (Flujo 2) |
| HU-06 | Reporte de actualización (72 h) | Media | RF-07, RF-08 | 3 | **Por hacer.** Flujo definido (Flujo 3) |
| HU-07 | Informe final (15 días) | Media | RF-09 | 3 | **Por hacer.** Flujo definido (Flujo 3) |
| HU-08 | Correo entrante de la ANCI: caso nuevo | Media | RF-10 | 3 | **Por hacer.** Mientras tanto, el canal se cubre con registro manual |
| HU-09 | Correo entrante de la ANCI: complementa ticket existente | Media | RF-11 | 3 | **Por hacer.** Mientras tanto, el canal se cubre con registro manual |
| HU-10 | Autocompletado de informes desde plantillas de UltraPort | Baja | RF-15 | — | **Bloqueada.** A la espera de las plantillas oficiales |
| HU-11 | Mecanismo de contingencia sin INVGATE | Baja | RF-16 | — | **Parcial.** El registro manual en desarrollo aporta la base; falta la definición conjunta con UltraPort |

## 5. Sprints

### Fase 1 · Definición del proyecto (semanas 1 a 4) · Completada

| Entregable | Responsable | Estado |
| --- | --- | --- |
| Product Backlog inicial priorizado (11 historias, DoR y DoD) | Rodrigo | Hecho |
| Alcance del MVP, acordado en el kick off y en la reunión del 25 de agosto | Equipo | Hecho |
| Especificación de requisitos v1.1 (IEEE 830): RF-01 a RF-16 y RNF-01 a RNF-09 | Equipo | Hecho |
| Presentación de la definición del proyecto e informe técnico de la Fase 1 | Rodrigo y Benjamín | Hecho |

**Fuera del alcance del MVP:** detección temprana de incidentes y creación de tickets nuevos.

### Sprint 1 · Parámetros, base de datos y entorno (semanas 5 y 6) · Cerrado en lo esencial

**Objetivo:** contar con el modelo de datos normalizado y el entorno de desarrollo operativo.

| Tarea | Responsable | Estado |
| --- | --- | --- |
| Entorno de desarrollo operativo | Benjamín y Rodrigo | Hecho |
| Modelo de datos normalizado: 20 tablas (8 principales y 12 de catálogo), 219 columnas y 39 claves foráneas | Rodrigo (con Benjamín) | Hecho |
| Migraciones versionadas con Evolve, scripts V0001 a V0005 (inicialización, tablas operacionales, tablas de informes, poblamiento de catálogos e historial) | Benjamín y Rodrigo | Hecho |
| [Diccionario de datos](Diccionario_de_Datos_SITRAC.xlsx), con fecha 30 de septiembre de 2026 | Rodrigo | Hecho |
| Análisis de la Ley N.° 21.663 y traducción de sus plazos en reglas configurables (el plazo del informe preliminar pasa de 72 a 24 horas si la empresa es clasificada como OIV) | Rodrigo | Hecho |
| Diseño de los flujos: [Flujo 1](Flujo1.png) (ingreso y aviso temprano), [Flujo 2](Flujo2.png) (acuse de la ANCI y recordatorios) y [Flujo 3](Flujo3.png) (informe preliminar y final) | Rodrigo | Hecho, con puntos por definir con UltraPort |

### Sprint 2 · Backend: recepción de tickets, plazos y notificaciones (semanas 7 a 10) · En curso

**Objetivo:** recibir los incidentes, calcular y vigilar los plazos legales y notificar al responsable de turno.

| Tarea | Responsable | Estado |
| --- | --- | --- |
| Recepción de tickets por webhook | Benjamín (con apoyo de Rodrigo) | Hecho |
| Cálculo y sellado de los plazos legales de cada incidente | Benjamín | Hecho |
| Evaluación del vencimiento de plazos en segundo plano | Benjamín | Hecho |
| Exposición de la información al prototipo web | Benjamín y Rodrigo | Hecho (prototipo) |
| Carga de catálogos y reglas de SLA mediante migraciones | Benjamín y Rodrigo | En curso |
| Formulario de registro manual y pantalla de aprobación | Benjamín y Rodrigo | En curso |
| Notificación automática al responsable de turno (HU-02) | Benjamín | Pendiente |
| Consulta directa a la API de INVGATE (HU-01) | Benjamín | Bloqueada (credenciales) |

### Próximas etapas

| Etapa | Semanas | Contenido |
| --- | --- | --- |
| Sprint Review e informe de avance | 10 a 13 | Cierre de sprints con retrospectiva, ajuste del backlog y consolidación del informe de avance |
| Pruebas de certificación | 14 a 18 | Pruebas, cierre del Definition of Done, informe final y presentación a comisión |

## 6. Bloqueos y dependencias externas

| Tema | A quién se consulta | Estado | Impacto | Acción |
| --- | --- | --- | --- | --- |
| Credenciales y autenticación de la API de INVGATE | Contraparte técnica de UltraPort | **Pendiente** | HU-01 | Avanzar con webhook; fijar una fecha límite para decidir si la API queda como evolución futura |
| Campos obligatorios del ticket ANCI | Contraparte técnica de UltraPort | **Parcial** | HU-03 a HU-07 | Los campos del informe preliminar ya están modelados en el Flujo 3; falta la confirmación de UltraPort |
| Plantillas oficiales (acta, EPR, informe) | Sponsor de UltraPort | **Pendiente** | HU-10 | Mantener HU-10 con prioridad baja |
| Diagrama de flujo del proceso | Sponsor de UltraPort | **Superado** | HU-02, HU-03 | El equipo diseñó los Flujos 1 a 3, que UltraPort aceptó |
| Volumen esperado de tickets y rendimiento | Contraparte técnica de UltraPort | **Pendiente** | Arquitectura | Solicitar los datos por correo |
| Nombre y rol exacto del tercer responsable de aprobación | Contraparte técnica o sponsor | **Pendiente** | HU-02, HU-03 | Confirmar apellido y cargo |
| Reglas de bloqueo por datos personales (RF-14) | Contraparte técnica de UltraPort | **Parcial** | HU-03 | El Flujo 1 contempla el bloqueo y la derivación a revisión; falta definir el flujo especial con UltraPort |
| Rol del Delegado de Ciberseguridad: ¿coincide con alguno de los responsables de turno? | Sponsor de UltraPort | **Nuevo** | HU-06, HU-07 | Consultar (Flujo 3) |
| Plazo y exigencias del informe posterior al día 15, si el caso sigue abierto | Sponsor de UltraPort | **Nuevo** | HU-07 | Consultar (Flujo 3) |

## 7. Decisiones y ajustes de alcance

| Decisión | Motivo | Efecto |
| --- | --- | --- |
| Recibir los tickets por **webhook** y dejar la consulta a la API de INVGATE como evolución futura | Las credenciales de la API siguen pendientes | El objetivo específico 1 pasa de «consumir mediante la API» a «recibir mediante webhook» |
| Cubrir los canales de **correo y Microsoft Teams con registro manual** | No depende de permisos de UltraPort | Se incorporan el formulario de registro manual y la pantalla de aprobación al Sprint 2 |
| Mantener el plazo del informe preliminar como **parámetro configurable** (72 h o 24 h) | UltraPort podría ser clasificada como Operador de Importancia Vital | El sistema se adapta sin cambios de código |
| Advertir a UltraPort sobre el **fin de soporte de .NET 8** (reunión presencial del 16 de septiembre) | Riesgo técnico de la plataforma | Decisión de actualización pendiente de UltraPort |
| Mantener HU-10 y HU-11 con **prioridad baja** | Dependen de definiciones del mandante | No condicionan el MVP |

El cronograma general no se modificó: las tareas nuevas se incorporaron dentro del Sprint 2.

## 8. Retrospectiva

> **Acta elaborada a partir del diario de reflexión de la Fase 2** (Sprint 1 y avance del Sprint 2). Fecha de realización: *por completar*.

| Qué funcionó | Qué mejorar | Acciones |
| --- | --- | --- |
| Mandante real y comprometido, con reuniones de coordinación frecuentes | Anticipar las dependencias externas y dejarlas con una fecha comprometida | Consolidar en una sola lista las consultas pendientes a UltraPort |
| Infraestructura y catálogo de tecnologías ya definidos por UltraPort | Validar los supuestos con el mandante antes de documentarlos | Replanificar los sprints restantes según lo que sí controla el equipo, si es necesario |
| División clara de roles entre gestión y backend | Fortalecer la comunicación con el entorno de trabajo | Documentar cada decisión y cada acuerdo con UltraPort |
| Confianza mutua y apoyo entre los integrantes del equipo | — | Fijar una fecha límite para decidir el futuro de la integración vía API |

## 9. Riesgos vigentes

| Riesgo | Probabilidad | Impacto | Tratamiento |
| --- | --- | --- | --- |
| Credenciales de INVGATE no disponibles a tiempo | Alta | Alto | Webhook y registro manual; fecha límite de decisión |
| Definiciones de UltraPort que llegan de forma progresiva | Alta | Medio | Validar supuestos antes de documentar; lista única de consultas |
| Fin de soporte de .NET 8 antes de la entrega | Media | Medio | Advertencia formal a UltraPort; decisión pendiente |
| Alcance mayor que una iteración, anticipado por el mandante | Media | Alto | MVP acotado a un flujo definido |

---

*Última actualización: 9 de octubre de 2026. Este documento se actualiza al cierre de cada sprint.*
