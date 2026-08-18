# Account Login Extras — Design

## Context

Authentication is email/password only. The reference system lists mobile-number login, verification codes, and third-party login. This change adds those flows while keeping the existing email/password path untouched.

## Goals

- Users can register and sign in with a phone number + one-time code.
- Users can sign in via a configured OAuth provider.
- Existing email/password users keep working unchanged.

## Non-Goals

- No SMS gateway integration for the MVP — a dev-only on-screen code mirrors the password-reset fallback.
- No real-name verification or notification settings (separate change).
- No passwordless magic-link email login.

## Decisions

### D1: Extend `OpenLearning.Auth`, not a new module
`PhoneNumber` is already on `ApplicationUser` (Identity). Add `PhoneCode { Id, PhoneNumber, Code, ExpiresAt, UsedAt }` stored in the DB (so it survives restarts) with a `PhoneCodeService.IssueAsync`/`VerifyAsync`. Code is 6 digits, 10-minute expiry, single-use.

### D2: Verification-code flow
`/Auth/PhoneLogin` takes a phone number → issues a code → stores it → shows the dev code (when `IHostEnvironment.IsDevelopment()`) → `/Auth/VerifyCode` accepts the code and signs the user in, creating an account (role Student) if the phone is new. Codes are deleted after use.

### D3: Third-party login
`AddAuthentication().AddGoogle()` / `.AddGitHub()` behind `Authentication:Google:ClientId` config. OAuth callback matches/creates a local user by email (existing emails link; new emails create a Student account with the provider's name). A `/Auth/ExternalLogin` handler kicks off the challenge.

## Risks / Trade-offs

- **Code guessing** → 6 digits with 10-min expiry and a 5-attempt lockout per phone; codes single-use.
- **OAuth email collision** → An email already bound to a local account links by email; otherwise a new account is created (verified identity via the provider).
- **SMS cost/gateway** → Dev-only on-screen fallback; production needs a provider (documented config point).

## Migration Plan

One migration adds `PhoneCodes`.

## Open Questions

- Should phone be required or optional on registration? MVP: optional, phone login is a separate flow.
