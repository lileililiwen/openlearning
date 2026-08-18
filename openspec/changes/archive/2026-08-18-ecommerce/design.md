# Ecommerce — Design

## Context

The LMS currently only supports free courses: `EnrollmentService.EnrollAsync` requires a published course and creates an enrollment directly. This change adds optional course pricing and a checkout flow that grants enrollment only after payment. It follows the established modular-monolith pattern (one class library per domain, central DbContext with assembly-scanned configurations, Razor Pages UI shell).

## Goals

- Instructors can price a course (free by default).
- Students can purchase a paid course through a checkout page; enrollment is created on payment.
- Orders are recorded and visible to the course owner.
- Free-course behavior is unchanged.

## Non-Goals

- No real payment gateway integration (Stripe/Adyen/etc.) in this change; payment is simulated.
- No carts, coupons, refunds, subscriptions, or invoicing.
- No instructor payout/withdrawal mechanics.
- No changes to existing progress or assessments behavior.

## Decisions

### D1: `Price` lives on `Course`
`Course.Price` is a nullable `decimal`; `null` and `0` both mean free. This makes pricing intrinsic to the course and lets catalog/detail pages display it without a join. Alternative considered: a separate `Pricing` entity in the ecommerce module — rejected, it would split the aggregate and complicate every existing query.

### D2: New `OpenLearning.Ecommerce` module with `Order` aggregate
`Order { Id, CourseId, StudentId, Amount, Status, CreatedAt, PaidAt, PaymentReference }`. Status is an enum (`Pending`, `Paid`). One order per (student, course) is NOT enforced at the DB level — a student may buy once and be prevented from duplicate purchases by the service (the enrollment unique constraint already blocks double enrollment).

### D3: Payment is simulated
Checkout creates a `Pending` order; the checkout page shows the summary and a "Pay (demo)" button that calls `OrderService.ConfirmPaymentAsync`, which marks the order `Paid` and creates the enrollment via `EnrollmentService.EnrollAsync`. Swapping in a real gateway later only changes the confirm step. Rationale: end-to-end testable now, no external dependencies.

### D4: Paid courses cannot be directly enrolled
The course details page shows "Buy" (→ checkout) for paid courses instead of the "Enroll" button. As a defense-in-depth guard, the `OnPostEnroll` handler rejects courses with `Price > 0` unless the student already holds a `Paid` order for that course (checked via `OrderService`). This closes the forged-POST hole without creating an `Enrollment → Ecommerce` cycle: the guard lives in the page/handler layer, and `EnrollmentService` itself is unchanged.

### D5: Access control
- Creating/editing a price: course owner only (part of the existing course edit form).
- Checkout: any authenticated Student for a published course; owner/instructor are not charged (they already own the course — treated as free to them).
- Order/sales view: course owner only.

## Risks / Trade-offs

- **Simulated payment could be mistaken for real** → The checkout page is explicitly labeled "Demo checkout — no real payment", and the design keeps a `PaymentReference` + status field to accommodate a real gateway.
- **Price changes after purchase** → Orders snapshot `Amount` at purchase time, so later price edits don't affect existing orders.
- **Owner enrolls without paying** → Owner sees course management, not checkout; guard in D4 prevents accidental payment for owners.

## Migration Plan

One EF migration (`AddEcommerce`) adds `Price` to `Courses` and creates `Orders`. Applied automatically on startup via `db.Database.Migrate()`. Rollback: drop migration and remove the `Orders` table and `Price` column.

## Open Questions

- Should paid-course access be tied to an order or a lifetime license? MVP treats a paid order as lifetime enrollment.
- Revenue reporting (per-instructor totals) beyond a per-course order list is deferred.
