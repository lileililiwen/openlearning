## Context

Today the platform models only `Course` and `Enrollment`. The brief talks about 班级 / 班级群 / 期次 as the natural unit of TA work. We introduce `ClassGroup` as a 1-to-many child of `Course`, so a single course can offer multiple terms (e.g. "2026 Spring", "2026 Summer", "VIP fast-track"). This mirrors how a real LMS splits a single curriculum into cohorts with different TAs and schedules.

We deliberately keep `Enrollment.ClassGroupId` optional so existing direct enrollments continue to work and so the change is shippable in isolation. Future enrollments can be tagged without breaking the past.

## Goals / Non-Goals

**Goals:**
- `ClassGroup` entity with lifecycle, capacity, status.
- TA assignment model that `ta-and-finance-roles` reads.
- Class-scoped roster, Q&A, announcements.
- Class-scoped CSV export for the class roster.

**Non-Goals:**
- Per-lesson scheduling inside a class (one class covers all lessons of the course).
- Live attendance (the existing live-streaming change handles presence during a live session).
- Per-class grading curves.

## Decisions

- **Class status computed lazily** on read (a property `EffectiveStatus`) that returns `Open` when `StartsAt ≤ UtcNow ≤ EndsAt`, regardless of stored status. Stored status is only `Upcoming` (set early) or `Closed` (set by the owner). Simplifies transitions without a scheduled job.
- **One-class-per-enrollment invariant**: an enrollment can attach to at most one `ClassGroupId`. Multiple re-enrollments (after revocation) can target different classes.
- **`IClassAssignmentLookup`** lives in `OpenLearning.Classes` and is the single source of truth for TA scoping. `ta-and-finance-roles` imports the interface; the dependency direction is one-way.
- **Class-scoped Q&A** uses a new `ClassGroupId` foreign key on `Question` / `Post`; existing rows have `null` and are course-wide. The visibility filter is `ClassGroupId IS NULL OR ClassGroupId IN (member classes)`.
- **Class-scoped announcements** add `ClassGroupId` to `Notification`; `notification-events-extensions` covers the schema change.

## Risks / Trade-offs

- [Risk: TA assigned to many classes accumulates heavy dashboards] → Mitigation: paginate; default sort by `EndsAt` descending so the active class is first.
- [Risk: capacity check on enroll-into-class is racy under concurrent inserts] → Mitigation: use a single SQL statement `UPDATE … WHERE capacity > current_count RETURNING id` or rely on Postgres advisory lock per class.
- [Risk: migrating existing enrollments with `null` class group hides them from class reports] → Mitigation: class reports are scoped to `ClassGroupId IS NOT NULL`; the existing course-level roster report remains the catch-all.
- [Risk: class Q&A split between course-wide and class-scoped confuses users] → Mitigation: the UI shows a tab toggle ("全部" / "本班") on the Q&A page.

## Migration Plan

1. Add `OpenLearning.Classes` module + EF migration `AddClassGroups`.
2. Existing data is untouched; new columns are nullable.
3. Add the admin/instructor pages; TA pages read from this module.
4. Verify a class can be created, a TA assigned, a student enrolled into the class, and a class-scoped post visible only to members.

## Open Questions

- Should a single student be allowed in multiple classes of the same course? Current decision: yes (different terms). Unique constraint is per `(Enrollment, ClassGroupId)` only, not per `(User, Course)`.
- Should we surface a "switch class" UX for students who happen to be in multiple? Out of scope here; future UX.