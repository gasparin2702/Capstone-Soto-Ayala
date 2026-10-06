// js/app.js
import { SitracAPI } from './api.js';

const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];
const H = 3600e3, MIN = 60e3, D = 24 * H;
const NOW0 = Date.now(), T0 = performance.now();
const now = () => NOW0 + (performance.now() - T0);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

const ICON = {
  user: '<circle cx="12" cy="8" r="4"/><path d="M4 21c1-4 4-6 8-6s7 2 8 6"/>',
  cpu: '<rect x="6" y="6" width="12" height="12" rx="2"/><path d="M9 2v4M15 2v4M9 18v4M15 18v4M2 9h4M2 15h4M18 9h4M18 15h4"/>',
  check: '<path d="M5 12.5l4.5 4.5L19 7.5"/>',
  alert: '<path d="M12 3l10 18H2z"/><path d="M12 10v5M12 18v.5"/>',
  send: '<path d="M21 3L10 14"/><path d="M21 3l-7 18-4-7-7-4z"/>',
  clip: '<path d="M20 12l-8 8a5 5 0 01-7-7l9-9a3.5 3.5 0 015 5l-9 9a2 2 0 01-3-3l8-8"/>',
  lock: '<rect x="5" y="11" width="14" height="9" rx="2"/><path d="M8 11V8a4 4 0 018 0v3"/>',
  shield: '<path d="M12 3l8 3v6c0 4.5-3.2 8-8 9-4.8-1-8-4.5-8-9V6l8-3z"/>',
  bell: '<path d="M6 16V11a6 6 0 0112 0v5l2 2H4z"/><path d="M10 21h4"/>',
  arrow: '<path d="M5 12h14M13 6l6 6-6 6"/>',
  x: '<path d="M6 6l12 12M18 6L6 18"/>',
  flag: '<path d="M5 21V4h11l-1.5 4L16 12H5"/>',
  eye: '<path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>',
  help: '<circle cx="12" cy="12" r="9"/><path d="M9.5 9.5a2.5 2.5 0 114 2c-.8.6-1.5 1-1.5 2M12 17v.5"/>',
  clock: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
  checkc: '<circle cx="12" cy="12" r="9"/><path d="M8 12.5l3 3 5-6"/>',
  inbox: '<path d="M3 13l3-8h12l3 8v6H3z"/><path d="M3 13h5l1 3h6l1-3h5"/>'
};
const ic = (n, extra = '') => `<svg class="i" viewBox="0 0 24 24" ${extra}>${ICON[n]}</svg>`;

/* ---------- formato ---------- */
const pad = n => String(n).padStart(2, '0');
function dur(ms) {
  if (!Number.isFinite(ms)) return '—';
  const a = Math.abs(ms);
  if (a >= 48 * H) { const d = Math.floor(a / D), h = Math.floor(a % D / H); return `${d}d ${pad(h)}h`; }
  const h = Math.floor(a / H), m = Math.floor(a % H / MIN), s = Math.floor(a % MIN / 1000);
  return `${pad(h)}:${pad(m)}:${pad(s)}`;
}
const fdate = t => new Intl.DateTimeFormat('es-CL', { timeZone: 'America/Santiago', day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false }).format(new Date(t)).replace(',', '');
const ftime = t => new Intl.DateTimeFormat('es-CL', { timeZone: 'America/Santiago', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false }).format(new Date(t));
function ago(ms) {
  const a = Math.abs(ms);
  if (a < MIN) return 'hace instantes';
  if (a < H) return `hace ${Math.floor(a / MIN)} min`;
  if (a < D) return `hace ${Math.floor(a / H)} h ${Math.floor(a % H / MIN)} min`;
  return `hace ${Math.floor(a / D)} d`;
}

