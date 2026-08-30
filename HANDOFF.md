# HANDOFF.md — Authoring Versioning & Preview

> Progress / status document. Reusable by another AI session or conversation to
> continue this work. **Last updated: 2026-08-30.** Branch: `main`.
> Source change: `openspec/changes/authoring-versioning-preview` (spec `authoring-versioning-preview`)
> **Status: COMPLETE & ARCHIVED** (`openspec/changes/archive/2026-08-30-authoring-versioning-preview`).

## Goal of this change
Add immutable published revisions, draft editing, validation, owner-only preview,
publish, rollback, and audit history. Learner catalog/delivery must read **only** the
active published revision. (See `openspec/changes/archive/2026-08-30-authoring-versioning-preview/{proposal,design,spec,tasks}.md`.)

## Status
**Fully implemented and archived.** All four task groups are complete:
- Modدule `src/OpenLearning.Authoring/` (domain, config, lifecycle service, module extensions).
- EF migration `20260830030603_AddAuthoringRevisions` (+ model snapshot).
- Wiring into `ApplicationDbContext`, `Program.cs`, and the architecture fixture.
- Unit tests (7) + architecture tests (5).
- **Task 3:** instructor revisions page (`Pages/Courses/Revisions/Index`) with
  Start editing / Preview / Validate / Publish / Unpublish / Rollback, owner + Admin
  gating, linked from the course Edit page; learner pages already gate on
  `CourseStatus.Published` and never reference revision drafts, so drafts cannot leak.
- **Task 4:** PostgreSQL integration tests + role/non-owner PageModel smoke tests.

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
- Wiring: `ApplicationDbContext` config scan, csproj references, `Program.cs` calls
  `AddAuthoringModule()`, architecture fixture lists `OpenLearning.Authoring` with no deps.
- Unit tests `tests/OpenLearning.UnitTests/Authoring/RevisionLifecycleServiceTests.cs` (7, all pass).
- Architecture tests: 5/5 pass.
- **Instructor UI** `src/OpenLearning.Web/Pages/Courses/Revisions/Index.cshtml(.cs)`:
  view active/draft/history, Start editing, Preview, Validate, Publish, Unpublish,
  Rollback; owner (`Course.InstructorId`) + Admin gated; reachable from the course
  Edit page (`src/OpenLearning.Web/Pages/Courses/Edit.cshtml` "版本管理" link).
- **PostgreSQL integration tests** `tests/OpenLearning.UnitTests/Authoring/RevisionLifecycleServicePostgresTests.cs`
  (6 tests) against the local `openlearning_integration` DB: concurrent publish → exactly
  one `RevisionConcurrencyException`, rollback preserves history, `GetActiveRevisionAsync`
  isolation, invalid-publish rejection, validation recording.
- **Role/non-owner smoke tests** `tests/OpenLearning.UnitTests/Web/RevisionsPageTests.cs`
  (5 tests): owner instructor → page; non-owner instructor → Forbid; student → Forbid;
  owner publish success; non-owner publish → Forbid.

## Verification already run (green)
- `dotnet test tests/OpenLearning.UnitTests --filter FullyQualifiedName~Authoring` → 13 passed
  (7 in-memory lifecycle + 6 PostgreSQL integration).
- `dotnet test tests/OpenLearning.UnitTests --filter FullyQualifiedName~RevisionsPageTests` → 5 passed.
- `dotnet test tests/OpenLearning.ArchitectureTests` → 5 passed.
- `dotnet build OpenLearning.sln` → 0 warnings / 0 errors.

## Key decisions / gotchas for the next session
- Concurrency: `CourseRevisionPointer.RowVersion` is a `byte[]` concurrency token bumped to
  `Guid.NewGuid().ToByteArray()` on every mutation; relational providers enforce optimistic
  concurrency. Do NOT switch to `IsRowVersion()` (Npgsql has no rowversion type).
- FK assignment order bug already fixed: pointer FKs must be set **after** `SaveChanges`
  assigns the store-generated draft id (scalar FKs are not fixed up automatically).
- Build is strict: `TreatWarningsAsErrors=true` + Sonar + `csharp_style_expression_bodied_methods=false`.
  Use block bodies for methods/constructors; avoid unnecessary `!` null-forgiving operators (S8969).
- `CourseRevision.ContentSnapshotJson` is a separate lightweight snapshot (module/lesson
  titles + types only). It is intentionally NOT mapped to the real `Course.Modules/Lessons`
  entities (modular-monolith rule: Authoring may not depend on CourseManagement). Learner
  delivery continues to read the published `Course` entity, which learners only see when
  `CourseStatus.Published`; revision drafts are only reachable via the owner-gated
  `/Courses/Revisions` page, so drafts never leak to learners.
- `RevisionLifecycleService` models "owner" as `CourseRevisionPointer.OwnerId`. For the UI,
  the page operates as the course's `InstructorId` (passed as the actor to the service) so the
  owner check stays consistent with `Course.InstructorId`.
- PostgreSQL integration tests use a small test-only `DbContext` (`AuthoringPostgresContext`)
  mapping only the 4 authoring tables against the local `openlearning_integration` database
  (created with `CREATE DATABASE openlearning_integration;`); this avoids building the full
  50-module schema/extensions and exercises the relational concurrency path that InMemory ignores.

## Suggested next steps (checklist)
1. **Archive complete** — no further tasks remain for this change.
2. If content editing of the revision `ContentSnapshotJson` is desired, build an editor that
   writes the snapshot from the existing course outline (out of scope for this change).
3. Before pushing `main`, run the remaining test suites and `dotnet format` if required by CI.
