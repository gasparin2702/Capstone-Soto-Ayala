// js/api.js
const API_BASE_URL = 'http://localhost:5116/api';

/**
 * Helper para obtener los headers de autenticación.
 */
const getAuthHeaders = (extraHeaders = {}) => {
    const token = localStorage.getItem('sitrac_token');
    return {
        ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
        ...extraHeaders
    };
};

/**
 * Wrapper de Fetch que maneja la autenticación y los errores 401.
 */
const authFetch = async (url, options = {}) => {
    options.headers = getAuthHeaders(options.headers);
    
    const response = await fetch(url, options);

    // Si el token expiró o es inválido, forzar logout
    if (response.status === 401) {
        localStorage.removeItem('sitrac_token');
        localStorage.removeItem('sitrac_user');
        window.location.href = 'login.html';
        throw new Error('Sesión expirada. Inicie sesión nuevamente.');
    }

    return response;
};

export const SitracAPI = {
    // --- AUTENTICACIÓN ---
    login: async (username, password) => {
        const res = await fetch(`${API_BASE_URL}/Auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        if (!res.ok) throw new Error('Credenciales inválidas');
        return await res.json();
    },

    // --- INCIDENTES ---
    obtenerIncidentesActivos: async () => {
        try {
            const response = await authFetch(`${API_BASE_URL}/incidentes`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            if (!Array.isArray(data)) return [];

            return data.map(item => {
                const toma = new Date(item.fechaTomaConocimiento).getTime();

                const hitos = [];
                if (item.fechaLimiteAvisoTemprano) {
                    hitos.push({ ms: 'A', limit: new Date(item.fechaLimiteAvisoTemprano).getTime(), win: 3 * 3600000 });
                }
                if (item.fechaLimiteInformePreliminar) {
                    hitos.push({
                        ms: 'P',
                        limit: new Date(item.fechaLimiteInformePreliminar).getTime(),
                        win: item.clasificacion === 'OIV' ? 24 * 3600000 : 72 * 3600000
                    });
                }
                if (item.fechaLimiteInformeFinal) {
                    hitos.push({ ms: 'F', limit: new Date(item.fechaLimiteInformeFinal).getTime(), win: 15 * 24 * 3600000 });
                }

                const codEstado = Number(item.codTipoEstadoIncidente);
                let hitoActual;
                if (codEstado === 1) hitoActual = hitos.find(h => h.ms === 'A');
                else if (codEstado === 2) hitoActual = hitos.find(h => h.ms === 'P');
                else if (codEstado === 3) hitoActual = hitos.find(h => h.ms === 'F');

                if (!hitoActual) {
                    const now = Date.now();
                    hitoActual = hitos.find(h => h.limit > now) || hitos[hitos.length - 1];
                }
                if (!hitoActual) {
                    hitoActual = { ms: 'A', limit: toma + 3 * 3600000, win: 3 * 3600000 };
                }

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
                    toma,
                    estadoOriginal: item.estado,
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
            const response = await authFetch(`${API_BASE_URL}/incidentes/${idIncidente}`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            return await response.json();
        } catch (error) {
            console.error(`Error al cargar el incidente ${idIncidente}:`, error);
            return null;
        }
    },

    obtenerHistorialIncidente: async (idIncidente) => {
        try {
            const response = await authFetch(`${API_BASE_URL}/incidentes/${idIncidente}/historial`);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            return Array.isArray(data) ? data : [];
        } catch (error) {
            console.error(`Error al cargar historial del incidente:`, error);
            return [];
        }
    },

    // --- ACCIONES ---
    confirmarAviso: async (idIncidente, payload) => {
        const res = await authFetch(`${API_BASE_URL}/incidentes/${idIncidente}/confirmar-aviso`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!res.ok) {
            const err = await res.json().catch(() => ({}));
            throw new Error(err.error || err.Error || `HTTP ${res.status}`);
        }
        return await res.json();
    },

    registrarInforme: async (idIncidente, payload) => {
        const res = await authFetch(`${API_BASE_URL}/incidentes/${idIncidente}/informe`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!res.ok) {
            const err = await res.json().catch(() => ({}));
            throw new Error(err.error || err.Error || `HTTP ${res.status}`);
        }
        return await res.json();
    },

    cerrarIncidente: async (idIncidente, payload) => {
        const res = await authFetch(`${API_BASE_URL}/incidentes/${idIncidente}/cerrar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        if (!res.ok) {
            const err = await res.json().catch(() => ({}));
            throw new Error(err.error || err.Error || `HTTP ${res.status}`);
        }
        return await res.json();
    }
};