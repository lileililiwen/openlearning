# Messaging Channels — Tasks

## 1. Channel Abstractions

- [ ] 1.1 Add `ISmsSender` + `IWebPushSender` interfaces with no-op defaults in the Notifications module
- [ ] 1.2 Add `ChannelOptions` config; `NotificationService` dispatches to enabled channels (best-effort)

## 2. Web Push

- [ ] 2.1 Add `PushSubscription` entity + config; subscribe/serve VAPID endpoints
- [ ] 2.2 `service-worker.js` + push registration JS; `WebPushSender` sends + prunes expired

## 3. SMS

- [ ] 3.1 `SmsSender` provider-adapter implementation behind `Messaging:Sms:Enabled`

## 4. Preferences

- [ ] 4.1 Extend `account-settings` preferences with SMS/push toggles respected by dispatch

## 5. Migration & Verification

- [ ] 5.1 Create EF Core migration
- [ ] 5.2 Build, start app, verify: disabled channels skip, enabled channels attempted, failures don't block in-app, push subscribe/prune
