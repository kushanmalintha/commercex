# CommerceX — Observability Design

**Document:** 16 — Observability Design  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 15 — Kubernetes Design  
**Next Document:** 17 — Testing Strategy

---

## 1. Purpose

This document defines the observability architecture for CommerceX.

CommerceX consists of an API Gateway, 12 independently deployable services, PostgreSQL databases, Redis, Kafka, Schema Registry, Docker containers, and Kubernetes workloads.

In a distributed system, observing only application logs is insufficient. CommerceX therefore uses three primary observability signals:

1. **Logs** — What happened?
2. **Metrics** — How often/how much/how fast?
3. **Traces** — Where did a request spend time across services?

The initial observability stack is based on:

- OpenTelemetry.
- ASP.NET Core instrumentation.
- Structured logging.
- Distributed tracing.
- Application and infrastructure metrics.
- Grafana dashboards.
- Kubernetes runtime information.

The design is intentionally practical for a learning project and avoids introducing a large enterprise observability platform.

---

## 2. Observability Objectives

CommerceX observability should allow developers to:

1. Determine whether a service is healthy.
2. Identify slow API requests.
3. Trace requests across multiple services.
4. Observe Kafka event processing.
5. Measure Redis cache performance.
6. Monitor database performance.
7. Detect service failures.
8. Detect excessive retries or failures.
9. Monitor Kubernetes Pods and deployments.
10. Correlate logs with distributed traces.
11. Monitor checkout workflow performance.
12. Diagnose failures without accessing application internals manually.
13. Build dashboards in Grafana.
14. Support local development and future CI/CD/deployment environments.

---

## 3. Observability Model

CommerceX follows:

```text
                  CommerceX
                      |
       +--------------+--------------+
       |              |              |
       v              v              v
     Logs          Metrics         Traces
       |              |              |
       +--------------+--------------+
                      |
                      v
                   Grafana
```

OpenTelemetry provides the common instrumentation and telemetry model.

---

## 4. Three Pillars

### 4.1 Logs

Logs describe discrete events.

Examples:

```text
Order created
Payment failed
Redis cache miss
Kafka message processed
Authentication failed
```

### 4.2 Metrics

Metrics provide numerical measurements.

Examples:

```text
HTTP request count
HTTP request duration
HTTP error rate
Kafka consumer lag
Redis cache hit ratio
Database connection usage
Pod CPU/memory
```

### 4.3 Traces

Traces follow a single request or operation across components.

Example:

```text
Client
  |
  v
Gateway
  |
  v
Order Service
  |
  +--> Inventory
  |
  +--> Promotion
  |
  +--> Payment
  |
  +--> Kafka
```

---

## 5. Observability Architecture

The conceptual architecture is:

```text
+----------------------+
| CommerceX Services   |
|                      |
| Gateway              |
| Auth                 |
| User                 |
| Product              |
| Inventory            |
| Cart                 |
| Order                |
| Payment              |
| Shipping             |
| Review               |
| Notification         |
| Promotion            |
| Search               |
+----------+-----------+
           |
           | OpenTelemetry
           v
+----------------------+
| Telemetry Collection |
| / Export             |
+----------+-----------+
           |
           +-----------> Metrics
           |
           +-----------> Traces
           |
           +-----------> Logs
                       |
                       v
                  +---------+
                  | Grafana |
                  +---------+
```

The exact telemetry backend can evolve, but OpenTelemetry remains the instrumentation standard.

---

## 6. OpenTelemetry

OpenTelemetry should be used for application telemetry.

It provides:

- Standardized tracing.
- Metrics instrumentation.
- Context propagation.
- Export mechanisms.
- Consistent telemetry across services.

Each .NET service should configure OpenTelemetry independently while following a common BuildingBlocks approach where useful.

---

## 7. OpenTelemetry Scope

Initial instrumentation should cover:

### ASP.NET Core

- Incoming HTTP requests.
- Request duration.
- HTTP status.
- Route information.

### HttpClient

- Outgoing REST calls.
- Dependency duration.
- Dependency failures.

### gRPC

- Outgoing gRPC calls.
- Incoming gRPC requests.
- RPC status.
- RPC duration.

### PostgreSQL

- Database operations.
- Query duration where supported.
- Database errors.

### Redis

- Redis command activity where instrumentation is available.
- Redis latency/errors.

### Kafka

- Producer activity.
- Consumer processing activity.
- Trace propagation where supported.

---

## 8. Distributed Tracing

A distributed trace should preserve context across service boundaries.

Example:

