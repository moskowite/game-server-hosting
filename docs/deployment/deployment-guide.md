# Email Verification - Deployment Checklist

Use this checklist when deploying the email verification feature to different environments.

## ✅ Development Environment

### Prerequisites
- [ ] .NET 8.0 SDK installed
- [ ] PostgreSQL database running
- [ ] MailHog installed and running

### Configuration
- [ ] `appsettings.Development.json` has email settings configured
- [ ] Provider set to "SMTP"
- [ ] SMTP host set to "localhost:1025"
- [ ] BaseUrl set to local development URL

### Testing
- [ ] MailHog accessible at http://localhost:8025
- [ ] Can register new user
- [ ] Verification email received in MailHog
- [ ] Email verification works with token
- [ ] Login blocked before verification
- [ ] Login succeeds after verification
- [ ] Resend verification email works

### Commands
```bash
# Development Quick Start
mailhog &                           # Start MailHog
cd src/GameServerHosting.Api
dotnet restore
dotnet run                          # Start application
```

---

## 🟡 Staging Environment

### Prerequisites
- [ ] Staging server configured
- [ ] PostgreSQL staging database configured
- [ ] SendGrid account created (can use same as production)

### Configuration Files
- [ ] `appsettings.Staging.json` updated with staging settings
- [ ] Provider set to "SendGrid"
- [ ] SendGrid API key configured (use environment variable)
- [ ] FromEmail set to staging email address
- [ ] BaseUrl set to staging domain

### Environment Variables
```bash
# Set these on staging server
export EmailSettings__Provider="SendGrid"
export EmailSettings__SendGrid__ApiKey="your_staging_sendgrid_api_key"
export EmailSettings__FromEmail="noreply@staging.yourdomain.com"
export EmailSettings__BaseUrl="https://staging.yourdomain.com"
```

### SendGrid Setup
- [ ] SendGrid account created
- [ ] API key created with "Mail Send" permission
- [ ] Staging sender verified in SendGrid
- [ ] Test email sent successfully

### Testing
- [ ] Can register user with real email
- [ ] Verification email received
- [ ] Email not in spam folder
- [ ] Verification link works
- [ ] Login flow works end-to-end

### Monitoring
- [ ] Application logs configured
- [ ] SendGrid dashboard accessible
- [ ] Email delivery monitoring set up

---

## 🔴 Production Environment

### Prerequisites
- [ ] Production server configured
- [ ] Production PostgreSQL database configured
- [ ] Production domain configured
- [ ] SSL/TLS certificate installed
- [ ] SendGrid production account ready

### SendGrid Production Setup
- [ ] Production SendGrid account created
- [ ] Production API key created (separate from staging)
- [ ] Domain authentication configured (SPF/DKIM)
- [ ] Sender address verified
- [ ] Production email templates reviewed
- [ ] Unsubscribe handling configured (if needed)

### Configuration Files
- [ ] `appsettings.Production.json` reviewed
- [ ] Provider set to "SendGrid"
- [ ] BaseUrl set to production domain (HTTPS)
- [ ] All sensitive data moved to environment variables

### Environment Variables
```bash
# Set these on production server (via secure secrets management)
export EmailSettings__Provider="SendGrid"
export EmailSettings__SendGrid__ApiKey="your_production_sendgrid_api_key"
export EmailSettings__FromEmail="noreply@yourdomain.com"
export EmailSettings__BaseUrl="https://yourdomain.com"
export EmailSettings__FromName="Your Product Name"
export JwtSettings__SecretKey="your_secure_production_jwt_secret"
```

### DNS Configuration
- [ ] SPF record added to DNS
- [ ] DKIM records added to DNS (from SendGrid)
- [ ] DMARC policy configured
- [ ] MX records configured (if receiving email)

### Security
- [ ] HTTPS enforced on all endpoints
- [ ] Environment variables secured (not in code)
- [ ] Secrets management solution in place
- [ ] Rate limiting configured on email endpoints
- [ ] CORS configured properly
- [ ] Firewall rules configured

### Testing (with real email)
- [ ] Test with multiple email providers (Gmail, Outlook, etc.)
- [ ] Verify emails not going to spam
- [ ] Test verification link from email
- [ ] Test resend verification
- [ ] Test forgot password flow
- [ ] Load test email sending

### Monitoring & Alerts
- [ ] Application logging configured
- [ ] SendGrid activity monitoring enabled
- [ ] Email delivery rate monitoring
- [ ] Bounce rate alerts configured
- [ ] Spam complaint alerts configured
- [ ] Error rate alerts configured

