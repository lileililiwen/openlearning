## Why

There is no audit trail. The reference system's Infrastructure lists Logs: operation logs and error logs. Operation logs enable accountability (who did what), and error logs aid debugging.

## What Changes

- Operation logs: record significant actions (course publish/delete, role changes, suspensions, refunds, withdrawals, verification decisions) with actor, action, target, timestamp, and IP.
- Error logs: capture unhandled exceptions with context into the DB (in addition to console logging).
- Admin pages to view/search logs; retention pruning.

## Capabilities

### New Capabilities
- `logging`: operation audit log and error log with admin views.

### Modified Capabilities

None.

## Impact

- New `OpenLearning.Logging` module: `OperationLog { Id, ActorId, ActorName, Action, TargetType, TargetId, Details, IpAddress, CreatedAt }`, `ErrorLog { Id, Message, StackTrace, Path, RequestMethod, UserId, CreatedAt }`.
- `LogService.RecordAsync` called at mutating call sites (Web layer composition); an exception-handling middleware writes `ErrorLog`.
- Admin pages `/Admin/Logs` (operations, errors) with filters + pagination; nightly prune job.