```text
Trace ID: abc123

Gateway
   |
   +-- Order Service
          |
          +-- Inventory gRPC
          |
          +-- Promotion gRPC
          |
          +-- Payment gRPC
          |
          +-- Kafka publish
```

All spans should share the same trace context when the operation remains part of the same distributed workflow.

---

## 9. Trace Context Propagation

For HTTP communication, standard W3C trace context should be used.

Important headers include:

```text
traceparent
tracestate
```

The application should not invent a custom distributed tracing protocol.

For Kafka, trace context should be propagated through message headers where supported.

For gRPC, OpenTelemetry instrumentation should propagate the context automatically where configured.

---

## 10. Correlation IDs

A correlation ID provides an additional operational identifier.

Example:

```text
X-Correlation-ID: 7b2...
```

The Gateway can accept an incoming correlation ID or create one when absent.

The ID should then be propagated to downstream operations.

Correlation IDs are useful for:

- Log searches.
- Incident investigation.
- Customer-support debugging.

Trace IDs remain the primary distributed tracing mechanism.

---

## 11. Trace ID vs Correlation ID

They serve related but different purposes.

| Identifier | Purpose |
|---|---|
| Trace ID | Connect distributed spans |
| Span ID | Identify an individual operation |
| Correlation ID | Operational/logging correlation |

The project can expose a trace/correlation identifier in API error responses where appropriate.

---

## 12. Structured Logging

All services should use structured logs.

Prefer:

```text
Order created {OrderId} {CustomerId}
```

with structured properties rather than constructing large free-form strings.

Conceptual JSON output:

```json
{
  "timestamp": "2026-09-13T10:00:00Z",
  "level": "Information",
  "service": "Order",
  "event": "OrderCreated",
  "orderId": "123",
  "customerId": "456",
  "traceId": "abc",
  "correlationId": "xyz"
}
```

---

## 13. Log Levels

The project should use conventional log levels.

### Trace

Very detailed diagnostic information.

Use sparingly.

### Debug

Developer-focused diagnostic information.

### Information

Normal important application events.

### Warning

Unexpected but recoverable situations.

### Error

Operation failed or requires attention.

### Critical

Severe failures affecting the service or system.

Production-like environments should avoid excessive Debug/Trace logging.

---

## 14. Logging Guidelines

Logs should answer:

```text
What happened?
Where?
When?
Which operation?
Which resource?
Which trace?
```

Useful properties include:

- Service name.
- Environment.
- Timestamp.
- Event name.
- Trace ID.
- Span ID.
- Correlation ID.
- Resource ID where appropriate.
- Error type.

---

## 15. Sensitive Logging Restrictions

Never log:

- Passwords.
- Password hashes.
- Access tokens.
- Refresh tokens.
- Password-reset tokens.
- JWT signing keys.
- Database passwords.
- Redis credentials.
- Kafka credentials.
- Payment credentials.

Avoid logging complete customer profiles or unnecessary personal information.

This follows the Security Design.

---

## 16. Business Event Logging

Important business operations should produce useful logs.

Examples:

### Auth

```text
User registered
Login succeeded
Login failed
Refresh token revoked
Password reset completed
```

### Product

```text
Product created
Product updated
Product deactivated
```

### Inventory

```text
Stock adjusted
Stock reserved
Stock released
```

### Order

```text
Order created
Order confirmed
Order cancelled
Order shipped
Order delivered
```

### Payment

```text
Payment succeeded
Payment failed
```

---

## 17. Metrics Architecture

Metrics should measure:

```text
Availability
Performance
Errors
Traffic
Dependencies
Business operations
Infrastructure
```

Metrics can be exported through OpenTelemetry and visualized in Grafana.

---

## 18. Standard HTTP Metrics

Each HTTP service should expose metrics for:

- Request count.
- Request duration.
- Request rate.
- Response status.
- Error rate.
- Active requests.

Useful dimensions include:

```text
service
route
method
status
```

Avoid high-cardinality labels such as raw customer IDs.

---

## 19. HTTP Latency

Latency should be measured using histograms where appropriate.

Important percentiles:

```text
p50
p95
p99
```

For example:

```text
Order API p95 latency = 350 ms
```

This is more useful than only tracking average latency.

---

## 20. HTTP Error Metrics

Track:

```text
2xx
4xx
5xx
```

Separately.

Examples:

```text
401 rate
403 rate
404 rate
429 rate
500 rate
```

A spike in 5xx responses can indicate a service or dependency failure.

A spike in 401/403 may indicate authentication/authorization issues.

