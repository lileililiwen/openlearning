# Learner Resume & Continuity Specification

## ADDED Requirements

### Requirement: Last-viewed lesson is recorded per enrollment

The system SHALL record the most recent lesson a learner views for each of their enrollments, so they can resume where they left off.

#### Scenario: Learner opens a lesson

- **WHEN** an enrolled learner opens a lesson in a published course
- **THEN** the enrollment's resume target is updated to that lesson

#### Scenario: Owner or preview does not record

- **WHEN** a course owner or unauthenticated user views a lesson
- **THEN** no resume target is recorded for that view

### Requirement: Continue learning resumes at the last lesson

Every "Continue learning" / "Resume" entry point SHALL navigate the learner to their recorded last-viewed lesson, not to the course details page.

#### Scenario: Learner with progress clicks Continue learning

- **GIVEN** a learner has a recorded resume lesson for a course
- **WHEN** they click "Continue learning" from the course details page or dashboard
- **THEN** they are taken to that lesson's view page (`/Courses/Lessons/View/{lessonId}`)

#### Scenario: Learner without progress clicks Continue learning

- **GIVEN** a learner is enrolled but has no recorded resume lesson
- **WHEN** they click "Continue learning"
- **THEN** they are taken to the first lesson of the course

### Requirement: Stale resume targets are ignored

The system SHALL treat a recorded resume lesson as invalid when it no longer exists, is unpublished, or does not belong to the course, and fall back to the first lesson.

#### Scenario: Resume lesson was deleted

- **GIVEN** a learner's resume target points to a deleted lesson
- **WHEN** they click "Continue learning"
- **THEN** they are taken to the first remaining lesson instead of a 404