/* ---------- semáforo SLA ---------- */
function classify(limit, win, sentAt) {
  if (!Number.isFinite(limit)) return { k: 'ok', c: 'ok', txt: 'Holgura', frac: 1, rem: Infinity };
  if (sentAt) return sentAt <= limit ? { k: 'done', c: 'ok', txt: 'Cumplido', frac: 1 } : { k: 'late', c: 'crit', txt: 'Enviado fuera de plazo', frac: 1 };
  const rem = limit - now();
  if (rem < 0) return { k: 'over', c: 'crit', txt: 'Incumplido', frac: 1, rem };
  const frac = Math.min(1, rem / win);
  if (rem <= H || frac <= 0.33) return { k: 'warn', c: 'warn', txt: 'Advertencia', frac, rem };
  return { k: 'ok', c: 'ok', txt: 'Holgura', frac, rem };
}
const sevName = ['', 'Bajo', 'Medio', 'Alto', 'Crítico'];
const sev = (g, big) => `<span class="sev${big ? ' big' : ''}" data-g="${g}"><span class="pips" aria-hidden="true"><i></i><i></i><i></i><i></i></span>${sevName[g] || 'Desconocido'}</span>`;
const MS = { A: 'Aviso Temprano', P: 'Informe Preliminar', F: 'Informe Final' };
const STEPS = ['Detectado', 'Aviso Temprano', 'Informe Preliminar', 'Informe Final', 'Cerrado'];

/* ---------- estado ---------- */
let rows = [];
let inc = null;
let events = [];
let filt = 'all';
let tlFilter = 'all';

/* ---------- carga de datos ---------- */
async function loadCola() {
  try {
    const data = await SitracAPI.obtenerIncidentesActivos();
    rows = Array.isArray(data) ? data : [];
  } catch (err) {
    console.error('[loadCola] error:', err);
    rows = [];
  }
  renderDash();
  greet();
}
window.loadCola = loadCola;

async function openRow(dbId) {
  if (dbId === undefined || dbId === null) return;
  const id = Number(dbId);

  const [det, hist] = await Promise.all([
    SitracAPI.obtenerDetalleIncidente(id),
    SitracAPI.obtenerHistorialIncidente(id)
  ]);
  if (!det) { toast('Error al cargar detalle del incidente.'); return; }

  const toma = new Date(det.fechaTomaConocimiento).getTime();
  const limA = det.fechaLimiteAvisoTemprano ? new Date(det.fechaLimiteAvisoTemprano).getTime() : toma + 3 * H;
  const limP = det.fechaLimiteInformePreliminar ? new Date(det.fechaLimiteInformePreliminar).getTime() : toma + (det.clasificacion === 'OIV' ? 24 : 72) * H;
  const limF = det.fechaLimiteInformeFinal ? new Date(det.fechaLimiteInformeFinal).getTime() : toma + 15 * D;

  let estadoUI = det.estado;
  if (estadoUI === 'En triage') estadoUI = 'Detectado';
  else if (estadoUI === 'Aviso enviado') estadoUI = 'Aviso Temprano';
  else if (estadoUI === 'Preliminar enviado') estadoUI = 'Informe Preliminar';

  const sentA = (estadoUI !== 'Detectado') ? limA - 1000 : null;
  const sentP = (estadoUI === 'Informe Preliminar' || estadoUI === 'Informe Final' || estadoUI === 'Cerrado') ? limP - 1000 : null;
  const sentF = (estadoUI === 'Cerrado') ? limF - 1000 : null;

  inc = {
    id: det.codigoTicketInvgate || `INC-DB-${det.idIncidente}`,
    dbId: det.idIncidente,
    ticket: det.codigoTicketInvgate || 'N/A',
    toma,
    cls: det.clasificacion,
    g: det.gravedad,
    estado: estadoUI,
    sistema: det.sistemaAfectado || 'No especificado',
    titulo: 'Incidente de Seguridad (' + (det.origen || 'N/A') + ')',
    vector: det.vectorAtaque || 'En investigación',
    origenDet: det.origenDeteccion || det.origen || 'S/I',
    dp: 'En revisión',
    win: { A: limA - toma, P: limP - toma, F: limF - toma },
    sent: { A: sentA, P: sentP, F: sentF },
    closed: estadoUI === 'Cerrado',
    notifDetenidas: sentA !== null,
    lim: { A: limA, P: limP, F: limF }
  };

  events = (Array.isArray(hist) ? hist : []).map(h => ({
    id: 'EVT-' + h.idEventoHistorial,
    a: h.esAccionAutomatica ? 'sys' : 'ana',
    who: h.responsable || 'Sistema',
    t: new Date(h.fechaCambio).getTime(),
    title: h.ambitoCambio,
    from: h.estadoAnterior,
    to: h.estadoNuevo,
    gFrom: h.gravedadAnterior,
    gTo: h.gravedadNueva,
    forced: h.esAccionAutomatica ? 'Evaluación automática SLA' : null,
    comment: h.comentario || 'Sin justificación registrada.'
  }));

  actualizarPaginator(id);
  go('det');
  renderAll();
}
window.openRow = openRow;

