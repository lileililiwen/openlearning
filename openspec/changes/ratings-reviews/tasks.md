# Ratings & Reviews — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Ratings` class library and add it to the solution
- [ ] 1.2 Add project references (Auth, CourseManagement, Enrollment, EF Core)
- [ ] 1.3 Add `Review` entity + config (unique course+user) and `ReviewService` (submit, aggregate, list for owner, remove)
- [ ] 1.4 Register assembly scanning in `ApplicationDbContext` and `AddRatingsModule` in `Program.cs`

## 2. UI

- [ ] 2.1 Rating/review form on the course detail page (enrolled students)
- [ ] 2.2 Aggregate rating on cards and detail page (feed the discovery sort)
- [ ] 2.3 Owner reviews page; admin remove action

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Run `dotnet build` and start the app
- [ ] 3.3 Verify submit/replace, aggregate display, owner view, admin removal, non-enrolled denial
