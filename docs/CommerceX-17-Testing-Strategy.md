# CommerceX — Testing Strategy

**Document:** 17 — Testing Strategy  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 16 — Observability Design  
**Next Document:** 18 — CI/CD Design

---

## 1. Purpose

This document defines the testing strategy for CommerceX.

CommerceX is a distributed e-commerce backend consisting of an API Gateway, 12 independently deployable services, PostgreSQL, Redis, Kafka, Schema Registry, Docker, and Kubernetes.

Testing therefore needs to validate both:

- Individual service correctness.
- Distributed system behavior.

The strategy follows a layered testing model:

```text
Unit Tests
    |
    v
Application/Domain Tests
    |
    v
Integration Tests
    |
    v
API/Contract Tests
    |
    v
Component Tests
    |
    v
End-to-End Tests
    |
    v
Deployment/Infrastructure Tests
```

The objective is high confidence without requiring every test to execute against the entire distributed platform.

---

## 2. Testing Objectives

CommerceX testing should verify that:

1. Business rules are correct.
2. Service boundaries are respected.
3. APIs behave according to their contracts.
4. Databases persist correct state.
5. Redis behavior is correct.
6. Kafka events are produced and consumed correctly.
7. gRPC contracts work correctly.
8. Authentication and authorization are enforced.
9. Distributed workflows handle failures.
10. Idempotency prevents duplicate business effects.
11. Docker images start correctly.
12. Kubernetes deployments become healthy.
13. Observability remains functional.
14. Regression defects are detected early.
15. Changes can be safely integrated through CI/CD.

---

## 3. Testing Principles

CommerceX follows these principles:

### 3.1 Test Close to the Failure

If a business rule can be tested in a unit test, do not require an end-to-end test to verify it.

### 3.2 Fast Tests First

The development workflow should run:

```text
Unit → Integration → Broader Tests
```

### 3.3 Test Business Rules Independently

Domain invariants should not depend on PostgreSQL, Redis, or Kafka.

### 3.4 Test Real Infrastructure Where It Matters

Integration tests should use real PostgreSQL, Redis, and Kafka-compatible infrastructure where behavior depends on the actual technology.

### 3.5 Test Failures Deliberately

Distributed systems must be tested under dependency failures and duplicate messages.

### 3.6 Automate Regression Testing

Important tests should execute automatically in CI.

---

## 4. Testing Pyramid

The recommended testing pyramid is:

```text
                 /                /                 / E2E              /------             / API /              / Contract            /------------          / Integration           /----------------        /   Unit Tests            /--------------------```

The majority of tests should be unit tests.

A smaller number should be integration, contract, and end-to-end tests.

---

## 5. Test Levels

CommerceX uses the following levels:

| Level | Main Purpose |
|---|---|
| Unit | Domain/application logic |
| Integration | Infrastructure and service components |
| API | HTTP endpoint behavior |
| Contract | REST/gRPC/Kafka compatibility |
| Component | Service behavior with dependencies |
| End-to-End | Complete business workflows |
| Infrastructure | Docker/Kubernetes behavior |
| Security | Authentication/authorization and abuse cases |
| Performance | Latency and throughput validation |

---

## 6. Unit Testing

Unit tests verify isolated application behavior.

Primary targets:

- Domain entities.
- Value objects.
- Domain services.
- Application handlers.
- Validators.
- Business rules.
- State transitions.
- Mapping logic where useful.

Unit tests should normally not require:

- PostgreSQL.
- Redis.
- Kafka.
- Kubernetes.
- External network access.

---

## 7. Domain Testing

Domain tests should verify business invariants.

Examples:

### Order

```text
Quantity > 0
Order total is correct
Invalid status transition rejected
Cancelled order cannot be reconfirmed
```

### Inventory

```text
Stock cannot become negative
Reservation cannot exceed available stock
Release restores reserved quantity correctly
```

### Promotion

```text
Expired promotion rejected
Inactive promotion rejected
Discount calculated correctly
```

### Review

```text
Rating must be 1–5
Invalid review rejected
```

---

## 8. Application Layer Testing

Application tests verify use-case orchestration.

Examples:

```text
CreateOrderHandler
CancelOrderHandler
ReserveStockHandler
ValidatePromotionHandler
ProcessPaymentHandler
```

Tests should verify:

- Correct dependency calls.
- Correct validation.
- Correct authorization.
- Correct domain operations.
- Correct error handling.
- Correct transaction boundaries.

External dependencies can be mocked when the test is specifically targeting orchestration logic.

---

## 9. Infrastructure Testing

Infrastructure implementation should be tested separately.

Examples:

- EF Core repositories.
- PostgreSQL mappings.
- Redis cart storage.
- Product cache.
- Kafka producers.
- Kafka consumers.
- gRPC clients.
- Outbox persistence.

Infrastructure tests should use real infrastructure where mocking would hide technology-specific behavior.

