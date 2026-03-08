# Day 12 — Password Policy + 2FA + Sessions

**Branch:** day-12-password-2fa-sessions
**Tests:** 106 passing (was 91, +15 new tests)

## What Was Built

### New Models
- `PasswordHistory` — stores last N password hashes per user
- `TwoFactorOtp` — 6-digit OTP with purpose, expiry, used flag
- `TrustedDevice` — device token valid 30 days, skips 2FA
- `UserSession` — session tracking with IP, UserAgent, expiry

### User Model Updates
- `TwoFactorEnabled` — boolean flag for 2FA

### New Services
- `IPasswordService / PasswordService`
  - ChangePasswordAsync — verifies current, validates strength, checks history, saves history
  - ValidatePasswordStrengthAsync — 8+ chars, upper, lower, digit, special
  - IsPasswordReusedAsync — checks last 5 password hashes
  - SavePasswordHistoryAsync — keeps only last 5 entries
  - IsPasswordExpiredAsync — 90 day expiry check
- `ITwoFactorService / TwoFactorService`
  - GenerateOtpAsync — 6-digit, 5 min expiry, Redis + DB dual storage
  - ValidateOtpAsync — Redis fast path + DB fallback
  - IsTrustedDeviceAsync — checks device token validity
  - AddTrustedDeviceAsync — issues 30-day trusted device token
  - RevokeAllTrustedDevicesAsync — revokes all devices for user

### New Controller
- `PasswordController`
  - POST /api/password/change
  - POST /api/password/validate-strength (anonymous)
  - POST /api/2fa/generate
  - POST /api/2fa/validate (with optional trust device)
  - POST /api/2fa/trust-device
  - DELETE /api/2fa/trusted-devices

### Database
- Migration: Day12_PasswordPolicy2FA
- New tables: PasswordHistories, TwoFactorOtps, TrustedDevices, UserSessions

### Redis
- Connection string updated with abortConnect=false for resilience
- OTP cached in Redis with 5 min TTL
- DB fallback when Redis unavailable

## Tests Added (PasswordServiceTests.cs)
- Password strength: strong, too short, no upper, no lower, no digit, no special
- Password history: new password not reused, reused password detected, keeps only last 5
- Change password: valid request, wrong current password, weak new password, resets force change flag
- Password expiry: recent change not expired, old change expired
