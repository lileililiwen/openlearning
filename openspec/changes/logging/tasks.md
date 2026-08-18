# Logging — Tasks

## 1. Module Setup

- [ ] 1.1 Create `src/OpenLearning.Logging` class library, add to solution, add references (Auth, EF Core)
- [ ] 1.2 Add `OperationLog` + `ErrorLog` entities + configs
- [ ] 1.3 Implement `LogService` (record operation, log error) + `LoggingExceptionMiddleware`
- [ ] 1.4 Register assembly scanning + `AddLoggingModule`

## 2. Call Sites & Middleware

- [ ] 2.1 Record operations at mutating Web handlers (publish/delete, roles, suspend, refund, withdrawal, verification, announcement)
- [ ] 2.2 Wire `LoggingExceptionMiddleware` to write error logs

## 3. Admin UI & Retention

- [ ] 3.1 `/Admin/Logs/Operations` + `/Admin/Logs/Errors` with filters/pagination
- [ ] 3.2 Retention prune (default 90 days)

## 4. Migration & Verification

- [ ] 4.1 Create EF Core migration
- [ ] 4.2 Build, start app, verify: operation recorded on a mutation, exception writes an error log, admin views filter, prune removes old rows, non-admin denied
