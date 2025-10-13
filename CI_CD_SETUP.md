# CI/CD Pipeline Setup Guide

## Overview

This document provides comprehensive information about the CI/CD pipeline setup for the Game Server Hosting platform using GitHub Actions.

## Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         GitHub Actions                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│  │  CI Pipeline │    │  PR Checks   │    │ CD Pipeline  │     │
│  │   (ci.yml)   │    │(pr-checks.yml)│   │   (cd.yml)   │     │
│  └──────────────┘    └──────────────┘    └──────────────┘     │
│         │                    │                    │             │
│         ▼                    ▼                    ▼             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐     │
│  │Build & Test  │    │  Validation  │    │   Deploy     │     │
│  │Code Quality  │    │  PR Analysis │    │  Staging/    │     │
│  │Security Scan │    │  Conflict Chk│    │  Production  │     │
│  └──────────────┘    └──────────────┘    └──────────────┘     │
│         │                                         │             │
│         ▼                                         ▼             │
│  ┌──────────────┐                        ┌──────────────┐     │
│  │  Artifacts   │                        │Smoke Tests   │     │
│  └──────────────┘                        └──────────────┘     │
└─────────────────────────────────────────────────────────────────┘
```

## Workflows

### 1. CI Pipeline (`ci.yml`)

**Purpose**: Continuous Integration for all code changes

**Trigger Events**:
- Push to `main` or `develop` branches
- Pull requests targeting `main` or `develop`
- Manual trigger via workflow_dispatch

**Jobs and Steps**:

#### Job 1: Build and Test
```yaml
- Checkout code
- Setup .NET 8.0
- Cache NuGet packages
- Restore dependencies
- Build solution (Release)
- Run tests with trx logger
- Publish test results
- Upload API artifacts
- Upload Client artifacts
```

**Optimizations**:
- NuGet package caching reduces restore time by ~80%
- Build artifacts retained for 7 days
- Test results published even on failure

#### Job 2: Code Quality Analysis
```yaml
- Checkout code
- Setup .NET 8.0
- Restore dependencies
- Run dotnet format verification
- Build with warnings analysis
```

**Quality Checks**:
- Code formatting consistency
- Compiler warnings detection
- Style guide adherence

#### Job 3: Security Scan
```yaml
- Checkout code
- Setup .NET 8.0
- Restore dependencies
- Scan for vulnerable packages
- Upload security report
```

**Security Coverage**:
- Direct dependencies
- Transitive dependencies
- Known CVE detection
- Report retention: 30 days

#### Job 4: Publish Packages (main branch only)
```yaml
- Checkout code
- Setup .NET 8.0
- Restore dependencies
- Publish API
- Publish Blazor Client
- Upload publish artifacts (30 days retention)
```

### 2. CD Pipeline (`cd.yml`)

**Purpose**: Continuous Deployment to staging and production

**Trigger Events**:
- Manual workflow dispatch with environment selection
- GitHub release published

**Environments**:
- `staging` - For QA and testing
- `production` - For live users

**Jobs and Steps**:

#### Job 1: Deploy API
```yaml
- Checkout code
- Setup .NET 8.0
- Restore dependencies
- Build solution
- Publish API
- Upload deployment package
- Deploy to cloud (placeholder)
- Generate deployment summary
```

#### Job 2: Deploy Client
```yaml
- Checkout code
- Setup .NET 8.0
- Restore dependencies
- Build solution
- Publish Blazor Client
- Upload wwwroot artifacts
- Deploy to static hosting (placeholder)
- Generate deployment summary
```

#### Job 3: Smoke Tests
```yaml
- Run API health checks
- Verify client availability
- Generate test summary
```

**Deployment Safety**:
- Environment protection rules
- Manual approval gates (recommended for production)
- Smoke tests post-deployment
- Rollback documentation

### 3. PR Checks (`pr-checks.yml`)

**Purpose**: Fast feedback on pull requests

**Trigger Events**:
- PR opened, synchronized, or reopened

**Jobs and Steps**:

#### Job 1: PR Validation
```yaml
- Fast Debug build
- Build warnings analysis
- PR size check (warns if >1000 lines)
- PR title format validation
```

**Validation Rules**:
- Conventional commits format (feat:, fix:, etc.)
- Linear issue ID format [MOS-X]
- Size recommendations for reviewability

#### Job 2: Label Validation
```yaml
- Check for PR labels
- Warn if unlabeled
```

#### Job 3: Merge Conflict Check
```yaml
- Detect merge conflicts
- Fail early if conflicts exist
```

### 4. Dependabot (`dependabot.yml`)

**Purpose**: Automated dependency updates

**Update Schedule**:
- **NuGet packages**: Weekly, Mondays at 9:00 AM
- **GitHub Actions**: Weekly, Mondays at 9:00 AM

**Configuration**:
- Max 10 open NuGet PRs
- Max 5 open Actions PRs
- Auto-labeling
- Conventional commit messages

## Environment Setup

### GitHub Repository Settings

#### 1. Environment Configuration

Navigate to **Settings → Environments** and create:

**Staging Environment**:
```yaml
Protection rules:
  - Required reviewers: None (or 1 for safety)
  - Wait timer: None
  - Deployment branches: Any branch
