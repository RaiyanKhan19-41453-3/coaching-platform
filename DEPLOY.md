# Deployment runbook — Coaching Platform

No code changes are required to deploy. Fill `.env` from `.env.example`, then run the production overlay.

## Required environment

| Variable | Notes |
|---|---|
| `POSTGRES_PASSWORD` | Strong DB password |
| `MINIO_ROOT_PASSWORD` | >= 8 chars |
| `JWT_KEY` | >= 32 chars, not the dev default; must match the exam service |
| `MINIO_PUBLIC_ENDPOINT` | Browser-reachable MinIO URL |
| `WEB_ORIGIN` | Deployed web origin for CORS |

Optional: `JWT_ISSUER`, `JWT_AUDIENCE`, `SEQ_URL`, `OTEL_ENDPOINT`, `RATE_TENANT_LIMIT`,
`RATE_AUTH_LIMIT`, `EXAM_INTEGRATION_KEY`, `PGADMIN_*`, ports.

## Deploy

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

A one-shot `migrate` job applies EF migrations before the API starts.

## Verify

```bash
curl -fsS http://<host>:18081/health/ready
curl -fsS http://<host>:18081/openapi/v1.json
curl -fsS http://<host>:13000/
```

## Switching object storage to AWS S3

Set `STORAGE_PROVIDER=s3`, `AWS_REGION`, `AWS_BUCKET`, and either an IAM role or
`AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`. No code change or redeploy of images is needed — the
API selects `S3ObjectStorage` from the `IObjectStorage` interface at startup. Copy existing
objects from MinIO to the bucket (storage keys in Postgres stay identical), set bucket CORS for
the web origin, and grant `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject`, `s3:ListBucket`.
Presigned URLs stored in `Asset.Url` expire and regenerate automatically. The MinIO container
can then be stopped.

## Integration

The exam service posts published results to `POST /api/integrations/exam-results` with header
`X-Integration-Key: <EXAM_INTEGRATION_KEY>`. Enable it with `RESULT_WEBHOOK` on the exam service.

## Scaling

API is stateless; run replicas behind a load balancer with identical env. Redis backs the rate
limiter and SignalR, Hangfire coordinates through Postgres. Postgres/Redis/MinIO remain single
services until you add HA for them.

## Rollback

Pin the previous GHCR image tag (`ghcr.io/<owner>/coaching-platform-api:<sha>`), restore the last
Postgres/MinIO backup, and restart.