---

## 10. PostgreSQL Integration Testing

PostgreSQL integration tests should verify:

- EF Core mappings.
- Migrations.
- CRUD operations.
- Constraints.
- Unique indexes.
- Transactions.
- Concurrency behavior.
- Query behavior.
- Repository implementations.

Example:

```text
Test
  |
  v
PostgreSQL Test Container
  |
  v
EF Core
  |
  v
Repository
```

---

## 11. Redis Integration Testing

Redis integration tests should verify:

### Cart

- Create cart.
- Retrieve cart.
- Add item.
- Update quantity.
- Remove item.
- Clear cart.
- TTL behavior.
- Expiration.

### Product Cache

- Cache miss.
- PostgreSQL fallback.
- Cache population.
- Cache hit.
- Invalidation.
- Expiration.

Tests should use a real Redis instance where possible.

---

## 12. Kafka Integration Testing

Kafka tests should verify:

- Event publication.
- Event serialization.
- Event deserialization.
- Topic configuration.
- Consumer processing.
- Consumer groups.
- Retry behavior.
- Idempotency.
- Dead-letter handling.
- Offset behavior where relevant.

Example:

```text
Producer
   |
   v
Kafka Test Broker
   |
   v
Consumer
   |
   v
Assert result
```

---

## 13. gRPC Integration Testing

gRPC tests should verify:

- Protobuf contracts.
- Request serialization.
- Response serialization.
- Status codes.
- Business error mapping.
- Deadlines.
- Cancellation.
- Authentication metadata.
- Idempotency.

Primary initial contracts:

```text
Inventory.ReserveStock
Inventory.ReleaseStock
Promotion.ValidatePromotion
Payment.ProcessPayment
```

---

## 14. API Testing

Each externally exposed REST API should have automated tests.

Test:

- HTTP method.
- Route.
- Authentication.
- Authorization.
- Request validation.
- Response status.
- Response body.
- Error format.
- Pagination.
- Filtering.
- Sorting.
- Idempotency where applicable.

---

## 15. API Status Code Tests

Tests should verify standard behavior.

Examples:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
429 Too Many Requests
500 Internal Server Error
503 Service Unavailable
```

The exact status code depends on the operation.

---

## 16. API Validation Testing

Invalid requests should be tested explicitly.

Examples:

```json
{
  "quantity": -1
}
```

should be rejected.

Other examples:

- Missing required fields.
- Invalid UUID.
- Invalid email.
- Empty product list.
- Excessive page size.
- Invalid promotion code.
- Invalid rating.
- Overly long text.

---

## 17. Authentication Testing

Auth tests should verify:

### Registration

- Valid registration succeeds.
- Duplicate email is rejected.
- Invalid password is rejected.
- Password is never stored in plaintext.

### Login

- Correct credentials succeed.
- Incorrect credentials fail.
- Generic authentication errors are returned.
- Access token is issued.
- Refresh token is issued.

---

## 18. Token Testing

Test:

- Valid access token.
- Expired access token.
- Invalid signature.
- Invalid issuer.
- Invalid audience.
- Missing token.
- Malformed token.
- Revoked refresh token.
- Expired refresh token.
- Refresh-token rotation.

---

## 19. Authorization Testing

Security tests should verify:

```text
Anonymous → Protected endpoint → 401
Customer → Admin endpoint → 403
Customer A → Customer B resource → denied
Admin → Admin endpoint → allowed
```

Authorization must be tested at service boundaries.

---

## 20. Resource Ownership Testing

For every customer-owned resource, test unauthorized access.

Examples:

```text
User profile
Address
Cart
Order
Review
Notification
```

Example:

```text
Customer A
    |
    v
GET Customer B's order
    |
    v
403/404
```

The API must not expose another customer's data.

---

## 21. Gateway Testing

Gateway tests should verify:

- Route mapping.
- Authentication handling.
- Authorization forwarding.
- Request forwarding.
- Response forwarding.
- Rate limiting.
- Correlation ID propagation.
- Trace propagation.
- Invalid route behavior.
- Upstream service failure.

---

## 22. Contract Testing

Contract tests ensure that service boundaries remain compatible.

CommerceX has three primary contract categories:

```text
REST
gRPC
Kafka
```

Contract testing is particularly important because services evolve independently.

---

## 23. REST Contract Testing

For REST APIs, verify:

- Route stability.
- Required fields.
- Response fields.
- Status codes.
- Error structure.
- Versioning.

Breaking API changes should require deliberate review.

---

## 24. gRPC Contract Testing

Protobuf contracts should be tested for compatibility.

Rules include:

- Do not reuse field numbers.
- Do not change field meaning.
- Add new fields compatibly.
- Preserve enum values.
- Maintain service/method semantics.

Contract tests should detect accidental breaking changes.

---

## 25. Kafka Contract Testing

Kafka event contracts should verify:

- Event type.
- Event version.
- Required envelope fields.
- Payload structure.
- Serialization format.
- Compatibility with consumers.

Example:

```text
OrderConfirmed v1
      |
      v