```

**Production Environment**:
```yaml
Protection rules:
  - Required reviewers: 1-2 team members
  - Wait timer: 5 minutes (optional)
  - Deployment branches: main only
```

#### 2. Secrets Configuration

Add these secrets in **Settings → Secrets and variables → Actions**:

**For Azure Deployment**:
```
AZURE_API_APP_NAME=your-api-app-name
AZURE_API_PUBLISH_PROFILE=<download from Azure Portal>
AZURE_STATIC_WEB_APPS_API_TOKEN=<from Azure Static Web Apps>
```

**For AWS Deployment**:
```
AWS_ACCESS_KEY_ID=<your-access-key>
AWS_SECRET_ACCESS_KEY=<your-secret-key>
AWS_S3_BUCKET=your-bucket-name
AWS_CLOUDFRONT_DISTRIBUTION_ID=<your-distribution-id>
```

#### 3. Branch Protection Rules

Configure for `main` branch in **Settings → Branches**:

```yaml
Protection rules:
  ✓ Require a pull request before merging
    - Require 1 approval
    - Dismiss stale reviews
  ✓ Require status checks to pass
    - build-and-test
    - code-quality
    - pr-validation
  ✓ Require conversation resolution
  ✓ Do not allow bypassing the above settings
```

## Deployment Instructions

### Manual Deployment

#### Using GitHub CLI:
```bash
# Deploy to staging
gh workflow run cd.yml -f environment=staging

# Deploy to production
gh workflow run cd.yml -f environment=production

# Check deployment status
gh run list --workflow=cd.yml
```

#### Using GitHub UI:
1. Go to **Actions** tab
2. Select **CD Pipeline**
3. Click **Run workflow**
4. Select environment (staging/production)
5. Click **Run workflow**

### Automated Deployment

Deployments trigger automatically on:
- Release published (staging)
- After manual approval (production)

## Monitoring and Observability

### Workflow Monitoring

```bash
# List recent runs
gh run list

# View specific run details
gh run view <run-id>

# Watch run in real-time
gh run watch

# View logs
gh run view <run-id> --log
```

### Artifact Management

```bash
# List artifacts
gh api repos/:owner/:repo/actions/artifacts

# Download artifacts
gh run download <run-id>

# Download specific artifact
gh run download <run-id> -n artifact-name
```

### Notification Setup

Configure notifications in **Settings → Notifications**:
- Workflow failures
- Deployment approvals
- Security alerts

Consider integrating:
- Slack notifications via webhooks
- Email alerts for production deployments
- Status badges on README

## Deployment Providers

### Azure Configuration

#### App Service (API)

1. Create App Service in Azure Portal
2. Download publish profile
3. Add to GitHub Secrets as `AZURE_API_PUBLISH_PROFILE`
4. Uncomment Azure deployment step in `cd.yml`

```yaml
- name: Deploy to Azure App Service
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ secrets.AZURE_API_APP_NAME }}
    publish-profile: ${{ secrets.AZURE_API_PUBLISH_PROFILE }}
    package: ./publish/api
