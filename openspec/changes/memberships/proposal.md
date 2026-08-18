## Why

The platform has no membership concept. Membership packages give students unlimited-access plans and recurring revenue, which the reference system lists under User Foundation. Without them the platform can only sell courses individually.

## What Changes

- Membership plans with a price, benefit description, and validity period (e.g. 30/90/365 days).
- Students purchase a plan; a `Membership` record tracks activation, expiry, and renewals.
- Benefits are enforced: active members get free enrollment in paid courses (and other perks listed in the plan).
- Expiration reminders via the existing notifications channel.

## Capabilities

### New Capabilities
- `memberships`: membership plans, purchase/renewal, validity enforcement, and expiry reminders.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Memberships` module: `MembershipPlan { Id, Name, Description, Price, DurationDays, IsActive }`, `Membership { Id, UserId, PlanId, StartedAt, ExpiresAt }`; `MembershipService` (purchase, renew, `IsActiveAsync`, `GetActiveAsync`).
- Checkout gains a membership path; enrollment enforces "member → free" for paid courses.
- Reminder job/page triggers a notification near expiry.
