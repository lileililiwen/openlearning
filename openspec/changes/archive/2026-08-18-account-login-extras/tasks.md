# Account Login Extras — Tasks

## 1. Phone-code Service

- [x] 1.1 Add `PhoneCode` entity + config (unique phone, expiry, single-use) and `PhoneCodeService` (issue, verify, delete)
- [x] 1.2 Register assembly scanning in `ApplicationDbContext` and register `PhoneCodeService` in the Auth module

## 2. Phone Login UI

- [x] 2.1 `/Auth/PhoneLogin` page: phone number → issues code (dev on-screen fallback)
- [x] 2.2 `/Auth/VerifyCode` page: code → sign-in or create Student account
- [x] 2.3 Link phone login from the login page

## 3. Third-party Login

- [x] 3.1 Configure Google/GitHub OAuth behind `Authentication:*` config
- [x] 3.2 `/Auth/ExternalLogin` handler + callback that links/creates the user
- [x] 3.3 External login buttons on the login page

## 4. Migration & Verification

- [x] 4.1 Create EF Core migration
- [x] 4.2 Build, start app, verify: phone code issue/verify, new-phone signup, wrong-code rejection, OAuth callback links by email