```

#### Static Web Apps (Client)

1. Create Static Web App in Azure Portal
2. Copy deployment token
3. Add to GitHub Secrets as `AZURE_STATIC_WEB_APPS_API_TOKEN`
4. Uncomment deployment step in `cd.yml`

### AWS Configuration

#### Elastic Beanstalk (API)

1. Create Elastic Beanstalk application
2. Configure IAM credentials
3. Add AWS credentials to GitHub Secrets
4. Uncomment AWS deployment step in `cd.yml`

#### S3 + CloudFront (Client)

1. Create S3 bucket with static website hosting
2. Create CloudFront distribution
3. Configure IAM credentials
4. Add configuration to GitHub Secrets

## Performance Benchmarks

Expected workflow execution times:

| Workflow | Average Duration | Cached Duration |
|----------|-----------------|-----------------|
| CI Pipeline | 3-5 minutes | 2-3 minutes |
| PR Checks | 1-2 minutes | 45-90 seconds |
| CD Pipeline | 4-6 minutes | 3-4 minutes |

**Optimization Tips**:
- NuGet cache reduces restore time by 80%
- Parallel jobs save 2-3 minutes
- Build artifact reuse speeds up deployment

## Troubleshooting

### Common Issues

#### 1. Build Failures

**Symptom**: Build fails with restore errors

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Update to latest packages
dotnet restore --force
```

#### 2. Test Failures

**Symptom**: Tests pass locally but fail in CI

**Solution**:
- Check environment variables
- Verify database connection strings
- Review test result artifacts
- Check for timing-dependent tests

#### 3. Deployment Failures

**Symptom**: Deployment step fails

**Solution**:
- Verify secrets are configured
- Check environment protection rules
- Review cloud provider logs
- Validate publish profile

#### 4. Permission Issues

**Symptom**: "Resource not accessible by integration"

**Solution**:
1. Go to **Settings → Actions → General**
2. Under "Workflow permissions"
3. Select "Read and write permissions"
4. Save changes

### Getting Help

- Check [workflow logs](#workflow-monitoring)
- Review [GitHub Actions status](https://www.githubstatus.com)
- Consult [.NET Actions docs](https://github.com/actions/setup-dotnet)
- Open issue in repository

## Best Practices

### Workflow Hygiene

✅ **Do**:
- Keep workflows DRY with reusable workflows
- Use semantic versioning for actions
- Cache dependencies aggressively
- Set appropriate artifact retention
- Use environment secrets, not repository secrets

❌ **Don't**:
- Hardcode secrets in workflows
- Use `latest` tags for actions
- Run workflows on every commit to feature branches
- Ignore security scan warnings
- Skip environment protection on production

### Git Workflow

Recommended branching strategy:

```
main (protected)
  ├── develop (protected)
  │    ├── feature/MOS-123-new-feature
  │    ├── fix/MOS-124-bug-fix
  │    └── chore/MOS-125-dependency-update
  └── hotfix/MOS-126-critical-fix
```

**Workflow**:
1. Create feature branch from `develop`
2. Open PR → PR checks run
3. Merge to `develop` → CI runs
4. Merge `develop` to `main` → CI + publish runs
5. Manual deploy to staging → CD runs
6. Manual deploy to production → CD runs with approval

## Security Considerations

### Secrets Management

- Never commit secrets to repository
- Use environment-specific secrets
- Rotate secrets regularly (quarterly)
- Audit secret access logs

### Dependency Security

- Review Dependabot PRs weekly
- Update vulnerable packages immediately
- Pin action versions to commits or tags
- Enable dependency vulnerability alerts

### Access Control

- Limit repository write access
- Require PR reviews for main branch
- Use environment protection rules
- Enable audit logging

## Future Enhancements

### Planned Improvements

- [ ] Integration tests with test database
- [ ] E2E tests using Playwright
- [ ] Code coverage reporting (Codecov/Coveralls)
- [ ] Performance benchmarking
- [ ] Docker container builds
- [ ] Kubernetes deployment
- [ ] Infrastructure as Code (Terraform)
- [ ] Automated changelog generation
- [ ] Release automation

### Advanced Features

- [ ] Matrix builds for multiple .NET versions
- [ ] Parallel test execution
- [ ] Visual regression testing
- [ ] Load testing in staging
- [ ] Blue-green deployments
- [ ] Canary releases
- [ ] Automatic rollback on errors

## References

### Documentation
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [.NET CLI Reference](https://docs.microsoft.com/en-us/dotnet/core/tools/)
- [Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)

### Tools
- [act - Local GitHub Actions](https://github.com/nektos/act)
- [actionlint - Workflow Linter](https://github.com/rhysd/actionlint)
- [GitHub CLI](https://cli.github.com/)

### Related Documentation
- [Workflows README](.github/workflows/README.md)
- [Technical Architecture](TECHNICAL_ARCHITECTURE.md)
- [Quick Start Guide](QUICK_START_GUIDE.md)

---

**Last Updated**: 2025-10-13  
**Maintained By**: DevOps Team  
**Related Linear Issue**: [MOS-8](https://linear.app/game-server-hosting/issue/MOS-8)
