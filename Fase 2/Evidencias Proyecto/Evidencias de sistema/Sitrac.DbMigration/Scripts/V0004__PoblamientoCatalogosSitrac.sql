-- ==============================================================================
-- SISTEMA: SITRAC-ANCI
-- OBJETIVO: Poblamiento inicial de catálogos paramétricos (Seed Data)
-- ==============================================================================

-- 1. Clasificación de Empresa
INSERT INTO TbAnciTipoClasificacionEmpresa (NombreTipoClasificacionEmpresa) VALUES 
('Esencial'), 
('OIV');

-- 2. Parametrización de SLAs (Se asocia por ID: 1=Esencial, 2=OIV)
INSERT INTO TbAnciTipoPlazoSla (CodTipoClasificacionEmpresa, HorasAvisoTemprano, HorasInformePreliminar, DiasInformeFinal) VALUES 
(1, 3, 72, 15), -- Esencial
(2, 3, 24, 15); -- OIV

-- 3. Tipos de Datos Personales
INSERT INTO TbAnciTipoDatosPersonales (NombreTipoDatosPersonales) VALUES 
('Si'), 
('No'), 
('Desconocido');

-- 4. Gravedades
INSERT INTO TbAnciTipoGravedad (NombreTipoGravedad, OrdenTipoGravedad) VALUES 
('Bajo', 1), 
('Medio', 2), 
('Alto', 3), 
('Critico', 4);

-- 5. Estados del Incidente (Flujo lógico)
INSERT INTO TbAnciTipoEstadoIncidente (NombreTipoEstadoIncidente, OrdenTipoEstadoIncidente) VALUES 
('En triage', 1),
('Aviso enviado', 2),
('Preliminar enviado', 3),
('Cerrado', 4);

-- 6. Canales de Entrada
INSERT INTO TbAnciTipoCanalEntrada (NombreTipoCanalEntrada, DescripcionTipoCanalEntrada) VALUES 
('INVGATE', 'Integración automática mediante API'),
('Correo ANCI', 'Avisos directos desde la entidad reguladora'),
('Manual', 'Ingreso manual por analista');

-- 7. Responsables Base (Equipo del proyecto y mandantes)
INSERT INTO TbAnciResponsable (NombreResponsable, EmailResponsable, TelefonoResponsable, DescripcionRolOrganizacional, IndEsDelegadoCiberseguridad) VALUES 
('Enzo Vadillo', 'Evadillo@ultraport.cl', '+56900000001', 'Jefe de Ciberseguridad', 1),
('Gonzalo Crosier', 'GCrosier@ultraport.cl', '+56900000002', 'Delegado de Ciberseguridad', 1),
('Angelo Michell', ' AMichell@ultraport.cl', '+56900000003', 'Delegado de Ciberseguridad', 1);