Notification Consumer
```

A producer change should not silently break existing consumers.

---

## 26. Consumer-Driven Contract Testing

Where practical, consumers should define expectations for the events they require.

For example:

```text
Search Service
     |
     v
expects ProductUpdated v1
```

The Product Service should be tested against those expectations.

The initial implementation can use schema compatibility checks and integration tests instead of introducing a dedicated contract-testing framework.

---

## 27. Component Testing

Component tests verify a service with its real infrastructure dependencies.

Example:

```text
Order Service
   |
   +--> PostgreSQL
   +--> Inventory test instance
   +--> Promotion test instance
   +--> Payment test instance
   +--> Kafka
```

Component tests sit between isolated integration tests and full end-to-end tests.

---

## 28. End-to-End Testing

End-to-end tests validate complete business workflows through the Gateway.

They should be limited in number because they are slower and more fragile.

Important workflows include:

1. Registration and login.
2. Product browsing.
3. Cart operations.
4. Successful checkout.
5. Payment failure.
6. Inventory failure.
7. Order cancellation.
8. Shipping progression.
9. Review creation.
10. Promotion usage.

---

## 29. Primary E2E Scenario — Successful Checkout

The main E2E test is:

```text
Register/Login
      |
      v
Browse Product
      |
      v
Add to Cart
      |
      v
Apply Promotion
      |
      v
Create Order
      |
      v
Reserve Inventory
      |
      v
Process Payment
      |
      v
Confirm Order
      |
      v
Shipping Created
      |
      v
Notification Generated
```

The test should verify final state across relevant services.

---

## 30. Checkout Failure — Inventory

Test:

```text
Cart
  |
  v
Order
  |
  v
Inventory
  |
  X insufficient stock
```

Expected behavior:

- Order is not confirmed.
- Reservation does not exceed available stock.
- Appropriate error is returned.
- No successful payment should be recorded if payment happens after reservation.
- No shipment should be created.

---

## 31. Checkout Failure — Payment

Test:

```text
Order
  |
  v
Inventory Reserved
  |
  v
Payment Failed
```

Expected behavior:

- Payment is marked failed.
- Order is not confirmed.
- Inventory reservation is released through the appropriate compensation flow.
- No shipment is created.
- Relevant events are published.

This validates the distributed transaction strategy.

---

## 32. Checkout Failure — Promotion

Test:

```text
Order
  |
  v
Promotion validation
  |
  X invalid/expired
```

Expected behavior:

- Order creation/checkout is rejected according to the defined flow.
- No inventory reservation is unnecessarily retained.
- No payment is processed.

---

## 33. Order Lifecycle Testing

Test all valid state transitions.

Example:

```text
PENDING
   |
   v
PAYMENT_PENDING
   |
   v
CONFIRMED
   |
   v
PROCESSING
   |
   v
SHIPPED
   |
   v
DELIVERED
```

Also test:

```text
PENDING → CANCELLED
PAYMENT_PENDING → CANCELLED
CONFIRMED → CANCELLED
```

where business rules permit.

Invalid transitions must fail.

---

## 34. Inventory Testing

Test:

- Initial stock.
- Stock adjustment.
- Reservation.
- Release.
- Insufficient stock.
- Concurrent reservations.
- Duplicate reservation.
- Duplicate release.
- Negative-stock prevention.

Concurrency tests are important because inventory is a shared business resource.

---

## 35. Payment Testing

Because payment is simulated, tests should verify deterministic behavior.

Test:

- Successful payment.
- Failed payment.
- Duplicate payment request.
- Unknown order.
- Invalid amount.
- Invalid currency.
- Idempotency behavior.

No real payment provider is required.

---

## 36. Shipping Testing

Test:

```text
CREATED
  ↓
IN_TRANSIT
  ↓
OUT_FOR_DELIVERY
  ↓
DELIVERED
```

Also test:

- Invalid transitions.
- Duplicate shipping events.
- Unknown order.
- Kafka event retry.
- Order status synchronization.

---

## 37. Notification Testing

Notification is asynchronous.

Tests should verify:

- Correct event consumption.
- Notification generation.
- Duplicate event handling.
- Retry behavior.
- Dead-letter behavior.
- Delivery simulation.
- Failure isolation.

A Notification failure must not change a confirmed Order to failed.

---

## 38. Search Testing

Search tests should verify:

- Product indexing.
- Product updates.
- Product deactivation.
- Search queries.
- Filtering.
- Sorting.
- Pagination.
- Eventual consistency.
- Re-indexing.

Example:

```text
ProductUpdated
      |
      v
Kafka
      |
      v
Search
      |
      v
