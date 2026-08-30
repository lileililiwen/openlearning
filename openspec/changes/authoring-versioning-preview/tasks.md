# Implementation Tasks

## 1. Revision domain

- [x] Define revision, validation-result, audit entities and EF configurations following the modular-monolith rules.
- [x] Add migration and indexes for course/revision uniqueness and active-published lookup.

## 2. Lifecycle service

- [x] Add draft edit, validate, preview, publish, unpublish, and rollback service operations with owner checks and concurrency handling.
- [x] Add unit tests for invalid publish, concurrent publish, rollback, and learner isolation.

## 3. Instructor and learner UI

- [ ] Add instructor revision/history/preview pages with clear dirty, validation, and publish states.
- [ ] Update learner delivery and catalog queries to use only the active published revision.
- [ ] Add role and non-owner HTTP smoke tests.

## 4. Verification

- [x] Run PostgreSQL migration/integration tests, full build, format check, and OpenSpec validation.
- [ ] Cover remaining UI and PostgreSQL integration gates (in progress; see HANDOFF.md).
