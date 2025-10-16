# CI Migration Test Fix

**Issue**: Database Migration Test job failing in CI  
**Job ID**: 52880153418  
**Date**: 2025-10-16

## Problem Analysis

The Database Migration Test job was failing with the following error:

```
Microsoft.EntityFrameworkCore.Tools.CommandException: Your startup project 'GameServerHosting.Api' doesn't reference Microsoft.EntityFrameworkCore.Design. This package is required for the Entity Framework Core Tools to work. Ensure your startup project is correct, install the package, and try again.

System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.EntityFrameworkCore.Design, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
```

### Root Cause

When running EF Core migrations with `dotnet ef database update`, the command specifies:
- `--project src/GameServerHosting.Infrastructure` (where the DbContext lives)
- `--startup-project src/GameServerHosting.Api` (the application entry point)

The EF Core tools require the **startup project** to have a reference to `Microsoft.EntityFrameworkCore.Design`, but only the Infrastructure project had this package installed.

### Additional Issue

The build logs also showed duplicate package reference warnings:
```
warning NU1504: Duplicate 'PackageReference' items found:
- Microsoft.Extensions.Configuration.Abstractions 8.0.0 (appeared twice)
- Microsoft.Extensions.Logging.Abstractions (versions 8.0.2 and 8.0.0)
```

## Solution

### 1. Add EF Core Design Package to API Project

**File**: `src/GameServerHosting.Api/GameServerHosting.Api.csproj`

Added the required package reference:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**Why `PrivateAssets="all"`?**
- This is a design-time only package needed for migrations
- Prevents the package from being included in the published output
- Follows EF Core best practices

### 2. Remove Duplicate Package References

**File**: `src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj`

Removed duplicate entries:
- `Microsoft.Extensions.Configuration.Abstractions` (line 38 - duplicate)
- `Microsoft.Extensions.Logging.Abstractions` (line 37 - duplicate)

These packages were already referenced on lines 22-23, so the duplicates at the end of the file were unnecessary.

## Changes Made

### Modified Files

1. **src/GameServerHosting.Api/GameServerHosting.Api.csproj**
   - ✅ Added `Microsoft.EntityFrameworkCore.Design` package

2. **src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj**
   - ✅ Removed duplicate `Microsoft.Extensions.Configuration.Abstractions`
   - ✅ Removed duplicate `Microsoft.Extensions.Logging.Abstractions`

## Expected Results

After these changes, the CI pipeline should:

1. ✅ **Build successfully** without duplicate package warnings
2. ✅ **Run `dotnet ef database update`** successfully in the migration test job
3. ✅ **Apply migrations** to the test PostgreSQL database
4. ✅ **Verify database schema** creation
5. ✅ **Test migration rollback** (if applicable)

## Why This Wasn't Caught Earlier

- **Local Development**: Developers likely had the package installed globally or were running migrations differently
- **Manual Testing**: The Makefile and PowerShell scripts work the same way as CI, so this issue would have manifested in local testing too
- **CI Environment**: The clean CI environment exposed this missing dependency

## Prevention

To prevent similar issues:

1. **Test migration scripts locally** before pushing to CI
2. **Run in clean environments** (e.g., Docker containers) to catch missing dependencies
3. **Keep package references organized** in .csproj files
4. **Run `dotnet restore` and `dotnet build`** after modifying project files

## Related Documentation

- [Database Migrations Guide](docs/development/database-migrations.md)
- [EF Core Design Package Documentation](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)
- [Migration Automation Summary](MIGRATION_AUTOMATION_SUMMARY.md)

## Testing

To verify the fix locally:

```bash
# Linux/macOS
make docker.db.up
make db.migrate

# Windows
docker compose up -d postgres
.\scripts\db-migrations.ps1 -Command migrate
```

The migration should now complete successfully without the "file not found" error.

---

**Fixed By**: Cursor Agent  
**Date**: 2025-10-16  
**PR**: #61  
**Branch**: cursor/MOS-110-automate-ef-core-migrations-for-dev-and-ci-64de
