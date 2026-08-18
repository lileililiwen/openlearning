# User Profiles — Design

## Context

The Auth module seeds `DisplayName` but there is no profile UI. Password management (change/reset) exists in Identity but is unwired. This change adds profile pages, account security, and public instructor pages.

## Goals

- Users can edit their display name, bio, and avatar.
- Users can change their password and reset a forgotten password.
- Instructors have a public page showing their bio and courses.

## Non-Goals

- No avatar upload/processing (avatar is a URL string for now; uploads deferred).
- No two-factor authentication.
- No email confirmation enforcement (remains optional).

## Decisions

### D1: Fields on `ApplicationUser`
Add `Bio` (string) and `AvatarUrl` (string, nullable). Keeps profile data with identity; no new table.

### D2: `ProfileService` in the Auth module
Methods: `UpdateProfileAsync(userId, displayName, bio, avatarUrl)`, `ChangePasswordAsync(userId, current, newPassword)`. Uses `UserManager` — lives naturally in `OpenLearning.Auth` (avoids a one-file module). Pages live under `Pages/Profile/`.

### D3: Forgot/reset password
Standard Identity flow: forgot page → `GeneratePasswordResetTokenAsync` → email the link (via the `notifications` email sender) → reset page with token. **Dev fallback:** when no email provider is configured, the reset link/token is displayed on-screen (clearly labeled dev-only) so the flow is testable; in production without email it is hidden.

### D4: Public instructor page
`Pages/Instructors/{id}.cshtml`: display name, avatar, bio, and the instructor's published courses (via `CourseService.GetByInstructorAsync` filtered to published for anonymous viewers). Requires no auth to view.

## Risks / Trade-offs

- **Dev-only reset links are insecure if deployed** → Gate the on-screen link behind `IHostEnvironment.IsDevelopment()`.
- **Avatar URL abuse** → Rendered as an `img` attribute only; no HTML injection (Razor encodes).

## Migration Plan

One migration adds `Bio` and `AvatarUrl` to `AspNetUsers`.

## Open Questions

- Should email confirmation become mandatory? Deferred.
