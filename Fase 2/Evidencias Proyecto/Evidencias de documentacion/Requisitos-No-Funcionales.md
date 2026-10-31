# Requisitos No Funcionales — SITRAC-ANCI

**Proyecto CAPSTONE 2026 (PTY4614) · DuocUC sede Valparaíso · Empresa mandante: UltraPort**
**Versión:** 1.0 (borrador) · **Fecha:** 30-10-2026 · **Autores:** Rodrigo Ayala López, Benjamín Soto Aguilar

## 1. Propósito y alcance

Este documento define los requisitos no funcionales (RNF) de SITRAC-ANCI con **valores medibles** y un **método de verificación** para cada uno. Parte de los RNF-01 a RNF-09 de la ERS v1.1 (se conservan sus identificadores para mantener la trazabilidad) y agrega los requisitos de rendimiento, seguridad, confiabilidad y mantenibilidad que la ERS no cuantificaba.

Cubre las cuatro categorías pedidas en la revisión: **rendimiento, seguridad, disponibilidad y usabilidad**, además de cumplimiento normativo, auditabilidad y mantenibilidad.

## 2. Cómo leer las tablas

- **Origen:** `ERS` si el requisito viene de la ERS v1.1; `Nuevo` si se incorpora en este documento.
- **Estado del valor:** `Normativo` si lo fija la Ley N° 21.663; `Propuesto` si es una meta definida por el equipo que debe validarse con UltraPort.
- **Verificación:** cómo se comprobará el cumplimiento durante las pruebas de la Fase 3.

> Los umbrales marcados como `Propuesto` son metas de diseño. Se recomienda mantener en el informe solo aquellos que el equipo pueda medir antes del cierre y declarar como pendiente el resto.

---

## 3. Cumplimiento normativo

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-01 | El sistema debe calcular y controlar los plazos legales de la Ley N° 21.663. | Alerta temprana: **3 horas**. Informe preliminar: **72 horas**. Informe final: **15 días corridos**, todos desde la fecha y hora de detección. El 100 % de los incidentes registrados debe tener sus tres fechas límite calculadas en `TbSitracIncidentePlazoSla` y su hito de aviso temprano creado en `TbSitracHito`. Los hitos siguientes se crean al avanzar el ciclo de vida. | Normativo | Pruebas unitarias del cálculo de plazos; consulta SQL que cruce `TbSitracIncidente`, `TbSitracIncidentePlazoSla` y `TbSitracHito`. | ERS |
| RNF-05 | Los plazos SLA deben ser configurables sin modificar el código. | Los plazos viven en `TbSitracTipoPlazoSla` (Esencial: 3 h, 72 h, 15 días; OIV: 3 h, 24 h, 15 días). Cambiar un plazo o la clasificación de la empresa se hace con datos, **sin recompilar ni volver a desplegar**, y se aplica a los incidentes registrados desde ese momento. | Propuesto | Prueba funcional: modificar el parámetro y registrar un incidente nuevo. | ERS |

## 4. Rendimiento

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-10 | Recepción de incidentes por webhook. | El 95 % de las solicitudes se recibe y persiste en **menos de 2 segundos** (percentil 95) con carga normal. | Propuesto | Pruebas de carga con herramienta de pruebas HTTP (por ejemplo, ejecución repetida de `Sitrac.Server.http` o k6). | Nuevo |
| RNF-11 | Latencia del primer aviso a los responsables. | Incidentes de prioridad 30 o más (envío inmediato): el aviso se entrega al canal de salida (SMTP o Teams) en **60 segundos o menos** desde la recepción, en el 95 % de los casos. Incidentes de prioridad menor (Outbox consolidado, evaluado cada 15 segundos): **5 minutos o menos**. | Propuesto | Comparación de marcas de tiempo entre `FechaRegistroSistema` y `FechaEnvio` de `TbSitracNotificacionLog`. | Nuevo |
| RNF-12 | Capacidad de procesamiento. | Procesar al menos **50 incidentes por hora** y **10 solicitudes concurrentes** sin errores ni pérdida de datos. | Propuesto, provisional hasta que UltraPort entregue el volumen esperado de tickets | Prueba de carga con datos sintéticos. | Nuevo |
| RNF-13 | Tiempo de carga de la interfaz. | La pantalla de aprobación carga en **menos de 2 segundos** en la red interna de UltraPort. | Propuesto | Medición con las herramientas de desarrollo del navegador. | Nuevo |

