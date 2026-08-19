## Context

Institutional onboarding is the textbook scenario where bulk import is non-optional: a 培训机构 with a 200-student summer cohort cannot manually create 200 accounts. The brief lists it as P0. The repo already has `IdentityUserManager` (from `OpenLearning.Auth`), `EnrollmentService` (from `OpenLearning.Enrollment`), and `class-groups` for class-scoped enrollment. We plug the import on top.

Email uniqueness is enforced at the database level via the Identity schema; the import path treats that as a hard constraint and reports duplicates as row errors rather than trying to merge. Phone uniqueness is a soft uniqueness check across new rows; existing phones (with `+86` normalisation in mind) are best-effort.

## Goals / Non-Goals

**Goals:**
- Bulk create accounts, bulk enroll, or both, in one upload.
- Sync ≤200 rows / async >200 rows.
- Partial-success with downloadable error file.
- Per-row action mode so the same template can mix "create", "create+enroll", and "enroll-only".
- TA scope: a TA can import into their assigned class.

**Non-Goals:**
- Bulk profile updates (display name, bio, avatar) — different capability.
- Bulk password reset — covered by `account-settings` forgot-password flow.
- LDAP / SCIM integration — different capability.

## Decisions

- **Three row actions, not two.** `EnrollExisting` is necessary because institutions often have pre-existing accounts for the same students.
- **Password is optional per row.** Default = generate a one-time reset token; admins who want deterministic passwords can supply them per row.
- **TA scope uses `IClassAssignmentLookup`** (from `ta-and-finance-roles`/`class-groups`) — rows targeting a different class are reported as `class not assigned`.
- **Async path goes through `async-io-jobs`** — same `IJob` wrapper as `question-import-export`.
- **Welcome notification** uses the existing `notifications` capability; new event types land in `notification-events-extensions`.

## Risks / Trade-offs

- [Risk: an Admin uploads a file with 1000 duplicate emails and the import creates no accounts] → Mitigation: error file lists every row with the duplicate; admin can fix and re-upload.
- [Risk: bulk-enrolling into a paid course without payment creates free access] → Mitigation: `EnrollmentService.EnrollAsync` rejects paid courses without an order; the row error preserves the account creation.
- [Risk: a TA abuses the class-scoped import to enroll students into a class they don't own] → Mitigation: the row's `ClassGroupIds` is validated against `IClassAssignmentLookup`; off-class ids are reported as errors.
- [Risk: bulk creation overloads Identity's password hasher] → Mitigation: BCrypt's default work factor is fine for hundreds of accounts per minute; rate limit set to 5 imports/hour/user.

## Migration Plan

1. Land `async-io-jobs` first.
2. Add `OpenLearning.StudentIO` module + EF migration `AddStudentIO`.
3. Wire the admin and TA import pages.
4. Run a smoke test creating 50 accounts, enrolling them into two courses, and verifying the welcome notifications.

## Open Questions

- Should imported accounts require email verification before they can sign in? Current decision: no — institutional accounts are pre-verified by the importing admin; the welcome notification carries the reset link.
- Should we surface a "bulk message to these students" feature? Out of scope; existing `notifications` + `class-groups` already allow class-scoped announcements.