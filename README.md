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
| Redis | localhost:16379 | Cache + SignalR backplane candidate |

Optional services:

```bash
docker compose --profile storage up -d   # MinIO object storage (:9000, console :9001)
docker compose --profile tools up -d     # pgAdmin (:5050)
```

Migrations are applied and demo data is seeded automatically by the API at startup (with retry while Postgres boots).

## Solution layout

```
src/CoachingPlatform.Domain        Pure domain: entities, value objects, enums (no EF dependency)
src/CoachingPlatform.Infrastructure  EF Core 10 model, configurations, audit interceptor, Npgsql
src/CoachingPlatform.Api           Minimal API, tenant resolution, health checks, SignalR hub
web                                React + Vite + TypeScript, Nginx proxy
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

## EF tooling

Tool manifest is committed at `.config/dotnet-tools.json`. To add a migration after changing the model:

```bash
docker run --rm -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 \
  bash -lc 'dotnet tool restore && dotnet ef migrations add MyMigration \
    --project src/CoachingPlatform.Infrastructure \
    --startup-project src/CoachingPlatform.Api'
```
