const API_BASE_URL = 'http://localhost:5116/api';
const H = 3600e3;
const D = 24 * H;

export const SitracAPI = {
    obtenerIncidentesActivos: async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/incidentes`);
            if (!response.ok) throw new Error('Error de red');
            const data = await response.json();
            
            return data.map(item => {
                const toma = new Date(item.fechaTomaConocimiento).getTime();
                const hitos = [];
                if (item.fechaLimiteAvisoTemprano) hitos.push({ ms: 'A', limit: new Date(item.fechaLimiteAvisoTemprano).getTime(), win: 3 * H });
                if (item.fechaLimiteInformePreliminar) hitos.push({ ms: 'P', limit: new Date(item.fechaLimiteInformePreliminar).getTime(), win: item.clasificacion === 'OIV' ? 24 * H : 72 * H });
                if (item.fechaLimiteInformeFinal) hitos.push({ ms: 'F', limit: new Date(item.fechaLimiteInformeFinal).getTime(), win: 15 * D });

                const now = Date.now();
                let hitoActual = hitos.find(h => h.limit > now) || hitos[hitos.length - 1] || { ms: 'A', limit: toma + 3 * H, win: 3 * H };

                let estadoUI = item.estado;
                if (estadoUI === 'En triage') estadoUI = 'Detectado';
                if (estadoUI === 'Aviso enviado') estadoUI = 'Aviso Temprano';
                if (estadoUI === 'Preliminar enviado') estadoUI = 'Informe Preliminar';

                return {
                    id: item.codigoTicketInvgate || `INC-DB-${item.idIncidente}`,
                    dbId: item.idIncidente,
                    origin: item.origen,
                    cls: item.clasificacion,
                    g: item.gravedad,
                    estado: estadoUI,
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

    obtenerDetalleIncidente: async (idIncidente) => {
        try {
            const response = await fetch(`${API_BASE_URL}/incidentes/${idIncidente}`);
            if (!response.ok) throw new Error('Error de red');
            return await response.json();
        } catch (error) {
            console.error(`Error al cargar el incidente ${idIncidente}:`, error);
            return null;
        }
    },

    obtenerHistorialIncidente: async (idIncidente) => {
        try {
            const response = await fetch(`${API_BASE_URL}/incidentes/${idIncidente}/historial`);
            if (!response.ok) throw new Error('Error de red');
            return await response.json();
        } catch (error) {
            console.error(`Error al cargar historial del incidente:`, error);
            return [];
        }
    }
};