## Why

SCORM is the only active standards-compliant content format for C# LMSs, and it is the gap this project explicitly aims to fill. Supporting SCORM 1.2 lets instructors reuse existing e-learning content instead of rebuilding it as plain lessons.

## What Changes

- New `scorm-content` capability: instructors attach a SCORM 1.2 package (zip) to a lesson they own.
- The package is unpacked, its `imsmanifest.xml` is parsed, and the manifest's first SCO becomes the launch entry point.
- Students launch the SCO in a frame with a SCORM 1.2 runtime API adapter (`Initialize`, `Terminate`, `GetValue`, `SetValue`, `Commit`, error handling). Runtime state (`suspend_data`, `lesson_location`, `lesson_status`, `score.raw`, `session_time`) is persisted per enrollment.
- When the SCO reports `lesson_status` = `completed`/`passed`, the lesson is automatically marked complete for that student (integrated with existing progress tracking).
- New `OpenLearning.Scorm` class library following the modular-monolith structure. Uses only the BCL (zip via `System.IO.Compression`, XML via `System.Xml.Linq`) — no new external dependencies.

## Capabilities

### New Capabilities
- `scorm-content`: SCORM 1.2 package upload/manifest parsing, SCO launch with a runtime API adapter, per-enrollment runtime state persistence, and automatic lesson completion from SCORM completion status.

### Modified Capabilities

None.

## Impact

- New `src/OpenLearning.Scorm` project referencing `OpenLearning.Auth`, `OpenLearning.CourseManagement`, `OpenLearning.Enrollment`, and `OpenLearning.Progress`.
- New tables: `ScormPackages` (per lesson) and `ScormRecords` (runtime state per enrollment + package); one EF Core migration.
- Uploaded packages stored under `wwwroot/scorm/<packageId>/` and served as static files.
- New UI: upload control on the lesson edit page, and a launch page with an iframe + `scorm-api.js` adapter. Runtime calls hit small endpoints wired in the composition root.
- No changes to existing capabilities.
