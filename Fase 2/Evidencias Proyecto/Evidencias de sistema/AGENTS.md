# AGENTS.md — SITRAC (Sistema de Gestión de Incidentes · Ley N° 21.663)

> **Read this file before changing code.** It is the fast-path context for any AI agent
> working in this repository. For a human-oriented deep dive (in Spanish), see
> `GUIA_DE_ESTUDIO.md`. `ARCHITECTURE.md` exists but is partially stale: it uses old
> English class/project names and claims authentication is not implemented. **The source
> code is the source of truth**; verify every claim before acting.

---

## 1. What this project is

SITRAC is a cybersecurity incident-management console for ANCI / Ultraport, built around
Chilean **Ley N° 21.663 (Marco de Ciberseguridad)**. It:

1. Receives incidents from **InvGate** through a webhook (or manual/email channels).
2. Creates regulatory **milestones (hitos)**: Early Notice (3 h), Preliminary Report
   (24 h OIV / 72 h Essential), Final Report (15 days).
3. Snapshots **SLA deadlines** per incident.
4. Computes a **priority score** for smart notification routing.
5. Notifies via **email (MailKit/SMTP)** and **Microsoft Teams (Adaptive Cards)**.
6. Uses an **Outbox** table plus background workers to consolidate alerts and avoid alert
   fatigue.
7. Advances the incident lifecycle automatically when deadlines expire (transactional).
8. Escalates SLA thresholds via idempotent background alerts.
9. Exposes a static **vanilla-JS frontend** (login + operator console).

Domain language and most identifiers are **Spanish**. Keep it that way when adding code.

---

## 2. Technology stack

| Area | Technology / version |
|---|---|
| Runtime | .NET 10 (`net10.0`), SDK pinned by `global.json` to `10.0.400` (`rollForward: latestFeature`) |
| API | ASP.NET Core Web API, controllers, Swagger (Swashbuckle) in Development |
| Data access | Dapper 2.1.89 + `Microsoft.Data.SqlClient` 7.1.1, raw parameterized SQL |
| Database | SQL Server (2022 in Docker), database `DbSitrac` |
| Migrations | EvolveDB 3.2.0, SQL scripts V0001–V0007, executed at API startup |
| Auth | JWT Bearer; PBKDF2-SHA256 password hashing (100k iterations, 16-byte salt, 32-byte hash) |
| Email | MailKit 4.18.1 / MimeKit over SMTP |
| Teams | Incoming webhook + Adaptive Card JSON via `HttpClient` |
| Background work | `BackgroundService` + `PeriodicTimer` |
| Frontend | Static HTML + CSS + native ES modules (no framework, no bundler, no npm) |
| Containers | Docker multi-stage (`Sitrac.dockerfile`) + `docker-compose.yml` (SQL Server, smtp4dev, API) |

There is **no test project**, no linter, and no npm toolchain. Verification is currently
`dotnet build` plus manual API/smoke testing. Do not invent `npm test`/`pytest` commands.

---

## 3. Repository map

