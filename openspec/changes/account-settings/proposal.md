## Why

User profiles can be edited, but the reference system's User Foundation lists real-name verification and notification settings, which are missing. Real-name verification builds trust for instructors and certificates; notification settings give users control over what they receive.

## What Changes

- Real-name verification: a user submits a real name + ID-type/number (or document upload); admin reviews and marks the identity verified (or rejected).
- Notification settings: per-user preferences toggling which notification types are delivered in-app and by email.

## Capabilities

### New Capabilities
- `account-settings`: real-name verification and notification preferences.

### Modified Capabilities

- `user-profiles`: profile page gains verification status and notification settings sections.
- `notifications`: delivery respects the user's preferences.

## Impact

- `ApplicationUser` gains `RealName`, `IdType`, `IdNumberHash` (or document ref), `IdentityStatus` (enum), `VerifiedAt`.
- New `NotificationPreference { Id, UserId, Type, InApp, Email }` or JSON column.
- `ProfileService` extends (submit verification, update preferences); admin review page; `NotificationService` filters by preference.
