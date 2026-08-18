# System Config — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.SystemConfig` class library, add to solution, add references (Auth, Notifications, EF Core)
- [ ] 1.2 Add `Setting` + `NotificationTemplate` entities + configs
- [ ] 1.3 Implement `SystemConfigService` (settings get/set with typed accessors, template CRUD, render)
- [ ] 1.4 Register assembly scanning + `AddSystemConfigModule`

## 2. Application of Settings

- [ ] 2.1 Wire key settings into catalog page size, upload limits, refund window, site name
- [ ] 2.2 `NotificationService` renders templates when present

## 3. Admin UI

- [ ] 3.1 `/Admin/System`: settings editor (whitelist) + notification template editor

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration + seed templates
- [ ] 4.2 Build, start app, verify: setting change affects behavior (e.g. page size), template render with tokens, invalid values fall back