A spike in 429 may indicate rate limiting or abuse.

---

## 21. Database Metrics

Database-related metrics should include where available:

- Connection count.
- Connection pool utilization.
- Query duration.
- Query failures.
- Timeout count.
- Transaction failures.
- Migration status where operationally useful.

PostgreSQL infrastructure metrics should also be observed.

---

## 22. Redis Metrics

Redis metrics should include:

- Cache hits.
- Cache misses.
- Hit ratio.
- Command latency.
- Connection failures.
- Memory usage.
- Evictions.
- Expired keys.
- Connection count.

For CommerceX, cart operations should also be observable.

---

## 23. Cache Hit Ratio

For product/category caching:

```text
Hit Ratio =
Hits / (Hits + Misses)
```

Example:

```text
Hits:   900
Misses: 100

Hit Ratio = 90%
```

A low hit ratio may indicate ineffective caching or inappropriate TTLs.

---

## 24. Kafka Metrics

Kafka observability is especially important in CommerceX because several workflows are asynchronous.

Important metrics include:

- Messages produced.
- Messages consumed.
- Producer failures.
- Consumer failures.
- Consumer processing duration.
- Consumer lag.
- Retry count.
- Dead-letter count.
- Rebalance events where useful.
- Broker availability.

---

## 25. Consumer Lag

Consumer lag indicates how far a consumer is behind the latest available Kafka messages.

Conceptually:

```text
Latest Offset
      |
      |  <-- lag -->
      |
Consumer Offset
```

Increasing lag may indicate:

- Consumer processing is too slow.
- Consumer is down.
- Too many messages.
- Insufficient partitions/consumers.

---

## 26. Kafka Event Processing Metrics

Each Kafka consumer should measure:

```text
Messages received
Messages successfully processed
Messages failed
Processing duration
Retries
Dead-lettered messages
```

For example:

```text
Notification
  processed = 10,000
  failed = 12
  DLQ = 2
```

---

## 27. Kafka Trace Correlation

Kafka message processing should retain relevant trace/correlation context.

Conceptually:

```text
Order Service
    |
    | publish OrderConfirmed
    | trace context
    v
Kafka
    |
    v
Notification Service
    |
    v
Notification processing span
```

This makes asynchronous workflows easier to diagnose.

---

## 28. Business Metrics

CommerceX should also expose selected business-level metrics.

Examples:

### Orders

- Orders created.
- Orders confirmed.
- Orders cancelled.
- Orders completed.
- Order creation failures.

### Payments

- Payments attempted.
- Payments succeeded.
- Payments failed.

### Inventory

- Reservations.
- Releases.
- Reservation failures.
- Stock adjustments.

### Cart

- Cart creations.
- Cart updates.
- Cart expirations.

### Reviews

- Reviews created.
- Reviews rejected.
- Reviews deleted.

These metrics should not expose unnecessary personal information.

---

## 29. Checkout Observability

Checkout is the most important distributed workflow to observe.

The target trace is:

```text
Client
  |
  v
Gateway
  |
  v
Order
  |
  +--> Product validation
  |
  +--> Promotion
  |
  +--> Inventory reservation
  |
  +--> Payment
  |
  +--> Order confirmation
  |
  +--> Kafka
        |
        +--> Shipping
        +--> Notification
```

The trace should make dependency latency and failures visible.

---

## 30. Checkout Metrics

Important metrics include:

- Checkout attempts.
- Checkout success rate.
- Checkout failure rate.
- Inventory reservation failures.
- Promotion failures.
- Payment failures.
- Checkout latency.
- Orders confirmed.
- Orders cancelled after downstream failures.

Example:

```text
Checkout success rate = confirmed checkouts / checkout attempts
```

---

## 31. Service Health

Each service should provide health endpoints.

Recommended:

```text
/health/live
/health/ready
```

These should be used by Kubernetes.

Health status should distinguish:

```text
Application process healthy
```

from:

```text
Required dependencies healthy
```

---

## 32. Liveness Observability

Liveness should detect severe process-level failure.

Examples:

- Application process is stuck.
- Application cannot serve basic requests.
- Critical internal state is unusable.

It should not depend unnecessarily on optional dependencies.

---

## 33. Readiness Observability

Readiness determines whether the service should receive traffic.

Examples:

```text
Order Service:
  PostgreSQL unavailable → Not Ready

Product Service:
  PostgreSQL unavailable → Not Ready
  Redis unavailable      → May remain Ready

Cart Service:
  Redis unavailable      → Not Ready
```

