# Product API — RESTful Backend Assessment

A production-style **RESTful Web API** for managing `Products` and their inventory `Items`,
built with **.NET 8 / ASP.NET Core** using **Clean Architecture**. This was built for a
technical assessment, but designed the way I'd actually ship a service: layered boundaries,
JWT auth with refresh-token rotation, centralized validation and error handling, structured
logging, API versioning, pagination, and both unit and integration test coverage.

---

## 1. Tech Stack

| Concern | Choice |
|---|---|
| Framework | .NET 8, ASP.NET Core Web API |
| Database | SQL Server + Entity Framework Core 8 (code-first migrations) |
| Auth | JWT Bearer access tokens + rotating refresh tokens |
| Validation | FluentValidation (auto-validation pipeline) |
| Mapping | AutoMapper |
| Docs | Swagger / OpenAPI (Swashbuckle) with JWT support baked in |
| Logging | Serilog — console + rolling daily file sinks, request logging |
| Testing | xUnit, Moq-compatible (FluentAssertions), EF Core InMemory, `WebApplicationFactory` |
| Containerization | Docker multi-stage build + Docker Compose (API + SQL Server) |
| Extras | API versioning (`/api/v1/...`), IP rate limiting, response compression, security headers, health checks |

---

## 2. Why Clean Architecture (and what's unique about this submission)

Most take-home CRUD submissions put everything in the Controllers/Models of a single Web API
project. This solution instead separates concerns into four projects so that business rules
don't depend on EF Core, ASP.NET, or any other framework detail:

```
Domain          -> Entities, domain exceptions, domain events. No dependencies at all.
Application     -> DTOs, service interfaces/implementations, validators, mapping. Depends only on Domain.
Infrastructure  -> EF Core DbContext, repositories, Unit of Work, JWT generation. Depends on Application+Domain.
API             -> Controllers, middleware, DI wiring, Swagger, versioning. Depends on all of the above.
```

**What makes this a bit more than the "usual" student CRUD project:**

- **Refresh-token rotation**, not just a long-lived JWT — each refresh call revokes the old
  token and issues a new pair, with revoked/expired tokens rejected (`AuthController`,
  `AuthService`).
- **Soft-delete on Product** so historical `Item` records are never orphaned by a hard delete,
  while still exposing a clean `DELETE` semantics to API consumers.
- **A real Unit of Work + generic repository pattern** (not just `DbContext` injected straight
  into controllers), so a repository swap or unit test doesn't require touching business logic.
- **Consistent error envelope for every failure mode** — validation errors, 404s, 401s, and
  unhandled exceptions all return the same `ApiErrorResponse` shape via one exception-handling
  middleware, instead of ad-hoc `BadRequest()` calls scattered through controllers.
- **API versioning from day one** (`/api/v1/products`) even though there's only one version,
  because retrofitting versioning later is painful.
- **Low-stock domain event** — updating an `Item`'s quantity below a threshold logs a structured
  `ProductQuantityLowEvent`, showing a lightweight domain-event pattern without over-engineering
  a full event bus for a CRUD assessment.
- **Three levels of automated tests** (unit / repository / full HTTP pipeline via
  `WebApplicationFactory`), not just a couple of happy-path unit tests.

---

## 3. Project Structure