/* ---------- paginador ---------- */
function calcularListaFiltrada() {
  let lista = rows.slice().sort((a, b) => a.limit - b.limit);
  if (filt === 'OIV' || filt === 'Esencial') lista = lista.filter(x => x.cls === filt);
  if (filt === 'crit') lista = lista.filter(x => {
    const c = classify(x.limit, x.win, null);
    return c.k === 'over' || (c.k === 'warn' && c.rem <= H);
  });
  return lista;
}

function actualizarPaginator(dbId) {
  const pager = $('.pager');
  const crumbId = $('#crumb-id');
  if (crumbId) crumbId.textContent = inc ? inc.id : '—';
  if (!pager) return;

  const listaActual = calcularListaFiltrada();
  const total = listaActual.length;
  const currentIndex = listaActual.findIndex(x => Number(x.dbId) === Number(dbId));
  const hayAnterior = currentIndex > 0;
  const haySiguiente = currentIndex >= 0 && currentIndex < total - 1;

  pager.innerHTML = `
    <button class="obtn" data-pg="prev" ${hayAnterior ? '' : 'disabled'} aria-label="Caso anterior">
      <svg class="i" viewBox="0 0 24 24"><path d="M15 5l-7 7 7 7"/></svg>Anterior
    </button>
    <span>Caso <b>${currentIndex >= 0 ? currentIndex + 1 : '—'}</b> de <b>${total || '—'}</b></span>
    <button class="obtn" data-pg="next" ${haySiguiente ? '' : 'disabled'} aria-label="Caso siguiente">
      Siguiente<svg class="i" viewBox="0 0 24 24"><path d="M9 5l7 7-7 7"/></svg>
    </button>`;
}

document.addEventListener('click', e => {
  const btn = e.target.closest('[data-pg]');
  if (!btn || btn.disabled) return;
  const dir = btn.dataset.pg;
  const lista = calcularListaFiltrada();
  const idx = lista.findIndex(x => Number(x.dbId) === Number(inc?.dbId));
  if (idx === -1) return;
  const target = dir === 'prev' ? lista[idx - 1] : lista[idx + 1];
  if (target) openRow(target.dbId);
});

/* ---------- navegación ---------- */
function go(v) {
  $$('.view').forEach(s => s.hidden = s.id !== 'v-' + v);
  $$('[role=tab]').forEach(b => b.setAttribute('aria-selected', b.dataset.go === v));
  window.scrollTo({ top: 0 });
  if (v === 'det' && inc) renderSla();
}

document.addEventListener('click', e => {
  // Botón "Revisar el caso más urgente"
  const btnUrgent = e.target.closest('button.pbtn');
  if (btnUrgent && btnUrgent.textContent.toLowerCase().includes('urgente')) {
    e.preventDefault();
    if (rows.length > 0) {
      const masUrgente = [...rows].sort((a, b) => a.limit - b.limit)[0];
      openRow(masUrgente.dbId);
    } else {
      toast('No hay incidentes en la cola.');
    }
    return;
  }
  // Tabs y data-go
  const g = e.target.closest('[data-go]');
  if (g) { e.preventDefault(); go(g.dataset.go); }
});

