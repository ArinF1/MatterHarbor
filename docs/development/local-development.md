# Local development

## Prerequisites

- .NET SDK 10.0.300 (the repository's `global.json` allows later patches)
- Node.js 22.13.0+ and npm 10+
- Docker and Docker Compose
- PowerShell 7 or Make (optional)

## Setup

```bash
docker compose up -d
dotnet restore MatterHarbor.sln
dotnet tool restore
dotnet build MatterHarbor.sln
npm --prefix src/MatterHarbor.Web install
```

Run API, worker, and web in separate terminals:

```bash
dotnet run --project src/MatterHarbor.Api
dotnet run --project src/MatterHarbor.Worker
npm --prefix src/MatterHarbor.Web run dev
```

The API applies `InitialCreate` and seeds two fictional tenants only in Development. It never migrates in other environments. Select Alex/Northwind or Casey/Contoso in the web app. Data persists in the `matterharbor-postgres` Docker volume. Stop dependencies with `docker compose down`; add `--volumes` only when intentionally deleting local data.

## Local services

| Service | Address |
| --- | --- |
| Web | http://localhost:5173 |
| API | http://localhost:5080 |
| OpenAPI (Development) | http://localhost:5080/openapi/v1.json |
| PostgreSQL | localhost:5432 |
| Jaeger UI | http://localhost:16686 |
| OTLP gRPC | http://localhost:4317 |

Development configuration contains only a disposable local database password. Production settings are intentionally incomplete and fail closed until OIDC, PostgreSQL, messaging, and telemetry configuration is supplied.

## Database changes

```bash
dotnet tool run dotnet-ef migrations add MeaningfulName --project src/MatterHarbor.Infrastructure --startup-project src/MatterHarbor.Api --output-dir Persistence/Migrations
dotnet tool run dotnet-ef database update --project src/MatterHarbor.Infrastructure --startup-project src/MatterHarbor.Api
```

Review generated SQL and apply the migration to local PostgreSQL before committing.

Shared environments use the versioned CI artifact, not `database update` or application startup. Follow the [controlled migration runbook](../operations/database-migrations.md).
