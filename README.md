# dotnet-clean-architecture-api-template

Production-oriented .NET 9 Web API template using Clean Architecture, CQRS, validation pipelines, SQL Server/SQLite + Redis, JWT auth, structured logging, health checks, Docker, and CI.

## Architecture

```text
┌───────────────────────────────┐
│           Template.Api        │
│  - Minimal API endpoints      │
│  - Auth / middleware / docs   │
└──────────────┬────────────────┘
               │ MediatR
┌──────────────▼────────────────┐
│       Template.Application     │
│  - Use cases (CQRS)           │
│  - Validators                 │
│  - Pipeline behaviors         │
│  - Result/Error model         │
└──────────────┬────────────────┘
               │ abstractions
┌──────────────▼────────────────┐
│      Template.Infrastructure   │
│  - EF Core (SQL Server/SQLite)│
│  - Redis distributed cache     │
│  - Health checks               │
└──────────────┬────────────────┘
               │
┌──────────────▼────────────────┐
│        Template.Domain         │
│  - Entities + domain events    │
└───────────────────────────────┘
```

## Project structure

```text
src/
  Template.Domain/
  Template.Application/
  Template.Infrastructure/
  Template.Api/
tests/
  Template.UnitTests/
  Template.IntegrationTests/
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
dotnet restore dotnet-clean-architecture-api-template.sln
dotnet build dotnet-clean-architecture-api-template.sln
dotnet run --project src/Template.Api/Template.Api.csproj
```

### Environment variables

```bash
# Optional: override DB provider (default is SqlServer when unset).
export Database__Provider="SqlServer"

# Required when Database__Provider=SqlServer.
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=TemplateDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True"

# Required when Database__Provider=Sqlite.
export ConnectionStrings__Sqlite="Data Source=template.db"

export ConnectionStrings__Redis="localhost:6379"
export Jwt__Issuer="Template.Api"
export Jwt__Audience="Template.Client"
export Jwt__SigningKey="dev-only-signing-key-change-this-to-at-least-32-characters"
```

Notes:
- `appsettings.Development.json` defaults to `Database:Provider=Sqlite` with `ConnectionStrings:Sqlite=Data Source=template.dev.db`.
- If you switch to SQL Server locally, set `Database__Provider=SqlServer` and provide `ConnectionStrings__DefaultConnection`.

## EF Core migrations

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/Template.Infrastructure --startup-project src/Template.Api
dotnet ef database update --project src/Template.Infrastructure --startup-project src/Template.Api
```

## Docker compose

```bash
docker compose up --build
```

## Tests

```bash
dotnet test tests/Template.UnitTests/Template.UnitTests.csproj
TESTCONTAINERS_MSSQL_IMAGE="mcr.microsoft.com/mssql/server:2022-latest" \
TESTCONTAINERS_REDIS_IMAGE="redis:7-alpine" \
TESTCONTAINERS_MSSQL_PASSWORD="YourStrong!Passw0rd" \
dotnet test tests/Template.IntegrationTests/Template.IntegrationTests.csproj
```

Integration tests require:
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