Updated search result
```

---

## 39. Promotion Testing

Test:

- Active promotion.
- Expired promotion.
- Inactive promotion.
- Start date.
- End date.
- Minimum order value.
- Percentage discount.
- Fixed discount.
- Invalid coupon.
- Usage rules.

Promotion calculations should be heavily unit tested.

---

## 40. Review Testing

Test:

- Valid rating.
- Rating outside 1–5.
- Customer ownership.
- Duplicate review rules if applicable.
- Update own review.
- Delete own review.
- Modify another customer's review.
- Product reference validation.

---

## 41. User Service Testing

Test:

- Profile creation.
- Profile retrieval.
- Profile update.
- Address creation.
- Address update.
- Address deletion.
- Customer ownership.
- Invalid address data.
- Authentication integration.

---

## 42. Product Service Testing

Test:

- Product creation.
- Product update.
- Product deactivation.
- Category management.
- Validation.
- Product cache.
- Cache invalidation.
- Pagination.
- Filtering.
- Sorting.
- Admin authorization.

---

## 43. Cart Testing

Test:

- Empty cart.
- Add item.
- Update quantity.
- Remove item.
- Clear cart.
- Multiple products.
- Invalid quantity.
- Product references.
- Cart expiration.
- Redis unavailable.
- Concurrent updates where relevant.

---

## 44. Idempotency Testing

Idempotency is critical in CommerceX.

Test duplicate:

```text
Order request
Payment request
Inventory reservation
Inventory release
Kafka event
Refresh token operation
```

Example:

```text
Payment Request
      |
      v
Payment succeeds
      |
      X network timeout
      |
      v
Client retries
      |
      v
Same idempotency key
      |
      v
Return original result
```

No duplicate payment should be created.

---

## 45. Kafka Duplicate Event Testing

Because Kafka is initially treated as at-least-once delivery:

```text
Event A
Event A again
```

must not produce duplicate business effects.

For example:

```text
OrderConfirmed
OrderConfirmed
```

should not create two shipments.

Consumers should use event IDs or business operation identifiers for deduplication.

---

## 46. Outbox Testing

If the Transactional Outbox Pattern is implemented:

Test:

```text
Business Transaction
      |
      +--> Database update
      +--> Outbox event
```

Both must commit atomically.

Also test:

- Outbox publishing after commit.
- Publisher retry.
- Duplicate publication.
- Failed broker.
- Unpublished outbox records.

---

## 47. Failure Testing

Distributed failure scenarios should be explicitly tested.

Examples:

- PostgreSQL unavailable.
- Redis unavailable.
- Kafka unavailable.
- Inventory unavailable.
- Payment unavailable.
- Promotion unavailable.
- Notification unavailable.
- Network timeout.
- Service restart.

---

## 48. Timeout Testing

Every synchronous dependency should have a timeout.

Test:

```text
Order
  |
  v
Payment
  |
  ... timeout
```

Expected behavior should be deterministic.

The system should not wait indefinitely for a downstream service.

---

## 49. Retry Testing

Retries should be tested carefully.

Test:

- Transient failure.
- Successful retry.
- Retry exhaustion.
- Non-retryable error.
- Duplicate side effects.

Do not retry all errors.

---

## 50. Graceful Degradation Testing

Example:

### Product Cache

```text
Redis unavailable
      |
      v
Product → PostgreSQL
```

Product reads should continue where designed.

### Notification

```text
Notification unavailable
      |
      v
Order remains confirmed
```

This validates failure isolation.

---

## 51. Security Testing

Security tests should cover:

- Authentication.
- Authorization.
- Resource ownership.
- Token expiry.
- Token validation.
- Rate limiting.
- Input validation.
- Error information disclosure.
- Secret handling.
- Database isolation.
- Kafka authorization where configured.
- Redis access restrictions.

---

## 52. Injection Testing

Test inputs designed to expose:

- SQL injection.
- Malformed JSON.
- Path traversal where applicable.
- Header manipulation.
- Oversized payloads.
- Invalid identifiers.
- Unexpected characters.

The API must treat all client input as untrusted.

---

## 53. Rate-Limit Testing

Test:

```text
Within limit → request succeeds
Limit reached → 429
After window → requests allowed again
```

Authentication endpoints should receive special attention.

---

## 54. Performance Testing

Performance testing should focus on realistic workloads rather than artificial maximum throughput.

Measure:

- API latency.
- Throughput.
- Database latency.
- Redis latency.
- Kafka processing throughput.
- Consumer lag.
- Checkout latency.

Important percentiles:

```text
p50
p95
p99
```

---

## 55. Performance Targets

The initial project can use these engineering targets:

```text
Normal API requests:
majority under approximately 500ms

Gateway overhead:
generally under approximately 100ms

Kafka processing:
bounded lag under expected workload