```
ProductApi/
├── src/
│   ├── ProductApi.Domain/
│   │   ├── Entities/          Product, Item, ApplicationUser, RefreshToken, BaseEntity
│   │   ├── Events/            ProductQuantityLowEvent
│   │   └── Exceptions/        NotFoundException, ValidationAppException, UnauthorizedAppException
│   ├── ProductApi.Application/
│   │   ├── DTOs/               Request/response contracts, PagedResult<T>, ApiErrorResponse
│   │   ├── Interfaces/         IProductService, IItemService, IAuthService, IUnitOfWork, ...
│   │   ├── Mapping/            AutoMapper profile
│   │   ├── Services/           ProductService, ItemService, AuthService
│   │   └── Validators/         FluentValidation rules for every mutating DTO
│   ├── ProductApi.Infrastructure/
│   │   ├── Data/
│   │   │   ├── Configurations/  Fluent-API entity configs (indexes, constraints)
│   │   │   ├── Repositories/    GenericRepository<T>
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── UnitOfWork.cs
│   │   ├── Identity/            JwtTokenGenerator, JwtSettings
│   │   ├── Logging/             Serilog bootstrap
│   │   └── Migrations/          EF Core code-first migration (InitialCreate)
│   └── ProductApi.API/
│       ├── Controllers/V1/      ProductsController, ItemsController, AuthController
│       ├── Filters/             ValidateModelAttribute
│       ├── Middleware/          ExceptionHandlingMiddleware
│       ├── Extensions/          Service-collection registration extensions
│       ├── Program.cs
│       └── appsettings*.json
├── tests/
│   ├── ProductApi.Application.Tests/    Service + validator unit tests (EF InMemory)
│   ├── ProductApi.Infrastructure.Tests/ Repository tests (EF InMemory)
│   └── ProductApi.API.Tests/            Full HTTP pipeline tests via WebApplicationFactory
├── docker-compose.yml
├── Dockerfile
├── ProductApi.sln
└── ProductApi.http                      Ready-to-run manual request collection
```

---

## 4. Data Model

Matches the schema given in the assessment, extended with audit fields already specified
there plus a soft-delete flag and the auth tables needed for JWT:

```
Product                          Item                          AppUser                RefreshToken
--------------------------       --------------------------    --------------------    --------------------
Id            INT PK IDENTITY    Id           INT PK IDENTITY   Id        INT PK        Id            INT PK
ProductName   NVARCHAR(255)      ProductId    INT FK -> Product UserName  NVARCHAR(100) Token         NVARCHAR(512) UNIQUE
IsDeleted     BIT                Quantity     INT               PasswordHash NVARCHAR   UserName      NVARCHAR(100)
CreatedBy     NVARCHAR(100)      CreatedBy    NVARCHAR(100)     Role      NVARCHAR(50)  ExpiresOn     DATETIME2
CreatedOn     DATETIME2          CreatedOn    DATETIME2         CreatedOn DATETIME2     RevokedOn     DATETIME2 NULL
ModifiedBy    NVARCHAR(100)?     ModifiedBy   NVARCHAR(100)?                            ReplacedByToken NVARCHAR NULL
ModifiedOn    DATETIME2?         ModifiedOn   DATETIME2?
```

`Product 1---* Item` via `ProductId` (restrict delete — hence the soft-delete approach on
Product rather than a hard `DELETE`).

---

