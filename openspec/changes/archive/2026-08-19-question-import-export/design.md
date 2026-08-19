## Context

Instructors on real LMS platforms overwhelmingly receive questions as Word/Excel documents from textbook publishers. Manual entry is the #1 reason teachers abandon platforms. The repo already has the data model (Question via `assessments`), the type enum (pending `question-types`), and the bank concept (pending `question-bank-admin`); this change adds the IO surface that is missing.

We follow the brief's directives: sync for ≤200 rows (fast feedback), async for larger (database protection), partial success (no all-or-nothing), strict ownership (server-side, never trust the UI), file whitelist + size limit, and SXSSF streaming for export.

## Goals / Non-Goals

**Goals:**
- Excel import/export for `Question` rows, scoped to the caller's ownership.
- Sync ≤200 rows with row-by-row errors; async >200 rows via `async-io-jobs`.
- Append and UpdateOrAppend modes.
- Streaming export (SXSSF) for memory safety.
- Per-user rate limit to protect the database.

**Non-Goals:**
- Word document parsing (rejected — unstable formats; instructors copy-paste into the template).
- Bulk edit of options, answers, or other per-question substructure beyond the template columns.
- Per-template format versioning for export (we always export the latest schema; old imports are still readable).

## Decisions

- **ClosedXML for parsing and SXSSF export.** ClosedXML is MIT, actively maintained, and gives us both reading and SXSSF-style streaming for writes. Alternative: NPOI (more flexible, more code); EPPlus (commercial licence).
- **Sync ceiling = 200 valid rows.** Rows that fail validation are not counted toward the limit. The threshold is configurable via `system-config` (`question.import.syncMaxRows`).
- **Async path goes through `async-io-jobs`**, which wraps `job-scheduler`. The job is a thin handler that calls `QuestionImportService.ProcessFileAsync`.
- **Streaming export uses ClosedXML SXSSFWorkbook**. Hard ceiling at 5000 rows for synchronous download; larger exports always go async.
- **Per-user rate limit** stored as `system-config` (`question.import.rateLimitPerHour`, default 5). The dispatcher counts attempts per user per hour.
- **Bank import** uses the same template but sets `IsBank = true` and reads `BankTopic` from an extra column not present in the per-quiz import.

## Risks / Trade-offs

- [Risk: an Instructor uploads a 50 MB file and the parser OOMs] → Mitigation: file size is rejected before parsing (10 MB default).
- [Risk: a malicious file uses Excel XXE / formula injection] → Mitigation: ClosedXML does not evaluate formulas; we read values only; the storage path does not execute.
- [Risk: an Instructor "update"s a question that another Instructor now sees] → Mitigation: ownership check inside the persistence step, not the validation step.
- [Risk: rate-limit evaded by multiple accounts] → Mitigation: per-account limit; documented that admins can override per-account.
- [Risk: error file leaks PII (student names in explanation field)] → Out of scope — explanations are authored by instructors, not students.

## Migration Plan

1. Land `async-io-jobs` first (this change depends on it).
2. Add `OpenLearning.QuestionIO` module + EF migration `AddQuestionIO`.
3. Wire the import + export pages.
4. Run a smoke test uploading a 1500-row file via async and confirm the notification + downloadable error file.
5. Verify ownership isolation: Instructor B cannot import into Instructor A's quiz.

## Open Questions

- Should the import support `.csv`? Rejected — escaping rules vary; instructors can save-as `.xlsx` from any spreadsheet app.
- Should we expose a JSON template in addition to Excel? Out of scope.