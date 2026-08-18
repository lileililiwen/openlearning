# Memberships — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Memberships` class library, add to solution, add references (Auth, CourseManagement, Enrollment, Notifications, EF Core)
- [ ] 1.2 Add `MembershipPlan` + `Membership` entities + configs
- [ ] 1.3 Implement `MembershipService` (plans, purchase, renew, active checks)
- [ ] 1.4 Register assembly scanning in `ApplicationDbContext` and `AddMembershipsModule` in `Program.cs`

## 2. UI

- [ ] 2.1 Membership plans page (public) + purchase/renewal flow
- [ ] 2.2 Active membership display on the student dashboard
- [ ] 2.3 Enforce member benefit: active member enrolls free in paid courses

## 3. Expiry Reminders

- [ ] 3.1 Reminder sweep on dashboard load (or timer) notifying memberships expiring within 7 days

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: plan purchase, renewal extends expiry, member enrolls free, non-member must pay, expiry reminder appears
