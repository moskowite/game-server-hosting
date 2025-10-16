# CI Migration Test Fix

**Issue**: Database Migration Test job failing in CI  
**Job IDs**: 52880153418, 52880608156, 52881518011, 52881976967  
**Date**: 2025-10-16

## Problem Analysis

### First Issue (Job 52880153418)

The Database Migration Test job was failing with the following error:

```
Microsoft.EntityFrameworkCore.Tools.CommandException: Your startup project 'GameServerHosting.Api' doesn't reference Microsoft.EntityFrameworkCore.Design. This package is required for the Entity Framework Core Tools to work. Ensure your startup project is correct, install the package, and try again.

System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.EntityFrameworkCore.Design, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
```

#### Root Cause #1

When running EF Core migrations with `dotnet ef database update`, the command specifies:
- `--project src/GameServerHosting.Infrastructure` (where the DbContext lives)
- `--startup-project src/GameServerHosting.Api` (the application entry point)

The EF Core tools require the **startup project** to have a reference to `Microsoft.EntityFrameworkCore.Design`, but only the Infrastructure project had this package installed.

#### Additional Issue

The build logs also showed duplicate package reference warnings:
```
warning NU1504: Duplicate 'PackageReference' items found:
- Microsoft.Extensions.Configuration.Abstractions 8.0.0 (appeared twice)
- Microsoft.Extensions.Logging.Abstractions (versions 8.0.2 and 8.0.0)
```

### Second Issue (Job 52880608156)

After fixing the first issue, the CI failed again with a different error:

```
Npgsql.PostgresException (0x80004005): 42883: function uuid_generate_v4() does not exist
```

#### Root Cause #2

The PostgreSQL test database didn't have the `uuid-ossp` extension enabled. This extension provides the `uuid_generate_v4()` function that the migrations use for generating default UUID values in the database schema.

In local development, developers likely enabled this extension manually or it was part of their existing database setup. The CI environment uses a fresh PostgreSQL container for each test run, which doesn't have this extension enabled by default.

### Third Issue (Job 52881518011)

After fixing the UUID extension issue, the CI failed once more with:

```
/home/runner/work/_temp/cecd0588-9859-4338-8245-afa298fe0cfb.sh: line 12: [: 0
0: integer expression expected
```

#### Root Cause #3

The database verification step was using `grep -c "| public |" tables.txt` to count tables from the `\dt` psql command output. However, this parsing approach was unreliable and produced a malformed result ("0\n0" instead of a single number), causing the integer comparison to fail.

The migrations had actually succeeded and created 7 tables, but the verification logic had a bug that prevented proper validation.

### Fourth Issue (Job 52881976967)

After fixing the verification logic, the CI failed in the rollback test step with:

```
System.InvalidOperationException: The migration '[06:25:48 INF] Starting Game Server Hosting API' was not found.
```

#### Root Cause #4

The migration rollback test was parsing the output of `dotnet ef migrations list` to extract migration names. However, the command builds and starts the application, which outputs log messages (from Serilog) that get mixed into the migration list output.

The existing filters (`grep -v "Build started" | grep -v "Build succeeded"`) only removed build messages, not application logs like `[06:25:48 INF] Starting Game Server Hosting API`. The script then tried to use this log message as a migration name, causing the rollback command to fail.

## Solution

### Fix #1: Add EF Core Design Package to API Project

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

### Fix #2: Remove Duplicate Package References

**File**: `src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj`

Removed duplicate entries:
- `Microsoft.Extensions.Configuration.Abstractions` (line 38 - duplicate)
- `Microsoft.Extensions.Logging.Abstractions` (line 37 - duplicate)

These packages were already referenced on lines 22-23, so the duplicates at the end of the file were unnecessary.

### Fix #3: Enable UUID Extension in CI

**File**: `.github/workflows/ci.yml`

Added a new step before applying migrations to enable the PostgreSQL UUID extension:

```yaml
- name: Enable PostgreSQL UUID extension
  run: |
    docker run --rm --network host postgres:16-alpine \
      psql "postgresql://postgres:postgres@localhost:5432/gameserverhosting_test" \
      -c "CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";"
    
    echo "✅ UUID extension enabled" >> $GITHUB_STEP_SUMMARY
```