/* ---------- render: cola ---------- */
function renderDash() {
  try {
    const r = rows.map(x => ({ ...x, cl: classify(x.limit, x.win, null) }));
    const under1 = r.filter(x => x.cl.k === 'warn' && x.cl.rem <= H).length;
    const over = r.filter(x => x.cl.k === 'over').length;
    const inv = r.filter(x => x.origin === 'INVGATE').length;
    const mail = r.filter(x => x.origin === 'Correo').length;
    const man = r.length - inv - mail;

    $('#kpis').innerHTML = `
      <div class="kpi"><span class="l">Incidentes activos</span><span class="n">${r.length}</span><span class="s"><span>${r.filter(x => x.cls === 'OIV').length} OIV</span><span>${r.filter(x => x.cls === 'Esencial').length} Esenciales</span></span></div>
      <div class="kpi warn"><span class="l">SLAs críticos · vencen en menos de 1 h</span><span class="n">${under1}</span><span class="s">Requieren acción inmediata</span></div>
      <div class="kpi crit"><span class="l">SLAs incumplidos</span><span class="n">${over}</span><span class="s">El motor ya avanzó su estado</span></div>
      <div class="kpi sys"><span class="l">Generados automáticamente</span><span class="n">${inv + mail}<span style="font-size:16px;color:var(--fg-3)"> / ${r.length}</span></span>
        <div class="bar-split" aria-hidden="true"><i style="width:${r.length ? inv / r.length * 100 : 0}%;background:var(--primary)"></i><i style="width:${r.length ? mail / r.length * 100 : 0}%;background:#7fb5e8"></i></div>
        <span class="s"><span>INVGATE ${inv}</span><span>Correo ${mail}</span><span>Manual ${man}</span></span></div>`;

    let list = r.slice().sort((a, b) => a.limit - b.limit);
    if (filt === 'OIV' || filt === 'Esencial') list = list.filter(x => x.cls === filt);
    if (filt === 'crit') list = list.filter(x => x.cl.k === 'over' || (x.cl.k === 'warn' && x.cl.rem <= H));

    $('#triage').innerHTML = list.map(x => {
      const c = x.cl;
      const t = c.k === 'over' ? '+' + dur(now() - x.limit) : dur(c.rem);
      const lab = c.k === 'over' ? 'Vencido' : c.k === 'warn' ? 'Advertencia' : 'Holgura';
      const isAuto = x.origin !== 'Manual';
      return `<tr tabindex="0" data-open="${x.dbId}">
        <td><span class="mono" style="font-weight:600">${esc(x.id)}</span></td>
        <td><span class="origin">${ic(x.origin === 'Correo' ? 'inbox' : x.origin === 'INVGATE' ? 'bell' : 'user')}${esc(x.origin)}${isAuto ? '<span class="auto">AUTO</span>' : ''}</span></td>
        <td><span class="chip ${x.cls === 'OIV' ? 'ana' : ''}">${esc(x.cls)}</span></td>
        <td>${sev(x.g)}</td>
        <td>${esc(x.estado)}</td>
        <td><div class="tr" data-limit="${x.limit}" data-win="${x.win}" data-ms="${MS[x.ms] || ''}"><div class="row"><span class="ms">${MS[x.ms] || ''} · ${lab}</span><span class="t c-${c.c}">${t}</span></div><div class="track"><i class="bg-${c.c}" style="width:${c.k === 'over' ? 100 : Math.max(3, c.frac * 100)}%"></i></div></div></td>
      </tr>`;
    }).join('') || '<tr><td colspan="6" style="color:var(--fg-3)">Sin incidentes para este filtro.</td></tr>';
  } catch (err) {
    console.error('[renderDash] error:', err);
  }
}

function tickDash() {
  $$('#triage .tr').forEach(el => {
    const limit = +el.dataset.limit, win = +el.dataset.win;
    if (!Number.isFinite(limit) || !Number.isFinite(win)) return;
    const c = classify(limit, win, null);
    const t = $('.t', el), ms = $('.ms', el), bar = $('.track i', el);
    if (!t || !ms || !bar) return;
    t.textContent = c.k === 'over' ? '+' + dur(now() - limit) : dur(c.rem);
    t.className = 't c-' + c.c;
    ms.textContent = el.dataset.ms + ' · ' + (c.k === 'over' ? 'Vencido' : c.k === 'warn' ? 'Advertencia' : 'Holgura');
    bar.className = 'bg-' + c.c;
    bar.style.width = (c.k === 'over' ? 100 : Math.max(3, c.frac * 100)) + '%';
  });
  const r = rows.map(x => classify(x.limit, x.win, null));
  const ns = $$('#kpis .n');
  if (ns.length >= 3) {
    ns[1].textContent = r.filter(c => c.k === 'warn' && c.rem <= H).length;
    ns[2].textContent = r.filter(c => c.k === 'over').length;
  }
}

$('#filters').addEventListener('click', e => {
  const b = e.target.closest('button'); if (!b) return;
  filt = b.dataset.f;
  $$('#filters button').forEach(x => x.setAttribute('aria-pressed', x === b));
  renderDash();
});

