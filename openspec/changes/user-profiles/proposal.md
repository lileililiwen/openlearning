## Why

Users can register and sign in but cannot manage their own account: no profile page, no password change, and the Identity password-reset flow is unusable without an email provider. A public instructor profile also gives learners confidence and a place to see an instructor's courses.

## What Changes

- **Profile page** (`/Profile`): display name, avatar, and short bio; shown to the user (editable) and, for instructors, as a public page.
- **Account security**: change password (current + new) and a working forgot-password / reset-password flow using Identity tokens and email (email required; without a provider, reset links are shown on-screen in dev only).
- **Public instructor page** (`/Instructors/{id}`): bio + list of published courses.

## Capabilities

### New Capabilities
- `user-profiles`: profile editing, avatar/bio, change password, forgot/reset password, and public instructor pages.

### Modified Capabilities

None.

## Impact

- `ApplicationUser` gains `Bio` (and optional `AvatarUrl`); one migration.
- New `ProfileService` (in the Auth module or a small `OpenLearning.Profiles` module) for updates and password changes.
- Forgot/reset pages added under `Pages/Auth/`; reuse the `notifications` email sender when available.
- Public instructor pages under `Pages/Instructors/`.