```
/workspace
├── Sitrac.sln                     Solution with 5 projects
├── global.json                    Pins .NET SDK 10.0.400
├── docker-compose.yml             SQL Server + smtp4dev + API
├── Sitrac.dockerfile              Multi-stage publish; copies Evolve scripts
├── ARCHITECTURE.md                Older architecture overview (partially stale)
├── AGENTS.md                      This file
├── GUIA_DE_ESTUDIO.md             Detailed Spanish study guide
│
├── Sitrac.Servidor/               ASP.NET Core Web API (presentation)
│   ├── Program.cs                 Composition root: config, DI, JWT, rate limit, CORS, Evolve
│   ├── Controllers/
│   │   ├── AutenticacionControlador.cs   /api/Auth (login, register)
│   │   └── IncidentesControlador.cs      /api/Incidentes (webhook + operator actions)
│   ├── appsettings.json           Dev defaults (connection string, JWT, SMTP, Teams)
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json    http profile → http://localhost:5116
│   └── Sitrac.Servidor.http       Scratch HTTP file (still points to weatherforecast)
│
├── Sitrac.Servicio/               Business logic (application layer)
│   ├── IncidenteServicio.cs       Core use cases, priority routing, transactional actions
│   ├── NotificacionServicio.cs    Email + Teams + consolidated summaries + escalation
│   ├── AutenticacionServicio.cs   Login, JWT issuance, PBKDF2 hash/verify
│   ├── AvanzadorCicloVidaServicio.cs     BackgroundService, 1 min
│   ├── DespachadorNotificacionServicio.cs BackgroundService, 15 s (Outbox)
│   ├── EscalamientoSlaServicio.cs        BackgroundService, 1 min (thresholds)
│   ├── Configuration/             ConfiguracionSmtp/Teams/Frontend/Jwt (+ SectionName)
│   ├── Dtos/                      *Solicitud / *Respuesta / ResultadoEnvio
│   ├── Helpers/CalculadoraPrioridad.cs   Priority scoring + labels/emojis
│   └── Interface/                 IIncidenteServicio, INotificacionServicio, IAutenticacionServicio
│
├── Sitrac.Repositorio/            Data access (Dapper)
│   ├── IncidenteRepositorio.cs    CRUD, list/detail/history, transactional state changes
│   ├── HitoRepositorio.cs         Milestones + idempotent notification reservation
│   ├── SlaRepositorio.cs          SLA parameters + per-incident snapshot
│   ├── RegistroNotificacionRepositorio.cs  Outbox insert/read/mark sent/failed/cancel
│   ├── ReglaEscalamientoRepositorio.cs     Escalation rules
│   ├── UsuarioRepositorio.cs      Users (login/register)
│   ├── Interfaces/                I*Repositorio contracts
│   └── Models/                    DB-shaped models / read models
│
├── Sitrac.AccesoDatos/            Connection factory
│   ├── IFabricaConexionBd.cs
│   └── FabricaConexionBd.cs       Reads "SitracConnection" via IConfiguration
│
├── Sitrac.MigracionBd/            Evolve SQL scripts (no Program.cs; not run standalone)
│   └── Scripts/
│       ├── V0001__InicializacionSitrac.sql          Catalogs + TbSitracIncidente
│       ├── V0002__TablasOperacionalesSitrac.sql     SLA, responsables, comunicaciones
│       ├── V0003__TablasInformesYRevisiones.sql     Reports, DPO, document annexes
│       ├── V0004__PoblamientoCatalogosSitrac.sql    Seed catalogs/SLA/responsables
│       ├── V0005__TablaHistorialSitrac.sql          Event history + change scopes
│       ├── V0006__HitosYEscalamiento.sql            Hitos, escalation rules, outbox
│       └── V0007__AuthUsuarios.sql                  TbSitracUsuario + seed admin
│
└── Sitrac.Frontend/               Static UI (served separately, port 5500)
    ├── login.html                 Login form (inline CSS)
    ├── index.html                 Operator console (dashboard/detail/design guide)
    ├── css/Style.css              Console styles (note: HTML links `css/style.css`)
    └── js/
        ├── api.js                 Fetch wrapper, JWT header, response mapping
        ├── login.js               Login + localStorage session
        └── app.js                 Console state, SLA countdowns, priority, modals, actions
```

---

## 4. Layering and dependency direction

```
Sitrac.Servidor (API)
   ├──> Sitrac.Servicio
   └──> Sitrac.Repositorio
Sitrac.Servicio
   └──> Sitrac.Repositorio
Sitrac.Repositorio
   └──> Sitrac.AccesoDatos
Sitrac.AccesoDatos
   └──> Microsoft.Data.SqlClient / IConfiguration only
```

Hard rules:

- Controllers orchestrate only: validate model state, call an application service, map
  exceptions to HTTP status codes. **No SQL, no business rules in controllers.**
- Services may depend on repository interfaces, options, logging, and notification
  abstractions. **Services must not use `SqlConnection` directly.**
- Repositories own SQL/Dapper and transactions. **Never call HTTP, SMTP, or Teams from a
  repository.**
- `Sitrac.AccesoDatos` must stay dependency-free of the other projects.
- Register every new abstraction in `Sitrac.Servidor/Program.cs` (lines ~35–48).
- Namespaces do not always match folder names: `Sitrac.Servicio/Interface/` uses
  namespace `Sitrac.Servicio.Interfaces`. Follow existing usage.

---

## 5. Composition root (`Sitrac.Servidor/Program.cs`)

Key responsibilities and approximate locations:

- Config loading with `appsettings.json`, environment-specific file, env vars, and user
  secrets in Development (lines ~10–24).
- Options binding for `SmtpConfig`, `TeamsConfig`, `FrontendSettings`, `JwtSettings`.
- DI registrations (lines ~35–48): connection factory singleton; repositories and
  services scoped; three hosted services.
