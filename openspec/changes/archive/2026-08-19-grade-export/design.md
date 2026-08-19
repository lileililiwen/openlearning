## Context

Instructors at every level (K-12, higher-ed, corporate training) need a paper / spreadsheet artifact of student work for offline review, parent meetings, and term reports. The data is already in the platform; the missing piece is a streaming, ownership-scoped, filterable export. The brief lists it as P0 because without it teachers cannot operate a real class.

We deliberately do NOT provide an import path for submissions/attempts — the brief is explicit that answer data must not be externally authorable. Even instructors can only grade existing submissions, never replace them via upload.

## Goals / Non-Goals

**Goals:**
- Excel export of submissions, quiz attempts, exam attempts, and course rosters.
- Streaming writes so memory is bounded.
- Sync ≤1000 / async >1000 via `async-io-jobs`.
- Ownership / TA scoping.

**Non-Goals:**
- PDF certificates of completion (covered by `certificates`).
- Import of externally-authored answer data.
- Real-time grade book UI — out of scope; existing `Pages/Courses/Assignments/Submissions.cshtml` covers in-app grading.

## Decisions

- **SXSSF streaming via ClosedXML** — same library as `question-import-export` for consistency.
- **Sync ceiling = 1000 rows**; the threshold is configurable via `system-config` (`grade.export.syncMaxRows`).
- **Filters stored as JSON** on `GradeExportJob` so the same row can be re-exported with the same filters (audit + reuse).
- **File retention 7 days** by default; configurable via `grade.export.retentionDays`.
- **No SQL aggregate** — we iterate the filtered query with keyset paging to keep memory bounded; SQL aggregate (e.g. `STRING_AGG`) is rejected because it materialises the whole result set.

## Risks / Trade-offs

- [Risk: a malicious Instructor fills filters to export the entire platform's attempts] → Mitigation: ownership filter at the SQL `WHERE` clause (not in C#), so cross-owner data is structurally unreachable.
- [Risk: file storage grows if exports aren't cleaned up] → Mitigation: cleanup job from `scheduled-business-jobs`.
- [Risk: an export of a large exam takes 30 minutes and the user thinks it hung] → Mitigation: progress notification (`export.progress`) at 25%, 50%, 75% — added to `notification-events-extensions`.

## Migration Plan

1. Land `async-io-jobs` first.
2. Add `OpenLearning.GradeExport` module + EF migration `AddGradeExport`.
3. Wire the export pages.
4. Verify ownership scoping on every endpoint.

## Open Questions

- Should we add a PDF export for printable grade sheets? Out of scope; the Excel file can be opened in Excel/Google Sheets and printed.
- Should `certificates` issue a PDF that is itself downloadable? Already covered by `certificates`.