Redis:
low-millisecond local access expected
```

These are development targets, not production guarantees.

Actual measurements should be recorded.

---

## 56. Load Testing

Load testing can be performed after the main workflows are implemented.

Possible tools:

- k6.
- JMeter.
- NBomber.

A lightweight tool is preferable for the learning project.

Initial load tests should focus on:

```text
Product reads
Search
Cart operations
Order creation
Checkout
```

---

## 57. Concurrency Testing

Important concurrency targets:

### Inventory

Two customers attempt to reserve the last item.

Expected:

```text
Customer A → succeeds
Customer B → fails
```

### Cart

Concurrent updates should not unexpectedly lose data.

### Payment

Duplicate concurrent payment requests must remain idempotent.

---

## 58. Database Concurrency

EF Core/PostgreSQL tests should verify appropriate concurrency behavior.

Possible mechanisms:

- Transactions.
- Row-level locking where needed.
- Optimistic concurrency.
- Atomic SQL updates.

Inventory is the primary area where concurrency correctness matters.

---

## 59. Container Testing

Docker tests should verify:

- Image builds.
- Container starts.
- Health endpoint works.
- Configuration injection works.
- Required ports are exposed.
- Service can connect to dependencies.
- Container does not require developer-local files.
- Container runs with appropriate privileges.

---

## 60. Image Security Testing

Built images should be scanned for:

- OS vulnerabilities.
- NuGet/package vulnerabilities.
- Known CVEs.
- Unnecessary packages.

The CI pipeline should fail for appropriately severe vulnerabilities according to the project's policy.

---

## 61. Kubernetes Testing

Kubernetes tests should verify:

- Namespace creation.
- Deployments.
- Services.
- Pod readiness.
- Service discovery.
- ConfigMaps.
- Secrets.
- Persistent volumes.
- Health probes.
- Resource limits.
- Rolling updates.
- Rollback.
- Pod restart recovery.

---

## 62. Kubernetes Failure Testing

Controlled tests should include:

```text
Delete Pod
   |
   v
Replacement created
   |
   v
Ready
```

Also:

```text
Scale Deployment
   |
   v
Multiple Pods
   |
   v
Traffic continues
```

---

## 63. Health Probe Testing

Verify:

### Liveness

A healthy process returns success.

### Readiness

A service with required dependencies unavailable becomes not ready.

### Startup

Slow-starting components are not restarted prematurely.

---

## 64. Observability Testing

Observability should be tested as part of the system.

Verify:

- Logs are generated.
- Trace IDs propagate.
- Metrics are emitted.
- Kafka processing is visible.
- Redis metrics are visible.
- Health endpoints work.
- Sensitive information is excluded.
- Grafana receives expected telemetry.

---

## 65. Test Data Strategy

Test data should be:

- Deterministic where possible.
- Isolated per test.
- Safe/non-production.
- Easy to recreate.
- Small enough for fast tests.

Examples:

```text
test@example.com
Test Product
Test Order
```

Never use real customer credentials or production personal data.

---

## 66. Database Test Isolation

Integration tests should isolate database state.

Possible approaches:

- Transaction rollback.
- Unique test database.
- Database reset.
- Container recreation.

For service-level integration tests, a dedicated disposable database/container is preferred when practical.

---

## 67. Testcontainers

Testcontainers is recommended for integration testing.

Conceptually:

```text
xUnit
 |
 +--> PostgreSQL Container
 +--> Redis Container
 +--> Kafka Container
```

This allows tests to run against realistic infrastructure without requiring manually installed services.

---

## 68. Test Project Structure

A possible repository structure is:

```text
tests/
├── Unit/
│   ├── Auth
│   ├── User
│   ├── Product
│   └── ...
├── Integration/
│   ├── Auth
│   ├── Product
│   ├── Order
│   └── ...
├── Contract/
│   ├── Rest
│   ├── Grpc
│   └── Kafka
└── EndToEnd/
    └── CommerceX
```

The exact structure can follow the service organization.

---

## 69. Service-Level Test Structure

Each service should keep its tests close to the service.

Example:

```text
src/Services/Order/
├── CommerceX.Order.Api
├── CommerceX.Order.Application
├── CommerceX.Order.Domain
└── CommerceX.Order.Infrastructure

tests/Services/Order/
├── CommerceX.Order.UnitTests
├── CommerceX.Order.IntegrationTests
└── CommerceX.Order.ApiTests
```

This supports independent service development.

---

## 70. Test Naming

Tests should clearly describe behavior.

Prefer:

```text
CreateOrder_ShouldRejectEmptyCart
ReserveStock_ShouldFailWhenQuantityExceedsAvailableStock
RefreshToken_ShouldRejectRevokedToken
```

Avoid:

```text
Test1
OrderTest
Works
```

---

## 71. Test Arrange-Act-Assert

Tests should generally follow:

```text
Arrange
   |
   v
Act
   |
   v
Assert
```

Example:

```text
Arrange:
Product has stock = 2

Act:
Reserve quantity = 3