## 5. Disponibilidad y confiabilidad

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-02 | El sistema debe operar de forma continua. | Operación 24x7x365. Disponibilidad mensual **igual o mayor a 99,5 %** (alrededor de 3,6 horas de indisponibilidad al mes), excluyendo mantenimientos acordados con UltraPort. | Propuesto | Registro de disponibilidad del servicio durante el período de pruebas. | ERS |
| RNF-03 | El cumplimiento del reporte no debe depender solo de INVGATE. | Con INVGATE caído, los puntos de entrada obligatorios (**correo y Teams**) y el registro manual en SITRAC siguen funcionando. Cobertura: **100 %** de esos canales. | Propuesto | Prueba con la integración de INVGATE deshabilitada. | ERS |
| RNF-14 | Resiliencia de las notificaciones. | Ante un fallo de SMTP o Teams, el sistema reintenta según `MaxReintentos` e `IntervaloEntreEnviosMs` (valor propuesto: **3 reintentos**) y registra los intentos y el error en `TbSitracNotificacionLog` (`Intentos`, `MensajeError`). Ningún aviso pendiente se pierde por un fallo temporal. | Propuesto | Prueba simulando un canal caído. | Nuevo |
| RNF-15 | No pérdida de incidentes recibidos. | **0 incidentes perdidos** después de responder la recepción: el incidente se persiste antes de confirmar el webhook. | Propuesto | Prueba de recepción con interrupción forzada del servicio. | Nuevo |
| RNF-16 | Respaldo y recuperación. | Respaldo diario de la base de datos. Pérdida máxima de datos (RPO) de **24 horas** y tiempo máximo de recuperación (RTO) de **4 horas**. | Propuesto, depende de la política de respaldos de UltraPort | Restauración de un respaldo en entorno de pruebas. | Nuevo |

## 6. Seguridad

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-08 | Control de acceso por roles (RBAC) para confirmar el aviso a la ANCI. | Solo los responsables autorizados (Enzo, Gonzalo y Ángelo) pueden confirmar. El **100 %** de los intentos de otros usuarios se rechaza y queda registrado. **Estado actual: no implementado** (los endpoints no tienen autenticación ni autorización). | Propuesto | Pruebas de autorización con usuarios autorizados y no autorizados. | ERS |
| RNF-17 | Autenticación. | Acceso con cuentas corporativas Microsoft (Microsoft Entra ID, tecnología incluida en el Catálogo de Tecnologías Autorizadas de UltraPort). No se aceptan accesos anónimos a la API ni a la interfaz. **Estado actual: no implementado**; `ARCHITECTURE.md` lo plantea como punto de extensión (JWT o Entra ID). | Propuesto, a confirmar con UltraPort | Prueba de acceso sin credenciales y con credenciales válidas. | Nuevo |
| RNF-18 | Aislamiento de red. | **0 puertos expuestos a internet**. El despliegue se realiza solo en servidores internos de UltraPort. | Propuesto, derivado de la restricción de despliegue | Revisión de configuración de red y escaneo de puertos interno y externo. | Nuevo |
| RNF-19 | Protección de credenciales. | **0 credenciales** (cadenas de conexión, SMTP, Teams, INVGATE) versionadas en el repositorio. Se configuran localmente. | Propuesto | Revisión del repositorio con una herramienta de detección de secretos. | Nuevo |
| RNF-20 | Prevención de inyección SQL. | **100 %** de las consultas usan parámetros (Dapper con parámetros). Ninguna consulta se arma concatenando texto del usuario. | Propuesto | Revisión de código y análisis estático. | Nuevo |
| RNF-21 | Cifrado en tránsito. | Las comunicaciones entre cliente y servidor usan **TLS 1.2 o superior**. | Propuesto, a confirmar con la infraestructura de UltraPort | Inspección de la configuración del servidor. | Nuevo |
| RNF-22 | Protección de datos personales. | Si un incidente declara datos personales, el flujo automático se detiene y queda en **revisión manual en el 100 % de los casos** (RF-14). | Propuesto | Prueba funcional con incidentes con y sin datos personales. | Nuevo |
| RNF-23 | Dependencias seguras. | **0 vulnerabilidades críticas conocidas** en paquetes NuGet al cierre de cada sprint. | Propuesto | Ejecutar `dotnet list package --vulnerable` en cada cierre de sprint. | Nuevo |

## 7. Auditabilidad y trazabilidad

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-06 | Registro cronológico e inalterable de las acciones relevantes. | El **100 %** de las acciones (registro, cambios de estado, notificaciones, aprobaciones, respuestas de la ANCI, informes) queda con usuario, fecha y hora, y tipo de acción. La aplicación no puede modificar ni eliminar registros históricos. | Propuesto | Revisión de permisos de la base de datos y prueba de intento de modificación. | ERS |
| RNF-24 | Consistencia horaria. | Fechas y horas almacenadas en **UTC**, con servidor sincronizado por NTP, y mostradas en hora de Chile. El cálculo de plazos se hace desde la fecha y hora de toma de conocimiento (RF-13). **Hallazgo:** las migraciones `V0001` a `V0005` usan `GETDATE()` (hora local del servidor) y `V0006` usa `GETUTCDATE()`; conviene unificar el criterio. | Propuesto | Pruebas del cálculo con cambio de horario de verano. | Nuevo |
| RNF-25 | Retención de la evidencia. | Evidencia y bitácora conservadas durante un período mínimo a definir con UltraPort (propuesta: **5 años**). | Propuesto, pendiente de validar con UltraPort | Revisión de la política de retención acordada. | Nuevo |

