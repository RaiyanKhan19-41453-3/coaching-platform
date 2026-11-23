# Architecture & contributor guide

## Dependency rule

```
Api  ──▶  Application  ──▶  Domain
 │             │
 └──▶ Infrastructure ──▶  Domain / Application contracts
```

- `Domain` depends on nothing. It contains entities, value objects, enums and pure policies
  (`Domain/Services`). No EF, no ASP.NET, no DI.
- `Application` owns use cases. It depends only on Domain plus small abstractions
  (`IApplicationDbContext`, `ICurrentUser`, `IObjectStorage`, `ITokenGenerator`). It never
  references EF Core types or ASP.NET Core.
- `Infrastructure` implements those abstractions (EF Core, MinIO, interceptors, projections).
- `Api` is a thin delivery layer: **controllers only**. It references Application interfaces,
  never MediatR handlers or EF directly.

## Request flow (every feature follows this)

```
HTTP → Controller → I*Service (DTO in/out) → MediatR Command/Query → Handler → IApplicationDbContext → EF Core → Postgres
                                                        └── OutboxMessage → Hangfire handler → projection / notification → SignalR
```

1. Add the API contract to `Application/DTOs/<Module>`.
2. Add the method to `I<Module>Service` and map it in `<Module>Service` to a command/query.
3. Add the command/query + validator + handler in `Application/Features/<Module>`.
4. Add or extend the controller action in `Api/Controllers`.
5. Add tests at the layer you changed (domain policy unit test, handler integration test,
   service contract test). Controllers stay trivial and do not need unit tests.

## Adding a new bounded context

Create `Domain/<Context>`, `Application/Features/<Context>`, `Application/DTOs/<Context>`,
`Application/Services/I<Context>Service`, `Api/Controllers/<Context>Controller`. Register the
service in `ServiceCollectionExtensions.AddApplication()`.

## Extracting a module into a microservice

Boundaries are already explicit, so extraction is mechanical:

1. Copy `Domain/<Context>` + `Application/Features/<Context>` + `DTOs/<Context>` into the new
   service; keep `IApplicationDbContext` (or replace it with that service's own DbContext).
2. Replace the in-process `<Context>Service` with an HTTP/gRPC client implementing the same
   `I<Context>Service` interface in the monolith's API layer — controllers do not change.
3. Replace outbox handlers for that module with a queue publisher/consumer; the outbox payload
   is already a plain JSON event, so consumers need no EF types.
4. Keep the module's tables in their own schema/database; cross-module reads must go through
   the extracted service's API or published events, never direct table joins.

## Testing

- `tests/CoachingPlatform.Tests/Unit` — domain policies, Money, password hashing
- `tests/CoachingPlatform.Tests/Integration` — real Postgres via Testcontainers; handlers,
  services, tenant isolation, concurrency, projections, audit
- `./scripts/run-tests.sh` runs everything inside Docker with no local SDK