- JWT Bearer validation (lines ~51–73), symmetric key from `JwtSettings:SecretKey`.
- Global fixed-window rate limiter: **100 requests/minute**, partitioned by username or
  remote IP, returns 429 (lines ~76–101).
- CORS policy `SitracPolicy` allows only `http://localhost:5500` and
  `http://127.0.0.1:5500`.
- Middleware order: CORS → RateLimiter → Authentication → Authorization → (Dev Swagger)
  → HTTPS redirection → controllers.
- `EjecutarMigracionesEvolve` (line ~133) runs at startup:
  - Reads `ConnectionStrings:SitracConnection`.
  - Connects to `master` and creates the target database with `CREATE DATABASE` if absent.
  - Runs Evolve from `../Sitrac.MigracionBd/Scripts`, with placeholder `${db}`.
  - Exceptions are logged and rethrown (API will not start if migrations fail).

When adding a new config section: create a `ConfiguracionX` class with
`public const string SectionName`, bind it in `Program.cs`, inject
`IOptions<ConfiguracionX>`, and document the keys in this file and the study guide.

---

## 6. Domain constants (critical)

### Incident states — `TbSitracIncidente.CodTipoEstadoIncidente`

| Value | Name | Meaning |
|---|---|---|
| 1 | `En triage` | New/being triaged; Early Notice pending |
| 2 | `Aviso enviado` | Early Notice confirmed; Preliminary Report pending |
| 3 | `Preliminar enviado` | Preliminary report filed; Final Report pending |
| 4 | `Cerrado` | Terminal; background engines stop evaluating it |

Frontend labels: `Detectado`, `Aviso Temprano`, `Informe Preliminar`, `Cerrado`.

### Milestone types — `TbSitracHito.CodTipoHito`

| Value | Name | Legal base |
|---|---|---|
| 1 | `Aviso Temprano` | 3 hours from knowledge |
| 2 | `Informe Preliminar` | 24 h (OIV) / 72 h (Essential) |
| 3 | `Informe Final` | 15 days |

Milestone states: `Pendiente`, `Cumplido`, `Cancelado`, `Aplazado`.

### Classification — `CodTipoClasificacionEmpresa`

- `1` = `Esencial`
- `2` = `OIV` (weights more; shorter preliminary deadline)

### Severity — `CodTipoGravedad`

`1` Bajo · `2` Medio · `3` Alto · `4` Crítico

### SLA catalog (seed)

| Classification | Early notice | Preliminary | Final |
|---|---|---|---|
| Esencial | 3 h | 72 h | 15 days |
| OIV | 3 h | 24 h | 15 days |

### Priority score — `CalculadoraPrioridad`

```
score = (gravedad × 3) + (clasificación × 2) + (urgencia × 40) + (pesoHito × 2)
urgencia = 1 - min(1, tiempoRestante / ventanaSLA)   // 0..1, overdue = 1
pesoHito: Aviso=3, Preliminar=2, Final=1
```

Labels: `Máxima` ≥ 30 · `Alta` ≥ 22 · `Media` ≥ 15 · `Baja` < 15.
**Routing rule:** score `>= 30` → immediate email + Teams; otherwise enqueue in Outbox
for consolidation. The same formula is duplicated in `Sitrac.Frontend/js/app.js`
(`PRIORITY_WEIGHTS`); change both if the model changes.

### Notification Outbox — `TbSitracNotificacionLog`

- Channels: `Email`, `Teams` (CHECK constraint).
- States: `Pendiente`, `Enviada`, `Fallida`, `Cancelada`.
- Pending selection: `Estado = 'Pendiente'`, `AuditNotDeleted = 1`,
  `FechaProgramada <= GETUTCDATE()`; batch size 50.
- Dispatcher marks all rows `Enviada` only if the consolidated send succeeds.

### Escalation rules — `TbSitracReglaEscalamiento`

Rules are per milestone type with `TiempoRestanteMinutos` (may be negative for overdue)
and optional `RepetirCadaMinutos`. The escalation worker picks the **most urgent rule
already crossed** (smallest threshold satisfying `remaining <= threshold`).

---

## 7. Key flows

### 7.1 InvGate webhook → incident

`POST /api/Incidentes/invgate/webhook` (anonymous, requires `X-Api-Key`):

1. `IncidentesControlador.RecepcionarTicketInvgate` validates `WebhookApiKey`
   (config or default `SITRAC_INVGATE_SECRET_2026`).
