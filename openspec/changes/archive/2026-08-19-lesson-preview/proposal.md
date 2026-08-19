## Why

Course details hide all content until enrollment ("Course content is available after you enroll"). The reference system lists "lesson preview" and "free trial" as standard discovery features that convert visitors into students.

## What Changes

- Instructors can mark a lesson as a preview lesson (visible to non-enrolled visitors).
- Course details shows preview lessons with a "Preview" badge; preview lessons are playable/readable without enrollment.
- Free-trial behavior for paid courses: the first lesson is previewable by default when no preview is set (optional flag).

## Capabilities

### New Capabilities
- `lesson-preview`: preview lessons visible and consumable without enrollment.

### Modified Capabilities

- `course-structure`: lessons gain a preview flag; the content-gating rule gains an exception for preview lessons.
- `course-discovery` (details page): course details renders preview lessons for non-enrolled visitors.

## Impact

- `Lesson` gains `bool IsPreview`. `LessonService` create/edit accept the flag.
- `Pages/Courses/Details` shows preview lessons and links to `/Courses/Lessons/View` for preview lessons; `View` allows access when `lesson.IsPreview && course.IsPublished`.
- Progress recording is skipped for non-enrolled preview viewers.
