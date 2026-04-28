# Deployment Notes

DocumentTracker can run as a normal ASP.NET Core MVC app on Windows, Linux, or macOS, or as a container image. The application process and PostgreSQL do not need to run on the same server as long as the connection string is configured correctly.

## Production Configuration

Production settings should come from environment variables, server configuration, or a secret manager. Do not put production passwords in `appsettings.json`.

Required environment variable:

```bash
ConnectionStrings__DocumentTracker="Host=your-db-host;Port=5432;Database=document_tracker;Username=app_user;Password=strong-password"
```

Optional role fallback for this training app:

```bash
Training__CurrentUserRole="DocumentAdmin"
```

In a real production system, replace the training role fallback with proper authentication and authorization.

## Container Build

Build the app image:

```bash
docker build -t documenttracker:local .
```

Run the app container against an existing PostgreSQL server:

```bash
docker run --rm -p 8080:8080 \
  -v documenttracker-data-protection-keys:/root/.aspnet/DataProtection-Keys \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DocumentTracker="Host=host.docker.internal;Port=5433;Database=document_tracker;Username=postgres;Password=postgres" \
  documenttracker:local
```

Open:

```text
http://localhost:8080
```

Health endpoint:

```text
http://localhost:8080/health
```

## Production Compose Example

The file `docker-compose.production.example.yml` shows an app-plus-database deployment shape:

```bash
cp .env.example .env
docker compose -f docker-compose.production.example.yml up --build -d
```

The example is suitable for training and server dry runs. For a real production deployment, review database backup strategy, TLS termination, password rotation, centralized logging, and migration process.

The production Compose example includes a named volume for ASP.NET Core Data Protection keys. Persisting those keys matters for antiforgery tokens, cookies, and any future encrypted payloads that must survive container restarts. A real production deployment should also protect those keys at rest with the hosting platform's secret or key-management system.

## Static Assets

Client-side libraries are declared in `src/DocumentTracker/libman.json` and restored by `Microsoft.Web.LibraryManager.Build` during build/publish. The generated files under `src/DocumentTracker/wwwroot/lib/` are ignored by Git.

This keeps Bootstrap, jQuery, jQuery Validation, and jQuery Unobtrusive Validation pinned and reproducible without committing downloaded vendor assets.

## Database Initialization

The included SQL scripts are enough for training and local provisioning:

```bash
psql -d document_tracker -f database/schema.sql
psql -d document_tracker -f database/seed.sql
```

For a larger production app, evolve this into a proper migration process. The repository intentionally keeps this simple so trainees can read and review every SQL statement.
