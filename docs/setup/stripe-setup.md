# Stripe Setup Guide

This guide will walk you through setting up Stripe for the Game Server Hosting application.

## Table of Contents
- [Creating a Stripe Account](#creating-a-stripe-account)
- [Getting API Keys](#getting-api-keys)
- [Configuring the Application](#configuring-the-application)
- [Testing Payments](#testing-payments)
- [Setting Up Webhooks](#setting-up-webhooks)
- [Going to Production](#going-to-production)

## Creating a Stripe Account

1. **Sign up for Stripe**
   - Go to [https://stripe.com](https://stripe.com)
   - Click "Start now" or "Sign up"
   - Fill in your email, name, and password
   - Verify your email address

2. **Complete your account setup**
   - Provide business information
   - Add banking details (required for live payments)
   - Complete identity verification

## Getting API Keys

### Test Mode Keys (Development)

1. **Navigate to the Dashboard**
   - Log in to your Stripe account
   - Click on "Developers" in the top navigation
   - Select "API keys" from the sidebar

2. **Locate Test Keys**
   - Ensure you're in "Test mode" (toggle in the top right)
   - You'll see two keys:
     - **Publishable key** (starts with `pk_test_`)
     - **Secret key** (starts with `sk_test_`)

3. **Copy the Keys**
   - Click "Reveal test key" for the secret key
   - Copy both keys for use in your application

### Production Keys (Live Mode)

1. **Switch to Live Mode**
   - Toggle the "Test mode" switch to OFF in the Stripe Dashboard
   - You may need to complete account activation first

2. **Locate Live Keys**
   - Go to "Developers" → "API keys"
   - You'll see:
     - **Publishable key** (starts with `pk_live_`)
     - **Secret key** (starts with `sk_live_`)

3. **Copy the Keys**
   - Click "Reveal live key" for the secret key
   - **IMPORTANT**: Keep these keys secure and never commit them to version control

## Configuring the Application

### Development Environment

1. **Update `appsettings.Development.json`**
   ```json
   {
     "Stripe": {
       "SecretKey": "sk_test_YOUR_TEST_SECRET_KEY",
       "PublishableKey": "pk_test_YOUR_TEST_PUBLISHABLE_KEY",
       "WebhookSecret": "whsec_YOUR_TEST_WEBHOOK_SECRET",
       "Currency": "usd",
       "SuccessUrl": "http://localhost:5000/checkout/success",
       "CancelUrl": "http://localhost:5000/checkout/cancel"
     }
   }
   ```

2. **Replace the placeholder values** with your actual test keys from Stripe

### Production Environment

1. **Use Environment Variables**
   - **NEVER** commit production keys to version control
   - Set environment variables on your hosting platform:
     ```bash
     export Stripe__SecretKey="sk_live_YOUR_LIVE_SECRET_KEY"
     export Stripe__PublishableKey="pk_live_YOUR_LIVE_PUBLISHABLE_KEY"
     export Stripe__WebhookSecret="whsec_YOUR_LIVE_WEBHOOK_SECRET"
     ```

2. **Alternative: Update `appsettings.Production.json`**
   - The file already has placeholders
   - These will be overridden by environment variables in production
   ```json
   {
     "Stripe": {
       "SecretKey": "REPLACE_WITH_STRIPE_PRODUCTION_SECRET_KEY_FROM_ENVIRONMENT_VARIABLES",
       "PublishableKey": "REPLACE_WITH_STRIPE_PRODUCTION_PUBLISHABLE_KEY_FROM_ENVIRONMENT_VARIABLES",
       "WebhookSecret": "REPLACE_WITH_STRIPE_PRODUCTION_WEBHOOK_SECRET_FROM_ENVIRONMENT_VARIABLES",
       "Currency": "usd",
       "SuccessUrl": "https://gameserverhosting.com/checkout/success",
       "CancelUrl": "https://gameserverhosting.com/checkout/cancel"
     }
   }
   ```

## Testing Payments

Stripe provides test card numbers for testing payment flows:

### Successful Payment
- **Card Number**: `4242 4242 4242 4242`
- **Expiry**: Any future date (e.g., `12/34`)
- **CVC**: Any 3 digits (e.g., `123`)
- **ZIP**: Any 5 digits (e.g., `12345`)

### Payment Requiring Authentication (3D Secure)
- **Card Number**: `4000 0027 6000 3184`

### Declined Payment
- **Card Number**: `4000 0000 0000 0002`

### Insufficient Funds
- **Card Number**: `4000 0000 0000 9995`

For more test cards, visit: [https://stripe.com/docs/testing](https://stripe.com/docs/testing)

## Setting Up Webhooks

Webhooks allow Stripe to notify your application about payment events.

### Development (Using Stripe CLI)

1. **Install Stripe CLI**
   ```bash
   # macOS
   brew install stripe/stripe-cli/stripe
   
   # Windows
   scoop install stripe
   
   # Linux
   # Download from https://github.com/stripe/stripe-cli/releases
   ```

2. **Login to Stripe**
   ```bash
   stripe login
   ```

3. **Forward webhooks to your local server**
   ```bash
   stripe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

4. **Copy the webhook signing secret** (starts with `whsec_`) and update `appsettings.Development.json`

### Production

1. **Navigate to Webhooks**
   - Go to Stripe Dashboard → "Developers" → "Webhooks"
   - Click "Add endpoint"

2. **Configure the endpoint**
   - **Endpoint URL**: `https://your-domain.com/api/webhooks/stripe`
   - **Events to listen to**:
     - `checkout.session.completed`
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`
     - `customer.created`
     - `customer.updated`

3. **Get the signing secret**
   - After creating the endpoint, click to reveal the "Signing secret"
   - Add this to your production environment variables as `Stripe__WebhookSecret`

## Going to Production

### Pre-Production Checklist

- [ ] Business information completed in Stripe Dashboard
- [ ] Banking details added and verified
- [ ] Identity verification completed
- [ ] Production API keys obtained
- [ ] Webhook endpoint configured and tested
- [ ] Environment variables set securely
- [ ] SSL/TLS certificate installed (HTTPS required)
- [ ] Test all payment flows in test mode
- [ ] Review Stripe's compliance requirements

### Switching to Live Mode

1. **Activate your Stripe account** (if not already done)
   - Complete all required information
   - Wait for approval (usually instant for most countries)

2. **Update configuration**
   - Set production environment variables
   - Ensure `appsettings.Production.json` references environment variables

3. **Deploy and verify**
   - Deploy your application
   - Test with a small live payment
   - Monitor the Stripe Dashboard for events

## Security Best Practices

1. **Never commit API keys** to version control
2. **Use environment variables** for production secrets
3. **Enable HTTPS** for all payment-related endpoints
4. **Validate webhook signatures** (already implemented in `StripeService`)
5. **Keep Stripe.NET package updated** for security patches
6. **Use PCI-compliant hosting** if handling card data directly
7. **Enable Stripe Radar** for fraud detection
8. **Set up monitoring and alerts** in Stripe Dashboard

## Troubleshooting

### Common Issues

1. **Authentication failed**
   - Verify your API key is correct
   - Ensure you're using the right key for the environment (test vs. live)

2. **Webhook signature verification failed**
   - Check that the webhook secret matches
   - Ensure the raw request body is used for signature verification

3. **Payment declined**
   - In test mode, use the test card numbers provided above
   - In live mode, check with the customer or try a different payment method

4. **CORS errors**
   - Ensure your frontend origin is allowed in CORS configuration
   - Check that the Stripe publishable key is being used on the frontend

## Additional Resources

- [Stripe Documentation](https://stripe.com/docs)
- [Stripe.NET Library](https://github.com/stripe/stripe-dotnet)
- [Stripe API Reference](https://stripe.com/docs/api)
- [Stripe Testing Guide](https://stripe.com/docs/testing)
- [Stripe Dashboard](https://dashboard.stripe.com)

## Support

For Stripe-related issues:
- Check the [Stripe Documentation](https://stripe.com/docs)
- Contact [Stripe Support](https://support.stripe.com)

For application-specific issues:
- Check the project's troubleshooting guide
- Contact the development team