2. `IncidenteServicio.RegistrarIncidenteInvgateAsync` (line ~46):
   - Insert incident in state 1, channel 1 (INVGATE).
   - Read SLA template for the company classification.
   - Insert per-incident SLA snapshot (`TbSitracIncidentePlazoSla`).
   - Create Early Notice milestone (`CodTipoHito = 1`, `Pendiente`).
   - Compute priority.
   - If `score >= 30`: send email and Teams immediately (failures are logged/returned,
     not persisted for retry). Else insert two Outbox rows (Email + Teams) scheduled
     +30 seconds.
3. Returns `201 Created` with `{ Mensaje, Id }`. Never expose internal exception messages.

### 7.2 Automatic lifecycle (`AvanzadorCicloVidaServicio`, every 1 min)

`IncidenteServicio.ProcesarCicloDeVidaAsync` (line ~132):

- Loads active incidents (`state < 4`) with SLA snapshot.
- Transition rules:
  - `1 → 2` when `now >= FechaLimiteAvisoTemprano`
  - `2 → 3` when `now >= FechaLimiteInformePreliminar`
  - `3 → 4` when `now >= FechaLimiteInformeFinal` **and a pending Final milestone
    still exists** (i.e., the Final report was not filed). If the Final report was
    already registered, the engine does not auto-close; a human must call `/cerrar`.
    Note: the auto-close transaction marks that pending Final milestone as `Cumplido`,
    even though no report was filed — verify if this is the intended legal semantics.
- Each transition is one DB transaction: close previous milestone, create next milestone,
  update incident state, insert history event (`IdTipoAmbitoCambio = 1`, automatic).
- Attempts to enqueue notifications for the following milestone (see gotchas in §12).

### 7.3 Outbox consolidation (`DespachadorNotificacionServicio`, every 15 s)

1. Fetch up to 50 due pending rows.
2. `NotificacionServicio.EnviarResumenConsolidadoAsync`:
   - Split Email vs Teams rows.
   - Build one consolidated email (grouped by severity: critical/high/medium-low).
   - Build one consolidated Teams Adaptive Card.
3. On success, mark each row `Enviada`; on failure, leave pending for the next cycle
   (no explicit attempt increment in this path).

### 7.4 SLA escalation (`EscalamientoSlaServicio`, every 1 min)

1. Load pending milestones not silenced.
2. Load rules per milestone type (cached per cycle).
3. Find most urgent crossed rule.
4. **Idempotency:** `HitoRepositorio.ReservarNotificacionAsync` performs a conditional
   `UPDATE` on the milestone (`UltimaReglaNotificada`, `ProximaNotificacionAt`,
   `ConteoNotificaciones`). Only one worker/cycle can win the reservation.
5. On success, send escalation email + Teams card.

### 7.5 Human actions (authenticated)

All under `[Authorize]` on `IncidentesControlador`:

- `POST /api/Incidentes/{id}/confirmar-aviso` — state must be 1; closes Early Notice,
  creates Preliminary milestone, transitions to 2, writes history; also enqueues a Teams
  notification for the new milestone.
- `POST /api/Incidentes/{id}/informe` — `tipo` `P` or `F`; validates expected state
  (P: 2, F: 3); inserts Preliminary or Final report + DPO fields, closes current
  milestone, creates next milestone (P only), transitions state, writes history.
  All in one transaction. Errors: `KeyNotFoundException` → 404,
  `InvalidOperationException` → 400.
- `POST /api/Incidentes/{id}/cerrar` — requires state 3 and no pending Final milestone;
  sets state 4, cancels pending milestones, writes history.

Every manual action requires a comment of **15–500 characters** (DTO DataAnnotations).

### 7.6 Authentication

- `POST /api/Auth/login` → validates user + PBKDF2 hash, returns JWT (8 h default) with
  claims: name identifier, username, role, full name.
- `POST /api/Auth/register` → creates an `Admin` user. **Currently anonymous and
  unguarded** (development bootstrap). Do not expand it; secure or remove before prod.
- Frontend stores `sitrac_token` and `sitrac_user` in `localStorage`; `authFetch` adds
  `Authorization: Bearer …` and redirects to `login.html` on 401.

---

## 8. Configuration and secrets

`appsettings.json` keys (also overridable with env vars / Docker env):

