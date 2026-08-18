# Dashboards — Design

## Context

The platform has three actors with different "next action" needs, but no personalized landing. Existing data (enrollments, progress, quiz attempts, orders, chat) is sufficient to build dashboards without new persistence. This change adds role-aware dashboards and, for signed-in users, makes the dashboard the home landing.

## Goals

- Each role gets a dashboard that answers "what should I do next".
- Dashboards link into existing management/learning flows.
- All aggregates are computed on demand from existing tables.

## Non-Goals

- No trend charts or time-series analytics (deferred to `platform-analytics`).
- No materialized/snapshot statistics.
- No per-student teaching analytics here (see `teacher-roster`).

## Decisions

### D1: Role-based landing
Signed-in users are redirected from `/` to their dashboard (`/Dashboard`, `/Dashboard/Teacher`, `/Admin`). Anonymous users keep seeing the public catalog. Rationale: the catalog is the discovery surface for anonymous visitors; the dashboard is the working surface for authenticated users.

### D2: On-demand aggregation
Student progress and quiz state are already queryable via `ProgressService`/`AttemptService`; enrollment/revenue stats via `EnrollmentService`/`OrderService`. Dashboards compose these calls. A `DashboardService` in the Web project (or small methods on existing services) keeps pages thin. No new tables for this change.

### D3: Student dashboard contents
- "Continue learning": most recently touched unfinished lesson per enrolled course (needs a last-accessed marker — see detail below).
- Enrolled courses with progress bars and quiz status (attempts vs. available quizzes).
- Certificates earned (count + links; certificates spec defines issuance).
- Recommendations: other published courses in the same categories, newest first.
- Recent activity feed from completions, quiz attempts, and chat messages.

**Detail:** "Continue learning" requires knowing the last-accessed lesson. Today there is no access-tracking. This change adds a lightweight `LessonAccess` record (or reuse `LessonCompletion` timestamps as a proxy). Decision: add `LessonAccess { Id, EnrollmentId, LessonId, LastAccessedAt }` in the Progress module so resume is exact.

### D4: Teacher dashboard contents
- Per course: enrollment count, revenue (paid orders), completion rate (students at 100%), quiz pass rate.
- Recent orders and recent quiz attempts.
- Quick links: edit course, orders, quizzes, chat, roster (once `teacher-roster` lands).

### D5: Platform dashboard contents
- Counts: students, instructors, courses (draft/published), enrollments, paid revenue, completion rate.
- Recent signups, recent courses, recent orders.
- Links into user management and course management (once `user-management` lands).

## Risks / Trade-offs

- **N+1 aggregation queries** → Dashboard queries are bounded (top-N lists, single aggregate queries per metric); acceptable at MVP scale, revisit with snapshots later.
- **Continue-learning marker adds writes** → A tiny upsert on lesson open is cheap and exact; fallback to most recent completion works if the table is absent.

## Migration Plan

Add the `LessonAccess` table in the Progress module (one migration). No other schema changes.

## Open Questions

- Should anonymous visitors be offered a sign-in CTA to dashboards? (Yes, on the catalog hero.)
- Dashboard refresh: on every page load vs. cached aggregates — on every load for now.
