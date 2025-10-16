# CI/CD Quick Reference

## Container Security Scan - Quick Guide

### 🚀 Performance Expectations

| Scenario | Expected Time | Cache Status |
|----------|--------------|--------------|
| **First Run** | 5-8 minutes | Building caches |
| **Subsequent Runs** | 1-3 minutes | Using caches |
| **Cache Expired** | 3-5 minutes | Rebuilding caches |

### 🔍 Understanding the Scan

#### What Gets Scanned?
- ✅ Application dependencies (NuGet packages)
- ✅ OS-level packages (Alpine Linux, .NET runtime)
- ✅ Known vulnerabilities with available fixes
- ❌ Vulnerabilities without fixes (ignored)

#### Severity Levels
- **CRITICAL**: Must fix before merge
- **HIGH**: Must fix before merge
- **MEDIUM**: Address in follow-up
- **LOW**: Monitor and address periodically

### 📊 Reading the Results

#### In GitHub Actions Summary
```
## Container Security Scan - API 🐳

### API Image Vulnerabilities
- Critical: 0
- High: 0

✅ No Critical or High severity vulnerabilities in API image
```

#### In Artifacts
- Download `trivy-scan-api` for detailed JSON report
- Download `trivy-scan-client` for client results

### 🔧 Common Tasks

#### Temporarily Ignore a CVE
1. Add to `.trivyignore`:
   ```
   CVE-2024-12345  # Description of why it's ignored
   ```
2. Document the reason
3. Set reminder to re-evaluate

#### Update Base Images
```bash
# For .NET runtime
# Edit Dockerfiles to use newer base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

# For nginx
FROM nginx:1.25-alpine
```

#### Clear Caches (if needed)
1. Go to Actions → Caches in GitHub
2. Delete caches starting with `buildx-` or `trivy-db-`
3. Next run will rebuild fresh

#### View Historical Trends
1. Go to Security tab in GitHub
2. Click "Dependabot" or "Code scanning"
3. View vulnerability trends over time

### 🚨 When Scans Fail

#### Step 1: Identify the Issue
```bash
# Check the scan results in artifacts
# Look for CRITICAL or HIGH severity CVEs
```

#### Step 2: Assess the CVE
- Is it in application code or base image?
- Is a fix available?
- What's the actual risk?

#### Step 3: Take Action
| Scenario | Action |
|----------|--------|
| **Fix Available** | Update dependency/base image |
| **No Fix Yet** | Document in `.trivyignore` with approval |
| **False Positive** | Add to `.trivyignore` with evidence |
| **Base Image Issue** | Wait for upstream fix, document |

#### Step 4: Re-run
```bash
git commit -m "fix: update dependencies for CVE-XXX"
git push
```

### 💡 Tips for Developers

#### Faster Local Testing
```bash
# Test Dockerfile changes locally with cache
docker build -f src/GameServerHosting.Api/Dockerfile -t test-api .

# Run Trivy locally
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image test-api
```

#### Before Pushing
- ✅ Check for NuGet package updates
- ✅ Review known CVEs in `.trivyignore`
- ✅ Test Docker builds locally
- ✅ Ensure base images are up-to-date

#### Monitoring
- Check CI times weekly
- Review CVE trends monthly
- Update base images quarterly

### 📚 Additional Resources

- [Full Optimization Docs](./CI_OPTIMIZATION_NOTES.md)
- [Trivy Documentation](https://aquasecurity.github.io/trivy/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [GitHub Actions Caching](https://docs.github.com/en/actions/using-workflows/caching-dependencies-to-speed-up-workflows)

### 🆘 Getting Help

If scans are consistently failing or taking too long:
1. Check cache hit rates in Action logs
2. Review recent base image changes
3. Verify network connectivity issues
4. Contact DevOps team

---

**Last Updated**: 2025-10-16  
**Maintained By**: DevOps Team

