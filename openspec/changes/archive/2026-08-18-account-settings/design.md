# Account Settings — Design

## Context

Profiles exist but lack identity verification and notification preferences.

## Goals

- Users can submit real-name information and see verification status.
- Admins review and approve/reject verification requests.
- Users toggle in-app/email delivery per notification type.

## Non-Goals

- No government/KYC integration — manual admin review of submitted info (or document file reference).
- No mandatory verification (only instructors are required to be verified to publish, per design below).

## Decisions

### D1: Fields on `ApplicationUser`
Add `RealName`, `IdType` (enum: NationalId/Passport/Other), `IdNumber` (stored hashed, never plaintext), `IdentityStatus` (Unverified/Pending/Verified/Rejected), `VerifiedAt`, `VerificationNote`. Keeps identity data with the user; no new table.

### D2: Verification flow
`/Profile` shows a verification form (real name, type, number, optional document URL). Submitting sets `Pending` and notifies admins (via notifications). Admin `/Admin/Identities` lists pending requests with approve/reject (sets status + `VerifiedAt`/note, notifies the user). Publishing a course requires `IdentityStatus == Verified` for instructors.

### D3: Notification preferences
`NotificationPreference { Id, UserId, NotificationType, InApp, Email }` with a default row per type on user creation (defaults all on). `/Profile` renders toggles. `NotificationService.CreateAsync`/`CreateForManyAsync` filter recipients: skip in-app if `InApp == false`, skip email if `Email == false`.

## Risks / Trade-offs

- **PII** → ID number stored as SHA-256 hash; only real name (if given) displayed to admins; document URL optional and gated.
- **Preference explosion** → One row per type seeded at registration; new types added later get a default via migration or on-demand defaulting.

## Migration Plan

One migration adds the user fields and `NotificationPreferences`.

## Open Questions

- Should identity verification be required for all users? MVP: only instructors must be verified to publish.
