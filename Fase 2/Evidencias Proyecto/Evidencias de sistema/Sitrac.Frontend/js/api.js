// js/api.js
const API_BASE_URL = 'http://localhost:5116/api';
const H = 3600e3;
const D = 24 * H;

export const SitracAPI = {
    /**
     * Trae la cola de incidentes activos (estado < 4).
     * Normaliza cada DTO para que app.js no tenga que saber del backend.
     */
    obtenerIncidentesActivos: async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/incidentes`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            if (!Array.isArray(data)) return [];

            return data.map(item => {
                const toma = new Date(item.fechaTomaConocimiento).getTime();

                // Construir los 3 hitos con sus ventanas reales
                const hitos = [];
                if (item.fechaLimiteAvisoTemprano) {
                    hitos.push({ ms: 'A', limit: new Date(item.fechaLimiteAvisoTemprano).getTime(), win: 3 * H });
                }
                if (item.fechaLimiteInformePreliminar) {
                    hitos.push({
                        ms: 'P',
                        limit: new Date(item.fechaLimiteInformePreliminar).getTime(),
                        win: item.clasificacion === 'OIV' ? 24 * H : 72 * H
                    });
                }
                if (item.fechaLimiteInformeFinal) {
                    hitos.push({ ms: 'F', limit: new Date(item.fechaLimiteInformeFinal).getTime(), win: 15 * D });
                }

                // Elegir el hito "activo" según el estado del incidente
                const codEstado = Number(item.codTipoEstadoIncidente);
                let hitoActual;
                if (codEstado === 1) hitoActual = hitos.find(h => h.ms === 'A');
                else if (codEstado === 2) hitoActual = hitos.find(h => h.ms === 'P');
                else if (codEstado === 3) hitoActual = hitos.find(h => h.ms === 'F');

                // Fallback: primer hito con deadline futuro
                if (!hitoActual) {
                    const now = Date.now();
                    hitoActual = hitos.find(h => h.limit > now) || hitos[hitos.length - 1];
                }
                if (!hitoActual) {
                    hitoActual = { ms: 'A', limit: toma + 3 * H, win: 3 * H };
                }

                // Estado legible para la UI
                let estadoUI = item.estado;
                if (estadoUI === 'En triage') estadoUI = 'Detectado';
                else if (estadoUI === 'Aviso enviado') estadoUI = 'Aviso Temprano';
                else if (estadoUI === 'Preliminar enviado') estadoUI = 'Informe Preliminar';

                return {
                    id: item.codigoTicketInvgate || `INC-DB-${item.idIncidente}`,
                    dbId: item.idIncidente,
                    origin: item.origen,
                    cls: item.clasificacion,
                    g: item.gravedad,
                    estado: estadoUI,
                    codEstado,
                    ms: hitoActual.ms,
                    win: hitoActual.win,
                    limit: hitoActual.limit,
                    live: true
                };
            });
        } catch (error) {
            console.error("Fallo al conectar con la API de SITRAC:", error);
            return [];
        }
    },

    /**
     * Detalle completo de un incidente.
     */
    obtenerDetalleIncidente: async (idIncidente) => {
        try {
            const response = await fetch(`${API_BASE_URL}/incidentes/${idIncidente}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error(`Error al cargar el incidente ${idIncidente}:`, error);
            return null;
        }
    },

    /**
     * Historial de eventos (timeline).
     */
    obtenerHistorialIncidente: async (idIncidente) => {
        try {
            const response = await fetch(`${API_BASE_URL}/incidentes/${idIncidente}/historial`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            return Array.isArray(data) ? data : [];
        } catch (error) {
            console.error(`Error al cargar historial del incidente:`, error);
            return [];
        }
    }

    // Los métodos POST para aviso / informe / cierre se agregarán aquí cuando
    // los endpoints estén listos en el backend.
};