## 8. Usabilidad

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-09 | Aprobación con el mínimo de interacciones. | Aprobar un aviso toma **2 interacciones o menos** desde que el responsable abre el enlace (objetivo: 1 clic más la confirmación). Tiempo total **menor a 30 segundos**. | Propuesto | Prueba de uso cronometrada con al menos 3 usuarios. | ERS |
| RNF-26 | Claridad de la interfaz. | Interfaz en español. Los mensajes de error indican la causa y la acción siguiente. El **plazo restante** de cada hito es visible en la pantalla del incidente. | Propuesto | Lista de verificación en pruebas de aceptación con UltraPort. | Nuevo |
| RNF-27 | Compatibilidad con navegadores. | Funciona en las **versiones vigentes de Microsoft Edge y Google Chrome**. | Propuesto | Pruebas manuales en ambos navegadores. | Nuevo |

## 9. Mantenibilidad y compatibilidad

| ID | Requisito | Métrica / umbral | Estado del valor | Verificación | Origen |
| --- | --- | --- | --- | --- | --- |
| RNF-04 | Compatibilidad con los estándares de UltraPort. | Uso de las tecnologías del Catálogo de Tecnologías Autorizadas: .NET 10 (LTS), SQL Server, Dapper y Evolve. El **100 %** de los cambios de esquema se realiza con migraciones versionadas. | Normativo interno de UltraPort | Revisión de `Sitrac.DbMigration` y del catálogo. | ERS |
| RNF-07 | Interoperabilidad con INVGATE. | Integración con INVGATE por webhook y, cuando UltraPort entregue las credenciales, por API. No se usan servidores intermedios de terceros externos a UltraPort. | Propuesto | Revisión de la arquitectura de despliegue. | ERS (ajustado) |
| RNF-28 | Arquitectura por capas. | Separación en `Server`, `Service`, `Repository` y `DataAccess`. Las reglas de negocio residen únicamente en `Sitrac.Service`. | Propuesto | Revisión de dependencias entre proyectos de la solución. | Nuevo |
| RNF-29 | Calidad de las pruebas. | Cobertura de pruebas unitarias **igual o mayor a 70 %** en `Sitrac.Service` (cálculo de SLA y ciclo de vida). | Propuesto | Informe de cobertura generado con la herramienta de pruebas de .NET. | Nuevo |
| RNF-30 | Documentación vigente. | README, `ARCHITECTURE.md`, diagramas UML y este documento actualizados al cierre de cada sprint. | Propuesto | Revisión en la Definition of Done de cada sprint. | Nuevo |
| RNF-31 | Soporte de la plataforma. | El estándar inicial de UltraPort fue .NET 8, cuyo soporte termina el **10 de noviembre de 2026**. UltraPort fue advertido en reunión presencial el 16 de septiembre de 2026 y el proyecto fue migrado a **.NET 10 (LTS)**. La arquitectura por capas debe permitir futuras actualizaciones. | Hecho documentado | Acta de la reunión y revisión de `global.json` y de los `.csproj`. | Nuevo |

---

## 10. Estado de cumplimiento al 30-10-2026 (según `ARCHITECTURE.md` y las migraciones)

| Situación | Requisitos |
| --- | --- |
| Respaldado por el diseño actual | RNF-01 (plazos y hitos), RNF-05 (plazos por clasificación en base de datos), RNF-06 (historial en `TbSitracEventoHistorial`), RNF-14 (Outbox con reintentos), RNF-20 (Dapper), RNF-28 (arquitectura por capas) |
| Por implementar | RNF-08 y RNF-17 (autenticación y autorización), RNF-21 (TLS, depende de UltraPort), RNF-22 (el flujo de revisión de datos personales no tiene endpoint) |
| Por medir | RNF-10 a RNF-13, RNF-02, RNF-09, RNF-29 |
| Por revisar | RNF-06 (que la aplicación no pueda modificar ni borrar el historial; hoy las tablas tienen borrado lógico con `AuditNotDeleted`), RNF-24 (uso mezclado de `GETDATE()` y `GETUTCDATE()`, que se mantiene por ahora) |

## 11. Cambios respecto a la ERS v1.1

| Requisito | Cambio |
| --- | --- |
| RNF-07 | La ERS indicaba construir sobre las capacidades nativas de INVGATE (workflows, triggers y reglas de correo entrante). En la Fase 2 la solución es un servicio propio en .NET 10 con base de datos SQL Server, que recibe los incidentes por webhook. El requisito se reformula para reflejar esa decisión. |
| RNF-01 al RNF-09 | Se conservan los identificadores y se agregan métricas, estado del valor y método de verificación. |

## 12. Pendientes de validación con UltraPort

1. Volumen esperado de tickets por hora, que fija el valor definitivo de RNF-12.
2. Política de respaldos y retención de UltraPort, que fija RNF-16 y RNF-25.
3. Autenticación corporativa y configuración de TLS en el servidor de despliegue (RNF-17 y RNF-21).
4. Meta de disponibilidad aceptada por UltraPort (RNF-02).
