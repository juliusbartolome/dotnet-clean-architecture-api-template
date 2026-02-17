# dotnet-clean-architecture-api-launchpad-starter

Production-oriented .NET 9 Web API launchpad starter using Clean Architecture, CQRS, validation pipelines, SQL Server + Redis, JWT auth, structured logging, health checks, Docker, and CI.

## Architecture

```text
┌──────────────────────────────────────────┐
│            LaunchpadStarter.Api          │
│  - Minimal API endpoints                 │
│  - Auth / middleware / docs              │
└───────────────────┬──────────────────────┘
                    │ MediatR
┌───────────────────▼──────────────────────┐
│        LaunchpadStarter.Application      │
│  - Use cases (CQRS)                      │
│  - Validators                            │
│  - Pipeline behaviors                    │
│  - Result/Error model                    │
└───────────────────┬──────────────────────┘
                    │ abstractions
┌───────────────────▼──────────────────────┐
│       LaunchpadStarter.Infrastructure    │
│  - EF Core (SQL Server)                  │
│  - Redis distributed cache               │
│  - Health checks                         │
└───────────────────┬──────────────────────┘
                    │
┌───────────────────▼──────────────────────┐
│           LaunchpadStarter.Domain        │
│  - Entities + domain events              │
└──────────────────────────────────────────┘
```

## Project structure

```text
src/
  LaunchpadStarter.Domain/
  LaunchpadStarter.Application/
  LaunchpadStarter.Infrastructure/
  LaunchpadStarter.Api/
tests/
  LaunchpadStarter.UnitTests/
  LaunchpadStarter.IntegrationTests/
.github/workflows/ci.yml
Dockerfile
docker-compose.yml
```

## Catalog feature

Entity: `Product`

- `Id`, `Sku`, `Name`, `Description`, `Price`, `Currency`, `IsActive`, `CreatedAt`, `UpdatedAt`

Use cases:

- `CreateProduct`
- `GetProductById`
- `SearchProducts` (pagination + filters)
- `UpdateProduct`
- `DeactivateProduct`

## API endpoints

Base route: `/api/v1/products`

- `POST /api/v1/products` (auth + `catalog.write` policy)
- `GET /api/v1/products/{id}` (anonymous)
- `GET /api/v1/products?isActive=true&minPrice=10&maxPrice=100&q=plan&page=1&pageSize=20` (anonymous)
- `PUT /api/v1/products/{id}` (auth + `catalog.write` policy)
- `DELETE /api/v1/products/{id}` (auth + `catalog.write` policy)
- `GET /health`

## Security

- JWT bearer auth with local symmetric key.
- Write operations require `scope=catalog.write`.
- Read operations are anonymous.

## Caching

Uses `IDistributedCache` (Redis) with TTL.

- `GetProductById`: cache key `catalog:product:{id}` (5 min)
- `SearchProducts`: versioned key `catalog:search:{version}:...` (2 min)
- Invalidation on create/update/deactivate:
  - remove product-by-id cache
  - bump search cache version

## Validation and error handling

- FluentValidation for commands/queries.
- MediatR pipeline behaviors:
  - validation
  - logging
  - performance timing
- ProblemDetails-compliant errors with `errorCode` extension.

## Run locally

```bash
dotnet restore dotnet-clean-architecture-api-launchpad-starter.sln
dotnet build dotnet-clean-architecture-api-launchpad-starter.sln
dotnet run --project src/LaunchpadStarter.Api/LaunchpadStarter.Api.csproj
```

### Environment variables

```bash
# Required when running the API directly (without Docker Compose).
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=LaunchpadStarterDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"

export ConnectionStrings__Redis="localhost:6379"
export JWT_ISSUER="LaunchpadStarter.Api"
export JWT_AUDIENCE="LaunchpadStarter.Client"
export JWT_SIGNING_KEY="dev-only-signing-key-change-this-to-at-least-32-characters"
```

Notes:
- The application is configured to use SQL Server only.

## EF Core migrations

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/LaunchpadStarter.Infrastructure --startup-project src/LaunchpadStarter.Api
dotnet ef database update --project src/LaunchpadStarter.Infrastructure --startup-project src/LaunchpadStarter.Api
```

## Docker compose

```bash
docker compose up --build
```

Compose reads variables from `.env` by default. This repository now includes local defaults there.

If you prefer machine-specific values, use `.env.local`:

```bash
docker compose --env-file .env.local up --build
```

Variables currently used by compose:
- `ASPNETCORE_ENVIRONMENT`
- `MSSQL_IMAGE`
- `REDIS_IMAGE`
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `REDIS_CONNECTION`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_SIGNING_KEY`

## Tests

```bash
dotnet test tests/LaunchpadStarter.UnitTests/LaunchpadStarter.UnitTests.csproj
TESTCONTAINERS_MSSQL_IMAGE="mcr.microsoft.com/mssql/server:2022-latest" \
TESTCONTAINERS_REDIS_IMAGE="redis:7-alpine" \
TESTCONTAINERS_MSSQL_PASSWORD="YourStrong!Passw0rd" \
dotnet test tests/LaunchpadStarter.IntegrationTests/LaunchpadStarter.IntegrationTests.csproj
```

Integration tests require:
- no required environment variables by default

Optional overrides:
- `TESTCONTAINERS_MSSQL_IMAGE`
- `TESTCONTAINERS_REDIS_IMAGE`
- `TESTCONTAINERS_MSSQL_PASSWORD`

## CI

- CI runs on pushes to `main` and on pull requests.
- Workflow uses GitHub Environment `ci`.
- Define `TESTCONTAINERS_MSSQL_PASSWORD` in the `ci` environment secrets.

## Key decisions

- CQRS via MediatR keeps write/read use cases isolated.
- Application layer depends on abstractions only.
- EF Core DbContext exposed behind `IApplicationDbContext`.
- Result pattern centralizes success/failure semantics and error codes.
- Redis caching uses explicit invalidation + versioned search keys.
- Thin API: endpoint-to-command/query mapping only.

## Performance Notes

### Profiling endpoints

- Use `dotnet-counters monitor --process-id <pid>` for runtime metrics.
- Use `dotnet-trace collect --process-id <pid>` to capture traces for high-latency requests.
- Use `X-Cache` response header (`HIT`/`MISS`) to verify cache behavior.

### SQL execution plan analysis

- Capture generated SQL from EF Core logs (`Information` level).
- Re-run SQL in SSMS/Azure Data Studio with Actual Execution Plan enabled.
- Validate:
  - index usage (`Sku` unique index)
  - key lookups/scans on search filters
  - row estimate accuracy for pagination queries
- Add/adjust indexes based on the observed predicate selectivity.
