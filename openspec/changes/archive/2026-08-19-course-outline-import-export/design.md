## Context

This is a P1 capability per the brief. Course outlines are smaller than question banks but still painful to author by hand — a 50-module / 300-lesson course is the realistic upper end. We follow the same pattern as `question-import-export`: sync ≤200 rows, async >200 rows, partial success, ownership isolation.

Media stays outside the import: the brief is explicit ("Excel只能导入元数据，视频还是要上传"). The `LessonContentUrl` column is a forward-looking convenience for instructors who already have externally-hosted lecture videos they want to link to; the lesson-edit page continues to be the only path for managed media uploads.

## Goals / Non-Goals

**Goals:**
- Excel import/export of course outline metadata (modules + lessons).
- Append and Replace modes.
- Streaming writes for export.
- Ownership-scoped.

**Non-Goals:**
- Importing media files.
- Importing quizzes, assignments, or exams via the same file — those go through `question-import-export` and dedicated authoring pages.
- Bulk-editing existing lessons beyond the Replace mode.

## Decisions

- **Replace mode preserves the course row and enrollments** — instructors sometimes restructure mid-term; replacing only the outline is what they want. Quizzes and assignments attached to lessons are detached (lesson id becomes orphaned) rather than deleted, and surfaced to the instructor as warnings.
- **Sync ceiling = 200 rows**; matches `question-import-export` for consistency.
- **Same `IJob` wrapper** via `async-io-jobs` so retries, locks, and admin UI are uniform.
- **Conflict resolution: by `(ModuleOrder, LessonOrder)`** — duplicates are errors, not silently merged.

## Risks / Trade-offs

- [Risk: Replace mode accidentally deletes an instructor's quizzes] → Mitigation: the import page shows a pre-flight summary ("this will delete 12 modules, 87 lessons, and orphan 5 quizzes") and requires explicit confirmation.
- [Risk: a `LessonContentUrl` is set to a malicious link] → Mitigation: the URL is not auto-fetched; the player page only embeds URLs the user explicitly attached via the lesson edit page. The import's text reference is informational until the lesson is edited.
- [Risk: an Instructor bulk-imports a course with thousands of empty modules] → Mitigation: row validation rejects empty titles and `ModuleOrder < 0`.

## Migration Plan

1. Land `async-io-jobs` first.
2. Add `OpenLearning.CourseOutlineIO` module + EF migration `AddCourseOutlineIO`.
3. Wire the import and export pages.
4. Verify the Replace-mode pre-flight summary.

## Open Questions

- Should Replace optionally include assignments / quizzes in the wipe? No — that would surprise instructors. Wipe is modules + lessons only.