### Backup Plan
- [ ] Backup SMTP provider configured (optional)
- [ ] Email delivery fallback strategy
- [ ] Incident response plan documented

---

## 📊 Post-Deployment Validation

### Functional Tests
- [ ] User registration sends email
- [ ] Email received within 1 minute
- [ ] Email renders correctly in major email clients
- [ ] Verification link works on first click
- [ ] Expired token shows appropriate error
- [ ] Resend verification generates new token
- [ ] Login blocked for unverified users
- [ ] Login successful after verification

### Performance Tests
- [ ] Email sending doesn't block API response
- [ ] Multiple concurrent registrations work
- [ ] Email delivery under load

### Email Deliverability
- [ ] Test with Gmail (check spam folder)
- [ ] Test with Outlook/Hotmail
- [ ] Test with corporate email servers
- [ ] Check email authentication headers
- [ ] Verify SPF/DKIM/DMARC pass

### User Experience
- [ ] Email subject line clear and appropriate
- [ ] Email body well-formatted
- [ ] CTA button prominent and clickable
- [ ] Email readable on mobile devices
- [ ] Email readable with images disabled
- [ ] Plain text version acceptable

---

## 🔧 Configuration Validation

### Development
```json
{
  "EmailSettings": {
    "Provider": "SMTP",
    "FromEmail": "dev@localhost",
    "FromName": "App (Dev)",
    "Smtp": {
      "Host": "localhost",
      "Port": 1025,
      "EnableSsl": false
    },
    "BaseUrl": "http://localhost:5000"
  }
}
```

### Staging
```json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "noreply@staging.yourdomain.com",
    "FromName": "Your App (Staging)",
    "SendGrid": {
      "ApiKey": "USE_ENVIRONMENT_VARIABLE"
    },
    "BaseUrl": "https://staging.yourdomain.com"
  }
}
```

### Production
```json
{
  "EmailSettings": {
    "Provider": "SendGrid",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Your App",
    "SendGrid": {
      "ApiKey": "USE_ENVIRONMENT_VARIABLE"
    },
    "VerificationTokenExpirationHours": 24,
    "BaseUrl": "https://yourdomain.com"
  }
}
```

---

## 📝 Rollback Plan

If issues occur in production:

### Immediate Actions
1. Check application logs for error details
2. Check SendGrid dashboard for delivery issues
3. Verify environment variables are set correctly

### Rollback Options

**Option 1: Disable Email Verification Requirement**
```csharp
// In Program.cs, temporarily change:
options.SignIn.RequireConfirmedEmail = false;
```

**Option 2: Fallback to SMTP**
```bash
# Set environment variables to use SMTP instead
export EmailSettings__Provider="SMTP"
export EmailSettings__Smtp__Host="your-smtp-server"
# ... other SMTP settings
```

**Option 3: Full Rollback**
- Deploy previous version without email verification
- Update database schema if needed
- Notify users of the change

---

## 📞 Support Contacts

### Internal Team
- Backend Developer: [Your contact]
- DevOps Engineer: [Your contact]
- Product Owner: [Your contact]

### External Services
- SendGrid Support: https://support.sendgrid.com
- SendGrid Status: https://status.sendgrid.com

---

## 📚 Reference Documentation

- [Email Verification Setup Guide](./EMAIL_VERIFICATION_SETUP.md)
- [Quick Start Guide](./EMAIL_VERIFICATION_QUICK_START.md)
- [Implementation Summary](./EMAIL_VERIFICATION_IMPLEMENTATION_SUMMARY.md)
- [SendGrid Documentation](https://docs.sendgrid.com/)
- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)

---

## ✅ Sign-off

### Development
- [ ] Developed by: _________________ Date: _______
- [ ] Code reviewed by: _____________ Date: _______
- [ ] Tested by: ____________________ Date: _______

### Staging
- [ ] Deployed by: __________________ Date: _______
- [ ] Tested by: ____________________ Date: _______
- [ ] Product Owner approval: _______ Date: _______

### Production
- [ ] Deployed by: __________________ Date: _______
- [ ] Validated by: _________________ Date: _______
- [ ] Product Owner approval: _______ Date: _______
- [ ] Post-deployment monitoring: ___ Date: _______

---

**Notes:**
_Add any environment-specific notes, issues encountered, or deviations from the plan here._

---

**Last Updated:** 2025-10-14