This follows the dependency classifications from previous documents.

---

## 34. Kubernetes Metrics

Kubernetes runtime metrics should include:

- Pod CPU.
- Pod memory.
- Pod restarts.
- Deployment availability.
- Replica count.
- Ready/unready Pods.
- Resource requests/limits.
- Container failures.

These metrics complement application telemetry.

---

## 35. Grafana

Grafana is the primary visualization tool for CommerceX observability.

Dashboards should provide:

- System overview.
- Service health.
- API performance.
- Kafka.
- Redis.
- PostgreSQL.
- Kubernetes.
- Checkout workflow.

---

## 36. Dashboard 1 — System Overview

The initial dashboard should show:

```text
+--------------------------------------+
| CommerceX System Overview            |
+--------------------------------------+
| Healthy Services | Failed Services  |
| Request Rate     | Error Rate        |
| p95 Latency      | Kafka Lag         |
| Redis Hit Ratio  | Pod Restarts      |
+--------------------------------------+
```

This should provide a quick operational picture.

---

## 37. Dashboard 2 — API Performance

Include:

- Request rate.
- p50/p95/p99 latency.
- 4xx rate.
- 5xx rate.
- Top slow endpoints.
- Top failing endpoints.
- Gateway latency.

Useful service filters:

```text
Gateway
Auth
Product
Cart
Order
...
```

---

## 38. Dashboard 3 — Kafka

Include:

- Topic throughput.
- Consumer lag.
- Consumer failures.
- Processing latency.
- Retry count.
- DLQ message count.

Topics should align with the Kafka Event Design.

---

## 39. Dashboard 4 — Redis

Include:

- Hit ratio.
- Miss rate.
- Memory usage.
- Commands/sec.
- Latency.
- Connection failures.
- Expirations.
- Evictions.

Cart-specific metrics should be visible where useful.

---

## 40. Dashboard 5 — PostgreSQL

Include:

- Connections.
- CPU/memory where available.
- Query latency.
- Transactions.
- Errors.
- Storage usage.
- Slow queries where available.

Service-specific database ownership should remain visible.

---

## 41. Dashboard 6 — Kubernetes

Include:

- Pod count.
- Ready Pods.
- Restart count.
- CPU usage.
- Memory usage.
- Deployment availability.
- Failed Pods.
- Pending Pods.

This helps distinguish application failures from cluster/runtime issues.

---

## 42. Alerting

The initial project should have a small set of meaningful alerts.

Examples:

- Service unavailable.
- High 5xx rate.
- High API latency.
- High Kafka consumer lag.
- Redis unavailable.
- PostgreSQL unavailable.
- Excessive Pod restarts.
- DLQ growth.
- High resource usage.

Do not create dozens of alerts that produce noise.

---

## 43. Alert Severity

A simple model is sufficient:

```text
Critical
Warning
```

### Critical

Requires immediate attention.

Examples:

```text
Order Service unavailable
PostgreSQL unavailable
Kafka unavailable
```

### Warning

Potential issue requiring investigation.

Examples:

```text
High latency
Growing consumer lag
High memory usage
```

---

## 44. Alert Design

Alerts should be based on sustained conditions rather than single transient events where practical.

Bad:

```text
One request failed → alert
```

Better:

```text
5xx rate > threshold for several minutes
```

This reduces alert fatigue.

---

## 45. Sampling Strategy

Distributed tracing can produce large amounts of telemetry.

For the learning project:

- Use a practical sampling rate.
- Retain enough traces to debug development workflows.
- Increase sampling temporarily when investigating a problem.

The exact production sampling strategy is not required for the initial project.

---

## 46. High Cardinality

Metrics labels should avoid unbounded values.

Avoid:

```text
customerId
orderId
productId
email
```

as normal metric labels.

These values can create enormous metric cardinality.

Use them in logs/traces where appropriate instead.

---

## 47. Log Retention

The learning environment does not require long-term log retention.

Retention should balance:

```text
Debugging value
        vs
Storage consumption
```

Development logs can have short retention.

Production-like retention requirements can be defined later.

---

## 48. Trace Retention

Traces should be retained long enough to investigate distributed failures.

For local development, short retention is sufficient.

Important traces include:

- Checkout.
- Authentication.
- Order creation.
- Payment processing.
- Inventory reservation.
- Kafka event processing.

---

## 49. Metrics Naming

Metric names should be consistent and descriptive.

Conceptual examples:

```text
commercex_http_requests_total
commercex_checkout_attempts_total
commercex_checkout_success_total
commercex_payment_failures_total
commercex_inventory_reservations_total
commercex_redis_cache_hits_total
commercex_redis_cache_misses_total
commercex_kafka_messages_processed_total
```

The exact naming should follow the selected OpenTelemetry/.NET conventions where possible.

---

## 50. Service Metadata

Telemetry should identify:

- Service name.
- Service version.
- Environment.
- Instance/container identifier where useful.

Example:

```text
service.name = commercex-order
service.version = 1.0.0
deployment.environment = minikube
```

This makes dashboards and traces easier to filter.

---

## 51. Environment Metadata

At minimum:

```text
Development
Test
Minikube
CI
```

should be distinguishable.

Telemetry from different environments should not be confused.

---

## 52. Error Telemetry

Errors should be represented consistently across:

- Logs.
- Metrics.
- Traces.

For example:

```text
Payment failed
```

should produce:

```text
Log: PaymentFailed
Metric: payment_failures_total += 1
Trace: Payment span status = error
```

This provides multiple ways to investigate the same failure.

---

## 53. Exception Handling

Unhandled exceptions should:

- Produce an error log.
- Mark the relevant trace span as failed.
- Increment appropriate error metrics.
- Return a safe API error response.
- Avoid leaking internal details.

The exception should not cause sensitive information to appear in telemetry.

---

## 54. Dependency Observability

Every important dependency should be observable.

Dependencies include:

```text
PostgreSQL
Redis
Kafka
Schema Registry
gRPC services
REST services
```

For each dependency, observe:

```text
Availability
Latency
Error rate
Timeouts
Retries
```

---

## 55. Timeout Metrics

Timeouts should be distinguishable from general failures.

Examples:

```text
Inventory gRPC timeout
Payment gRPC timeout
PostgreSQL timeout
Redis timeout
Kafka publish timeout
```

This makes downstream dependency problems easier to identify.

---

## 56. Retry Metrics

Retries should be measured.

Example:

```text
commercex_dependency_retries_total
```

Excessive retries may indicate a deeper problem.

Retry loops should never be invisible.

---

## 57. Observability for Idempotency

Important idempotent operations should expose metrics for duplicate requests/events.

Examples:

```text
Duplicate payment request
Duplicate inventory reservation
Duplicate Kafka event
Duplicate order request
```

These metrics help verify that the distributed reliability design is functioning.

---

## 58. Kafka Dead-Letter Observability

DLQ activity should be highly visible.

Track:

```text
DLQ messages
DLQ rate
DLQ by event type
DLQ by consumer
```

A growing DLQ should trigger investigation.

---

## 59. Search Service Observability

Search should monitor:

- Search requests.
- Search latency.
- Search errors.
- Empty-result rate.
- Index synchronization failures.
- Product event processing.
- Search document update latency.

Because Search is eventually consistent with Product, synchronization lag is especially useful.

---

## 60. Notification Service Observability

Notification should monitor:

- Events received.
- Notifications generated.
- Delivery simulations.
- Failed processing.
- Retries.
- DLQ messages.
- Processing latency.

Notification failures should not invalidate an already confirmed order.

---

## 61. Shipping Service Observability

Shipping should monitor:

- Shipment creation.
- Shipment state transitions.
- Kafka events received.
- Event processing failures.
- Processing latency.
- Delivery events.

Shipping state should correlate with Order events.

---

## 62. Auth Service Observability

Auth requires additional security-oriented metrics.

Track:

- Login attempts.
- Successful logins.
- Failed logins.
- Registration attempts.
- Token refresh attempts.
- Refresh failures.
- Password reset requests.
- Password reset successes.
- Authorization failures where appropriate.

Avoid metric labels containing email addresses or other high-cardinality personal data.

---

## 63. Gateway Observability

The Gateway should be the first place to inspect external API behavior.

Track:

- Incoming request rate.
- Route latency.
- Upstream latency.
- Upstream failures.
- 401/403.
- 429 rate-limit responses.
- 5xx errors.
- Active requests.

The Gateway should propagate trace context downstream.

---

## 64. Observability of Health Checks

Health-check endpoints should not overwhelm telemetry.

Health requests can be:

- Excluded from high-level business request metrics where appropriate.
- Identified separately.
- Filtered in dashboards.

Otherwise, Kubernetes probes can dominate request metrics.

---

## 65. Local Development Observability

The developer should be able to:

```text
Start CommerceX
      |
      v
Generate requests
      |
      v
Open Grafana
      |
      +--> View metrics
      +--> View dashboards
      +--> Investigate traces/logs
```