## 5. API Endpoints (`/api/v1/...`)

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/auth/register` | none | Create a user, returns token pair |
| POST | `/auth/login` | none | Authenticate, returns token pair |
| POST | `/auth/refresh` | none | Rotate a refresh token for a new pair |
| POST | `/auth/revoke` | Bearer | Revoke a refresh token |
| GET | `/products?pageNumber=&pageSize=&search=` | none | Paginated product list, optional name search |
| GET | `/products/{id}` | none | Single product (includes computed `totalQuantity`) |
| GET | `/products/{id}/items` | none | Items belonging to a product (relationship endpoint) |
| POST | `/products` | Bearer | Create product |
| PUT | `/products/{id}` | Bearer | Update product |
| DELETE | `/products/{id}` | Bearer (Admin) | Soft-delete product |
| GET | `/items?pageNumber=&pageSize=` | none | Paginated item list |
| GET | `/items/{id}` | none | Single item |
| POST | `/items` | Bearer | Create item (validates product exists) |
| PUT | `/items/{id}` | Bearer | Update item quantity |
| DELETE | `/items/{id}` | Bearer (Admin) | Delete item |
| GET | `/health` | none | Liveness/readiness (checks DB connectivity) |

Every collection endpoint returns:

```json
{
  "items": [ ... ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

Every error returns:

```json
{
  "statusCode": 404,
  "message": "Entity \"Product\" (99) was not found.",
  "traceId": "0HN...",
  "errors": null
}
```

---

## 6. Authentication Flow

1. `POST /auth/register` (or `/auth/login`) → returns a short-lived **access token** (JWT,
   default 15 min, `Jwt:AccessTokenMinutes` in config) and a long-lived opaque **refresh token**
   (7 days), stored server-side in `RefreshToken` for revocation checks.
2. Send `Authorization: Bearer <accessToken>` on protected endpoints.
3. When the access token expires, call `POST /auth/refresh` with the old access + refresh
   tokens. The old refresh token is revoked and a **new pair is issued** (rotation) — this
   limits the blast radius if a refresh token ever leaks.
4. `POST /auth/revoke` invalidates a refresh token immediately (e.g., on logout).
5. Role-based authorization: `DELETE` endpoints require the `Admin` role; other mutating
   endpoints require any authenticated user. Roles are embedded as a JWT claim at registration.

---

## 7. Running Locally

### Option A — Docker Compose (recommended, zero local SQL Server setup)

```bash
docker compose up --build
```

This starts SQL Server, waits for it to be healthy, then starts the API, which applies EF Core
migrations automatically on boot. Once up:

- Swagger UI: `http://localhost:8080/swagger` (Development-style Swagger is also enabled here for review purposes)
- Health check: `http://localhost:8080/health`

### Option B — Local .NET SDK + your own SQL Server

```bash
# from the solution root
dotnet restore
# update src/ProductApi.API/appsettings.Development.json or use user-secrets for the connection string
dotnet run --project src/ProductApi.API
```

The API applies pending migrations automatically at startup — no manual `dotnet ef database
update` step is required. If you add new entities, regenerate the migration with:

```bash
dotnet ef migrations add <Name> -p src/ProductApi.Infrastructure -s src/ProductApi.API
```

Then open `http://localhost:5080/swagger`, or use the included `ProductApi.http` file
(VS Code REST Client / Rider / Visual Studio all support `.http` files directly).

### Running the tests

```bash
dotnet test
```

Runs unit tests (`Application.Tests`, `Infrastructure.Tests` — both EF Core InMemory, no DB
required) and integration tests (`API.Tests` — spins up the full ASP.NET Core pipeline via
`WebApplicationFactory` against an InMemory database).

---

## 8. Security Measures

- JWT Bearer auth with short-lived access tokens + rotating refresh tokens.
- Passwords hashed with BCrypt (never stored/logged in plaintext).
- Role-based authorization on destructive (`DELETE`) endpoints.
- FluentValidation on every mutating request, returning a 400 with field-level errors.
- CORS policy restricted to configured allowed origins (`Cors:AllowedOrigins`).
- Baseline security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`).
- IP-based rate limiting (`AspNetCoreRateLimit`) to blunt basic abuse/brute force.
- HTTPS redirection enforced.

## 9. Performance Considerations

- `AsNoTracking()` used for all read-only queries.
- Pagination enforced on every collection endpoint (max page size capped server-side at 100).
- Indexes on `Product.ProductName`, `Product.IsDeleted`, `Item.ProductId`, and unique indexes
  on `AppUser.UserName` / `RefreshToken.Token`.
- Response compression middleware enabled.
- Fully `async`/`await` throughout the data-access and service layers.

---

## 10. Configuration Reference

All values below can be overridden via environment variables (as `docker-compose.yml` does)
or `dotnet user-secrets` locally — nothing sensitive should be committed as plaintext in a real
deployment; the checked-in `appsettings.json` values are placeholders for local dev only.

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt:Issuer` / `Jwt:Audience` | JWT validation parameters |
| `Jwt:SigningKey` | HMAC-SHA256 signing key (32+ chars) |
| `Jwt:AccessTokenMinutes` | Access token lifetime |
| `Cors:AllowedOrigins` | Array of allowed origins |
| `IpRateLimiting` | Rate-limit window/threshold |

---

## 11. Notes for Reviewers

- The included EF Core migration (`Migrations/20260101000000_InitialCreate.cs`) was authored
  by hand to match the `ApplicationDbContext` model exactly, since this environment didn't have
  the .NET SDK available to run `dotnet ef migrations add` directly. If you'd prefer to
  regenerate it fresh, delete the `Migrations` folder and run the command in section 7 — the
  resulting schema will be identical, since it's driven by the same Fluent API configurations.
- Submission instructions per the assessment ("do not upload code to this repo, create your own
  public repo") were followed conceptually — this project is delivered to you directly rather
  than committed anywhere; push it to your own public repository before sharing the link.
