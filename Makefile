API_DIR := backend/UniversityPortal.API
DOTNET  ?= dotnet.exe
PG_NAME := up-postgres
PG_PORT := 5433
PG_DB   := university_portal
PG_VOLUME := up_pgdata

.PHONY: help db-up db-start db-stop db-psql migrate migration run watch

help: ## List the available commands
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  %-12s %s\n", $$1, $$2}'

db-up: ## Create the PostgreSQL container (first time only; needs PG_PASSWORD)
ifndef PG_PASSWORD
	$(error Set PG_PASSWORD first, e.g. export PG_PASSWORD=yourpassword)
endif
	docker run --name $(PG_NAME) -e POSTGRES_PASSWORD=$(PG_PASSWORD) -e POSTGRES_DB=$(PG_DB) -p 127.0.0.1:$(PG_PORT):5432 -v $(PG_VOLUME):/var/lib/postgresql/data --restart unless-stopped -d postgres:17

db-start: ## Start the existing PostgreSQL container
	docker start $(PG_NAME)

db-stop: ## Stop the PostgreSQL container (data stays in the volume)
	docker stop $(PG_NAME)

db-psql: ## Open a psql shell on the portal database
	docker exec -it $(PG_NAME) psql -U postgres -d $(PG_DB)

migrate: ## Apply pending EF Core migrations
	cd $(API_DIR) && $(DOTNET) ef database update

migration: ## Add a migration: make migration name=AddSomething
ifndef name
	$(error Usage: make migration name=SomeName)
endif
	cd $(API_DIR) && $(DOTNET) ef migrations add $(name) --output-dir Infrastructure/Persistence/Migrations

run: ## Run the API
	cd $(API_DIR) && $(DOTNET) run

watch: ## Run the API with hot reload
	cd $(API_DIR) && $(DOTNET) watch run