$('#triage').addEventListener('click', e => {
  const tr = e.target.closest('tr[data-open]'); if (!tr) return;
  openRow(tr.dataset.open);
});
$('#triage').addEventListener('keydown', e => {
  if (e.key !== 'Enter') return;
  const tr = e.target.closest('tr[data-open]'); if (!tr) return;
  openRow(tr.dataset.open);
});

/* ---------- detalle ---------- */
function renderHead() {
  if (!inc) return;
  $('#head').innerHTML = `
    <div class="head-top"><span class="inc-id">${esc(inc.id)}</span>
      <div class="badges">${sev(inc.g, true)}<span class="chip ana">${esc(inc.cls)}</span><span class="chip">${esc(inc.estado)}</span><span class="chip warn">${ic('shield')}Datos personales: ${esc(inc.dp)}</span>${inc.closed ? '<span class="chip ok">Cerrado</span>' : ''}</div>
      <h1>${esc(inc.titulo)}</h1></div>
    <dl class="meta">
      <div><dt>Sistema afectado</dt><dd>${esc(inc.sistema)}</dd></div>
      <div><dt>Vector de ataque</dt><dd>${esc(inc.vector)}</dd></div>
      <div><dt>Origen de detección</dt><dd>${esc(inc.origenDet)}</dd></div>
      <div><dt>Ticket INVGATE</dt><dd class="mono">${esc(inc.ticket)}</dd></div>
      <div><dt>Toma de conocimiento</dt><dd class="mono">${fdate(inc.toma)}</dd></div></dl>`;
  const cur = STEPS.indexOf(inc.estado);
  $('#stepper').innerHTML = STEPS.map((s, i) =>
    `<div class="step ${i < cur ? 'done' : i === cur ? 'cur' : ''}"><span class="d"></span>${s}</div>`
  ).join('');
}

const SLA_DEF = [
  ['A', 'Aviso Temprano', '3 h desde la toma de conocimiento'],
  ['P', 'Informe Preliminar', '24/72 h según clasificación'],
  ['F', 'Informe Final', '15 días desde la toma de conocimiento']
];
function renderSla() {
  if ($('#v-det').hidden || !inc) return;
  const cls = {};
  SLA_DEF.forEach(([k]) => cls[k] = classify(inc.lim[k], inc.win[k], inc.sent[k]));
  const pending = SLA_DEF.map(([k]) => k).filter(k => !inc.sent[k]);

  let nx = '';
  if (inc.closed) {
    nx = `<div class="next ok">${ic('check')}Ciclo legal completado. Incidente cerrado, el motor ya no lo evalúa.</div>`;
  } else if (pending.length) {
    const k = pending.sort((a, b) => inc.lim[a] - inc.lim[b])[0];
    const c = cls[k], over = c.k === 'over';
    nx = `<div class="next ${c.c}">${ic(over ? 'alert' : 'flag')}<span>${over ? 'Plazo vencido sin confirmar' : 'Próximo plazo legal'}: <b>${MS[k]}</b></span><span class="t c-${c.c}">${over ? '+' + dur(now() - inc.lim[k]) + ' de retraso' : dur(c.rem) + ' restantes'}</span></div>`;
  }

  $('#slaPanel').innerHTML = `
    <div class="sec-h"><h2>${ic('clock')}Plazos legales</h2><span class="note">${ic('lock')}Las fechas no se pueden cambiar · fijadas el ${fdate(inc.toma)}</span></div>${nx}
    <div class="rings">${SLA_DEF.map(([k, name, w]) => {
      const c = cls[k];
      let big, lab, ico;
      if (c.k === 'done') { big = 'Enviado'; lab = 'a las ' + ftime(inc.sent[k]).slice(0, 5); ico = 'checkc'; }
      else if (c.k === 'late') { big = '+' + dur(inc.sent[k] - inc.lim[k]); lab = 'de retraso al enviar'; ico = 'alert'; }
      else if (c.k === 'over') { big = dur(now() - inc.lim[k]); lab = 'de retraso'; ico = 'alert'; }
      else { big = dur(c.rem); lab = 'restantes'; ico = 'clock'; }
      let foot;
      if (inc.sent[k]) foot = `Confirmado por el equipo el ${fdate(inc.sent[k])}.`;
      else if (c.k === 'over') foot = 'Aún no hay confirmación de envío a la ANCI. Cuando lo envíes, regístralo aquí.';
      else if (c.k === 'warn') foot = 'La ventana se está cerrando. Conviene preparar el documento ahora.';
      else foot = 'Vas bien de tiempo.';
      const pct = Math.round(c.frac * 100);
      return `<article class="sla ${c.c}">
        <div class="sla-top"><span class="sico ${c.c}">${ic(ico)}</span><div><h3>${name}</h3><div class="win">${w}</div></div><span class="chip ${c.c}">${c.txt}</span></div>
        <div class="cd">${big}<small>${lab}</small></div>
        <div class="prog ${c.c}"><i style="width:${pct}%"></i></div>
        <div class="dl">Vence el <b class="mono">${fdate(inc.lim[k])}</b></div>
        <div class="foot">${foot}</div></article>`;
    }).join('')}</div>`;
  renderKv();
}
function renderKv() {
  if (!inc) return;
  const kvRows = [
    ['Clasificación empresa', inc.cls],
    ['Canal de entrada', 'INVGATE (webhook)'],
    ['Analista asignado', 'C. Fuentes'],
    ['Recordatorios', inc.notifDetenidas ? '<span class="chip ok">Detenidos</span>' : '<span class="chip warn">Activos · cada 60 min</span>'],
    ['Revisión de datos personales', inc.dp]
  ];
  $('#kv').innerHTML = kvRows.map(([a, b]) => `<div><dt>${a}</dt><dd>${b}</dd></div>`).join('');
}

