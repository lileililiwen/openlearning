# System Config — Design

## Context

Platform behavior is hard-coded. Admins need configurable parameters and notification copy.

## Goals

- Admins edit key-value settings that code reads.
- Admins edit notification templates per type.
- Templates render with placeholders at notification time.

## Non-Goals

- No feature flags or A/B config.
- No environment-specific secrets in settings (those stay in appsettings).
- No template versioning/history in MVP.

## Decisions

### D1: New `OpenLearning.SystemConfig` module
`Setting { Id, Key, Value }` (unique key). `NotificationTemplate { Id, NotificationType, Title, Body, IsActive }` (one per type). `SystemConfigService`: `GetAsync(key)`, `SetAsync(key,value)`, `GetIntAsync`, `GetBoolAsync`, template CRUD, `RenderAsync(type, values)` replacing `{placeholder}` tokens.

### D2: Settings consumption
`Program.cs` registers a typed settings provider; pages/services read e.g. `Site.Name`, `Catalog.PageSize`, `Upload.MaxBytes`, `Refund.WindowDays` with code defaults when unset. Only the keys that code actually reads are exposed in the UI (a whitelist, not arbitrary keys).

### D3: Template application
`NotificationService.CreateAsync` looks up an active template for the type; if present it renders title/body with `{CourseTitle}`, `{Score}`, etc. tokens passed by the caller; falls back to the caller-provided text when no template exists. Email body reuses the rendered title/body.

## Risks / Trade-offs

- **Invalid values** → Typed getters parse with fallback defaults and a validation pass in the admin form.
- **Template breakage** → Unknown tokens render as-is (never throw); documented.

## Migration Plan

One migration creates `Settings` and `NotificationTemplates`; seed default templates.

## Open Questions

- Should settings be cached? MVP: read through EF on demand (low volume).
