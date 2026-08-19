## Context

Coupon bulk import is a marketing-team workflow: a campaign produces a list of unique codes (per influencer, per channel, per event) that need to land in the database quickly. The brief lists it as P2 — nice-to-have but a clear productivity win. We follow the same pattern as the other IO changes (`question-import-export`, `student-bulk-import`, `course-outline-import-export`): a small dedicated module that wraps `async-io-jobs` for the async case.

Coupons are append-only by design (per `commerce-extras`); the import does not introduce a "update" path. The unique-code constraint is enforced by the existing `Coupon.Code` unique index; the import treats collisions as row errors.

## Goals / Non-Goals

**Goals:**
- Excel bulk coupon creation.
- Sync ≤200 rows / async >200 rows.
- Append-only; unique codes enforced.
- Audit logging.

**Non-Goals:**
- Coupon updates via Excel.
- Coupon deletion via Excel.
- Per-coupon targeted user lists (different capability).

## Decisions

- **Sync ceiling = 200 rows**; matches the other IO changes.
- **Code regex**: `^[A-Za-z0-9_-]{4,32}$` — no spaces, common enough for short codes.
- **Rate limit = 5 imports / hour / admin**; same default as question imports.
- **Async via `async-io-jobs`** so the framework's retention and admin visibility are reused.

## Risks / Trade-offs

- [Risk: an Admin uploads a file with thousands of codes all colliding with existing coupons] → Mitigation: error file lists every collision; admin can deduplicate and re-upload.
- [Risk: a malicious code allows SQL injection] → Mitigation: code is regex-validated; storage uses parameter binding (EF Core default).
- [Risk: rate limit evaded by Admin accounts] → Mitigation: per-account limit; can be tuned via `system-config` (`coupon.import.rateLimitPerHour`).

## Migration Plan

1. Land `async-io-jobs` first.
2. Add `OpenLearning.CouponIO` module + EF migration `AddCouponIO`.
3. Wire the admin pages.
4. Verify a 100-row upload and a 1500-row async upload end-to-end.

## Open Questions

- Should codes support Unicode for non-English markets? Current decision: no — keep ASCII to avoid homoglyph issues; admin can map Unicode via DisplayName if needed.