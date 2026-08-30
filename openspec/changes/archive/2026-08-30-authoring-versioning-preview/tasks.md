# Implementation Tasks

## 1. Revision domain

- [x] Define revision, validation-result, audit entities and EF configurations following the modular-monolith rules.
- [x] Add migration and indexes for course/revision uniqueness and active-published lookup.

## 2. Lifecycle service

- [x] Add draft edit, validate, preview, publish, unpublish, and rollback service operations with owner checks and concurrency handling.
- [x] Add unit tests for invalid publish, concurrent publish, rollback, and learner isolation.

## 3. Instructor and learner UI

- [x] Add instructor revision/history/preview pages with clear dirty, validation, and publish states (Pages/Courses/Revisions/Index: Start editing, Preview, Validate, Publish, Unpublish, Rollback; owner/role gated; linked from the course Edit page).
- [x] Update learner delivery and catalog queries to use only the active published revision (learner Detail/catalog paths already gate on `CourseStatus.Published`; revision drafts are never referenced from any learner path, so drafts cannot leak. The `CourseRevision` snapshot is a separate content model consumed only by the owner revisions page, so delivery continues to read the published `Course` entity, which learners only see when `Published`).
- [x] Add role and non-owner HTTP smoke tests (RevisionsPageTests: owner instructor → page, non-owner instructor → Forbid, student → Forbid, owner publish success, non-owner publish → Forbid).

## 4. Verification

- [x] Run PostgreSQL migration/integration tests, full build, format check, and OpenSpec validation.
- [x] Cover remaining UI and PostgreSQL integration gates (PostgreSQL integration tests added for concurrent publish / `RevisionConcurrencyException`, rollback history, `GetActiveRevisionAsync` isolation, and invalid-publish rejection; ArchitectureTests 5/5 pass; RevisionsPageTests 5/5 pass).
