# Account Settings — Tasks

## 1. Data & Service

- [x] 1.1 Add real-name fields + `IdentityStatus` to `ApplicationUser`; add `NotificationPreference` entity + config
- [x] 1.2 Extend `ProfileService`: submit verification, update preferences; add `IdentityService` (list pending, approve/reject)
- [x] 1.3 Seed default preferences on registration; `NotificationService` respects preferences

## 2. UI

- [x] 2.1 Profile verification form + status display
- [x] 2.2 Profile notification-settings toggles
- [x] 2.3 Admin `/Admin/Identities` review page (approve/reject + note)

## 3. Enforcement

- [x] 3.1 Publishing a course requires verified identity for instructors

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: submit verification → pending → approve/reject → status shown; preferences suppress notifications; unverified instructor cannot publish
