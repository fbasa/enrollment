#Rationale Notes (key choices, 1–2 sentences each)

#Vertical slices with MediatR: 
Keeps each feature’s commands/queries cohesive and testable; reduces cross-module coupling and aligns with “teachability” via focused files.

Dapper over EF Core: Predictable SQL, minimal overhead, and first-class control of indexes/TVPs to meet performance goals and avoid N+1.

DbUp migrations: Simple, reliable SQL-first migrations that match our Dapper posture and are easy to run in CI/CD and docker-compose.

Optimistic concurrency (ROWVERSION): Prevents lost updates for offerings/enrollments; clients resolve 409s explicitly—great for teaching real-world concurrency.

Snapshot isolation (RCSI) + UPDLOCK on enroll: Balances concurrency with correctness; we lock the offering row only for short capacity calculations while readers remain non-blocking.

Polly for DB resiliency: Retries on transient SQL errors (e.g., failovers); centralized policy improves reliability without littering handlers.

JWT + role/policy auth: Simple local identity or external IdP; policies enable business overrides (capacity/prereq waiver) with auditable reasons.

ProblemDetails + correlation IDs: Consistent error shape and end-to-end X-Correlation-Id for faster support and traceability across UI/API/logs.

Keyset pagination + capped page sizes: Stable, high-performance paging for large datasets with RFC 5988 Link headers for navigability.

OpenTelemetry + Serilog: Standardized traces/metrics and structured logs; demo-friendly with Seq/Jaeger locally, OTLP-ready for Azure Monitor.

Signals-first Angular state: Fine-grained reactivity with minimal boilerplate; ideal for optimistic updates in enrollment flows.

Async validators & debounced querying: Good UX and reduced load for catalog searches and prereq checks.

Feature-flagged Payments: Cleanly optional without branching complexity; keeps core enrollment domain crisp for teaching.

Idempotent POSTs (idempotency keys): Safe retries from flaky networks/clients; vital for create-enrollment and payment operations.

Least-privilege DB user & secrets hygiene: App role limited to needed verbs; secrets via env/Key Vault—no PII in logs.

UTC in DB, local render (Asia/Manila): Avoids DST/locale pitfalls; UI helpers convert for display and input.

API Versioning (v1): Enables additive evolution; students see real versioning patterns in practice.

Testcontainers SQL + Playwright e2e: Reproducible integration tests and realistic e2e coverage for enroll/drop/waitlist promotion.
