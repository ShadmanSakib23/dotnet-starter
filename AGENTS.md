# StarterApp — Agent Instructions

ASP.NET Core 10 REST API with JWT auth, PostgreSQL, and EF Core. Clean layered architecture.

## Build & Run

```bash
dotnet restore
dotnet build
docker-compose up -d          # Start PostgreSQL (port 5435 → 5432)
dotnet ef database update     # Apply migrations (required before first run)
dotnet run                    # http://localhost:5000 / https://localhost:5001
```

Health checks: `GET /health`, `GET /db/test`

## Architecture

```
Controllers → Services → Repositories → EF Core (PostgreSQL)
```

| Layer | Location | Role |
|-------|----------|------|
| Controllers | `Controllers/` | HTTP entry point, exception→HTTP status mapping |
| Services | `Services/` | Business logic, auth checks |
| Repositories | `Repositories/` | Data access only |
| Models | `Models/` | EF Core entities |
| DTOs | `DTOs/` | API contracts (never expose models directly) |

Interfaces live alongside implementations. DI is wired in `Extensions/ServiceCollectionExtensions.cs`.

## Key Conventions

- **DateTime**: Always `DateTime.UtcNow`, never `DateTime.Now`
- **Enums**: Stored as strings in DB via `HasConversion<string>()`; serialized as strings via `JsonStringEnumConverter`
- **Decimal**: Use `decimal` for monetary values (amounts are `decimal(18,2)`)
- **Async**: All data operations must be `async/await` — never `.Result` or `.Wait()`
- **Repository base**: `Repository<T>` calls `SaveChangesAsync()` inside `AddAsync()`; don't save separately after `AddAsync`
- **DB column naming**: snake_case (e.g., `user_id`, `processed_at`); UUID PKs default to `gen_random_uuid()`

## Authentication

- JWT with access tokens (15 min) and refresh tokens (7 days)
- Config in `appsettings.json` under `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`; `ClockSkew = TimeSpan.Zero`
- Extract user ID in controllers: check `ClaimTypes.NameIdentifier` then fall back to `"sub"` claim
- Passwords hashed with `IPasswordHasher<User>` — never store plaintext; check with `VerifyHashedPassword()` returning `PasswordVerificationResult`
- **Always** validate that the JWT `userId` matches the resource owner before returning/mutating data

## Database & Migrations

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

- Fluent API config classes in `Data/Configurations/`
- Cascade deletes: deleting a `User` removes all their `Transaction` records
- Indexes on `email`, `username`, `user_id`, `processed_at`

## Adding New Features

When adding a new domain entity, follow this checklist in order:

1. `Models/` — EF Core entity
2. `Data/Configurations/` — Fluent API config, add to `ApplicationDbContext`
3. `DTOs/` — Request/response contracts
4. `Repositories/` — Interface (`IXxxRepository`) extending `IRepository<T>` + implementation
5. `Interfaces/` — Service interface (`IXxxService`)
6. `Services/` — Service implementation
7. `Controllers/` — Controller with `[Authorize]`, `[ProducesResponseType]` attributes
8. `Extensions/ServiceCollectionExtensions.cs` — Register new scoped services/repos
9. `dotnet ef migrations add <Name>` — Generate migration

## Error Handling Pattern

Controllers catch exceptions and return appropriate status codes:

| Exception | Status |
|-----------|--------|
| `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |
| `UnauthorizedAccessException` | 403 |

Custom exceptions are in `Exceptions/`.

## Pitfalls

- App fails to start without `dotnet ef database update`
- JWT secret in `appsettings.json` is for dev only — production needs env vars or a secrets manager
- Specialized queries (filtering, pagination) must go in specialized repositories (`ITransactionRepository`), not the generic base
- `PagedResult<T>` wraps list responses with pagination metadata; always use it for collection endpoints