Assert:
Reservation fails
Stock remains 2
```

---

## 72. Mocking Strategy

Mocks are useful for isolated application tests.

Good candidates:

- External service clients.
- Event publishers.
- Time provider.
- Token service.
- Random/token generators.

Avoid mocking infrastructure merely to avoid writing integration tests.

For example, repository tests should use PostgreSQL rather than only mocking `DbContext`.

---

## 73. Time Abstraction

Time-dependent functionality should use an abstraction or .NET time facilities that can be controlled in tests.

Important areas:

- Token expiration.
- Password reset expiration.
- Promotion expiration.
- Cart TTL.
- Order timestamps.

This prevents flaky tests.

---

## 74. Randomness and Token Testing

Security tokens should use secure randomness in production.

Tests can inject deterministic token generators where the application architecture permits.

This allows predictable assertions without weakening production security.

---

## 75. Test Flakiness

Tests should avoid dependence on:

- Real network timing.
- Current wall-clock time without control.
- Random external state.
- Test execution order.
- Developer machine configuration.

Flaky tests should be fixed rather than repeatedly retried.

---

## 76. Regression Testing

Every bug fix should add a regression test where practical.

Example:

```text
Bug discovered
    |
    v
Fix implemented
    |
    v
Regression test added
    |
    v
Future CI runs catch recurrence
```

This is especially important for distributed workflows.

---

## 77. Definition of Done for Code Changes

A feature is considered test-complete when:

- Unit tests pass.
- Relevant integration tests pass.
- API/contract tests pass where applicable.
- Security tests pass where applicable.
- Regression tests are added.
- Observability behavior is verified where relevant.
- Docker build succeeds for affected service.
- No new high-severity dependency issue is introduced.

---

## 78. Pull Request Testing

A pull request should normally execute:

```text
Restore
  |
  v
Build
  |
  v
Unit Tests
  |
  v
Relevant Integration Tests
  |
  v
API/Contract Tests
  |
  v
Security/Dependency Checks
```

Broader E2E and container tests can run in appropriate CI stages.

---

## 79. CI Test Stages

The eventual CI pipeline can be:

```text
Stage 1 — Build
Stage 2 — Unit Tests
Stage 3 — Integration Tests
Stage 4 — Contract Tests
Stage 5 — Security Checks
Stage 6 — Docker Build
Stage 7 — Container Tests
Stage 8 — E2E Tests
```

The exact pipeline is defined in Document 18.

---

## 80. Test Environment Strategy

CommerceX can use:

### Local

Developer workstation + Docker/Testcontainers.

### CI

Ephemeral containers/services.

### Minikube

Full Kubernetes integration.

The same application behavior should be validated progressively across environments.

---

## 81. Environment-Specific Tests

Not every test belongs in every environment.

| Test | Local | CI | Minikube |
|---|---:|---:|---:|
| Unit | Yes | Yes | Optional |
| Integration | Yes | Yes | Yes |
| API | Yes | Yes | Yes |
| Contract | Yes | Yes | Yes |
| E2E | Optional | Yes | Yes |
| Load | Optional | Optional | Yes |
| Kubernetes | No | Optional | Yes |
| Failure tests | Yes | Yes | Yes |

---

## 82. Test Coverage

Coverage should be used as an indicator, not the only quality metric.

The most important target is meaningful coverage of:

- Domain rules.
- Application handlers.
- Security.
- Critical infrastructure behavior.
- Checkout workflow.

100% line coverage is not required.

---

## 83. Critical Path Coverage

High confidence should be achieved around:

```text
Authentication
Product availability
Cart
Inventory reservation
Order creation
Payment
Order confirmation
Shipping
Kafka events
```

These represent the most important business paths.

---

## 84. Mutation Testing

Mutation testing can be introduced later to assess whether tests detect intentional logic changes.

It is not required for the initial implementation.

The project should first establish reliable unit/integration coverage.

---

## 85. Performance Regression Testing

After baseline performance measurements are established, CI or scheduled tests can compare:

```text
Current p95
vs
Baseline p95
```

Large regressions should trigger investigation.

Automated performance gates can be introduced later.

---

## 86. Test Reporting

CI should publish:

- Test results.
- Failed test names.
- Code coverage.
- Integration test results.
- Security scan results.
- Docker scan results where configured.

Reports should make failures easy to diagnose.

---

## 87. Failure Diagnosis

A failed distributed test should provide:

- Test name.
- Trace/correlation ID.
- Service logs.
- Relevant dependency logs.
- Kafka information where relevant.
- Container/Pod status.

This connects the Testing and Observability designs.

---

## 88. Security and Test Data

Tests must not use:

```text
Real passwords
Real access tokens
Real credit cards
Real customer data
Production secrets
```

Use synthetic values.

---

## 89. Test Database Security

Test databases should:

- Use test-only credentials.
- Be network-restricted.
- Be disposable.
- Never contain production data.

---

## 90. Contract Evolution Testing

When an event/API contract changes:

```text
Producer Change
      |
      v
