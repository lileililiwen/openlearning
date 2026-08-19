## Context

`Enrollment` is a row created at purchase/enroll time and never gains an expiry. The brief requires 课程有效期 with periodic revocation. We add nullable expiry to keep the existing free / paid / membership flows intact: an `AccessExpiresAt = null` row is the "no expiry" case, which is the historical behaviour.

Grace periods, holding periods, and revocation reasons are a familiar pattern in SaaS (Notion, Coursera). We pick 3-day default because (a) refunds typically settle within 24–48h, (b) it gives a learner time to renew without losing study continuity.

Membership-granted enrollments already exist (`memberships` spec). We piggy-back the new `AccessExpiresAt` so the membership lifecycle is the single source of truth for the enrollment's deadline.

## Goals / Non-Goals

**Goals:**
- Optional `AccessExpiresAt` on `Enrollment`, with course-level default and per-row override.
- Grace period and revocation by the `scheduled-business-jobs` expiry job.
- Re-enrollment allowed once the prior row is `Revoked`.
- Notifications on T-7 days expiring and on revocation.

**Non-Goals:**
- Freezing / unfreezing progress — progress remains viewable.
- Per-lesson expiry (the whole enrollment has one expiry; lessons share it).
- Trial periods / "X days free before pay" — out of scope.

## Decisions

- **`AccessExpiresAt` nullable**, not required. Existing rows get `null` and behave unchanged.
- **Revoked reason is a string** (`"expired"`, `"refund"`, `"admin"`, …) rather than an enum, so future reasons don't require a migration.
- **Grace days configurable** in `system-config` so admins can tune without a code change.
- **Re-enrollment is a new row, not a reactivation of the old one** — keeps history clean for refunds and audit.
- **`AccessExpiresAt` for membership-granted enrollments = `min(Membership.ExpiresAt, course default)`** — matches user expectation that the membership is the binding constraint.

## Risks / Trade-offs

- [Risk: instructors accidentally set an expiry in the past] → Mitigation: validation rejects `AccessExpiresAt < UtcNow` on save with a clear error.
- [Risk: a revoked enrollment loses the `CompletedLessons` set, breaking resume] → Mitigation: revocation only flips `RevokedAt`; `LessonCompletion` rows are preserved, so the learner can still see "you completed 7/10 lessons" after re-enrolling.
- [Risk: a learner mid-exam when the expiry job runs] → Mitigation: revocation does not abort in-flight sessions; the next attempt is blocked.
- [Risk: grace period hides "true" expiry from analytics] → Mitigation: analytics (existing `platform-analytics`) reads `AccessExpiresAt` directly; reports show both expiry and revocation dates.

## Migration Plan

1. Add EF migration `AddAccessPeriod` to add the four columns (`Enrollment.AccessExpiresAt`, `Enrollment.RevokedAt`, `Enrollment.RevokedReason`, `Course.DefaultAccessDays`).
2. Existing rows: `AccessExpiresAt = null`, `RevokedAt = null`, `RevokedReason = null`. Behaviour unchanged.
3. Add the admin/instructor edit page.
4. Register the `enrollment.expiry.revoke` job via `scheduled-business-jobs`.
5. Verify a demo enrollment with `AccessExpiresAt = UtcNow - 4 days` is revoked on the next job tick.

## Open Questions

- Should revocation block viewing too, or only writes? Current decision: writes only — viewing preserves history.
- Should a free re-enrollment after revocation reset progress? Current decision: no — keep existing progress; allow "restart" via a future `ResetProgress` admin action.