**Why this is needed:**
- EF Core migrations use `uuid_generate_v4()` for default UUID values
- This function comes from the `uuid-ossp` PostgreSQL extension
- The extension must be enabled before migrations can run
- Fresh CI database containers don't have this extension by default

### Fix #4: Improve Database Verification Logic

**File**: `.github/workflows/ci.yml`

Replaced the unreliable grep-based table counting with direct SQL queries:

```yaml
- name: Verify database schema
  run: |
    echo "" >> $GITHUB_STEP_SUMMARY
    echo "### Database Tables Created" >> $GITHUB_STEP_SUMMARY
    
    # Count tables using SQL query (more reliable than parsing psql output)
    TABLE_COUNT=$(docker run --rm --network host postgres:16-alpine \
      psql "postgresql://postgres:postgres@localhost:5432/gameserverhosting_test" \
      -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE';")
    
    # Trim whitespace from the result
    TABLE_COUNT=$(echo "$TABLE_COUNT" | tr -d ' \n\r')
    
    # ... rest of verification logic
```

**Why this is better:**
- Uses SQL's `information_schema` to count tables directly
- More reliable than parsing human-readable psql output
- Handles whitespace properly with `tr -d ' \n\r'`
- Also lists individual table names for better visibility in CI summary

### Fix #5: Robust Migration List Parsing

**File**: `.github/workflows/ci.yml`

Fixed the migration rollback test to properly filter migration names:

```yaml
# Get the list of migrations, filtering out build messages and log output
# Migration names follow the pattern: timestamp_MigrationName (e.g., 20251013232951_InitialCreate)
MIGRATIONS=$(dotnet ef migrations list \
  --project . \
  --startup-project ../GameServerHosting.Api \
  --context GameServerDbContext \
  --no-connect 2>&1 | \
  grep -E '^[0-9]{14}_' | \
  tr -d ' \r')
```

**Why this is better:**
- Uses regex pattern `^[0-9]{14}_` to match only actual migration names (14-digit timestamp + underscore)
- Captures both stdout and stderr with `2>&1` then filters
- Removes all whitespace and carriage returns with `tr -d ' \r'`
- Ignores all build messages, log output, and other noise
- Only captures lines that look like valid EF Core migration names

## Changes Made

### Modified Files

1. **src/GameServerHosting.Api/GameServerHosting.Api.csproj**
   - ✅ Added `Microsoft.EntityFrameworkCore.Design` package

2. **src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj**
   - ✅ Removed duplicate `Microsoft.Extensions.Configuration.Abstractions`
   - ✅ Removed duplicate `Microsoft.Extensions.Logging.Abstractions`

3. **.github/workflows/ci.yml**
   - ✅ Added step to enable PostgreSQL `uuid-ossp` extension before migrations
   - ✅ Ensures migrations can use `uuid_generate_v4()` function
   - ✅ Improved database verification to use SQL queries instead of parsing psql output
   - ✅ Added table name listing for better CI visibility
   - ✅ Fixed migration list parsing to filter out application logs using regex pattern matching

## Expected Results

After these changes, the CI pipeline should:

1. ✅ **Build successfully** without duplicate package warnings
2. ✅ **Enable UUID extension** in the test database
3. ✅ **Run `dotnet ef database update`** successfully in the migration test job
4. ✅ **Apply migrations** to the test PostgreSQL database without UUID errors
5. ✅ **Verify database schema** creation
6. ✅ **Test migration rollback** (if applicable)

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

## 📊 Summary

**Four issues resolved:**
1. ✅ **Issue #1**: Missing `Microsoft.EntityFrameworkCore.Design` package in API project
2. ✅ **Issue #2**: Missing `uuid-ossp` PostgreSQL extension in CI database
3. ✅ **Issue #3**: Unreliable table counting causing verification failure
4. ✅ **Issue #4**: Migration list parsing picking up application logs as migration names

All issues are now fixed with robust solutions:
- Package references corrected
- PostgreSQL extension automatically enabled
- Database verification using direct SQL queries
- Migration parsing using regex pattern matching to filter out noise

The Database Migration Test job should now pass completely on the next CI run!

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
