# Game Server Hosting - Database Migration Scripts
# PowerShell script for database management on Windows

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("add-migration", "migrate", "drop", "reset", "status", "help")]
    [string]$Command,
    
    [Parameter(Mandatory=$false)]
    [string]$Name = ""
)

# Configuration
$InfrastructureProject = "src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj"
$ApiProject = "src/GameServerHosting.Api/GameServerHosting.Api.csproj"

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Show-Help {
    Write-ColorMessage "`nGame Server Hosting - Database Management" "Cyan"
    Write-ColorMessage "`nAvailable commands:" "Green"
    Write-ColorMessage "  add-migration  - Create a new EF Core migration (requires -Name parameter)"
    Write-ColorMessage "  migrate        - Apply all pending migrations to the database"
    Write-ColorMessage "  drop           - Drop the database (WARNING: This will delete all data!)"
    Write-ColorMessage "  reset          - Drop database and reapply all migrations (WARNING: This will delete all data!)"
    Write-ColorMessage "  status         - Show the status of migrations"
    Write-ColorMessage "  help           - Show this help message"
    Write-ColorMessage "`nExamples:" "Yellow"
    Write-ColorMessage "  .\scripts\db-migrations.ps1 -Command add-migration -Name AddUserPreferences"
    Write-ColorMessage "  .\scripts\db-migrations.ps1 -Command migrate"
    Write-ColorMessage "  .\scripts\db-migrations.ps1 -Command drop"
    Write-ColorMessage "  .\scripts\db-migrations.ps1 -Command reset"
    Write-ColorMessage ""
}

function Add-Migration {
    if ([string]::IsNullOrEmpty($Name)) {
        Write-ColorMessage "Error: NAME is required for add-migration command" "Red"
        Write-ColorMessage "Usage: .\scripts\db-migrations.ps1 -Command add-migration -Name MigrationName" "Yellow"
        exit 1
    }
    
    Write-ColorMessage "Creating migration: $Name" "Cyan"
    
    Push-Location "src/GameServerHosting.Infrastructure"
    try {
        dotnet ef migrations add $Name `
            --project . `
            --startup-project ../GameServerHosting.Api `
            --context GameServerDbContext `
            --verbose
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorMessage "✓ Migration '$Name' created successfully" "Green"
        } else {
            Write-ColorMessage "✗ Failed to create migration" "Red"
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

function Apply-Migrations {
    Write-ColorMessage "Applying migrations to database..." "Cyan"
    
    Push-Location "src/GameServerHosting.Infrastructure"
    try {
        dotnet ef database update `
            --project . `
            --startup-project ../GameServerHosting.Api `
            --context GameServerDbContext `
            --verbose
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorMessage "✓ Migrations applied successfully" "Green"
        } else {
            Write-ColorMessage "✗ Failed to apply migrations" "Red"
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

function Drop-Database {
    Write-ColorMessage "WARNING: This will drop the database and delete all data!" "Red"
    $confirmation = Read-Host "Are you sure you want to continue? [y/N]"
    
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
        Write-ColorMessage "Dropping database..." "Cyan"
        
        Push-Location "src/GameServerHosting.Infrastructure"
        try {
            dotnet ef database drop `
                --project . `
                --startup-project ../GameServerHosting.Api `
                --context GameServerDbContext `
                --force
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "✓ Database dropped successfully" "Green"
            } else {
                Write-ColorMessage "✗ Failed to drop database" "Red"
                exit 1
            }
        }
        finally {
            Pop-Location
        }
    } else {
        Write-ColorMessage "Operation cancelled" "Yellow"
    }
}

function Reset-Database {
    Write-ColorMessage "WARNING: This will drop the database and delete all data!" "Red"
    $confirmation = Read-Host "Are you sure you want to continue? [y/N]"
    
    if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
        Write-ColorMessage "Dropping database..." "Cyan"
        
        Push-Location "src/GameServerHosting.Infrastructure"
        try {
            dotnet ef database drop `
                --project . `
                --startup-project ../GameServerHosting.Api `
                --context GameServerDbContext `
                --force
            
            if ($LASTEXITCODE -ne 0) {
                Write-ColorMessage "✗ Failed to drop database" "Red"
                exit 1
            }
            
            Write-ColorMessage "Applying all migrations..." "Cyan"
            
            dotnet ef database update `
                --project . `
                --startup-project ../GameServerHosting.Api `
                --context GameServerDbContext `
                --verbose
            
            if ($LASTEXITCODE -eq 0) {
                Write-ColorMessage "✓ Database reset successfully" "Green"
            } else {
                Write-ColorMessage "✗ Failed to apply migrations" "Red"
                exit 1
            }
        }
        finally {
            Pop-Location
        }
    } else {
        Write-ColorMessage "Operation cancelled" "Yellow"
    }
}

function Get-MigrationStatus {
    Write-ColorMessage "Checking migration status..." "Cyan"
    
    Push-Location "src/GameServerHosting.Infrastructure"
    try {
        dotnet ef migrations list `
            --project . `
            --startup-project ../GameServerHosting.Api `
            --context GameServerDbContext
    }
    finally {
        Pop-Location
    }
}

# Main script logic
switch ($Command) {
    "add-migration" { Add-Migration }
    "migrate" { Apply-Migrations }
    "drop" { Drop-Database }
    "reset" { Reset-Database }
    "status" { Get-MigrationStatus }
    "help" { Show-Help }
    default { Show-Help }
}
