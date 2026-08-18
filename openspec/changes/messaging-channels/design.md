# Messaging Channels — Design

## Context

Notifications have in-app + email. SMS and web push add reach, especially for codes and reminders.

## Goals

- SMS delivery behind a provider adapter (no-op when unconfigured).
- Web push via service worker + VAPID.
- Channel failures never affect in-app delivery.

## Non-Goals

- No multi-provider selection logic (one SMS adapter point).
- No rich push media in MVP.
- No delivery receipts/analytics.

## Decisions

### D1: Channel abstraction
Generalize `IEmailSender` usage: `NotificationService` consults a `ChannelOptions` (enabled flags from config) and calls `IEmailSender`, `ISmsSender`, `IWebPushSender` in turn. Each is best-effort with internal try/catch. `ISmsSender { SendAsync(phone, message) }` and `IWebPushSender { SendAsync(userId, title, body, link) }` with no-op defaults registered unless enabled.

### D2: Web push
`PushSubscription { Id, UserId, Endpoint, P256Dh, Auth, CreatedAt }` (unique per `(UserId, Endpoint)`). A `/push/subscribe` POST stores the subscription (user gated); `/push/vapid-public-key` serves the public key; a `service-worker.js` handles `push` events. `WebPushSender` iterates the user's subscriptions, sends (using a `WebPush`-style library or raw HTTP), and prunes expired endpoints.

### D3: Preferences integration
`account-settings` adds per-type `Sms`/`Push` toggles (default on when the channel is enabled). `NotificationService` checks the preference for each enabled channel.

## Risks / Trade-offs

- **Provider cost** → SMS only fires for types configured to use it; no-op default prevents accidental sends.
- **Endpoint churn** → Expired push endpoints pruned on send failure (documented).

## Migration Plan

One migration creates `PushSubscriptions`.

## Open Questions

- Should SMS be restricted to phone-verified users? MVP: send to any user with a phone.
