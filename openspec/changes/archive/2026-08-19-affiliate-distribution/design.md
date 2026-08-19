## Context

The brief asks for 渠道/分销 with share links, attribution, commission, and payout. Today we have no distributor concept, no `CommissionEntry`, and no payout request review — `instructor-revenue` (archived) covered instructors but not third-party distributors. The closest existing surface is `instructor-revenue` (archived spec), which gives us the pattern (ledger entries, payout request, admin review) to mirror. We extend the same pattern to distributors.

We will:
- Store share-link attribution in a first-party cookie (`ol_aff`) with an anonymous id, hashed server-side to the click. The cookie lifetime matches the 30-day attribution window.
- Apply a holding period (default 7 days) on commissions before they become available, mirroring industry practice to absorb refunds.
- Reuse `job-scheduler` for periodic settlement — this change ships no scheduled jobs, only the work that the settlement job calls.
- Reuse `notifications` for "payout request submitted / approved / rejected" messages.

## Goals / Non-Goals

**Goals:**
- Distributor role + dedicated dashboard.
- Share-link generation, click tracking, attribution on paid orders.
- Commission ledger with Pending → Available → Paid (or Reversed) transitions.
- Payout request flow with admin/finance review.
- Hooks for the `scheduled-business-jobs` change to run periodic settlement.

**Non-Goals:**
- Multi-tier affiliate networks (only single-tier direct attribution).
- A/B-tested commission rates per distributor — every distributor gets the platform-wide rate.
- Real-time click-stream analytics (we record totals only).
- Tax / invoicing for distributors — handled by `invoice-management` if needed.

## Decisions

- **First-party cookie attribution** (no third-party tracking). Reasons: GDPR-friendly, no external service required.
- **30-day attribution window**. Alternative: 7 days (rejected — too short for considered purchases); 60 days (rejected — affiliate-link abuse window).
- **Hashed IP** in `AffiliateClick` (SHA-256 + server salt) for fraud detection without storing PII.
- **Commission rate as a system parameter** (`distribution.commission.percent`, default 10). Admin can edit it via the existing `system-config` UI; rate changes apply to new orders only.
- **Holding period** as a system parameter (`distribution.commission.holdingDays`, default 7). Same admin-config pattern.
- **Payout review by Admin and Finance** — mirrors `instructor-revenue`. Once `ta-and-finance-roles` ships, `RequireFinanceOrAdmin` replaces `RequireAdmin`.
- **Settlement statements are immutable** once closed; corrections require an explicit reversal entry.

## Risks / Trade-offs

- [Risk: cookie-blocking browsers break attribution] → Mitigation: document the limitation; offer an admin "manual attribution" tool to attach a paid order to a distributor post-hoc (operator-only, gated by Admin).
- [Risk: a single customer clicks many distributors' links and the most-recent wins] → Mitigation: documented behaviour; first-touch attribution is *not* used because considered purchases span days.
- [Risk: clawback after payout complicates the ledger] → Mitigation: clawback is a negative `CommissionEntry` in the next period rather than mutating a paid row; ledger totals remain auditable.
- [Risk: bad actors generate self-clicks] → Mitigation: the attribution service can ignore clicks whose `IpHash` matches the buyer's later order IP. Out of scope to ship here; tracked as a follow-up.
- [Risk: settlement job runs before refund window closes] → Mitigation: holding period defaults to 7 days, longer than typical refund window; admin can tune.

## Migration Plan

1. Land `OpenLearning.Distribution` module with entities, services, pages.
2. Run EF migration `AddDistribution` on dev DB.
3. Promote a demo user to `Distributor` and create a share link to verify the flow end-to-end (click → signup → order → commission → payout request → approve).
4. Verify a refund reverses the commission.
5. Verify the settlement job hook is callable (smoke-test via job-scheduler's Run-now).
6. Rollback: `Remove` migration + disable Distributor role.

## Open Questions

- Should commission be a flat 10% or vary by course category? Current decision: flat, configurable. Per-category override is a follow-up.
- Should the public redirect `/D/C/{slug}` be no-indexed? Yes — add `<meta name="robots" content="noindex">` on the redirect page (currently a 302, so indexability is moot, but defensive).