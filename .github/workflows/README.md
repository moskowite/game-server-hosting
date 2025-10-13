# GitHub Actions Workflows

This directory contains the CI/CD pipeline configuration for the Game Server Hosting platform.

## Workflows Overview

### 🔨 CI Pipeline (`ci.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

**Jobs:**

1. **Build and Test**
   - Restores NuGet dependencies with caching
   - Builds the solution in Release configuration
   - Runs all tests with detailed reporting
   - Publishes build artifacts for API and Client

2. **Code Quality Analysis**
   - Runs `dotnet format` to check code formatting
   - Builds with strict compiler warnings
   - Generates code quality reports

3. **Security Scan**
   - Scans for vulnerable NuGet packages
   - Checks both direct and transitive dependencies
   - Uploads security scan results as artifacts

4. **Publish Packages** (main branch only)
   - Creates production-ready publish outputs
   - Uploads artifacts for deployment
   - Retention: 30 days

### 🚀 CD Pipeline (`cd.yml`)

**Triggers:**
- Manual workflow dispatch with environment selection
- GitHub releases (published)

**Environments:**
- Staging
- Production

**Jobs:**

1. **Deploy API**
   - Publishes the ASP.NET Core API
   - Uploads deployment package
   - Placeholder for cloud deployment (Azure/AWS)

2. **Deploy Client**
   - Publishes the Blazor WebAssembly client
   - Uploads static files
   - Placeholder for static hosting deployment

3. **Smoke Tests**
   - Runs post-deployment health checks
   - Validates API and Client availability

### ✅ PR Checks (`pr-checks.yml`)

**Triggers:**
- Pull request opened, synchronized, or reopened

**Jobs:**

1. **PR Validation**
   - Fast Debug build
   - Build warning analysis
   - PR size analysis (warns on large PRs > 1000 lines)
   - PR title format validation (conventional commits or Linear issue ID)

2. **Label Validation**
   - Checks for PR labels
   - Warns if no labels are present

3. **Merge Conflict Check**
   - Detects merge conflicts before review

### 🔄 Dependabot (`dependabot.yml`)

**Automated dependency updates:**
- **NuGet packages**: Weekly on Mondays at 9:00 AM
- **GitHub Actions**: Weekly on Mondays at 9:00 AM
- Automatic PR creation with proper labeling
- Limits: 10 NuGet PRs, 5 Actions PRs

## Workflow Status Badges

Add these badges to your main README.md:

```markdown
[![CI Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml)
[![CD Pipeline](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/cd.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/cd.yml)
```

## Configuring Deployments

### Azure Deployment

1. **Azure App Service (API)**
   ```yaml
   - name: Deploy to Azure App Service
     uses: azure/webapps-deploy@v2
     with:
       app-name: ${{ secrets.AZURE_API_APP_NAME }}
       publish-profile: ${{ secrets.AZURE_API_PUBLISH_PROFILE }}
       package: ./publish/api
   ```

2. **Azure Static Web Apps (Client)**
   ```yaml
   - name: Deploy to Azure Static Web Apps
     uses: Azure/static-web-apps-deploy@v1
     with:
       azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
       repo_token: ${{ secrets.GITHUB_TOKEN }}
       action: "upload"
       app_location: "./publish/client/wwwroot"
   ```

### AWS Deployment

1. **AWS Elastic Beanstalk (API)**
   ```yaml
   - name: Deploy to AWS Elastic Beanstalk
     uses: einaregilsson/beanstalk-deploy@v21
     with:
       aws_access_key: ${{ secrets.AWS_ACCESS_KEY_ID }}
       aws_secret_key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
       application_name: game-server-api
       environment_name: ${{ github.event.inputs.environment }}
       version_label: ${{ github.run_number }}
       region: us-east-1
       deployment_package: ./publish/api
   ```

2. **AWS S3 + CloudFront (Client)**
   ```yaml
   - name: Deploy to AWS S3 and CloudFront
     run: |
       aws s3 sync ./publish/client/wwwroot s3://${{ secrets.AWS_S3_BUCKET }} --delete
       aws cloudfront create-invalidation --distribution-id ${{ secrets.AWS_CLOUDFRONT_DISTRIBUTION_ID }} --paths "/*"
     env:
       AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
       AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
       AWS_REGION: us-east-1
   ```

## Required Secrets

Configure these secrets in your GitHub repository settings:

### For Azure:
- `AZURE_API_APP_NAME` - Name of your Azure App Service for API
- `AZURE_API_PUBLISH_PROFILE` - Publish profile for API deployment
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - Token for Static Web Apps

### For AWS:
- `AWS_ACCESS_KEY_ID` - AWS access key
- `AWS_SECRET_ACCESS_KEY` - AWS secret key
- `AWS_S3_BUCKET` - S3 bucket name for client hosting
- `AWS_CLOUDFRONT_DISTRIBUTION_ID` - CloudFront distribution ID

## Environment Protection Rules

It's recommended to configure environment protection rules:

1. Go to **Settings → Environments**
2. Create environments: `staging` and `production`
3. For `production`, configure:
   - Required reviewers (at least 1)
   - Wait timer (optional, e.g., 5 minutes)
   - Deployment branches (only `main`)

## Running Workflows Manually

### CI Pipeline
```bash
# Via GitHub CLI
gh workflow run ci.yml

# Or use the GitHub UI: Actions → CI Pipeline → Run workflow
```

### CD Pipeline
```bash
# Deploy to staging
gh workflow run cd.yml -f environment=staging

# Deploy to production
gh workflow run cd.yml -f environment=production
```

## Monitoring and Troubleshooting

### View Workflow Runs
```bash
# List recent workflow runs
gh run list

# View specific run
gh run view RUN_ID

# Watch a run in progress
gh run watch
```

### Download Artifacts
```bash
# List artifacts from a run
gh run view RUN_ID --log

# Download artifacts
gh run download RUN_ID
```

### Common Issues

1. **Build Failures**: Check NuGet package restore and .NET SDK version
2. **Test Failures**: Review test results artifact for details
3. **Deployment Failures**: Verify secrets and environment configuration
4. **Permission Errors**: Ensure GitHub Actions has necessary permissions

## Performance Optimization

The workflows include several optimizations:

- **NuGet Package Caching**: Speeds up dependency restoration
- **Incremental Builds**: Uses `--no-restore` and `--no-build` flags
- **Parallel Jobs**: Code quality and security scans run independently
- **Artifact Retention**: Balanced retention periods (7-90 days)

## Future Enhancements

Consider adding:

- [ ] Integration tests with database
- [ ] E2E tests with Playwright
- [ ] Performance benchmarking
- [ ] Code coverage reporting with Codecov
- [ ] Container image building and publishing
- [ ] Infrastructure as Code (Terraform/Bicep) validation
- [ ] Automated release notes generation

## Contributing

When modifying workflows:

1. Test changes in a feature branch first
2. Use `act` for local workflow testing: https://github.com/nektos/act
3. Keep workflows DRY using reusable workflows or composite actions
4. Document any new secrets or configuration requirements

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET Actions](https://github.com/actions/setup-dotnet)
- [Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)
- [Security Hardening](https://docs.github.com/en/actions/security-guides/security-hardening-for-github-actions)
