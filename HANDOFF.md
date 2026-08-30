# HANDOFF.md — Change Status & Handoff

> Progress / status document. Reusable by another AI session or conversation to
> continue this work. **Last updated: 2026-08-30.** Branch: `main`.
>
> Most recent change: **extensibility-integration-contracts** — COMPLETE & ARCHIVED
> (`openspec/changes/archive/2026-08-30-extensibility-integration-contracts`).
> Previous changes `ui-state-and-feedback-contract`, `learner-resume-continuity`,
> `outcomes-mastery-operations` also COMPLETE & ARCHIVED.

## Latest change: Extensibility & Integration Contracts (extensibility-integration-contracts)

> **Status: COMPLETE & ARCHIVED** (`openspec/changes/archive/2026-08-30-extensibility-integration-contracts`).

### Goal
Define a shared integration contract for LTI, SCORM, webhooks, and future external
tools: lifecycle/version compatibility, idempotency, bounded retry, timeout behavior,
dead-letter, audit records, and operator diagnostics — without duplicating protocol logic
inside the contract module.

### What is DONE
- **New module** `src/OpenLearning.Integrations/` (no cross-module deps; allowed deps = none).
  - `Models/IntegrationModels.cs` — `IntegrationRegistration` (type, `ContractVersion`,
    `CapabilitiesJson`, `TenantId`, `IsEnabled`, `EndpointReference`, `SecretReference`),
    `IntegrationDelivery` (idempotency key, status, attempts, error category, redacted
    `PayloadReference`), `IntegrationAuditRecord` (action, disposition, redacted payload, actor).
  - `Configuration/IntegrationConfiguration.cs` — 3 `IEntityTypeConfiguration` classes
    (unique `(TenantId, IdempotencyKey)`, indexes on tenant/registration/status).
  - `Contracts/IIntegrationAdapter.cs` — protocol-neutral `IIntegrationAdapter`
    (Validate/Execute/Health), `IntegrationDeliveryRequest`, `IntegrationResult`,
    `IntegrationErrorCategory`.
  - `Adapters/HttpIntegrationAdapterBase.cs` + `Webhook`/`Lti`/`Scorm` adapters — shared
    HTTP delivery with timeout enforcement and failure classification (4xx permanent,
    5xx/timeout retryable); protocol modules stay free of transport concerns.
  - `Services/IntegrationOperationsService.cs` — declare, tenant-scoped enable/disable,
    idempotent delivery (duplicate key returns prior disposition, no external call),
    bounded retry → dead-letter, unknown contract version fails closed, disable fails
    closed, secret redaction before any persistence/audit, replay that preserves the
    original idempotency key and writes a replay audit.
- **EF migration** `20260830084811_AddIntegrations` (+ updated `ApplicationDbContextModelSnapshot.cs`)
  creating `IntegrationRegistration`, `IntegrationDelivery`, `IntegrationAuditRecord`.
- **Wiring**: `ApplicationDbContext` config scan, `Data` + `Web` + test-project references,
  `Program.cs` calls `AddIntegrationsModule()` (registers the service + 3 adapters),
  architecture fixture lists `OpenLearning.Integrations` with no deps, solution lists the new project.
- **Admin UI** `src/OpenLearning.Web/Pages/Admin/Integrations/Index` — declare integration,
  enable/disable, list deliveries + audit, replay dead-letter; gated to `RequireAdmin`
  (with an explicit role check so it is testable without the MVC pipeline).
- **Tests** (20, all green):
  - `Integrations/IntegrationOperationsServiceTests.cs` (12) — declare validation,
    unknown-version fails closed with no external call, disabled fails closed, duplicate
    idempotency returns prior disposition, timeout exhaustion → dead-letter, permanent
    failure dead-letters immediately, success audit without secrets, tenant-scoped
    enable/replay authorization.
  - `Integrations/IntegrationOperationsServicePostgresTests.cs` (4, against dedicated
    `openlearning_integration_integrations`) — relational persist, unique-index enforcement,
    cross-tenant replay throws, dead-letter persistence.
  - `Web/IntegrationsPageTests.cs` (4) — admin can view, non-admin forbidden, admin declares,
    admin replays dead-letter.

