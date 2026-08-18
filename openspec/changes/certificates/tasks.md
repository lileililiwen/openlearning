# Certificates — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Certificates` class library and add it to the solution
- [ ] 1.2 Add project references (Auth, CourseManagement, Progress, EF Core)
- [ ] 1.3 Add `Certificate` entity + config (unique enrollment, code) and `CertificateService` (EnsureIssued, get for enrollment/user)
- [ ] 1.4 Register assembly scanning in `ApplicationDbContext` and `AddCertificatesModule` in `Program.cs`

## 2. UI

- [ ] 2.1 Printable certificate page (`/Certificates/View`) with print CSS
- [ ] 2.2 Certificate links/badges on course details, student dashboard, and profile
- [ ] 2.3 Trigger `EnsureIssued` on course details/dashboard load

## 3. Migration & Verification

- [ ] 3.1 Create EF Core migration
- [ ] 3.2 Run `dotnet build` and start the app
- [ ] 3.3 Verify issuance at 100%, no duplicates, view/print access (student, owner, admin), and denial for others
