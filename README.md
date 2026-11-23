# Coaching Platform

A multi-tenant coaching-center management system built as a **.NET 10 modular monolith** with a **React** frontend. Everything runs in Docker — no local SDK, Node or database install required.

## Run it

```bash
cp .env.example .env          # optional; sensible defaults are built in
docker compose up -d --build
```

| Service | URL | Notes |
| --- | --- | --- |
| Web | http://localhost:13000 | React (Vite build served by Nginx) |
| API | http://localhost:18081 | ASP.NET Core, OpenAPI-ready minimal API |
| Health | http://localhost:18081/health/ready | Postgres readiness |
| SignalR | http://localhost:18081/hubs/notifications | Real-time hub |
| Postgres | localhost:15432 | `coaching` / `coaching` |
| Redis | localhost:16379 | SignalR backplane for multi-replica push |

Optional services:

```bash
docker compose --profile storage up -d   # MinIO object storage (:9000, console :9001)
docker compose --profile tools up -d     # pgAdmin (:5050)
```

Migrations are applied and demo data is seeded automatically by the API at startup (with retry while Postgres boots).

## Solution layout

```
src/CoachingPlatform.Domain          Pure domain: entities, value objects, enums (no EF dependency)
src/CoachingPlatform.Application     CQRS command/query slices, MediatR pipeline, FluentValidation, auth commands
src/CoachingPlatform.Infrastructure  EF Core 10 model, configurations, audit interceptor, Npgsql
src/CoachingPlatform.Api             Minimal API, JWT, tenant resolution, health checks, SignalR, Hangfire jobs
web                                  React + Vite + TypeScript, Nginx proxy
```

## Domain modules

| Module | Aggregates |
| --- | --- |
| Tenancy | `Tenant`, `TenantSetting`, `FeatureFlag` |
| Identity | `User`, `TenantMembership`, `Role`, `Permission`, `RolePermission`, `UserRole`, `RefreshToken`, `StudentGuardian` |
| Academics | `AcademicYear`, `Term`, `Course`, `Subject`, `Topic`, `Batch`, `BatchTeacher`, `Enrollment`, `BatchTopic`, `LearningMaterial` |
| Scheduling | `Room`, `ScheduleRule`, `ClassSession`, `Attendance`, `LeaveRequest` |
| Assessment | `Question`, `Assessment`, `AssessmentQuestion`, `AssessmentAttempt`, `StudentAnswer`, `GradeEntry`, `Rubric`, `RubricCriterion` |
| Finance | `FeeStructure`, `Invoice`, `InvoiceLine`, `Payment`, `Discount`, `Scholarship`, `LedgerEntry`, `Expense`, `Refund` |
| Communication | `Announcement`, `Notification`, `NotificationPreference`, `Conversation`, `ConversationParticipant`, `Message`, `MessageReceipt` |
| CRM | `Lead`, `AdmissionApplication`, `ApplicationDocument`, `FollowUp` |
| Content | `Asset` |
| Platform | `AuditLog`, `OutboxMessage` |

See [docs/DOMAIN-MODEL.md](docs/DOMAIN-MODEL.md) for the design decisions behind the schema.

## Cross-cutting design

- **Tenant isolation** — every tenant row carries `TenantId`; `AppDbContext` builds a global query filter per entity and the audit interceptor stamps new rows. Unique indexes are composite with `TenantId` and filtered on `IsDeleted = false`.
- **Soft delete** — `ISoftDeletable` with `DeletedAt`/`DeletedBy`, honoured by the same global filter.
- **Concurrency** — every mutable row carries a `RowVersion` token; the audit interceptor rotates it on each write so EF emits optimistic-concurrency `WHERE` clauses (409 on conflict).
- **Money** — `Money` is an owned type (`decimal(18,2)` + ISO currency) so currency never gets lost.
- **Audit** — `AuditInterceptor` appends an `AuditLog` row with a JSON change set for every insert/update/delete.
- **Transactional outbox** — `OutboxMessage` is the pattern for reliable domain events (notification dispatch, ledger posting).
- **Indexing** — every foreign key and high-traffic query path has an explicit index.

## CQRS slices

`ISender.Send(...)` is the only way into the write model. Each slice is one file containing its
request, FluentValidation validator and handler:

| Area | Commands / queries |
| --- | --- |
| Academics | `CreateCourse`, `CreateBatch`, `EnrollStudent`, `GetBatches` |
| Scheduling | `CreateScheduleRule`, `GenerateSessions`, `MarkAttendance`, `GetSessionAttendance` |
| Assessment | `CreateAssessment`, `SubmitAttempt` (auto-grades objective types, writes `GradeEntry`) |
| Finance | `CreateInvoice`, `RecordPayment` (updates invoice, posts double-entry ledger rows) |

