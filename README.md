# DocumentTracker

DocumentTracker is a small ASP.NET Core MVC training app for junior developers learning to maintain an existing C# application. It uses Razor views, Bootstrap, PostgreSQL, Dapper, services, repositories, validation, dependency injection, `ILogger`, xUnit, and Moq.

It intentionally avoids Entity Framework Core, Minimal APIs, Blazor, React, complex authentication frameworks, microservices, CQRS, and MediatR.

## Project Structure

- `src/DocumentTracker/Controllers`: MVC controllers. They handle routes, model state, redirects, and view selection.
- `src/DocumentTracker/Services`: business rules, validation beyond data annotations, role checks, and logging decisions.
- `src/DocumentTracker/Repositories`: Dapper and SQL only. User input is passed through parameters.
- `src/DocumentTracker/Models`: database/domain models and enums.
- `src/DocumentTracker/ViewModels`: models designed for Razor forms and pages. These prevent overposting.
- `src/DocumentTracker/Views`: Razor pages for list, details, create, edit, and soft delete confirmation.
- `database`: PostgreSQL schema and seed scripts.
- `tests/DocumentTracker.Tests`: xUnit tests for services, controllers, SQL inspection, and an optional PostgreSQL-backed repository example.
- `Dockerfile`: production-style container build for the MVC app.
- `docker-compose.yml`: local PostgreSQL-only development stack.
- `docker-compose.production.example.yml`: app-plus-database deployment example.
- `src/DocumentTracker/libman.json`: pinned client-side library restore for Bootstrap, jQuery, and validation scripts.
- `docs/deployment.md`: deployment and production configuration notes.

## Setup

1. Install .NET 10 SDK.
2. Install Docker Desktop or another Docker-compatible runtime.
3. Start PostgreSQL:

```bash
docker compose up -d
```

The Compose stack runs PostgreSQL on host port `5433`, creates the `document_tracker` database, applies `database/schema.sql`, loads `database/seed.sql`, and creates a separate `document_tracker_test` database for integration-style tests.

4. Run the app:

```bash
dotnet run --project src/DocumentTracker/DocumentTracker.csproj
```

5. Open the URL printed by `dotnet run`.

## Manual PostgreSQL Setup

Docker is recommended for training because every developer gets the same database, user, password, port, schema, and seed data. If you prefer a local PostgreSQL installation instead, create the database manually:

```bash
createdb document_tracker
psql -d document_tracker -f database/schema.sql
psql -d document_tracker -f database/seed.sql
```

Then configure the connection string in `src/DocumentTracker/appsettings.json`, user secrets, or environment variables.

## Connection String

```json
{
  "ConnectionStrings": {
    "DocumentTracker": "Host=localhost;Port=5433;Database=document_tracker;Username=postgres;Password=postgres"
  }
}
```

For environment variables, use:

```bash
export ConnectionStrings__DocumentTracker="Host=localhost;Port=5433;Database=document_tracker;Username=postgres;Password=postgres"
```

If port `5433` is already in use, change the host-side port in `docker-compose.yml` and update the connection string to match.

## Client-Side Library Provisioning

`wwwroot/lib/` is not committed source. It is generated from `src/DocumentTracker/libman.json` during build through `Microsoft.Web.LibraryManager.Build`.

The pinned libraries are:

- Bootstrap `5.3.3`
- jQuery `3.7.1`
- jQuery Validation `1.21.0`
- jQuery Validation Unobtrusive `4.0.0`

To restore them explicitly:

```bash
dotnet build src/DocumentTracker/DocumentTracker.csproj
```

This keeps the app realistic: frontend assets are versioned by manifest, restored during builds, and excluded from Git.

## Role Check

Soft delete requires the `DocumentAdmin` role. This training app does not use a full authentication framework. The role provider first checks `HttpContext.User.IsInRole("DocumentAdmin")`, then falls back to:

```json
{
  "Training": {
    "CurrentUserRole": "DocumentAdmin"
  }
}
```

Set `Training:CurrentUserRole` to another value, such as `Reviewer`, to see delete rejected.

## Running Tests

```bash
dotnet test DocumentTracker.slnx
```

The repository integration-style test only runs real database work when this environment variable is set:

```bash
docker compose up -d
export DOCUMENTTRACKER_TEST_CONNECTION_STRING="Host=localhost;Port=5433;Database=document_tracker_test;Username=postgres;Password=postgres"
dotnet test DocumentTracker.slnx
```

Use the dedicated `document_tracker_test` database because the integration test recreates schema objects and deletes rows from `documents`.