function renderTl() {
  if (!inc) return;
  const list = events.slice().sort((a, b) => b.t - a.t).filter(e => tlFilter === 'all' || e.a === tlFilter);
  $('#tl').innerHTML = list.map(e => {
    const sys = e.a === 'sys', forced = !!e.forced;
    const trans = (e.from || e.to)
      ? `<div class="trans">${e.from ? `<span class="chip">${esc(e.from)}</span>${ic('arrow')}` : ''}<span class="chip ${forced ? 'sys' : ''}">${esc(e.to || '')}</span></div>`
      : '';
    return `<li class="ev ${forced ? 'forced' : ''}">
      <span class="node ${e.a}" aria-hidden="true">${sys ? ic('cpu') : esc((e.who || '?').charAt(0))}</span>
      <div class="evc">
        <div class="evh"><span class="who">${esc(e.who)}</span><span class="chip ${e.a}">${sys ? (forced ? 'Acción automática' : 'Automático') : 'Analista'}</span>
          <span class="when"><span class="mono">${fdate(e.t)}</span><br>${ago(now() - e.t)}</span></div>
        ${forced ? `<div class="forcedtag">${ic('alert')}El sistema actuó solo. Motivo: ${esc(forced)}</div>` : ''}
        <div class="evt"><b style="font-weight:500">${esc(e.title || '')}</b></div>${trans}
        ${e.comment ? `<p class="cm">${esc(e.comment)}</p>` : ''}
        <div class="evid mono">${esc(e.id)}</div></div></li>`;
  }).join('') || '<li style="color:var(--fg-3);padding:12px 20px">Sin eventos registrados.</li>';
}
$('#tlf').addEventListener('click', e => {
  const b = e.target.closest('button'); if (!b) return;
  tlFilter = b.dataset.a;
  $$('#tlf button').forEach(x => x.setAttribute('aria-pressed', x === b));
  renderTl();
});

