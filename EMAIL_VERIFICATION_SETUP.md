# Email Verification Implementation

This document describes the email verification system that has been implemented for the Game Server Hosting platform.

## Overview

The email verification system ensures that users verify their email addresses before they can access the platform. This implementation includes:

- Email service supporting both **SendGrid** and **SMTP**
- Professional HTML email templates
- Email verification endpoints
- Password reset functionality
- Resend verification email capability

## Features

### 1. Email Service Configuration

The email service can be configured to use either SendGrid or SMTP:

#### SendGrid (Recommended for Production)
- Fast and reliable email delivery
- Easy setup with API key
- Automatic handling of bounces and unsubscribes
- Detailed analytics

#### SMTP (Good for Development)
- Works with any SMTP server
- Great for local development with tools like MailHog or smtp4dev
- No external dependencies

### 2. Email Templates

Professional HTML email templates with:
- Modern, responsive design
- Clear call-to-action buttons
- Security warnings
- Expiration notices
- Plain text fallback

### 3. Verification Flow

1. User registers with email and password
2. System sends verification email with unique token
3. User clicks verification link in email
4. Email is marked as verified
5. User can now log in

## Configuration

### appsettings.json

```json
{
  "EmailSettings": {
    "Provider": "SMTP",  // or "SendGrid"
    "FromEmail": "noreply@gameserverhosting.com",
    "FromName": "Game Server Hosting",
    "SendGrid": {
      "ApiKey": "YOUR_SENDGRID_API_KEY"
    },
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "Username": "",
      "Password": "",
      "EnableSsl": false
    },
    "Templates": {
      "EmailVerificationSubject": "Verify Your Email - Game Server Hosting",
      "PasswordResetSubject": "Reset Your Password - Game Server Hosting"
    },
    "VerificationTokenExpirationHours": 24,
    "BaseUrl": "http://localhost:5000"
  }
}
```

### Environment-Specific Configuration

#### Development (appsettings.Development.json)
- Uses SMTP with localhost (MailHog/smtp4dev)
- Port 1025 (default for MailHog)
- No authentication required
- BaseUrl: http://localhost:5000

#### Production (appsettings.Production.json)
- Uses SendGrid
- API key from environment variables
- BaseUrl: https://gameserverhosting.com

## API Endpoints

### 1. Register
**POST** `/api/auth/register`

Registers a new user and sends verification email.

**Request:**
```json
{
  "userName": "johndoe",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "message": "Registration successful! Please check your email to verify your account.",
  "email": "john@example.com",
  "requiresEmailVerification": true
}
```

### 2. Verify Email
**POST** `/api/auth/verify-email`

Verifies user's email address using the token from the verification email.

**Request:**
```json
{
  "email": "john@example.com",
  "token": "verification_token_from_email"
}
```

**Response:**
```json
{
  "message": "Email verified successfully! You can now log in."
}
```

### 3. Resend Verification Email
**POST** `/api/auth/resend-verification-email`

Resends the verification email if user didn't receive it or if the previous token expired.

**Request:**
```json
{
  "email": "john@example.com"
}
```

**Response:**
```json
{
  "message": "If the email exists in our system and is not yet verified, a verification link has been sent."
}
```

### 4. Login
**POST** `/api/auth/login`

Logs in a user (requires verified email).

