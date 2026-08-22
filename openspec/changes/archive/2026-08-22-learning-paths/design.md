# Learning Paths — Design

## Context

The course aggregate owns only within-course structure. Cross-course sequencing needs a separate aggregate without adding navigation collections to courses or users.

## Goals

- Model versioned required and elective course sequences.
- Calculate eligibility and completion from existing enrollment/progress data.
- Preserve historical learner assignments when a published path changes.

## Non-Goals

- Credit or graduation rules; those belong to `credit-and-graduation`.
- Automatic course enrollment or payment bypass.

## Decisions

### D1: Dedicated domain

Create `LearningPath`, `LearningPathStage`, `LearningPathCourse`, and `PathEnrollment` in `OpenLearning.LearningPaths`. References use IDs and services, not cross-module navigation properties.

### D2: Immutable published versions

Publishing creates a version snapshot. Existing path enrollments remain on their assigned version; new learners receive the latest published version.

### D3: Derived progress

Completion is calculated from existing enrollment and course-progress services. Required items must complete; each elective group records a minimum selection count.

### D4: Reuse existing course access and completion data

The module reuses `Course` for published-course validation and pricing, active
`Enrollment` rows for access state, and `LessonCompletion` plus the existing
course module/lesson structure to derive completion. Learner actions link to
the existing course detail checkout/enrollment workflow; a learning path never
creates a course enrollment. Management pages follow the existing Razor Pages
owner-or-Admin authorization pattern.

## Risks / Trade-offs

- Version snapshots add storage but prevent silent requirement changes.
- Cross-module reads can be expensive; query projections and indexed IDs are required.

## Migration Plan

Add path tables and indexes without migrating existing courses or enrollments.