The observability stack should remain usable in Docker/Minikube.

---

## 66. Docker Observability

Containers should:

- Write logs to stdout/stderr.
- Expose health endpoints.
- Export telemetry.
- Provide service metadata.

Docker itself provides basic container state, while application telemetry provides business/runtime information.

---

## 67. Kubernetes Observability

Kubernetes adds:

```text
Pod status
Deployment status
Restarts
Resource usage
Readiness
```

Application telemetry adds:

```text
Request latency
Errors
Business metrics
Dependency latency
Distributed traces
```

Both layers are necessary.

---

## 68. Observability and Security

Observability must not weaken security.

Telemetry systems should protect:

- Customer information.
- Authentication information.
- Secrets.
- Payment-related information.

Access to dashboards should be controlled.

Production observability systems should require authentication.

---

## 69. Observability Access

For the learning environment:

```text
Developer
   |
   v
Grafana
```

For production-like environments:

```text
Authorized Operator
   |
   v
Authenticated Grafana
```

Observability data should not be publicly exposed.

---

## 70. Troubleshooting Workflow

A recommended troubleshooting sequence is:

```text
1. Check Gateway
2. Check service health
3. Check error rate
4. Find trace
5. Inspect slow/failing dependency
6. Inspect service logs
7. Check Kafka/Redis/PostgreSQL
8. Check Kubernetes Pods/resources
9. Identify root cause
10. Verify recovery
```

This creates a repeatable debugging process.

---

## 71. Example: Slow Checkout

Suppose checkout takes 4 seconds.

Start with:

```text
Gateway p95 latency
```

Then inspect the checkout trace:

```text
Gateway       100ms
Order          80ms
Inventory     200ms
Promotion      50ms
Payment      3500ms  <-- problem
```

The trace immediately identifies Payment as the bottleneck.

Metrics can then determine whether the problem is isolated or systemic.

---

## 72. Example: Notification Failure

```text
Order Confirmed
      |
      v
Kafka
      |
      v
Notification
      |
      X processing failure
      |
      v
Retry
      |
      X
      |
      v
DLQ
```

Observability should show:

- Notification failure.
- Retry count.
- DLQ message.
- Original trace/correlation context where available.

The Order should remain confirmed.

---

## 73. Example: Redis Failure

### Product

```text
Product Request
      |
      v
Redis failure
      |
      v
PostgreSQL
      |
      v
Response
```

Metrics should show:

```text
Redis errors ↑
Product latency ↑
PostgreSQL traffic ↑
```

### Cart

```text
Cart Request
      |
      v
Redis unavailable
      |
      v
Dependency failure
```

Metrics/logs should make the reason clear.

---

## 74. Example: Kafka Lag

```text
Kafka
 |
 +--> 100,000 messages
 |
 +--> Notification consumers
          |
          v
       Processing slowly
```

Dashboard:

```text
Consumer Lag ↑
Processing Latency ↑
CPU ↑
```

This suggests scaling consumers or improving processing.

---

## 75. Observability Testing

Observability itself should be tested.

Verify:

- Logs contain expected fields.
- Trace IDs propagate.
- Metrics are emitted.
- Health checks work.
- Kafka traces/correlation are propagated.
- Redis metrics appear.
- Database instrumentation works.
- Errors generate telemetry.
- Sensitive values do not appear in telemetry.

---

## 76. Observability Failure Behavior

Telemetry failure should not normally bring down business services.

For example:

```text
Grafana unavailable
        |
        v
CommerceX continues operating
```

Telemetry export should be designed so that observability infrastructure does not become a critical business dependency.

The system should prefer losing telemetry over losing customer transactions.

---

## 77. Telemetry Backpressure

Telemetry exporters should be configured so that temporary backend unavailability does not block business operations indefinitely.

Potential mechanisms include:

- Batching.
- Asynchronous export.
- Bounded queues.
- Timeouts.

The exact exporter configuration depends on the selected OpenTelemetry backend.

---

## 78. Observability and Performance

Instrumentation introduces some overhead.

The project should:

- Use efficient structured logging.
- Avoid excessive Debug logging.
- Avoid high-cardinality metrics.
- Sample traces appropriately.
- Avoid logging every low-level operation unnecessarily.
- Monitor telemetry overhead.

Observability should help performance analysis rather than become a performance problem.

---

## 79. Initial Observability Stack

The initial stack should remain simple.

### Application

```text
ASP.NET Core
OpenTelemetry
Structured Logging
```

### Visualization

```text
Grafana
```

