## Why

The MVP delivers free courses, but there is no way to monetize content. Adding course pricing and a checkout flow turns OpenLearning into a genuinely usable LMS for instructors who want to charge for their courses.

## What Changes

- Courses gain a price: instructors set an optional `Price` when creating/editing a course. `null`/0 means the course is free and the existing enroll flow is unchanged.
- New `ecommerce` capability: students "Buy" a paid course through a checkout page, a payment is recorded (simulated in this MVP), and enrollment is created automatically on successful payment.
- Paid courses do NOT allow direct enrollment; the details page shows a "Buy" action instead of "Enroll", and duplicate purchases are prevented.
- New `OpenLearning.Ecommerce` class library following the modular-monolith structure, wired into the central `ApplicationDbContext` via assembly scanning.
- Payment is simulated locally (no external gateway) so the flow is testable end-to-end; a real provider can be swapped in later.

## Capabilities

### New Capabilities
- `ecommerce`: Course pricing is set by the course owner; students purchase paid courses via a checkout flow; orders record amount, status, and date; enrollment is granted on payment; instructors can view their course orders.

### Modified Capabilities
- `course-management`: course create/edit now also accepts an optional price (free by default).

## Impact

- `Course` entity gains a nullable `Price` decimal column (free when null/0); `course-management` delta spec updated accordingly.
- New `src/OpenLearning.Ecommerce` project referencing `OpenLearning.Auth`, `OpenLearning.CourseManagement`, and `OpenLearning.Enrollment`.
- New table `Orders` (`Id, CourseId, StudentId, Amount, Status, CreatedAt, PaidAt, PaymentReference`); one EF Core migration.
- New Razor Pages: checkout (order summary + demo pay), and per-course order/sales view for the owner; course details page gains price display and Buy action.
- No changes to enrollment behavior for free courses.