| Section | Key | Notes |
|---|---|---|
| `ConnectionStrings` | `SitracConnection` | SQL Server; Evolve creates DB if missing |
| `JwtSettings` | `SecretKey`, `Issuer`, `Audience`, `ExpirationHours` | Dev default key is insecure |
| `WebhookApiKey` | — | Expected `X-Api-Key` for InvGate |
| `SmtpConfig` | `Host`, `Port`, `EnableSsl`, `UserName`, `Password`, `DestinatarioDemo`, `NombreRemitente`, `TimeoutMs`, `MaxReintentos`, `IntervaloEntreEnviosMs` | `EstaConfigurado()` requires Host/UserName/Password |
| `TeamsConfig` | `Habilitado`, `WebhookUrl`, `UmbralPrioridadMinima`, `TimeoutMs` | Disabled by default |
| `FrontendSettings` | `BaseUrl` | Used for deep links inside emails/cards |

- `SmtpConfig.UserName`/`Password` are intentionally blank in `appsettings.json`
  (use user secrets / env vars).
- Docker Compose supplies env-var overrides; `smtp4dev` captures mail and exposes a UI at
  `http://localhost:5000`.
- Never commit real secrets. Prefer `dotnet user-secrets` (UserSecretsId is configured)
  or environment variables.

---

## 9. Coding conventions

- **Language:** identifiers, SQL columns, comments, logs, and UI copy are Spanish.
  Use Spanish PascalCase for classes (`IncidenteServicio`), `I` prefix for interfaces,
  `Async` suffix for async methods, `_camelCase` for private fields.
- **Style:** block-scoped namespaces, 4 spaces, nullable enabled, implicit usings.
  Match the surrounding file; do not reformat unrelated code.
- **Logging:** `ILogger<T>` with structured placeholders and bracketed context, e.g.
  `_logger.LogInformation("[Dispatcher] Enviando lote de {Count}", count);`.
- **Errors:** throw `KeyNotFoundException` for missing entities,
  `InvalidOperationException` for invalid state transitions; controllers map them to
  404/400/500. Do not expose `ex.Message` on generic 500s.
- **DTO validation:** DataAnnotations on request DTOs; check `ModelState.IsValid` in
  controllers.
- **SQL:** always parameterized, `using var connection = _connectionFactory.CreateConnection();`,
  multi-table operations wrapped in `BeginTransaction` + `Commit`/`Rollback`, and
  soft-delete filters (`AuditNotDeleted = 1`).
- **Frontend:** native ES modules; escape untrusted text with `esc()`; keep API access
  inside `js/api.js`; keep domain labels in Spanish.
- **No comments** unless they explain non-obvious business/legal rules.

---

## 10. Build, run, and verification

Prerequisites: .NET SDK 10.0.400+; SQL Server (local or Docker); optional SMTP capture
server; a static file server for the frontend on port 5500.

```bash
# Restore + build (only reliable automated verification today)
dotnet restore Sitrac.sln
dotnet build Sitrac.sln

# Run API (Swagger in Development at /swagger)
dotnet run --project Sitrac.Servidor/Sitrac.Servidor.csproj --launch-profile http
# → http://localhost:5116

# Full local stack (SQL Server + smtp4dev + API)
docker compose up --build
# API:      http://localhost:5116
# SQL:      localhost:1433 (sa / Sitrac2026!Strong)
# SMTP UI:  http://localhost:5000

# Frontend (CORS only allows port 5500)
npx serve Sitrac.Frontend -l 5500
# or VS Code Live Server on port 5500
```

Migrations run automatically when the API starts. If Evolve cannot find the scripts,
start the process with the repository/project working directory such that the relative
path `../Sitrac.MigracionBd/Scripts` resolves correctly (the Docker image copies scripts
to `/Sitrac.MigracionBd/Scripts` explicitly).

There is no test suite. Before claiming success: build must pass, API must start, and
the relevant endpoint should return the expected status/body with a real DB.

---

## 11. Database and migration rules

- Every table uses the `TbSitrac` prefix and audit columns
  (`AuditNotDeleted`, `AuditCreateDate`, `AuditLastUpdateDate`, `AuditUserLastUpdate`).
- **Never edit an applied migration.** Add a new `V000N__Descripcion.sql`; Evolve applies
  scripts in version order and records them in its schema history table.
- Evolve does not create the database itself; `Program.cs` does it via `master`.
- Seeded catalog IDs are meaningful and hardcoded throughout services:
  classification 1/2, severity 1–4, states 1–4, milestone types 1–3, channels 1–3,
  change scopes 1–4 (5 exists), personal-data types 1/2/3. Changing seed IDs breaks logic.
