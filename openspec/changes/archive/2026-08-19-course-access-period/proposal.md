## Why

The brief assumes paid courses can have a validity period (课程有效期) and that expiry should revoke access. Today `Enrollment` has no expiry field; `memberships` carries expiry for the membership itself but not for an individual course enrollment. The periodic-expiry scheduled job from `scheduled-business-jobs` therefore has nothing to act on. We add `Enrollment.AccessExpiresAt`, a grace window, and the rules that determine when the learner keeps access, when access is revoked, and how re-enrollment works.

## What Changes

- `Enrollment` gains `AccessExpiresAt` (nullable) and `RevokedAt` / `RevokedReason` (set by the expiry job or by an admin).
- A new admin / instructor page sets the period per enrollment (default = no expiry), and the course-level setting (`Course.DefaultAccessDays`) seeds it.
- A grace period (`system-config` parameter `enrollment.expiry.graceDays`, default 3) allows read-only access between expiry and revocation; the learner sees a banner.
- The expiry job (`scheduled-business-jobs`) revokes access on `AccessExpiresAt + graceDays` and notifies the learner.
- Re-enrollment after expiry requires a fresh purchase (or an active membership that covers the course), matching the existing rules in `enrollment` and `memberships`.
- Membership-based enrollments inherit `Membership.ExpiresAt` as the enrollment's expiry at the time of enrollment; renewing the membership extends enrollment expiry for free courses only (paid re-enrollment rules unchanged).

## Capabilities

### New Capabilities

- `course-access-period`: enrollment expiry model, grace period, admin/instructor override, learner-facing banner, re-enrollment rules.

### Modified Capabilities

- `enrollment`: an enrollment with `AccessExpiresAt` set SHALL be treated as expired when `UtcNow > AccessExpiresAt`. The duplicate-enrollment prevention still applies, but expired enrollments can be re-purchased.
- `memberships`: a membership-granted free enrollment SHALL inherit `Membership.ExpiresAt` as its `AccessExpiresAt`.
- `progress-tracking`: progress and last-access are preserved after expiry (so the learner sees their history); only *write* actions (mark-complete, new attempt) are denied.
- `certificates`: certificates earned before expiry remain viewable after expiry.
- `course-management`: a `Course.DefaultAccessDays` field (nullable; empty = unlimited) seeds new enrollments.

## Impact

- New module-internal migration `AddAccessPeriod` adds `AccessExpiresAt`, `RevokedAt`, `RevokedReason` to `Enrollment`, and `DefaultAccessDays` to `Course`. No new module.
- `EnrollmentService` gains `SetExpiryAsync`, `RevokeAsync`, `IsAccessExpiredAsync`, and a new code path for re-enrollment that allows a new row when the old one is `Revoked` or expired.
- Pages: `Pages/Courses/Enrollments/Edit.cshtml(.cs)` for instructors (set per-student expiry), `Pages/Admin/Enrollments.cshtml(.cs)` for Admin/Finance.
- Learner UX: a banner on `/MyCourses` lists expiring/expired enrollments with a "renew" CTA.
- `scheduled-business-jobs` registers the `enrollment.expiry.revoke` job that calls `EnrollmentService.RevokeAsync` for each expired enrollment past the grace period.
- Notifications: a `notifications` event `enrollment.expiring-soon` (T-7 days) and `enrollment.expired` are emitted; covered by `notification-events-extensions`.