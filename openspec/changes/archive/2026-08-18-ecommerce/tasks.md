# Ecommerce — Tasks

## 1. Course Pricing

- [x] 1.1 Add nullable `Price` to `Course` entity and its EF configuration
- [x] 1.2 Include price in course create/edit pages (owner-only)

## 2. Module Setup

- [x] 2.1 Create `src/OpenLearning.Ecommerce` class library and add it to the solution
- [x] 2.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)
- [x] 2.3 Add `Order` entity + `OrderConfiguration`, and `OrderStatus` enum
- [x] 2.4 Register `ApplyConfigurationsFromAssembly` in `ApplicationDbContext` and `AddEcommerceModule` in `Program.cs`

## 3. Services

- [x] 3.1 Implement `OrderService`: create pending order, confirm payment (marks Paid + enrolls student), has-paid check, order list for owner
- [x] 3.2 Guard enrollment: reject direct enrollment in paid courses without a paid order (handler-level check)

## 4. UI

- [x] 4.1 Course details page: display price, show Buy action for paid courses instead of Enroll
- [x] 4.2 Checkout page: order summary + demo Pay button that confirms payment
- [x] 4.3 Order/sales list page per course (owner-only)

## 5. Migration & Verification

- [x] 5.1 Create EF Core migration (`AddEcommerce`)
- [x] 5.2 Run `dotnet build` and start the app
- [x] 5.3 Verify paid flow end-to-end (price course → student buys → enrolled → duplicate buy rejected → owner sees order)
