.PHONY: help db.add-migration db.migrate db.drop db.reset db.status docker.db.up docker.db.down

# Colors for output
CYAN = \033[0;36m
GREEN = \033[0;32m
YELLOW = \033[0;33m
RED = \033[0;31m
NC = \033[0m # No Color

# Configuration
INFRASTRUCTURE_PROJECT = src/GameServerHosting.Infrastructure/GameServerHosting.Infrastructure.csproj
API_PROJECT = src/GameServerHosting.Api/GameServerHosting.Api.csproj
CONNECTION_STRING ?= "Host=localhost;Database=gameserverhosting_dev;Username=postgres;Password=postgres"

help: ## Show this help message
	@echo "$(CYAN)Game Server Hosting - Database Management$(NC)"
	@echo ""
	@echo "$(GREEN)Available commands:$(NC)"
	@grep -E '^[a-zA-Z._-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(CYAN)%-20s$(NC) %s\n", $$1, $$2}'
	@echo ""
	@echo "$(YELLOW)Examples:$(NC)"
	@echo "  make db.add-migration NAME=AddUserPreferences"
	@echo "  make db.migrate"
	@echo "  make db.drop"
	@echo "  make db.reset"

db.add-migration: ## Create a new EF Core migration (usage: make db.add-migration NAME=MigrationName)
	@if [ -z "$(NAME)" ]; then \
		echo "$(RED)Error: NAME is required$(NC)"; \
		echo "Usage: make db.add-migration NAME=MigrationName"; \
		exit 1; \
	fi
	@echo "$(CYAN)Creating migration: $(NAME)$(NC)"
	@cd src/GameServerHosting.Infrastructure && \
		dotnet ef migrations add $(NAME) \
			--project . \
			--startup-project ../GameServerHosting.Api \
			--context GameServerDbContext \
			--verbose
	@echo "$(GREEN)✓ Migration '$(NAME)' created successfully$(NC)"

db.migrate: ## Apply all pending migrations to the database
	@echo "$(CYAN)Applying migrations to database...$(NC)"
	@cd src/GameServerHosting.Infrastructure && \
		dotnet ef database update \
			--project . \
			--startup-project ../GameServerHosting.Api \
			--context GameServerDbContext \
			--verbose
	@echo "$(GREEN)✓ Migrations applied successfully$(NC)"

db.drop: ## Drop the database (WARNING: This will delete all data!)
	@echo "$(RED)WARNING: This will drop the database and delete all data!$(NC)"
	@read -p "Are you sure you want to continue? [y/N] " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "$(CYAN)Dropping database...$(NC)"; \
		cd src/GameServerHosting.Infrastructure && \
		dotnet ef database drop \
			--project . \
			--startup-project ../GameServerHosting.Api \
			--context GameServerDbContext \
			--force; \
		echo "$(GREEN)✓ Database dropped successfully$(NC)"; \
	else \
		echo "$(YELLOW)Operation cancelled$(NC)"; \
	fi

db.reset: ## Drop database and reapply all migrations (WARNING: This will delete all data!)
	@echo "$(RED)WARNING: This will drop the database and delete all data!$(NC)"
	@read -p "Are you sure you want to continue? [y/N] " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "$(CYAN)Dropping database...$(NC)"; \
		cd src/GameServerHosting.Infrastructure && \
		dotnet ef database drop \
			--project . \
			--startup-project ../GameServerHosting.Api \
			--context GameServerDbContext \
			--force; \
		echo "$(CYAN)Applying all migrations...$(NC)"; \
		cd src/GameServerHosting.Infrastructure && \
		dotnet ef database update \
			--project . \
			--startup-project ../GameServerHosting.Api \
			--context GameServerDbContext \
			--verbose; \
		echo "$(GREEN)✓ Database reset successfully$(NC)"; \
	else \
		echo "$(YELLOW)Operation cancelled$(NC)"; \
	fi

db.status: ## Show the status of migrations
	@echo "$(CYAN)Checking migration status...$(NC)"
	@cd src/GameServerHosting.Infrastructure && \
		dotnet ef migrations list \
			--project . \
			--startup-project ../GameServerHosting.Api \
			--context GameServerDbContext

docker.db.up: ## Start PostgreSQL database in Docker
	@echo "$(CYAN)Starting PostgreSQL database...$(NC)"
	@docker compose up -d postgres
	@echo "$(GREEN)✓ PostgreSQL started successfully$(NC)"
	@echo "Waiting for database to be ready..."
	@until docker compose exec -T postgres pg_isready -U postgres; do \
		sleep 1; \
	done
	@echo "$(GREEN)✓ Database is ready$(NC)"

docker.db.down: ## Stop PostgreSQL database
	@echo "$(CYAN)Stopping PostgreSQL database...$(NC)"
	@docker compose stop postgres
	@echo "$(GREEN)✓ PostgreSQL stopped$(NC)"

docker.db.reset: ## Remove PostgreSQL database volume and restart
	@echo "$(RED)WARNING: This will remove the database volume and all data!$(NC)"
	@read -p "Are you sure you want to continue? [y/N] " -n 1 -r; \
	echo; \
	if [[ $$REPLY =~ ^[Yy]$$ ]]; then \
		echo "$(CYAN)Removing PostgreSQL container and volume...$(NC)"; \
		docker compose down postgres; \
		docker volume rm gameserver-postgres-data || true; \
		echo "$(CYAN)Starting fresh PostgreSQL database...$(NC)"; \
		docker compose up -d postgres; \
		echo "Waiting for database to be ready..."; \
		until docker compose exec -T postgres pg_isready -U postgres; do \
			sleep 1; \
		done; \
		echo "$(GREEN)✓ Database reset successfully$(NC)"; \
	else \
		echo "$(YELLOW)Operation cancelled$(NC)"; \
	fi

# Additional helper commands
build: ## Build the entire solution
	@echo "$(CYAN)Building solution...$(NC)"
	@dotnet build GameServerHosting.sln
	@echo "$(GREEN)✓ Build complete$(NC)"

restore: ## Restore NuGet packages
	@echo "$(CYAN)Restoring packages...$(NC)"
	@dotnet restore GameServerHosting.sln
	@echo "$(GREEN)✓ Restore complete$(NC)"

test: ## Run all tests
	@echo "$(CYAN)Running tests...$(NC)"
	@dotnet test GameServerHosting.sln
	@echo "$(GREEN)✓ Tests complete$(NC)"

clean: ## Clean build artifacts
	@echo "$(CYAN)Cleaning...$(NC)"
	@dotnet clean GameServerHosting.sln
	@echo "$(GREEN)✓ Clean complete$(NC)"