Contract Tests
      |
      +--> Existing Consumer Compatibility
      |
      v
Approve Change
```

Breaking changes should be intentional and versioned.

---

## 91. Kafka Event Ordering Testing

Where ordering matters, tests should verify partition-key behavior.

For Order events:

```text
OrderId
   |
   v
Same Kafka partition
   |
   v
Preserved order
```

Tests should verify that related events are not accidentally distributed across incompatible partitioning strategies.

---

## 92. Eventual Consistency Testing

Search and notification workflows are eventually consistent.

Tests should not assume immediate cross-service state synchronization.

Example:

```text
Product Updated
      |
      v
Kafka
      |
      v
Search Consumer
      |
      v
Search Updated
```

The test should allow the consumer to process the event before asserting final search state.

---

## 93. Event Replay Testing

Because Kafka supports replay, the system should be tested for replay safety where relevant.

Example:

```text
Product events replayed
       |
       v
Search
       |
       v
Correct final index
```

Consumers should remain idempotent.

---

## 94. Deployment Smoke Tests

After deployment, a small smoke suite should verify:

```text
Gateway reachable
Auth reachable
Product reachable
Cart reachable
Order reachable
Health endpoints healthy
Kafka connected
Redis connected
PostgreSQL connected
```

A deployment should not be considered successful merely because Pods are running.

---

## 95. Post-Deployment Verification

A deployment verification sequence:

```text
Deployment
   |
   v
Pods Ready
   |
   v
Health Checks
   |
   v
Gateway Smoke Test
   |
   v
Authentication Test
   |
   v
Product Read
   |
   v
Checkout Smoke Test
```

This provides confidence that the distributed system is actually operational.

---

## 96. Testing Anti-Patterns

CommerceX should avoid:

### 96.1 Testing Everything Through E2E

Slow and difficult to diagnose.

### 96.2 Mocking Everything

Can hide real infrastructure problems.

### 96.3 No Failure Tests

Distributed systems must be tested under failure.

### 96.4 Ignoring Idempotency

Retries and duplicate events are expected.

### 96.5 Ignoring Security Tests

Authentication and authorization are core requirements.

### 96.6 Test Order Dependencies

Tests should be independently executable.

### 96.7 Real Production Data

Never use production personal data in tests.

### 96.8 Uncontrolled Time

Expiration tests become flaky.

### 96.9 Unbounded Performance Tests

Tests should use controlled workloads.

### 96.10 Treating Coverage as Quality

Coverage does not guarantee correct assertions.

---

## 97. Initial Testing Scope

### Implement Initially

- Unit tests for all domain rules.
- Application handler tests.
- PostgreSQL integration tests.
- Redis integration tests.
- Kafka integration tests.
- gRPC integration tests.
- REST API tests.
- Authentication/authorization tests.
- Resource ownership tests.
- Idempotency tests.
- Critical failure tests.
- Checkout E2E tests.
- Docker smoke tests.
- Kubernetes smoke tests.
- Observability tests.
- Dependency/security scanning.
- Basic performance tests.

### Defer

- Large-scale load testing.
- Chaos engineering platforms.
- Mutation testing across the entire codebase.
- Full production performance gates.
- Complex consumer contract frameworks.
- Multi-region disaster testing.

---

## 98. Implementation Checklist

### Unit

- [ ] Domain rules tested.
- [ ] State transitions tested.
- [ ] Validators tested.
- [ ] Application handlers tested.
- [ ] Security policies tested.

### Integration

- [ ] PostgreSQL tests.
- [ ] Redis tests.
- [ ] Kafka tests.
- [ ] gRPC tests.
- [ ] Repository tests.
- [ ] Outbox tests if implemented.

### API

- [ ] REST endpoint tests.
- [ ] Validation tests.
- [ ] Authentication tests.
- [ ] Authorization tests.
- [ ] Ownership tests.
- [ ] Error response tests.
- [ ] Rate-limit tests.

### Distributed Workflows

- [ ] Successful checkout.
- [ ] Inventory failure.
- [ ] Payment failure.
- [ ] Promotion failure.
- [ ] Order cancellation.
- [ ] Shipping lifecycle.
- [ ] Notification processing.
- [ ] Search synchronization.

### Reliability

- [ ] Idempotency.
- [ ] Duplicate Kafka events.
- [ ] Retry behavior.
- [ ] Timeout behavior.
- [ ] Dependency failure.
- [ ] Recovery behavior.

### Containers/Kubernetes

- [ ] Docker image tests.
- [ ] Health checks.
- [ ] Kubernetes deployment smoke tests.
- [ ] Pod restart test.
- [ ] Service discovery test.
- [ ] Rolling update test.

### Observability

- [ ] Logs.
- [ ] Metrics.
- [ ] Traces.
- [ ] Correlation IDs.
- [ ] Sensitive-data filtering.

### CI

- [ ] Automated unit tests.
- [ ] Automated integration tests.
- [ ] Contract tests.
- [ ] Security checks.
- [ ] Docker build tests.
- [ ] Test reports.

---

## 99. Test Matrix by Service

| Service | Unit | Integration | API | Contract | E2E |
|---|---:|---:|---:|---:|---:|
| Auth | Yes | PostgreSQL | Yes | Yes | Yes |
| User | Yes | PostgreSQL | Yes | Yes | Yes |
| Product | Yes | PostgreSQL/Redis/Kafka | Yes | Yes | Yes |
| Inventory | Yes | PostgreSQL/gRPC/Kafka | Yes | Yes | Yes |
| Cart | Yes | Redis | Yes | Yes | Yes |
| Order | Yes | PostgreSQL/gRPC/Kafka | Yes | Yes | Yes |
| Payment | Yes | PostgreSQL/gRPC | Internal | Yes | Yes |
| Shipping | Yes | PostgreSQL/Kafka | Yes | Yes | Yes |
| Review | Yes | PostgreSQL | Yes | Yes | Yes |
| Notification | Yes | Kafka/PostgreSQL | Internal | Yes | Yes |
| Promotion | Yes | PostgreSQL/gRPC | Yes | Yes | Yes |
| Search | Yes | PostgreSQL/Kafka | Yes | Yes | Yes |

---

## 100. Quality Gates

A change should not proceed to deployment if:

- Build fails.
- Required unit tests fail.
- Critical integration tests fail.
- Contract compatibility fails.
- Security checks identify unacceptable vulnerabilities.
- Docker image cannot start.
- Critical E2E workflows fail.

The exact CI gate thresholds are defined in the CI/CD Design.

---

## 101. Definition of Test Readiness

A service is test-ready when:

```text
Build succeeds
     +