- Reports/DPO tables are written by `RegistrarInformeTransaccionalAsync`; document
  annexes exist in the schema but are not yet used by the API.
- Some SQL uses `GETDATE()` (local server time) and some uses `GETUTCDATE()`, while C#
  business logic uses `DateTime.Now`. Avoid introducing new mixed semantics; if touching
  time logic, choose one convention and note the impact.

---

## 12. Known gotchas / rough edges (verify before relying on them)

1. **Stale documentation:** `ARCHITECTURE.md` references `Sitrac.Server`,
   `Sitrac.Service`, `*Repository` (English) and says auth is not implemented. Trust the
   code, not that file.
2. **Seeded admin hash is a placeholder.** `V0007__AuthUsuarios.sql` inserts a fake hash
   for `admin`; it does not match a real PBKDF2 hash for `Admin123!`, so that login will
   fail. Create a user via `/api/Auth/register` (development) or generate a legitimate
   PBKDF2 hash with `AutenticacionServicio.HashPassword`.
3. **`/api/Auth/register` is anonymous and always creates `Admin`.** Development
   bootstrap only; must be protected/removed for production.
4. **Lifecycle notification enqueue mismatch:** after auto-advancing, the code looks up
   a pending milestone of type `nuevoEstado + 1` instead of the milestone it just created
   (`hitoACrear.CodTipoHito`). As written, auto-advanced Preliminary milestones may not
   get enqueued and the lookup may return `null`. Confirm before changing behavior.
5. **Immediate critical notifications are not retried.** They bypass the Outbox; SMTP or
   Teams failures are returned/logged but not persisted.
6. **Priority formula is duplicated** in backend `CalculadoraPrioridad` and frontend
   `app.js`; keep them in sync.
7. **Frontend URLs are hardcoded** to `http://localhost:5116/api` in `api.js` and
   `login.js`; CORS allows only port 5500. Update both for deployment.
8. **CSS file-name case mismatch:** HTML links `css/style.css`, file is `css/Style.css`.
   Case-sensitive Linux servers may return 404 for styles.
9. **`Sitrac.MigracionBd` has no executable entry point**; scripts are applied by the API
   at startup. Do not try to run it as a standalone migration tool.
10. **`Sitrac.Servidor.http` still contains a weatherforecast sample**; it is not a real
    API smoke test.
11. **Soft delete vs. hard delete:** queries filter `AuditNotDeleted = 1`; updates usually
    do not set it. Prefer soft delete for new features.
12. **No integration/unit tests and no CI configuration** in the repository.

---

## 13. Where to make common changes

| Goal | Touch |
|---|---|
| New REST endpoint | Controller + application service interface/implementation + DI if new service |
| New business rule / transition | `IncidenteServicio` (validate state before repository calls) |
| New SQL query | Repository interface + implementation (parameterized, soft-delete aware) |
| New table/column/catalog | New `V000N__*.sql`; update models/repositories |
| New notification channel | Implement `INotificacionServicio` (or extend it) + options + DI |
| Change priority model | `CalculadoraPrioridad` **and** frontend `PRIORITY_WEIGHTS`/`calcularPrioridad` |
| Change SLA windows | Seed/catalog rows (`TbSitracTipoPlazoSla`) and any hardcoded fallbacks |
| Change escalation thresholds | `TbSitracReglaEscalamiento` rows; no code change needed |
| Change JWT/auth | `AutenticacionServicio` + `Program.cs` JWT block + `ConfiguracionJwt` |
| Change UI | `Sitrac.Frontend/index.html`, `Style.css`, `js/app.js`, `js/api.js` |
| Change config | `Configuracion*` class + `Program.cs` binding + `appsettings*.json` + docs |

---

## 14. Agent checklist before finishing

- [ ] Read the affected files and the nearest existing implementation; follow its pattern.
- [ ] Preserve Spanish naming, layering, and DI conventions.
- [ ] Parameterize SQL; keep multi-table mutations transactional; respect soft delete.
- [ ] Validate state transitions in the service, not only in the controller.
- [ ] Do not expose internal exception messages to API clients.
- [ ] Never commit secrets or real credentials.
- [ ] If schema changed, add a new immutable migration (do not edit old ones).
- [ ] If priority/config/constants changed, update the frontend and both docs.
- [ ] Run `dotnet build Sitrac.sln`; if possible, start the API and exercise the endpoint.
- [ ] Update `AGENTS.md` / `GUIA_DE_ESTUDIO.md` when architecture or flows change.