## Docker Commands

Start PostgreSQL:

```bash
docker compose up -d
```

Stop PostgreSQL without deleting data:

```bash
docker compose down
```

Reset PostgreSQL data, schema, and seed rows:

```bash
docker compose down -v
docker compose up -d
```

## Production-Style Deployment

Build the app container:

```bash
docker build -t documenttracker:local .
```

Run the production example stack:

```bash
cp .env.example .env
docker compose -f docker-compose.production.example.yml up --build -d
```

The app listens on `http://localhost:8080` in the production example. The health endpoint is available at:

```text
http://localhost:8080/health
```

See `docs/deployment.md` for production configuration notes, including environment variables and the limits of the training role fallback.

The production Compose example also persists ASP.NET Core Data Protection keys in a named volume so antiforgery tokens and future cookie-based features survive container restarts.

## Feature Walkthrough

List/search flow:
`GET /Documents?searchTerm=finance` calls `DocumentsController.Index`, then `DocumentService.SearchAsync`, then `DocumentRepository.SearchAsync`. The repository runs parameterized SQL using `@SearchTerm` and `@SearchPattern`. Only rows with `deleted_at_utc IS NULL` are returned.

Create flow:
`GET /Documents/Create` shows a Razor form backed by `DocumentCreateViewModel`. `POST /Documents/Create` validates model state, calls `DocumentService.CreateAsync`, checks duplicate document numbers, then saves through `DocumentRepository.CreateAsync` inside a transaction.

Edit flow:
`GET /Documents/Edit/{id}` loads an editable view model only if the document exists and is not deleted. `POST /Documents/Edit/{id}` validates the view model, rejects deleted records, checks duplicate document numbers, updates `UpdatedAtUtc`, and saves through the repository.

Delete flow:
`GET /Documents/Delete/{id}` shows a confirmation page. `POST /Documents/Delete/{id}` calls `DocumentService.SoftDeleteAsync`, which requires `DocumentAdmin`, verifies the document still exists and is not already deleted, then sets `DeletedAtUtc`.

## MVC Request Lifecycle In This App

1. The browser requests a route such as `/Documents/Create`.
2. ASP.NET Core routing selects `DocumentsController`.
3. The controller validates HTTP concerns such as model state, route IDs, and redirects.
4. The service applies business rules such as uniqueness, deleted-record checks, status validation, role checks, and timestamps.
5. The repository opens a PostgreSQL connection and executes parameterized Dapper SQL.
6. The controller returns a Razor view or redirect.
7. Razor renders HTML using the view model.

## Where To Set Breakpoints

- `DocumentsController.Index`: start here when tracing list/search.
- `DocumentRepository.SearchAsync`: inspect SQL parameters for server-side search.
- `DocumentsController.Create` POST: inspect model binding and `ModelState`.
- `DocumentService.CreateAsync`: inspect data annotation validation and uniqueness checks.
- `DocumentService.UpdateAsync`: inspect deleted-record protection and `UpdatedAtUtc`.
- `DocumentService.SoftDeleteAsync`: inspect the role check and soft-delete rule.
- `DocumentRepository.CreateAsync`, `UpdateAsync`, and `SoftDeleteAsync`: inspect transaction usage.

## Review Checklist For Trainees

- Controllers stay thin and do not contain SQL.
- Services contain business rules and return friendly validation errors.
- Repositories contain Dapper SQL only.
- SQL uses parameters for user input.
- Create/edit pages use view models, not the `Document` model directly.
- POST actions have `[ValidateAntiForgeryToken]`.
- Document number uniqueness is checked in service code and enforced by the database.
- Edit rejects missing and deleted documents.
- Delete is a soft delete and requires `DocumentAdmin`.
- Logs contain IDs and outcomes, not sensitive form data.
- Tests cover success, validation failures, duplicate numbers, missing/deleted edit cases, delete role checks, SQL parameter use, and controller validation behavior.

## Common AI-Generated Code Mistakes To Look For

- Adding Entity Framework Core even though the app uses Dapper.
- Putting SQL in controllers or services.
- Filtering search results only in JavaScript instead of on the server.
- Concatenating user input into SQL strings.
- Binding create/edit forms directly to the database model, causing overposting risk.
- Physically deleting rows instead of setting `DeletedAtUtc`.
- Forgetting `[ValidateAntiForgeryToken]` on POST actions.
- Trusting only UI validation and skipping service-level validation.
- Returning raw exception messages to users.
- Logging full submitted descriptions or other unnecessary user-entered data.
