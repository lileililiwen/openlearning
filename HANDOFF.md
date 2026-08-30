# HANDOFF.md — Authoring Versioning & Preview

> Progress / status document. Reusable by another AI session or conversation to
> continue this work. **Last updated: 2026-08-30.** Branch: `main`.
> Source change: `openspec/changes/authoring-versioning-preview` (spec `authoring-versioning-preview`).

## Goal of this change
Add immutable published revisions, draft editing, validation, owner-only preview,
publish, rollback, and audit history. Learner catalog/delivery must read **only** the
active published revision. (See `openspec/changes/authoring-versioning-preview/{proposal,design,spec,tasks}.md`.)

## Status
**Core back-end complete and committed-ready.** Instructor/learner UI pages and
HTTP smoke tests are NOT yet implemented (deferred — see Remaining below).

## What is DONE

- New module `src/OpenLearning.Authoring/` (no cross-module deps; allowed deps = none).
  - `Models/AuthoringModels.cs` — `CourseRevision`, `CourseRevisionPointer`,
    `RevisionValidationResult`, `RevisionAudit`, `RevisionState`, internal `ContentSnapshot` DTOs.
  - `Configuration/AuthoringConfiguration.cs` — 4 `IEntityTypeConfiguration` classes
    (indexes on `(CourseId,State)`, `(CourseId)`, pointer FKs, `RowVersion` concurrency token).
  - `Services/RevisionLifecycleService.cs` — `EnsureDraftAsync`, `EditDraftAsync`,
    `ValidateDraftAsync`, `GetPreviewAsync`, `PublishAsync`, `UnpublishAsync`,
    `RollbackAsync`, `GetActiveRevisionAsync`. Owner checks throw
    `UnauthorizedRevisionOperationException`; concurrent publish throws `RevisionConcurrencyException`.
  - `AuthoringModuleExtensions.cs` — `AddAuthoringModule`.
- EF migration `20260830030603_AddAuthoringRevisions` (tables `CourseRevision`,
  `CourseRevisionPointer`, `RevisionValidationResult`, `RevisionAudit`) + updated
  `ApplicationDbContextModelSnapshot.cs`.
- Wiring:
  - `ApplicationDbContext.cs` registers the config assembly scan.
  - `OpenLearning.Data.csproj`, `OpenLearning.Web.csproj`, `OpenLearning.UnitTests.csproj`
    reference the module.
  - `OpenLearning.Web/Program.cs` calls `AddAuthoringModule()` (+ `using OpenLearning.Authoring;`).
  - Architecture fixture (`ModuleArchitectureTests.cs`) lists `OpenLearning.Authoring` with no deps.
- Unit tests `tests/OpenLearning.UnitTests/Authoring/RevisionLifecycleServiceTests.cs`
  (7 tests) covering draft isolation, validation errors, publish promote, invalid-publish
  rejection, owner authorization, and rollback. **All 7 pass.**
- Architecture tests: **5/5 pass.**

## Remaining / BLOCKED (not done)

- **Task 3 (UI):** Instructor revision/history/preview Razor pages (dirty/validation/publish
  states) and learner delivery/catalog queries switched to `GetActiveRevisionAsync`.
  No pages or controllers exist yet for this module.
- **Task 3 (HTTP smoke tests):** role + non-owner Razor smoke tests (spec verification asks for them).
- **Task 4 (PostgreSQL integration tests):** the `RevisionConcurrencyException` path and
  atomic promotion are only covered by unit tests on the InMemory provider. A relational
  integration test (two concurrent publishes, stale-content/revoked-access) is still needed.
  InMemory ignores transactions, so the explicit-transaction path was intentionally avoided;
  `PublishAsync` uses two `SaveChanges` (promote+new draft, then repoint pointer).
- The service models "owner" as the actor that created the first draft (`CourseRevisionPointer.OwnerId`),
  not `CourseManagement.Course.InstructorId`. Wire to the real course owner when building the UI.

## Key decisions / gotchas for the next session
- Concurrency: `CourseRevisionPointer.RowVersion` is a `byte[]` concurrency token bumped to
  `Guid.NewGuid().ToByteArray()` on every mutation; relational providers enforce optimistic
  concurrency. Do NOT switch to `IsRowVersion()` (Npgsql has no rowversion type).
- FK assignment order bug already fixed: pointer FKs must be set **after** `SaveChanges`
  assigns the store-generated draft id (scalar FKs are not fixed up automatically).
- Build is strict: `TreatWarningsAsErrors=true` + Sonar + `csharp_style_expression_bodied_methods=false`.
  Use block bodies for methods/constructors; avoid unnecessary `!` null-forgiving operators (S8969).

## Verification already run (green)
- `dotnet test tests/OpenLearning.UnitTests --filter FullyQualifiedName~Authoring` → 7 passed.
- `dotnet test tests/OpenLearning.ArchitectureTests` → 5 passed.
- `dotnet build src/OpenLearning.Data` → 0 warnings / 0 errors.

## Suggested next steps (checklist)
1. Add instructor pages under `src/OpenLearning.Web/Pages/Courses/...` calling
   `RevisionLifecycleService`; guard with owner/role authorization.
2. Replace learner catalog/delivery course reads with `GetActiveRevisionAsync` so drafts never leak.
3. Add PostgreSQL integration tests for publish concurrency + rollback history.
4. Run full solution build + `dotnet format` + remaining test suites before marking Task 4 done.
5. Archive the OpenSpec change when Tasks 3–4 are complete (`openspec archive ...`).