### Verification (green)
- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors (8.0.424 SDK).
- `dotnet test tests/OpenLearning.UnitTests --filter FullyQualifiedName~Integrations` → 20 passed.
- `dotnet test tests/OpenLearning.ArchitectureTests` → 5 passed.
- `dotnet format --verify-no-changes` → clean for the whole solution.

### Key decisions / gotchas
- **Self-contained delivery runtime**: the delivery/retry/dead-letter loop lives in
  `IntegrationOperationsService` over the base `DbContext` (no dependency on the `Jobs`/
  `AsyncIO` modules). This keeps the module dependency-clean (allowed deps = none) and the
  architecture test green. Production wiring can push deliveries to the existing job queue
  behind the same adapter boundary without changing the contract.
- **No secrets stored**: `IntegrationRegistration.SecretReference` holds only a reference
  (never the value); `IntegrationDelivery.PayloadReference` and audit `RedactedPayload` are
  run through `Redact()` which masks long token-like runs and secret-keyed JSON values
  (`***REDACTED***`). The delivery row therefore never holds the raw payload.
- **Fails closed**: unknown contract version, disabled integration, missing adapter, and
  validation failure all short-circuit before any external call and record an audit entry.
- **Tenant scoping**: every read/write enforces `TenantId`; cross-tenant enable/replay
  throws `UnauthorizedIntegrationOperationException`. The admin page uses a single
  `platform` tenant (multi-tenant is out of scope for this change but the contract supports it).
- **Adapter extensibility**: protocol modules register their own `IIntegrationAdapter`
  implementations; `Webhook`/`Lti`/`Scorm` ship as built-ins. Unknown adapter type →
  permanent dead-letter (no external side effect).
- Build is strict: `TreatWarningsAsErrors=true` + Sonar + `EnforceCodeStyleInBuild=true`.
  Block bodies for methods, no unused private members, `_camelCase` field prefix
  (incl. private `const`), `Regex` constructed with a `TimeSpan` timeout (S6444), and
  `dotnet format --verify-no-changes` must stay clean.

---

## Suggested next steps (checklist)

1. **Archive complete** — `extensibility-integration-contracts` archived; no further tasks remain for it.
2. **Next spec to implement next turn: `localization-foundation`** — establish the
   ASP.NET Core localization foundation and migrate learner-facing Chinese/English literals
   into `.resx` resources (English default + Chinese) so `<html lang>` can be set correctly
   and no page mixes languages. (This is the spec the user selected as the follow-up to
   `extensibility-integration-contracts`.)
3. Remaining active specs after that: `learner-accessibility-compliance`,
   `learner-experience-quality`, `lesson-sequencing-navigation`, `offline-sync-resilience`,
   `platform-quality-release-gates`, `responsive-design-system`.
4. Before pushing `main`, run the full test suites and `dotnet format` if required by CI,
   and apply the `AddIntegrations` migration to the target database (`dotnet ef database update`).

---

## Previously archived changes (summary)

### UI State & Feedback Contract (ui-state-and-feedback-contract) — COMPLETE & ARCHIVED
Shared loading/empty/error/toast partials, global `TempData["Message"]` toast (single
renderer, per-page blocks removed), `ConfirmTagHelper` for destructive actions, input
preservation on failed posts, and progressive-enhancement for mark-complete / save-note.
Verification green (build, `dotnet format`, `ConfirmTagHelperTests` ×3).

### Learner Resume Continuity (learner-resume-continuity) — COMPLETE & ARCHIVED
`IResumeService` + `ResumeService` reuse `LessonAccess` (no schema migration) so every
"Continue learning" / "Resume" entry point navigates to the last-viewed lesson. 6 service
unit tests + 5 architecture tests + build/format green.

### Outcomes & Mastery Operations (outcomes-mastery-operations) — COMPLETE & ARCHIVED
`OpenLearning.Outcomes` module (models, config, operations service, module extensions), EF
migration `AddOutcomes`, instructor outcomes UI with mastery + intervention matrix, and
`DbOutcomeActivitySource` adapter. 19 service/integration/web tests + 5 architecture tests,
build/format green. Next follow-up: wire `IOutcomeActivitySource` adapters for Assignment/
Exam results so mastery covers all assessable activity types.
