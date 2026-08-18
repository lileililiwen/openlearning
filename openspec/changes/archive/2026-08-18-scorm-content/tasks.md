# SCORM Content — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Scorm` class library and add it to the solution
- [x] 1.2 Add project references (Auth, CourseManagement, Enrollment, Progress, EF Core)

## 2. Data Model

- [x] 2.1 Add `ScormPackage` and `ScormRecord` entities + `IEntityTypeConfiguration` classes
- [x] 2.2 Register assembly scanning in `ApplicationDbContext` and `AddScormModule` in `Program.cs`

## 3. Services

- [x] 3.1 Implement `ScormService`: owner check, zip extraction (path-traversal safe), `imsmanifest.xml` parsing, package persistence, remove
- [x] 3.2 Implement `ScormRuntimeService`: init (return persisted state), commit (upsert state), completion integration with `ProgressService`

## 4. UI & Runtime

- [x] 4.1 Upload/remove control on the lesson edit page (owner-only)
- [x] 4.2 Launch page: iframe to entry point + SCORM 1.2 API adapter (`scorm-api.js`)
- [x] 4.3 Runtime JSON endpoints (`/scorm/runtime/init`, `/scorm/runtime/commit`)
- [x] 4.4 Launch links on lesson view page for enrolled students

## 5. Migration & Verification

- [x] 5.1 Create EF Core migration (`AddScorm`)
- [x] 5.2 Run `dotnet build` and start the app
- [x] 5.3 Verify with a minimal SCORM 1.2 package: upload → launch → init/commit/completed → state persisted + lesson completed
