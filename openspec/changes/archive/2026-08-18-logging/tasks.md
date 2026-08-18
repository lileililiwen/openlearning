# Logging — Tasks

## 1. Module Setup

- [x] 1.1 Create `src/OpenLearning.Logging` class library, add to solution, add references (Auth, EF Core)
- [x] 1.2 Add `OperationLog` + `ErrorLog` entities + configs
- [x] 1.3 Implement `LogService` (record operation, log error) + `LoggingExceptionMiddleware`
- [x] 1.4 Register assembly scanning + `AddLoggingModule`

## 2. Call Sites & Middleware

- [x] 2.1 Record operations at mutating Web handlers (publish/delete, roles, suspend, refund, withdrawal, verification, announcement)
- [x] 2.2 Wire `LoggingExceptionMiddleware` to write error logs

## 3. Admin UI & Retention

- [x] 3.1 `/Admin/Logs/Operations` + `/Admin/Logs/Errors` with filters/pagination
- [x] 3.2 Retention prune (default 90 days)

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: operation recorded on a mutation, exception writes an error log, admin views filter, prune removes old rows, non-admin denied
