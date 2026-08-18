# User Profiles — Tasks

## 1. Data & Service

- [x] 1.1 Add `Bio` and `AvatarUrl` to `ApplicationUser` + config
- [x] 1.2 Implement `ProfileService` in `OpenLearning.Auth`: update profile, change password
- [x] 1.3 Create EF Core migration

## 2. Profile UI

- [x] 2.1 Profile page (`/Profile`): edit display name, bio, avatar
- [x] 2.2 Change-password form on the profile page

## 3. Password Reset

- [x] 3.1 Forgot-password page → Identity reset token (email via `notifications` sender; dev-only on-screen fallback)
- [x] 3.2 Reset-password page accepting token + new password

## 4. Public Instructor Page

- [x] 4.1 `/Instructors/{id}` page: bio, avatar, published courses (public)

## 5. Verification

- [x] 5.1 Run `dotnet build` and start the app
- [x] 5.2 Verify profile edit, change password, reset flow, and public instructor page