### Runtime

```text
Docker
Kubernetes/Minikube
```

A telemetry collection backend can be added as required by the chosen deployment architecture.

---

## 80. Recommended Telemetry Flow

A practical model is:

```text
Application
    |
    v
OpenTelemetry SDK
    |
    v
Telemetry Export
    |
    +--> Metrics backend
    +--> Trace backend
    +--> Log backend
    |
    v
Grafana
```

The exact backend technologies can be finalized during implementation.

---

## 81. OpenTelemetry Collector

An OpenTelemetry Collector can be introduced between services and telemetry backends.

Conceptually:

```text
CommerceX Services
        |
        v
OpenTelemetry Collector
        |
   +----+----+
   |    |    |
   v    v    v
Logs Metrics Traces
```

The Collector is useful for standardizing export and routing.

For the initial learning environment, it should be introduced only when it improves the telemetry architecture without creating unnecessary operational overhead.

---

## 82. Telemetry Resource Attributes

Services should include standard resource attributes such as:

```text
service.name
service.version
deployment.environment
```

Optional:

```text
service.instance.id
host.name
```

These attributes help identify which service generated a telemetry record.

---

## 83. Version Tracking

Telemetry should allow comparison across application versions.

Example:

```text
service.name = commercex-order
service.version = 1.2.0
```

This is useful after deployments:

```text
Version 1.1 → p95 = 250ms
Version 1.2 → p95 = 600ms
```

A regression can then be identified quickly.

---

## 84. Service-Level Objectives

The initial project can define simple operational targets.

Examples:

### Availability

```text
Normal API services should generally be available during expected operation.
```

### Latency

```text
Majority of normal API requests should complete under approximately 500ms
in the intended local/test environment.
```

### Error Rate

```text
Unexpected 5xx errors should remain low during normal operation.
```

### Kafka

```text
Consumer lag should normally remain bounded.
```

These targets are engineering goals, not guarantees.

---

## 85. Observability Implementation Checklist

### Application Instrumentation

- [ ] Add OpenTelemetry to services.
- [ ] Instrument ASP.NET Core.
- [ ] Instrument HttpClient.
- [ ] Instrument PostgreSQL.
- [ ] Instrument Redis.
- [ ] Instrument gRPC.
- [ ] Instrument Kafka where supported.
- [ ] Configure service metadata.

### Logging

- [ ] Configure structured logging.
- [ ] Standardize log fields.
- [ ] Add trace ID.
- [ ] Add correlation ID.
- [ ] Define log levels.
- [ ] Exclude sensitive information.

### Metrics

- [ ] HTTP request metrics.
- [ ] HTTP latency metrics.
- [ ] HTTP error metrics.
- [ ] Business metrics.
- [ ] Redis metrics.
- [ ] Kafka metrics.
- [ ] Database metrics.
- [ ] Kubernetes metrics.

### Tracing

- [ ] Configure distributed tracing.
- [ ] Propagate W3C trace context.
- [ ] Propagate Kafka trace context.
- [ ] Trace gRPC.
- [ ] Trace database/Redis dependencies.
- [ ] Validate checkout trace.

### Grafana

- [ ] Create system dashboard.
- [ ] Create API dashboard.
- [ ] Create Kafka dashboard.
- [ ] Create Redis dashboard.
- [ ] Create PostgreSQL dashboard.
- [ ] Create Kubernetes dashboard.
- [ ] Create checkout dashboard.

### Alerting

- [ ] Service availability alert.
- [ ] High 5xx alert.
- [ ] High latency alert.
- [ ] Kafka lag alert.
- [ ] Redis failure alert.
- [ ] PostgreSQL failure alert.
- [ ] Pod restart alert.
- [ ] DLQ alert.

### Testing

- [ ] Verify telemetry export.
- [ ] Verify trace propagation.
- [ ] Verify dashboard data.
- [ ] Verify health probes.
- [ ] Verify sensitive-data exclusion.
- [ ] Test telemetry backend failure.

---

## 86. Recommended Initial Dashboards

The minimum useful Grafana set is:

| Dashboard | Primary Purpose |
|---|---|
| System Overview | Overall health |
| API Performance | HTTP traffic/latency/errors |
| Checkout | Distributed business workflow |
| Kafka | Events and consumer lag |
| Redis | Cache/cart performance |
| PostgreSQL | Database health |
| Kubernetes | Pod/cluster health |

These dashboards should be implemented incrementally rather than all at once.

---

## 87. Observability Anti-Patterns

CommerceX should avoid:

