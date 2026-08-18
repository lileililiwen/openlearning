# Operations Config — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Operations` class library, add to solution, add references (Auth, CourseManagement, EF Core)
- [x] 1.2 Add `Banner`, `Popup`, `Campaign`, `HomepageFeature` entities + configs
- [x] 1.3 Implement `OperationsService` (admin CRUD, active queries)
- [x] 1.4 Register assembly scanning + `AddOperationsModule`

## 2. UI

- [x] 2.1 Homepage carousel renders active banners; featured categories/courses section
- [x] 2.2 Pop-up endpoint + layout one-per-session display
- [x] 2.3 `/Admin/Operations` page (banners, pop-ups, campaigns, homepage features)

## 3. Migration & Verification

- [x] 3.1 Create EF Core migration
- [x] 3.2 Build, start app, verify: banner ordering/activation, campaign window filtering, pop-up once per session, featured content, non-admin denied