/* ---------- intervención ---------- */
function renderActs() {
  if (!inc) return;
  const finalReady = !!inc.sent.F, dis = inc.closed;
  $('#acts').innerHTML = `
    <button class="btn ${!inc.sent.A && !dis ? 'primary' : ''}" data-act="aviso" data-key="A" ${inc.sent.A || dis ? 'disabled' : ''}>${ic('send')}<span>Confirmar Aviso Enviado<small>${inc.sent.A ? 'Ya registrado' : 'Detiene los recordatorios automáticos'}</small></span></button>
    <button class="btn ${inc.sent.A && !inc.sent.P && !dis ? 'primary' : ''}" data-act="informe" data-key="I" ${dis || (inc.sent.P && inc.sent.F) ? 'disabled' : ''}>${ic('clip')}<span>Adjuntar Informe<small>Preliminar o Final, con documento de respaldo</small></span></button>
    <button class="btn danger" data-act="cerrar" data-key="C" ${dis || !finalReady ? 'disabled' : ''}>${ic('lock')}<span>Cerrar Ticket<small>${dis ? 'Incidente cerrado' : finalReady ? 'Fin del ciclo legal' : 'Requiere Informe Final enviado'}</small></span></button>`;
  renderHint();
}
function renderHint() {
  if (!inc) return;
  const dis = inc.closed;
  const o = ['A', 'P', 'F'].filter(k => !inc.sent[k] && inc.lim[k] > now()).sort((a, b) => inc.lim[a] - inc.lim[b])[0];
  const nxt = o ? { k: o, to: o === 'A' ? 'Informe Preliminar' : o === 'P' ? 'Informe Final' : 'Cerrado (por vencimiento)' } : null;
  $('#hint').innerHTML = dis
    ? `<b>Caso cerrado</b><span>El sistema ya no lo evalúa ni lo modifica.</span>`
    : nxt
      ? `<b>Qué pasará si no haces nada</b><span>Si ${MS[nxt.k]} vence sin acción en <span class="mono" style="color:var(--fg)">${dur(inc.lim[nxt.k] - now())}</span>, el sistema avanzará solo a «${nxt.to}» y lo dejará registrado.</span>`
      : `<b>Todos los plazos están al día</b><span>Solo falta cerrar el ticket.</span>`;
}

$('#acts').addEventListener('click', e => {
  const b = e.target.closest('[data-act]'); if (!b || b.disabled) return;
  modal(b.dataset.act);
});

/* ---------- modal ---------- */
let lastFocus = null;
function modal(kind) {
  if (!inc) return;
  lastFocus = document.activeElement;
  const cfg = {
    aviso: { t: 'Confirmar aviso temprano enviado', d: 'Registra que el aviso fue enviado a la ANCI.', btn: 'Confirmar envío', ph: 'Ej.: Aviso enviado por el portal de la ANCI, folio ANCI-2026-…', extra: '' },
    informe: {
      t: 'Adjuntar informe', d: 'Registra el envío del informe y vincula el documento.', btn: 'Registrar informe', ph: 'Resume qué contiene el informe y cómo se envió a la ANCI.',
      extra: `<div class="fld"><label for="f-tipo">Tipo de informe <em>*</em></label><select id="f-tipo"><option value="P" ${inc.sent.P ? 'disabled' : ''}>Informe Preliminar</option><option value="F" ${inc.sent.F ? 'disabled' : ''} ${inc.sent.P ? 'selected' : ''}>Informe Final</option></select></div>
      <div class="fld"><label for="f-file">Documento <em>*</em></label><input id="f-file" type="file"></div>`
    },
    cerrar: { t: 'Cerrar ticket', d: 'Estado terminal. El motor dejará de evaluar este incidente.', btn: 'Cerrar incidente', danger: true, ph: 'Indica el motivo del cierre y la referencia del Informe Final.', extra: '' }
  }[kind];
  if (!cfg) return;

  $('#modal').innerHTML = `<header><div><h2 id="mt">${cfg.t}</h2><p>${esc(inc.id)} · ${cfg.d}</p></div><button class="x" data-close aria-label="Cerrar">${ic('x')}</button></header>
    <form id="mf" novalidate><div class="body">${cfg.extra}
      <div class="fld"><label for="f-com">Comentario <em>(obligatorio)</em></label><textarea id="f-com" placeholder="${cfg.ph}" maxlength="500"></textarea><div class="cnt"><span id="f-hint">Cuéntale al equipo qué hiciste (mínimo 15 caracteres)</span><span id="f-n" class="mono">0/500</span></div></div>
      <div class="audit">${ic('lock')}<span>Esto quedará firmado como <b>C. Fuentes</b> con la hora del servidor. Cuando los endpoints POST estén listos, se persistirá en <b>TbAnciEventoHistorial</b>.</span></div></div>
    <footer><button type="button" class="mb" data-close>Cancelar</button><button type="submit" class="mb go ${cfg.danger ? 'danger' : ''}" id="f-ok" disabled>${cfg.btn}</button></footer></form>`;
  $('#ov').hidden = false;

  const com = $('#f-com'), ok = $('#f-ok');
  const check = () => {
    const n = com.value.trim().length;
    $('#f-n').textContent = com.value.length + '/500';
    const file = $('#f-file');
    const fileOk = !file || file.files.length > 0;
    ok.disabled = !(n >= 15 && fileOk);
    $('#f-hint').style.color = n >= 15 ? 'var(--ok)' : 'var(--fg-3)';
  };
  com.addEventListener('input', check);
  const f = $('#f-file'); if (f) f.addEventListener('change', check);

  $('#mf').addEventListener('submit', async e => {
    e.preventDefault();
    if (ok.disabled) return;

    const comment = com.value.trim();
    const tipoInforme = $('#f-tipo') ? $('#f-tipo').value : null;
    const archivo = $('#f-file')?.files?.[0]?.name || null;

    ok.disabled = true;
    ok.textContent = 'Procesando…';

    try {
      await commit(kind, comment, tipoInforme, archivo);
    } catch (err) {
      console.error('[modal/submit] error:', err);
      toast('Error al registrar la acción.');
      ok.disabled = false;
      ok.textContent = cfg.btn;
    }
  });

  setTimeout(() => ($('#f-tipo') || com)?.focus(), 20);
}
function closeModal() {
  $('#ov').hidden = true;
  if (lastFocus) lastFocus.focus();
}
$('#ov').addEventListener('click', e => {
  if (e.target.id === 'ov' || e.target.closest('[data-close]')) closeModal();
});
document.addEventListener('keydown', e => {
  if (e.key === 'Escape' && !$('#ov').hidden) closeModal();
});

