# Course Tags — Design

## Context

Categories are single-valued free text on `Course`. Tags add lightweight multi-valued classification and improve discovery.

## Goals

- Instructors tag courses during create/edit.
- Students filter the catalog by tag and see tag badges.
- Tags stay in sync with a small vocabulary (admin maintenance is a separate change).

## Non-Goals

- No hierarchical/structured tags (flat vocabulary).
- No auto-tagging or ML.
- No tag-based recommendations (can reuse later).

## Decisions

### D1: Flat `Tag` + join
`Tag { Id, Name, Slug, IsActive }` (unique slug). `CourseTag { CourseId, TagId }` join with a composite key. Course navigation `Tags` added; queries filter `course.Tags.Any(t => t.Slug == tag)`.

### D2: Tag input on course form
Create/Edit accept a comma-separated tag string or a multi-select of known tags; unknown names are created on save (auto-vocabulary), consistent with the category free-text approach.

### D3: Catalog filter
`CourseService.SearchAsync` gains an optional `tag` slug param (AND semantics: course must have the tag). The catalog page adds a tag dropdown/badge filter and renders `Tag` badges on cards.

## Risks / Trade-offs

- **Vocabulary drift** → Auto-creating tags on save keeps the form frictionless; admin `category-tag-admin` later can rename/retire.
- **Filter complexity** → AND join over `CourseTag` is a single extra `Where(Any(...))`; acceptable.

## Migration Plan

One migration creates `Tags` and `CourseTags`.

## Open Questions

- Should tags be public/visible to anonymous catalog viewers? Yes — same as categories.
