# Email Verification - Quick Start Guide

This guide will help you quickly set up and test the email verification system.

## Prerequisites

- .NET 8.0 SDK installed
- PostgreSQL database running
- MailHog (for local email testing) - **Recommended for development**

## Quick Setup (5 minutes)

### 1. Install MailHog (for local email testing)

**macOS:**
```bash
brew install mailhog
```

**Linux:**
```bash
sudo apt-get install golang-go
go install github.com/mailhog/MailHog@latest
```

**Windows:**
Download from: https://github.com/mailhog/MailHog/releases

**Or use Docker:**
```bash
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
```

### 2. Start MailHog

```bash
mailhog
```

MailHog will start on:
- SMTP Server: `localhost:1025`
- Web UI: `http://localhost:8025`

### 3. Start the API

```bash
cd src/GameServerHosting.Api
dotnet restore
dotnet run
```

The API will start on `http://localhost:5000`

### 4. Test the Email Verification Flow

Open your browser and navigate to:
- API: http://localhost:5000/swagger
- MailHog UI: http://localhost:8025

## Testing with cURL

### Step 1: Register a New User

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "johndoe",
    "email": "john@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "password": "SecurePass123!"
  }'
```

**Expected Response:**
```json
{
  "message": "Registration successful! Please check your email to verify your account.",
  "email": "john@example.com",
  "requiresEmailVerification": true
}
```

### Step 2: Check MailHog for Verification Email

1. Open http://localhost:8025
2. You should see a new email with subject "Verify Your Email - Game Server Hosting"
3. Click on the email to view it
4. The email will contain a verification link that looks like:
   ```
   http://localhost:5000/verify-email?token=VERIFICATION_TOKEN
   ```
5. Copy the entire token value from the URL

### Step 3: Verify the Email

```bash
curl -X POST http://localhost:5000/api/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "token": "PASTE_TOKEN_HERE"
  }'
```

**Expected Response:**
```json
{
  "message": "Email verified successfully! You can now log in."
}
```

### Step 4: Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "rememberMe": false
  }'
```

**Expected Response:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "tokenType": "Bearer",
  "expiresIn": 7200,
  "userId": "...",
  "userName": "johndoe",
  "email": "john@example.com",
  "role": "User"
}
```

## Testing with Swagger

1. Navigate to http://localhost:5000/swagger
2. Find the **Auth** section
3. Test endpoints in this order:
   - `POST /api/auth/register` - Register new user
   - Check MailHog for verification email
   - `POST /api/auth/verify-email` - Verify email with token
   - `POST /api/auth/login` - Login successfully

## Testing Scenarios

### Scenario 1: Login Before Email Verification

Try to login immediately after registration (before verifying email):

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "rememberMe": false
  }'
```

**Expected Response (401 Unauthorized):**
```json
{
  "message": "Please verify your email address before logging in. Check your inbox for the verification link."
}
```

### Scenario 2: Resend Verification Email

If the user didn't receive the email or it expired:

```bash
curl -X POST http://localhost:5000/api/auth/resend-verification-email \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com"
  }'
```

**Expected Response:**
```json
{
  "message": "If the email exists in our system and is not yet verified, a verification link has been sent."
}
```

Check MailHog again for the new verification email.

### Scenario 3: Invalid Token

Try to verify with an invalid or expired token:

```bash
curl -X POST http://localhost:5000/api/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "token": "INVALID_TOKEN"
  }'
```

**Expected Response (400 Bad Request):**
```json
{
  "message": "Email verification failed. The link may have expired.",
  "errors": [...]
}
```

## Using the HTTP Test File

We've included a test file with all common scenarios:

1. Open `src/GameServerHosting.Api/email-verification-tests.http` in VS Code
2. Install the REST Client extension if you haven't already
3. Click "Send Request" above each test

## Configuration

All email settings are in `appsettings.Development.json`:

```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "FromEmail": "dev@gameserverhosting.local",
    "FromName": "Game Server Hosting (Dev)",
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "Username": "",
      "Password": "",
      "EnableSsl": false
    },
    "BaseUrl": "http://localhost:5000"
  }
}
```

This configuration is already set up to work with MailHog out of the box!

## Troubleshooting

### MailHog Not Receiving Emails

1. **Check MailHog is running:**
   ```bash
   curl http://localhost:8025
   ```
   Should return HTML content

2. **Check API logs:**
   Look for lines like:
   ```
   [INF] Verification email sent to john@example.com
   ```

3. **Verify SMTP settings:**
   Make sure `appsettings.Development.json` has:
   - `Provider`: "SMTP"
   - `Smtp.Host`: "localhost"
   - `Smtp.Port`: 1025

### Email Service Not Configured Warning

If you see this in logs:
```
[WRN] SMTP host is not configured. Email will be logged instead of sent.
```

Check that `EmailSettings.Smtp.Host` is set to "localhost" in your config.

### Token Validation Fails

1. **Token expired:**
   - Default expiration is 24 hours
   - Request a new verification email

2. **Copy/paste issues:**
   - Make sure you copied the entire token
   - Remove any extra whitespace

3. **URL encoding:**
   - The token should be URL-encoded in the verification link
   - When using cURL, paste the raw token (not URL-encoded)

## What's Happening Behind the Scenes?

1. **Registration:**
   - User account is created with `EmailConfirmed = false`
   - ASP.NET Core Identity generates a secure verification token
   - Email is sent via configured provider (SMTP/SendGrid)

2. **Email Verification:**
   - User clicks link or submits token via API
   - Token is validated by ASP.NET Core Identity
   - `EmailConfirmed` is set to `true` on the user account

3. **Login:**
   - System checks if `EmailConfirmed = true`
   - If false, login is denied with helpful message
   - If true, JWT tokens are generated and returned

## Next Steps

Once you've verified the system works locally:

1. **Production Setup:**
   - Sign up for SendGrid
   - Add API key to production configuration
   - Update base URL to your production domain

2. **Frontend Integration:**
   - Create verification page at `/verify-email`
   - Add verification status messages
   - Implement resend verification button

3. **Monitoring:**
   - Monitor email delivery rates
   - Track verification completion rates
   - Set up alerts for failed email deliveries

## Quick Reference

**Endpoints:**
- `POST /api/auth/register` - Register user (sends verification email)
- `POST /api/auth/verify-email` - Verify email with token
- `POST /api/auth/resend-verification-email` - Resend verification email
- `POST /api/auth/login` - Login (requires verified email)
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password with token

**MailHog URLs:**
- SMTP: `localhost:1025`
- Web UI: `http://localhost:8025`

**API URLs:**
- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Health: `http://localhost:5000/health`

## Support

For detailed information, see `EMAIL_VERIFICATION_SETUP.md`