/* ---------- commit (simulado hasta tener POST) ---------- */
async function commit(kind, comment, tipo, archivo) {
  console.log(`[commit] kind=${kind} id=${inc?.dbId} comment="${comment}" tipo=${tipo} archivo=${archivo}`);
  toast(`Simulación: acción «${kind}» registrada (esperando endpoints POST).`);
  closeModal();

  await new Promise(r => setTimeout(r, 400));

  await loadCola();
  if (inc && inc.dbId) await openRow(inc.dbId);
}

/* ---------- casos similares (mock) ---------- */
function renderSimilar() {
  const sim = $('#sim');
  if (!sim) return;
  sim.innerHTML = '<li style="color:var(--fg-3);padding:8px 0">Los casos similares se calcularán cuando exista el endpoint <code>/similares</code>.</li>';
}

/* ---------- toast ---------- */
let tt;
function toast(m) {
  const t = $('#toast');
  if (!t) return;
  t.textContent = m;
  t.hidden = false;
  clearTimeout(tt);
  tt = setTimeout(() => t.hidden = true, 3200);
}

/* ---------- greeting ---------- */
function greet() {
  const r = rows.map(x => classify(x.limit, x.win, null));
  const urg = r.filter(c => c.k === 'over' || (c.k === 'warn' && c.rem <= H)).length;
  $('#greet').textContent = r.length === 0
    ? 'No hay casos activos.'
    : `Tienes ${r.length} casos activos. ${urg === 0 ? 'Ninguno necesita atención inmediata.' : urg === 1 ? '1 necesita atención ahora.' : urg + ' necesitan atención ahora.'}`;
}

/* ---------- render global ---------- */
function renderAll() {
  renderHead();
  renderSla();
  renderTl();
  renderActs();
  renderDash();
  renderSimilar();
  greet();
}

/* ---------- init ---------- */
(async function init() {
  await loadCola();

  setInterval(() => {
    const clockEl = $('#clock');
    if (clockEl) clockEl.textContent = ftime(now()) + ' · GMT-3';
    const nextEl = $('#nextEval');
    if (nextEl) {
      const s = new Date().getSeconds();
      nextEl.textContent = '00:' + pad(s === 0 ? 0 : 60 - s);
    }
    if (!$('#v-det').hidden) { renderSla(); renderHint(); }
    if (!$('#v-dash').hidden) { tickDash(); greet(); }
    // Auto-refresh de la cola cada 60s
    if (new Date().getSeconds() === 0 && !document.hidden) loadCola();
  }, 1000);

  const clockEl = $('#clock');
  if (clockEl) clockEl.textContent = ftime(now()) + ' · GMT-3';
})();