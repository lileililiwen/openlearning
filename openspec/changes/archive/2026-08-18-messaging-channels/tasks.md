# Messaging Channels — Tasks

## 1. Channel Abstractions

- [x] 1.1 Add `ISmsSender` + `IWebPushSender` interfaces with no-op defaults in the Notifications module
- [x] 1.2 Add `ChannelOptions` config; `NotificationService` dispatches to enabled channels (best-effort)

## 2. Web Push

- [x] 2.1 Add `PushSubscription` entity + config; subscribe/serve VAPID endpoints
- [x] 2.2 `service-worker.js` + push registration JS; `WebPushSender` sends + prunes expired

## 3. SMS

- [x] 3.1 `SmsSender` provider-adapter implementation behind `Messaging:Sms:Enabled`

## 4. Preferences

- [x] 4.1 Extend `account-settings` preferences with SMS/push toggles respected by dispatch

## 5. Migration & Verification

- [x] 5.1 Create EF Core migration
- [x] 5.2 Build, start app, verify: disabled channels skip, enabled channels attempted, failures don't block in-app, push subscribe/prune