Unit tests exist
     +
Integration tests exist
     +
API/contract tests exist where relevant
     +
Security behavior tested
     +
Health endpoint works
     +
Failure behavior tested
```

---

## 102. Definition of Release Readiness

The CommerceX platform is release-ready for the intended environment when:

- All required services build.
- Critical tests pass.
- Database migrations succeed.
- Docker images build.
- Kubernetes deployment succeeds.
- Health checks pass.
- Gateway smoke tests pass.
- Checkout E2E test passes.
- Critical observability signals are visible.
- No unacceptable security issue remains.

---

## 103. Testing Architecture Summary

The final testing architecture is:

```text
                         CommerceX
                            |
             +--------------+--------------+
             |              |              |
             v              v              v
          Unit Tests   Integration     Contract Tests
                            |
                            v
                      Component Tests
                            |
                            v
                       E2E Tests
                            |
              +-------------+-------------+
              |                           |
              v                           v
       Docker Tests                 Kubernetes Tests
              |                           |
              +-------------+-------------+
                            |
                            v
                     CI/CD Quality Gates
```

The testing strategy is intentionally layered so that most defects are detected before expensive end-to-end testing.

---

## 104. Architectural Decisions

The following decisions are established for the CommerceX testing baseline:

1. Testing follows a layered testing pyramid.
2. Unit tests form the majority of the test suite.
3. Domain rules are tested independently of infrastructure.
4. Application orchestration is tested independently where practical.
5. Real PostgreSQL is used for database integration testing.
6. Real Redis is used for Redis integration testing.
7. Real Kafka-compatible infrastructure is used for event integration testing.
8. gRPC contracts are tested through integration/contract tests.
9. REST APIs are tested through automated API tests.
10. Authentication and authorization receive dedicated security tests.
11. Customer resource ownership is explicitly tested.
12. Idempotency is explicitly tested for important operations.
13. Duplicate Kafka event processing is tested.
14. Checkout is the primary end-to-end workflow.
15. Distributed failure scenarios are tested deliberately.
16. Time-dependent behavior uses controllable time abstractions where necessary.
17. Testcontainers is recommended for disposable integration infrastructure.
18. Docker images receive smoke and security testing.
19. Kubernetes deployments receive smoke and recovery testing.
20. Observability behavior is included in testing.
21. Test data is synthetic and isolated.
22. Performance testing focuses on realistic workloads.
23. Code coverage is an indicator rather than the sole quality metric.
24. E2E tests remain limited to critical workflows.
25. CI executes automated quality gates.
26. Advanced chaos, mutation, and large-scale performance testing are deferred.

---

## 105. Relationship With Other Documents

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
- Observability Design

It provides testing requirements for:

- CI/CD Design
- Development Roadmap
- Final Project Report

---

## 106. Baseline Status

This document establishes the **CommerceX Testing Strategy baseline**.

Future test implementation should follow these decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 18 — CI/CD Design
