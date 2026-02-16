# Architecture Deep Dive

## 1) System Overview

```text
Client
  -> LaunchpadStarter.Api (Minimal API, auth, middleware, Swagger, health checks)
  -> MediatR
  -> LaunchpadStarter.Application (CQRS handlers, validators, pipeline behaviors, Result model)
  -> Abstractions (IApplicationDbContext, ICacheService, ICacheVersionService)
  -> LaunchpadStarter.Infrastructure (EF Core SQL Server/SQLite, Redis cache, health checks)
  -> LaunchpadStarter.Domain (Product entity, domain events)
```

Core idea: API is thin, use-cases live in Application, technical details live in Infrastructure, and business state/behavior live in Domain.

## 2) Project Boundaries

- `src/LaunchpadStarter.Api`
  - HTTP transport only: endpoints, auth policies, middleware, exception mapping.
  - No business rules.
- `src/LaunchpadStarter.Application`
  - Business use-cases (commands/queries), validation, and cross-cutting MediatR behaviors.
  - Depends only on abstractions and domain.
- `src/LaunchpadStarter.Infrastructure`
  - EF Core provider wiring (SQL Server or SQLite), Redis cache implementation, health checks, DI registration.
- `src/LaunchpadStarter.Domain`
  - `Product` aggregate-like entity + domain event type.
- `tests/LaunchpadStarter.UnitTests`, `tests/LaunchpadStarter.IntegrationTests`
  - Handler/validator unit tests and endpoint integration tests with Testcontainers + test auth scheme.

## 3) Request Lifecycle (Write Path)

Example: `POST /api/v1/products`

1. Endpoint receives payload and forwards `CreateProductCommand` via `ISender`.
2. MediatR pipeline executes:
   - `ValidationBehavior` (FluentValidation)
   - `LoggingBehavior`
   - `PerformanceBehavior`
3. Handler checks SKU uniqueness in DB.
4. Domain creates `Product` (`Product.Create(...)`) with normalized values.
5. EF Core persists entity.
6. Cache invalidation:
   - remove `catalog:product:{id}`
   - bump `catalog:search:version`
7. `Result<ProductDto>` returned.
8. API maps success to `201 Created` or failure to ProblemDetails.

## 4) Request Lifecycle (Read Path + Caching)

### `GET /api/v1/products/{id}`

1. Query handler checks Redis key `catalog:product:{id}`.
2. Cache hit: returns DTO + `CacheHit=true`.
3. Cache miss: reads the configured relational database provider, caches for 5 minutes, returns DTO.
4. API adds `X-Cache: HIT|MISS`.

### `GET /api/v1/products` (search)

1. Handler reads current search version (default `v1`).
2. Builds versioned cache key with filters + paging.
3. Cache miss runs EF query with filters, sorting, pagination.
4. Caches result for 2 minutes.
5. Writes bump version so stale search keys become obsolete.

## 5) Security Model

- JWT bearer auth configured in API startup.
- Write endpoints require policy `CatalogWrite` with claim `scope=catalog.write`.
- Read endpoints are anonymous.
- Swagger is configured with bearer security definition for local testing.
- Integration tests replace JWT auth handlers with a `Test` authentication scheme (`TestAuthHandler`) to isolate endpoint behavior.

## 6) Error Handling and Contracts

- Application returns `Result` / `Result<T>` with typed `Error` codes.
- API maps business failures to HTTP status:
  - `catalog.not_found` -> `404`
  - `catalog.conflict` -> `409`
  - fallback -> `400`
- Validation exceptions are transformed into `HttpValidationProblemDetails` with `errorCode=validation.failed`.
- Unexpected exceptions become `500` with `errorCode=server.unexpected`.

## 7) Operational Characteristics

- Structured logging with Serilog.
- Correlation ID middleware (`X-Correlation-ID`) for request tracing.
- Global rate limiting (`100 req/min`) to protect the API.
- Health checks:
  - SQL Server (`AddDbContextCheck`)
  - Redis connectivity read/write probe.
- Container-first local environment via `docker-compose.yml`.
- CI pipeline runs restore/build/unit/integration tests.
- CI pre-pulls integration images (`mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04`, `redis:7-alpine`) before running integration tests.

## 8) Database Provider Strategy

- Infrastructure reads `Database:Provider` and supports:
  - `SqlServer` using `ConnectionStrings:DefaultConnection` with retry-on-failure.
  - `Sqlite` using `ConnectionStrings:Sqlite` (fallback `Data Source=launchpadstarter.db`).
- Default provider is `SqlServer` if `Database:Provider` is not set.
- Development configuration (`src/LaunchpadStarter.Api/appsettings.Development.json`) now sets:
  - `Database:Provider=Sqlite`
  - `ConnectionStrings:Sqlite=Data Source=launchpadstarter.dev.db`

## 9) Catalog Feature Matrix

- Create product (authorized)
- Get product by id (anonymous, cached)
- Search products with filters + pagination (anonymous, cached)
- Update product (authorized, cache invalidation)
- Deactivate product (authorized, cache invalidation)

Product fields:
- `Id`, `Sku`, `Name`, `Description`, `Price`, `Currency`, `IsActive`, `CreatedAt`, `UpdatedAt`

## 10) Domain Model Notes

- Base `Entity` tracks domain events in-memory.
- `DomainEvents` is marked `[NotMapped]` to prevent EF Core from trying to persist this collection.

## 11) Testing Architecture (Current)

- Integration tests spin up SQL Server and Redis via Testcontainers.
- Test host overrides auth to `TestAuthHandler` and grants `scope=catalog.write`.
- Test host replaces distributed cache with in-memory cache for deterministic behavior.
- Current integration tests still attach bearer tokens in requests, but authentication is handled by the injected `Test` scheme.

## 12) Why This LaunchpadStarter Works Well Publicly

- Demonstrates clear architectural separation instead of a monolithic API layer.
- Shows real production concerns (auth, caching, health, observability, rate limits).
- Includes both unit and integration testing strategy.
- Uses pragmatic CQRS without overengineering.

## 13) Recent Commit Impact (Latest)

Latest updates now reflected in this document:

- `c6587fc`: integration tests simplified authorization header setup.
- `0ca9dd1`: CI image handling tightened and test auth handler introduced.
- `caf73ff`: integration test token generation now reads JWT params from configuration.
- `88ed9ac`: integration tests use custom auth wiring + in-memory distributed cache.
- `8b668e3`: integration test Redis connection format and SQL image alignment updated.
- `1a4c602`: SQLite provider support added and wired into development config.
- `3505a14`: CI pre-pull step added for integration test container images.
- `e8e8e3b`: `[NotMapped]` applied to `Entity.DomainEvents`.

## 14) 30-Second Pinned-Repo Pitch

Production-oriented .NET 9 Clean Architecture API launchpad starter with a complete Catalog vertical slice. It combines Minimal APIs, CQRS with MediatR, FluentValidation pipeline behaviors, EF Core persistence with SQL Server/SQLite support, Redis caching with explicit invalidation, JWT scope-based authorization, health checks, rate limiting, structured logging, Dockerized local stack, and CI-backed unit/integration tests.
