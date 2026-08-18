## Why

Notifications deliver in-app and email. The reference system's Infrastructure lists SMS and push as additional channels. Adding channel adapters (SMS provider, web push) extends reach for time-sensitive events like verification codes and expiry reminders.

## What Changes

- Extend the notifications channel model from in-app/email to include SMS and web push.
- `IEmailSender` pattern generalizes to `INotificationChannel` implementations: InApp (existing), Email (existing), Sms, WebPush.
- Per-channel enablement via config; failures never block in-app delivery.
- Web push: subscribe (service worker + VAPID), send best-effort.

## Capabilities

### New Capabilities
- `messaging-channels`: SMS and web-push delivery channels for notifications.

### Modified Capabilities

- `notifications`: channel dispatch becomes provider-based; preferences (`account-settings`) extend to SMS/push.

## Impact

- `OpenLearning.Notifications` gains `SmsSender` (provider-adapter interface, no-op default) and `WebPushSender` (VAPID + subscription table `PushSubscription { Id, UserId, Endpoint, P256Dh, Auth, CreatedAt }`).
- `NotificationService` dispatches to enabled channels per event.
- `account-settings` preferences add SMS/push toggles.
