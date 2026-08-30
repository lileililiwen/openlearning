# Proposal: Authoring Versioning and Preview

## Problem

The current course lifecycle can publish a course, but it does not define a safe authoring boundary between draft edits and the learner-visible version. Open edX separates Studio authoring from LMS delivery and provides preview-oriented author workflows. OpenLearning needs equivalent correctness without copying that architecture.

## Change

Add immutable published revisions, draft editing, validation, learner-safe preview, publish, rollback, and audit history. Keep the existing course-management aggregate as the owner of lifecycle rules and expose the workflow through instructor pages.

## Non-goals

- Collaborative real-time editing.
- Branching authoring for multiple simultaneous editors.
- Replacing the existing course-management specification.

## Impact

Adds a course revision boundary, validation results, instructor workflow UI, authorization tests, and migration coverage. Learner delivery reads only the active published revision.
