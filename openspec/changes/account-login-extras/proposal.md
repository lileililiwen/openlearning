## Why

Registration and login currently require an email address and password. Mobile-first users expect to sign up or sign in with a phone number plus a one-time verification code, and to use third-party identity providers. "Forgot password" exists (email-based) but needs a phone-based fallback.

## What Changes

- Phone-number registration/login with a one-time SMS-style verification code (dev fallback shows the code on-screen, mirroring the password-reset pattern).
- Third-party login (Google/GitHub-style OAuth) via `AddAuthentication().AddGoogle()/.AddGitHub()` when configured.
- Real-name verification and notification settings move to a later change; this change only touches authentication entry points.

## Capabilities

### New Capabilities
- `account-login-extras`: phone + verification-code authentication and third-party OAuth login.

### Modified Capabilities

- `user-auth`: registration/login gains phone-code and third-party flows as alternatives to email/password.

## Impact

- New `OpenLearning.Account` module (or extend Auth): `PhoneCodeService` (issue/verify codes), and a `PhoneSignInManager` wrapper over Identity.
- `Pages/Auth/` gains `PhoneLogin`, `VerifyCode`, and OAuth callback handling; appsettings gains OAuth client config.
- No changes to existing email/password paths.
