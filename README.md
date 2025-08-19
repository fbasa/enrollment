# Notes

## Vertical slices with MediatR: 
Keeps each feature’s commands/queries cohesive and testable; reduces cross-module coupling and aligns via focused files.

## Dapper over EF Core: 
Predictable SQL, minimal overhead, and first-class control of indexes/TVPs to meet performance goals and avoid N+1.

## DbUp migrations: 
Simple, reliable SQL-first migrations that match our Dapper posture and are easy to run in CI/CD and docker-compose.

## Optimistic concurrency (ROWVERSION): 
Prevents lost updates for offerings/enrollments; clients resolve 409s explicitly—great for teaching real-world concurrency.

## Snapshot isolation (RCSI) + UPDLOCK on enroll: 
Balances concurrency with correctness; we lock the offering row only for short capacity calculations while readers remain non-blocking.

## Polly for DB resiliency: 
Retries on transient SQL errors (e.g., failovers), centralized policy improves reliability without littering handlers.

## JWT + role/policy auth: 
Simple local identity or external IdP; policies enable business overrides (capacity/prereq waiver) with auditable reasons.

## ProblemDetails + correlation IDs: 
Consistent error shape and end-to-end X-Correlation-Id for faster support and traceability across UI/API/logs.

## Keyset pagination + capped page sizes: 
Stable, high-performance paging for large datasets with RFC 5988 Link headers for navigability.

## OpenTelemetry + Serilog: 
Standardized traces/metrics and structured logs; demo-friendly with Seq/Jaeger locally, OTLP-ready for Azure Monitor.

## Feature-flagged domains: 
Cleanly optional without branching complexity; keeps core enrollment domain simple.

## Idempotent POSTs (idempotency keys): 
Safe retries from flaky networks/clients; vital for create-enrollment and payment operations.

## Least-privilege DB user & secrets hygiene: 
App role limited to needed verbs; secrets via env/Key Vault-no PII in logs.

## API Versioning (v1): 
Semantic change management with Microsoft.AspNetCore.Mvc.Versioning; OpenAPI groups per version.

## Output Caching
- HTTP Output Caching (fastest win for GETs). 
- MediatR Query Caching (feature-level, testable). 
- Entity ETags + 304 (network-level efficiency). 

## Rate limiting: 
Token/leaky-bucket (per-IP/tenant) caps abusive patterns on sensitive endpoints (auth/enrollments).

## Real-time updates
Real-time enrollment updates end-to-end with SignalR.

## Messaging Queue (RabbitMQ + SendGrid)
- Real email messaging queue with a safe, production-ready pattern:
- Outbox table (transactional write with your business change) 
- Dispatcher (polls outbox - enqueues to MQ)
- Queue (RabbitMQ by default; in-memory fallback)
- Worker (consumes queue - sends email via SendGrid)

## Global Exception Handler 
Consistent responses: always RFC 7807 with type, title, detail, status, instance, correlationId, traceId, and (when applicable) a per-field errors dictionary.Smart DB mapping: unique key - 409, deadlock/timeout - 503 (+ retry signal), login/db open issues properly classified.

* Notes & guidance
- Safety: Idempotency should be used only for side-effecting commands where repeat submissions must not duplicate effects. Reads don’t need it.
- Locks: With a Redis multiplexer, concurrent duplicates are serialized (SET NX). Without it, duplicates may still both execute, but retries will replay the cached result—good enough for dev.
- TTL: Tune Idempotency:StoreTtl (e.g., 10–60 min) based on your client’s retry window.
- Key scope: We scope by method + path + user + key + requestHash. This avoids collisions across routes/users and prevents accidental replay with a different payload.
- Clients must send the header: X-Idempotency-Key: 8d5e0a6f-8a5a-4b04-a0a2-0c6b1c5f6f9a
- Errors: If a duplicate comes while the first is in flight, we wait briefly for the result; if still not available, we throw a 409-style exception (currently InvalidOperationException; you can replace with your ConflictException).
