# User Profiles — Tasks

## 1. Data & Service

- [ ] 1.1 Add `Bio` and `AvatarUrl` to `ApplicationUser` + config
- [ ] 1.2 Implement `ProfileService` in `OpenLearning.Auth`: update profile, change password
- [ ] 1.3 Create EF Core migration

## 2. Profile UI

- [ ] 2.1 Profile page (`/Profile`): edit display name, bio, avatar
- [ ] 2.2 Change-password form on the profile page

## 3. Password Reset

- [ ] 3.1 Forgot-password page → Identity reset token (email via `notifications` sender; dev-only on-screen fallback)
- [ ] 3.2 Reset-password page accepting token + new password

## 4. Public Instructor Page

- [ ] 4.1 `/Instructors/{id}` page: bio, avatar, published courses (public)

## 5. Verification

- [ ] 5.1 Run `dotnet build` and start the app
- [ ] 5.2 Verify profile edit, change password, reset flow, and public instructor page
