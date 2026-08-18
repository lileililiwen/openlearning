# SCORM Content — Design

## Context

The LMS delivers lessons as authored markdown. SCORM 1.2 packages (zips with an `imsmanifest.xml` describing SCOs) are the dominant interoperable e-learning format. This change adds the ability to attach a SCORM 1.2 package to a lesson, launch it through a runtime API bridge, and persist student state — the differentiator this project's README calls out as the gap in the C# LMS space.

## Goals

- Instructors upload a SCORM 1.2 package per lesson (owner-only).
- Enrolled students launch the package; the SCO runs in a frame and talks to a SCORM 1.2 `API` object implemented client-side and backed by server persistence.
- Runtime state survives reloads (suspend data, location, status, score, session time).
- Completing the SCO marks the lesson complete in the existing progress model.

## Non-Goals

- No SCORM 2004 support (different API object and data model; deferred).
- No sequencing/navigation trees, objectives, or SCORM sequencing rules — only the package's first SCO is launched.
- No authored question scoring beyond what the SCO reports.
- No upload size/security hardening beyond basic zip validation.

## Decisions

### D1: New `OpenLearning.Scorm` module
Entities, EF configurations, and services live in a new class library referencing Auth, CourseManagement, Enrollment, and Progress. It does not reference `OpenLearning.Data`; services inject the base `DbContext` and use `Set<T>()` per the established pattern.

### D2: Domain model
- `ScormPackage { Id, LessonId, Title, ScormVersion, EntryPoint, PackagePath, UploadedAt }` — one package per lesson.
- `ScormRecord { Id, EnrollmentId, ScormPackageId, LessonLocation, SuspendData, LessonStatus, ScoreRaw, SessionTime, UpdatedAt }` — one row per (enrollment, package); unique index.

Deleting a lesson cascades to its package; deleting an enrollment cascades to its SCORM records.

### D3: Package storage and upload
Upload extracts the zip to `<webRoot>/scorm/<packageId>/` (the Web project passes its `WebRootPath`), parses `imsmanifest.xml` with `XDocument`, reads the first `<organization>/<item>` → resource `href`, and persists a `ScormPackage`. The entry point is served under `wwwroot` so the SCO is a plain static resource. Zip extraction is guarded against path traversal (entry names are resolved against the target root).

### D4: SCORM 1.2 runtime API
The launch page renders a full-screen iframe pointing at the package entry point and includes `scorm-api.js` in the *parent* window. SCOs discover the API via `window.parent.API` (the SCORM 1.2 convention). The adapter implements the standard 1.2 surface: `Initialize`, `Terminate`, `GetValue`, `SetValue`, `Commit`, `GetLastError`, `GetErrorString`, `GetDiagnostic`, `GetVersion`, plus an element allow-list for the core CMI data model. Values are cached client-side and flushed to the server on `Commit`/`Terminate` (and interval) via fetch to minimal endpoints.

### D5: Server runtime endpoints
Small JSON endpoints wired in the composition root:
- `POST /scorm/runtime/init` — authenticates the user, verifies the enrollment belongs to the user, returns persisted state.
- `POST /scorm/runtime/commit` — upserts the `ScormRecord`.

All requests carry the auth cookie (same-origin), so endpoints resolve the current user from `HttpContext.User`.

### D6: Lesson completion integration
When a commit reports `lesson_status` = `completed` or `passed`, the runtime service calls `ProgressService.MarkCompleteAsync` for the student's lesson, so the existing progress percentage reflects SCORM completion. A later `not completed` status does not unmark (keeps the progress model simple).

## Risks / Trade-offs

- **Malicious zips / path traversal** → Extraction resolves entry names under the target root and rejects absolute paths or `..` traversal; only known package folders are writable.
- **SCOs that talk to a different API location** → The adapter is exposed on both `window.API` and `window.API_1484_11`-style aliases, and the launch page hosts the API in the SCO's immediate parent, the common 1.2 pattern.
- **State loss on abrupt close** → Commits are flushed on `Terminate` plus an interval heartbeat.
- **No sequencing** → Acceptable for MVP; the package's first SCO is the entry point.

## Migration Plan

One EF migration (`AddScorm`) adds `ScormPackages` and `ScormRecords`. Applied on startup. Rollback: drop the migration and remove the tables.

## Open Questions

- Full `cmi.core.student_name`/`cmi.core.student_id` mapping to the LMS user — MVP returns the user's display name/id from the launch page context.
- SCORM 2004 — deferred; the data model is version-agnostic enough to extend.
