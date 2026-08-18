# Logging — Design

## Context

Mutating actions leave no audit trail and exceptions only reach the console.

## Goals

- Record significant user/admin operations.
- Persist unhandled exceptions with request context.
- Admin views with filtering.

## Non-Goals

- No full request logging (body/payload capture).
- No log shipping/ELK integration in MVP.
- No per-entity diffing.

## Decisions

### D1: New `OpenLearning.Logging` module
`OperationLog { Id, ActorId?, ActorName, Action, TargetType?, TargetId?, Details, IpAddress, CreatedAt }` (index `(CreatedAt)`, `(Action)`). `ErrorLog { Id, Message, StackTrace, Path, RequestMethod, UserId?, CreatedAt }`. `LogService.RecordAsync(action, targetType, targetId, details)` and `LogErrorAsync(...)`.

### D2: Recording call sites
The Web layer records at mutating handlers (course publish/delete, role toggle, suspension, refund review, withdrawal review, verification decision, announcement post). A `LoggingExceptionMiddleware` (after `UseExceptionHandler`) writes `ErrorLog` for unhandled exceptions, including the request path/method/user.

### D3: Admin UI + retention
`/Admin/Logs/Operations` and `/Admin/Logs/Errors`: filterable (action, actor, date) paginated tables. A background/prune-on-open job deletes records older than a configurable retention (default 90 days).

## Risks / Trade-offs

- **Table growth** → Retention prune + pagination bounds size.
- **Log completeness** → Best-effort at the call sites; not every read is logged (only mutations).

## Migration Plan

One migration creates `OperationLogs` and `ErrorLogs`.

## Open Questions

- Should error logs include request bodies? MVP: no (privacy/size).
