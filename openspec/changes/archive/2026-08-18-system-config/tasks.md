# System Config — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.SystemConfig` class library, add to solution, add references (Auth, Notifications, EF Core)
- [x] 1.2 Add `Setting` + `NotificationTemplate` entities + configs
- [x] 1.3 Implement `SystemConfigService` (settings get/set with typed accessors, template CRUD, render)
- [x] 1.4 Register assembly scanning + `AddSystemConfigModule`

## 2. Application of Settings

- [x] 2.1 Wire key settings into catalog page size, upload limits, refund window, site name
- [x] 2.2 `NotificationService` renders templates when present

## 3. Admin UI

- [x] 3.1 `/Admin/System`: settings editor (whitelist) + notification template editor

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration + seed templates
- [x] 4.2 Build, start app, verify: setting change affects behavior (e.g. page size), template render with tokens, invalid values fall back