### 87.1 Logging Everything

Excessive logs make debugging harder.

### 87.2 Logging Secrets

Never log credentials or tokens.

### 87.3 High-Cardinality Metrics

Do not use customer/order IDs as standard metric labels.

### 87.4 No Distributed Tracing

Logs alone are insufficient for complex checkout workflows.

### 87.5 Telemetry as a Hard Dependency

A Grafana/backend outage should not stop orders.

### 87.6 No Correlation

Distributed operations should be traceable.

### 87.7 Ignoring Kafka Lag

Asynchronous systems require consumer monitoring.

### 87.8 Monitoring Only Infrastructure

Healthy Pods do not guarantee healthy business behavior.

### 87.9 Monitoring Only Business Metrics

Infrastructure failures must also be visible.

### 87.10 Too Many Alerts

Alert noise reduces the value of monitoring.

---

## 88. Initial Implementation Scope

### Implement Initially

- OpenTelemetry in .NET services.
- Structured logging.
- Correlation/trace IDs.
- HTTP instrumentation.
- gRPC instrumentation.
- PostgreSQL instrumentation.
- Redis instrumentation.
- Kafka instrumentation where practical.
- Health endpoints.
- Core application metrics.
- Business metrics for checkout/order/payment/inventory.
- Grafana.
- System/API/Kafka/Redis/PostgreSQL/Kubernetes dashboards.
- Basic alerting.
- Sensitive-data filtering.
- Local Docker/Minikube observability.

### Defer

- Full enterprise SIEM.
- Advanced anomaly detection.
- Distributed profiling.
- Long-term compliance retention.
- Multi-region observability.
- Complex service-level management.
- Large-scale telemetry pipelines.
- Advanced APM vendor-specific features.

---

## 89. Architecture Summary

The final initial observability architecture is:

```text
                 +----------------------+
                 | CommerceX Services   |
                 +----------+-----------+
                            |
                            v
                  +-------------------+
                  | OpenTelemetry SDK |
                  +---------+---------+
                            |
                            v
                  +-------------------+
                  | Telemetry Export   |
                  +---------+---------+
                            |
             +--------------+--------------+
             |              |              |
             v              v              v
           Logs          Metrics         Traces
             |              |              |
             +--------------+--------------+
                            |
                            v
                       +---------+
                       | Grafana |
                       +---------+
```

Infrastructure telemetry also contributes:

```text
PostgreSQL
Redis
Kafka
Kubernetes
Docker
```

The goal is a unified view of application behavior and infrastructure health.

---

## 90. Architectural Decisions

The following decisions are established for the CommerceX observability baseline:

1. CommerceX uses logs, metrics, and traces as its primary observability signals.
2. OpenTelemetry is the standard application telemetry framework.
3. Grafana is the primary visualization tool.
4. Services use structured logging.
5. Distributed traces use standard W3C trace context.
6. Correlation IDs are supported for operational log correlation.
7. HTTP, gRPC, PostgreSQL, Redis, and Kafka operations should be observable.
8. Checkout is treated as the primary distributed workflow for tracing.
9. Kafka consumer lag is a key operational metric.
10. Redis cache hit/miss behavior is monitored.
11. PostgreSQL health and query performance are monitored.
12. Kubernetes Pod health and resource usage are monitored.
13. Business metrics complement infrastructure metrics.
14. High-cardinality identifiers are not used as normal metric labels.
15. Secrets and sensitive personal/authentication data are excluded from telemetry.
16. Health endpoints support Kubernetes liveness/readiness.
17. Telemetry failures must not normally stop business operations.
18. Alerts should be limited to meaningful operational conditions.
19. Observability configuration is environment-aware.
20. The observability stack should remain simple enough for local Docker/Minikube use.
21. An OpenTelemetry Collector may be introduced when it provides clear architectural value.
22. Advanced enterprise monitoring capabilities are deferred.

---

## 91. Relationship With Other Documents

This document builds on:

- Project Charter
- System Requirements
- Functional Requirements
- Non-Functional Requirements
- System Architecture
- Microservices Architecture
- Service Boundaries
- Data Architecture
- API Design
- gRPC Design
- Kafka Event Design
- Redis Caching Strategy
- Security Design
- Docker Design
- Kubernetes Design

It provides observability requirements for:

- Testing Strategy
- CI/CD Design
- Development Roadmap
- Final Project Report

---

## 92. Baseline Status

This document establishes the **CommerceX Observability Design baseline**.

Future observability implementation should follow these decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 17 — Testing Strategy
