## Why

The platform has hard-coded defaults (e.g. page size, upload limits, notification text). The reference system's Admin Backend requires System Configuration: parameter settings and notification templates.

## What Changes

- Key-value system settings editable by admins (site name, default page size, upload limits, refund window, contact email, etc.).
- Notification templates: editable title/body templates per notification type with placeholders.
- Settings are read through a typed `SystemSettings` service; templates apply at notification creation time.

## Capabilities

### New Capabilities
- `system-config`: admin-editable parameters and notification templates.

### Modified Capabilities

- `notifications`: notification creation renders from templates.
- `lms-core`: site name and other parameters come from settings.

## Impact

- New `OpenLearning.SystemConfig` module: `Setting { Id, Key, Value }` (unique key), `NotificationTemplate { Id, Type, Title, Body, IsActive }`.
- `SystemConfigService` (get/set settings, template CRUD, `RenderAsync(type, values)`); `NotificationService` uses `RenderAsync` when a template exists.
- Admin pages `/Admin/System` (settings + templates).
