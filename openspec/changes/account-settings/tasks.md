# Account Settings — Tasks

## 1. Data & Service

- [ ] 1.1 Add real-name fields + `IdentityStatus` to `ApplicationUser`; add `NotificationPreference` entity + config
- [ ] 1.2 Extend `ProfileService`: submit verification, update preferences; add `IdentityService` (list pending, approve/reject)
- [ ] 1.3 Seed default preferences on registration; `NotificationService` respects preferences

## 2. UI

- [ ] 2.1 Profile verification form + status display
- [ ] 2.2 Profile notification-settings toggles
- [ ] 2.3 Admin `/Admin/Identities` review page (approve/reject + note)

## 3. Enforcement

- [ ] 3.1 Publishing a course requires verified identity for instructors

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: submit verification → pending → approve/reject → status shown; preferences suppress notifications; unverified instructor cannot publish
