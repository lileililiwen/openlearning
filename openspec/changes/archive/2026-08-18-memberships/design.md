# Memberships — Design

## Context

Paid courses are sold individually. A membership tier gives students a subscription-style pass and gives the platform recurring revenue.

## Goals

- Admin can define membership plans (price, duration, benefits text).
- Students can purchase/renew a plan and see their active membership.
- Active members receive the plan's benefits (free paid-course enrollment in MVP).
- Users get an expiry reminder notification.

## Non-Goals

- No recurring billing/auto-charge (renewal is a manual purchase; auto-rebill deferred).
- No benefit matrix beyond "free paid-course enrollment" in MVP.
- No refunds of memberships (handled by a later finance change).

## Decisions

### D1: New `OpenLearning.Memberships` module
`MembershipPlan { Id, Name, Description, Price, DurationDays, IsActive }`, `Membership { Id, UserId, PlanId, StartedAt, ExpiresAt }` (index on `(UserId, ExpiresAt)`). `MembershipService`: `GetPlansAsync`, `PurchaseAsync(userId, planId)`, `RenewAsync` (extends `ExpiresAt`), `IsActiveAsync`, `GetActiveAsync`.

### D2: Benefit enforcement at enrollment
The Web `Details`/`Enroll` handlers check `MembershipService.IsActiveAsync`; an active member skips the order requirement and enrolls free. Composition stays in the Web page to avoid a Memberships↔Enrollment cycle.

### D3: Expiry reminders
A page-load sweep (dashboard) or a background timer checks memberships expiring within 7 days and raises one `Notification` each (deduped by type+day via the notifications module).

## Risks / Trade-offs

- **No auto-rebill** → Manual renewal is simpler and avoids payment-subscription complexity; documented as a future item.
- **Benefit scope creep** → Keep the benefit to free paid-course enrollment; other perks are display-only text for now.

## Migration Plan

One migration creates `MembershipPlans` and `Memberships`.

## Open Questions

- Should membership override instructor/admin? No — memberships apply to Students only.