**Request:**
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!",
  "rememberMe": false
}
```

**Response (Success):**
```json
{
  "accessToken": "jwt_token_here",
  "refreshToken": "refresh_token_here",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "userId": "user_guid",
  "userName": "johndoe",
  "email": "john@example.com",
  "role": "User"
}
```

**Response (Unverified Email):**
```json
{
  "message": "Please verify your email address before logging in. Check your inbox for the verification link."
}
```

## Development Setup

### Option 1: Using MailHog (Recommended)

MailHog is a fake SMTP server for testing email functionality in development.

1. **Install MailHog:**
   ```bash
   # macOS
   brew install mailhog
   
   # Linux
   go get github.com/mailhog/MailHog
   
   # Windows
   # Download from https://github.com/mailhog/MailHog/releases
   ```

2. **Run MailHog:**
   ```bash
   mailhog
   ```

3. **Access Web UI:**
   Open http://localhost:8025 to view emails

4. **Configuration:**
   The development settings are already configured for MailHog (localhost:1025)

### Option 2: Using smtp4dev

smtp4dev is a modern alternative to MailHog with a better UI.

1. **Install via Docker:**
   ```bash
   docker run -p 3000:80 -p 1025:25 rnwood/smtp4dev
   ```

2. **Access Web UI:**
   Open http://localhost:3000 to view emails

### Option 3: Using SendGrid (Development)

If you prefer to test with real email delivery:

1. Sign up for a free SendGrid account at https://sendgrid.com
2. Create an API key
3. Update `appsettings.Development.json`:
   ```json
   {
     "EmailSettings": {
       "Provider": "SendGrid",
       "SendGrid": {
         "ApiKey": "YOUR_SENDGRID_API_KEY"
       }
     }
   }
   ```

## Production Setup

### Using SendGrid

1. **Sign up for SendGrid:**
   - Go to https://sendgrid.com
   - Choose appropriate plan (free tier available)

2. **Create API Key:**
   - Go to Settings → API Keys
   - Create new API key with "Full Access" or "Mail Send" permission
   - Save the API key securely

3. **Configure Domain Authentication:**
   - Go to Settings → Sender Authentication
   - Authenticate your domain for better deliverability

4. **Set Environment Variables:**
   ```bash
   export EmailSettings__SendGrid__ApiKey="your_sendgrid_api_key"
   export EmailSettings__FromEmail="noreply@yourdomain.com"
   export EmailSettings__BaseUrl="https://yourdomain.com"
   ```

5. **Update Production Config:**
   The production config is already set up to use SendGrid. Just ensure environment variables are set.

### Using SMTP (Alternative)

If you prefer to use your own SMTP server:

1. **Update Production Config:**
   ```json
   {
     "EmailSettings": {
       "Provider": "SMTP",
       "Smtp": {
         "Host": "smtp.yourdomain.com",
         "Port": 587,
         "Username": "smtp_username",
         "Password": "smtp_password",
         "EnableSsl": true
       }
     }
   }
   ```

2. **Set via Environment Variables:**
   ```bash
   export EmailSettings__Provider="SMTP"
   export EmailSettings__Smtp__Host="smtp.yourdomain.com"
   export EmailSettings__Smtp__Username="your_username"
   export EmailSettings__Smtp__Password="your_password"
   ```

## Testing

### Manual Testing

1. **Start the API:**
   ```bash
   cd src/GameServerHosting.Api
   dotnet run
   ```

2. **Start MailHog (if using):**
   ```bash
   mailhog
   ```

3. **Register a User:**
   ```bash
   curl -X POST http://localhost:5000/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "userName": "testuser",
       "email": "test@example.com",
       "firstName": "Test",
       "lastName": "User",
       "password": "TestPass123!"
     }'
   ```

4. **Check MailHog:**
   - Open http://localhost:8025
   - View the verification email
   - Copy the verification token from the URL

5. **Verify Email:**
   ```bash
   curl -X POST http://localhost:5000/api/auth/verify-email \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "token": "token_from_email"
     }'
   ```

6. **Login:**
   ```bash
   curl -X POST http://localhost:5000/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "TestPass123!",
       "rememberMe": false
     }'
   ```

## Security Considerations

1. **Token Expiration:**
   - Email verification tokens expire after 24 hours by default
   - Configurable via `VerificationTokenExpirationHours`

2. **Email Enumeration Prevention:**
   - Resend verification endpoint doesn't reveal if email exists
   - Consistent responses for valid and invalid emails

3. **HTTPS in Production:**
   - Always use HTTPS for verification links in production
   - Update `BaseUrl` in production settings

4. **Rate Limiting:**
   - Consider implementing rate limiting on email endpoints
   - Prevents abuse of verification email sending

5. **Secure Token Storage:**
   - Tokens are generated by ASP.NET Core Identity
   - Uses secure cryptographic methods
   - Never stored in plain text

## Troubleshooting

### Emails Not Sending

1. **Check Configuration:**
   - Verify email settings in appsettings.json
   - Check logs for errors

2. **SMTP Connection Issues:**
   - Verify SMTP host and port
   - Check firewall settings
   - Ensure EnableSsl matches server requirements

3. **SendGrid Issues:**
   - Verify API key is correct
   - Check SendGrid account status
   - Review SendGrid activity logs

### Emails Going to Spam

1. **Configure SPF/DKIM:**
   - Set up proper DNS records
   - Use SendGrid domain authentication

2. **From Address:**
   - Use a verified sender address
   - Avoid using personal email domains

### Token Validation Fails

1. **Token Expired:**
   - Default expiration is 24 hours
   - User can request a new verification email

2. **URL Encoding:**
   - Token must be properly URL-encoded
   - Frontend should handle encoding automatically

## Dependencies

The following NuGet packages were added:

- `SendGrid` (v9.29.3) - SendGrid email service client
- `MailKit` (v4.8.0) - SMTP email sending library

## Files Modified/Created

### Created:
- `src/GameServerHosting.Api/Models/EmailSettings.cs` - Email configuration model
- `src/GameServerHosting.Api/Models/VerifyEmailRequest.cs` - Email verification request model
- `src/GameServerHosting.Api/Models/ResendVerificationEmailRequest.cs` - Resend verification request model
- `EMAIL_VERIFICATION_SETUP.md` - This documentation file

### Modified:
- `src/GameServerHosting.Api/Services/IEmailService.cs` - Added email verification method
- `src/GameServerHosting.Api/Services/EmailService.cs` - Complete rewrite with SendGrid/SMTP support
- `src/GameServerHosting.Api/Controllers/AuthController.cs` - Added verification endpoints
- `src/GameServerHosting.Api/Program.cs` - Added email settings configuration
- `src/GameServerHosting.Api/GameServerHosting.Api.csproj` - Added NuGet packages
- `src/GameServerHosting.Api/appsettings.json` - Added email settings
- `src/GameServerHosting.Api/appsettings.Development.json` - Added development email settings
- `src/GameServerHosting.Api/appsettings.Production.json` - Added production email settings

## Next Steps

1. **Frontend Integration:**
   - Create email verification page
   - Add resend verification button
   - Show verification status messages

2. **Email Templates:**
   - Customize templates with your branding
   - Add more email types (welcome, notifications, etc.)

3. **Monitoring:**
   - Set up email delivery monitoring
   - Track verification rates
   - Monitor bounce rates

4. **Rate Limiting:**
   - Implement rate limiting on email endpoints
   - Prevent abuse

## Support

For issues or questions, please contact the development team or refer to the official documentation.