MediatR pipeline: `LoggingBehavior` → `TenantGuardBehavior` → `ValidationBehavior` → handler.
`IRequiresTenant` requests are rejected with 403 when no tenant is resolved.

## Authentication

All `/api/*` business endpoints require a bearer token. Auth endpoints are public:

```bash
# 1. Provision a center + admin in one transaction
curl -X POST localhost:18081/api/auth/register-tenant -H 'Content-Type: application/json' \
  -d '{"name":"Star Coaching","slug":"star","adminEmail":"admin@star.local","adminPassword":"Password123!","adminFirstName":"Alice","adminLastName":"Admin"}'

# 2. Login (tenantSlug optional when the user has one membership)
curl -X POST localhost:18081/api/auth/login -H 'Content-Type: application/json' \
  -d '{"email":"admin@star.local","password":"Password123!","tenantSlug":"star"}'

# 3. Use the accessToken as: Authorization: Bearer <token>
curl localhost:18081/api/auth/me -H 'Authorization: Bearer <token>'
```

- Access tokens are 30-minute HS256 JWTs carrying `sub`, `email`, `tenant_id` and `role` claims.
- Refresh tokens are stored **hashed**, expire in 14 days and **rotate** on every refresh (old token is revoked).
- Passwords use PBKDF2-SHA256 with a per-user salt and constant-time comparison.
- Override `Jwt__Key` in `.env` for anything but local development.

## Background jobs (Hangfire)

Hangfire uses its own `hangfire` schema in the same Postgres database. Recurring jobs:

| Job | Schedule | Purpose |
| --- | --- | --- |
| `outbox-dispatch` | every minute | Drains `OutboxMessages`, dispatches to typed handlers, stamps `ProcessedAt`; retries 5× then dead-letters |
| `refresh-token-cleanup` | daily | Deletes refresh tokens expired more than 30 days ago |

Events handled today: `tenant.provisioned`, `enrollment.created`, `attendance.marked`,
`assessment.submitted`, `invoice.created`, `payment.received`. Each event and its `ProcessedAt`
stamp commit in **one transaction**, so a retry cannot duplicate side effects.

Dashboard: http://localhost:18081/jobs (Development only). Manual drain:
`POST /api/jobs/outbox/dispatch` with a bearer token.

## Reporting read models

Write tables are never queried by dashboards. Four projection tables are maintained from the
outbox and a nightly rebuild:

| Projection | Grain | Fed by |
| --- | --- | --- |
| `AttendanceStat` | student × batch × month | `attendance.marked` |
| `FeeSummary` | student × month | `invoice.created`, `payment.received` |
| `BatchUtilization` | batch | `enrollment.created` |
| `GradeSummary` | student × subject | `assessment.submitted` |

Endpoints (JWT required, read-only, no-tracking):

```
GET /api/reports/dashboard
GET /api/reports/attendance?batchId=&studentId=&from=&to=
GET /api/reports/fees?studentId=&from=&to=
GET /api/reports/utilization
GET /api/reports/grades?studentId=&subjectId=
POST /api/jobs/projections/rebuild     # manual/nightly repair
```

The `projection-rebuild` Hangfire job (daily) recomputes every projection from the write tables,
so backfills and drift repair are a single operation. Incremental handlers and the rebuild
normalise periods to the first of the month, so they produce identical rows.

## Tests

```bash
./scripts/run-tests.sh
```

21 tests run in a .NET SDK container; Testcontainers starts a real Postgres 17 container via
the host Docker socket (`--network host` lets the test process reach it). Integration tests
exercise the real EF model, global query filters, audit interceptor, MediatR pipeline and
handlers — no mocked DbContexts. Unit tests cover `Money` and PBKDF2 password hashing.

## Realtime notifications

- `NotificationHub` is authenticated and puts each connection in a `user:{userId}` group
  (plus `tenant:{tenantId}`).
- After an outbox handler commits its notification, `OutboxDispatcher` pushes the payload to
  that user's group — **after** the transaction commits, so a transport failure cannot cause a
  duplicate notification on retry.
- SignalR uses a Redis backplane (`Redis:ConnectionString`) so messages fan out across API
  replicas. Remove that setting to run without Redis.
- The React container signs in, opens `/hubs/notifications` with the access token, and shows
  live notifications. Nginx proxies the WebSocket upgrade.

End-to-end check:

```bash
docker run --rm --network host -v "$PWD":/work -w /work node:22-alpine \
  sh -c 'npm install --no-save @microsoft/signalr@10.0.11 >/dev/null && node scripts/verify-realtime.mjs'
```

## EF tooling

Tool manifest is committed at `.config/dotnet-tools.json`. To add a migration after changing the model:

```bash
docker run --rm -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc 'dotnet tool restore && dotnet ef migrations add MyMigration \
    --project src/CoachingPlatform.Infrastructure \
    --startup-project src/CoachingPlatform.Api'
```
