# CommerceX — Development Roadmap

**Document:** 19 — Development Roadmap  
**Project:** CommerceX  
**Status:** Implementation Baseline  
**Previous Document:** 18 — CI/CD Design  
**Next Document:** 20 — Final Project Report

---

## 1. Purpose

This document defines the implementation roadmap for CommerceX.

CommerceX is a learning-focused distributed e-commerce backend designed to progressively introduce:

- Microservices.
- REST APIs.
- gRPC.
- Apache Kafka.
- Redis.
- PostgreSQL.
- Docker.
- Kubernetes.
- OpenTelemetry.
- Grafana.
- Automated testing.
- GitHub Actions CI/CD.

The roadmap intentionally avoids implementing all infrastructure and services simultaneously.

The project will be developed incrementally so that each phase produces a working and verifiable system.

---

## 2. Roadmap Objectives

The roadmap should:

1. Establish infrastructure before business services depend on it.
2. Implement services in dependency-aware order.
3. Keep each service independently buildable.
4. Introduce distributed communication gradually.
5. Test each service before moving to dependent services.
6. Introduce Kubernetes after Docker-based service execution is stable.
7. Introduce CI/CD after the build and test process is stable.
8. Maintain working project milestones throughout development.
9. Avoid unnecessary enterprise complexity.
10. Produce a final system that demonstrates practical distributed-system engineering.

---

## 3. Overall Development Strategy

The implementation follows:

```text
Architecture
     ↓
Infrastructure Foundation
     ↓
Auth
     ↓
User
     ↓
Product
     ↓
Promotion
     ↓
Cart
     ↓
Inventory
     ↓
Order
     ↓
Payment
     ↓
Shipping
     ↓
Notification
     ↓
Review
     ↓
Search
     ↓
Observability
     ↓
Kubernetes
     ↓
CI/CD
     ↓
Final Validation
```

Some infrastructure and testing capabilities will be introduced earlier and refined throughout the project.

---

## 4. Development Principles

### 4.1 Incremental Implementation

Implement one meaningful capability at a time.

### 4.2 Build Before Integrate

A service should build independently before integrating it with other services.

### 4.3 Test Before Proceeding

Each phase should have explicit validation criteria.

### 4.4 Infrastructure Supports Business Services

Do not introduce infrastructure features without a clear service requirement.

### 4.5 Preserve Service Boundaries

No implementation shortcut should create cross-service database ownership.

### 4.6 Keep the System Working

At the end of each phase, CommerceX should remain runnable.

---

# Phase 0 — Infrastructure Foundation

## 5. Objective

Establish the technical foundation required by the services.

### Main Components

- Docker.
- Docker Compose.
- PostgreSQL.
- Redis.
- Kafka.
- Schema Registry.
- Basic Kubernetes manifests.
- Local Kubernetes environment.
- Git repository structure.

---

## 6. Phase 0 Tasks

### 6.1 Repository

- Create monorepo.
- Establish `src`, `tests`, `infra`, and `docs`.
- Configure Git.
- Establish branch strategy.

### 6.2 Docker

- Create Docker Compose infrastructure.
- Configure PostgreSQL.
- Configure Redis.
- Configure Kafka.
- Configure Schema Registry.
- Configure persistent volumes.
- Configure health checks.

### 6.3 PostgreSQL

Create logical databases:

```text
commercex_auth
commercex_users
commercex_products
commercex_inventory
commercex_orders
commercex_payments
commercex_shipping
commercex_reviews
commercex_notifications
commercex_promotions
commercex_search
```

### 6.4 Redis

Verify:

- Connection.
- Persistence configuration where required.
- Basic key operations.

### 6.5 Kafka

Verify:

- Broker startup.
- Topic creation.
- Producer/consumer connectivity.
- Consumer groups.

### 6.6 Schema Registry

Verify:

- Startup.
- Kafka connectivity.
- Schema registration capability.

---

## 7. Phase 0 Validation

The phase is complete when:

- Infrastructure starts successfully.
- PostgreSQL is accessible.
- Redis is accessible.
- Kafka is accessible.
- Schema Registry is accessible.
- Docker Compose starts without manual intervention beyond expected configuration.
- Basic connectivity tests pass.
- Initial Kubernetes infrastructure manifests are validated.
- Changes are committed to Git.

---

# Phase 1 — Auth Service

## 8. Objective

Implement secure identity and authentication foundations.

Auth is the first business service because most protected services depend on authenticated customer identity.

---

## 9. Phase 1 Scope

Implement:

- Registration.
- Login.
- Password hashing.
- Access tokens.
- Refresh tokens.
- Logout/revocation.
- Password reset.
- Role support.
- PostgreSQL persistence.
- EF Core migrations.
- REST API.
- Unit tests.
- Integration tests.

---

## 10. Auth Architecture

```text
Client
  |
  v
Gateway
  |
  v
Auth Service
  |
  +--> PostgreSQL
```

Initial Auth implementation should not depend on the remaining business services.

---

## 11. Auth Validation

Verify:

- Duplicate registration rejection.
- Secure password hashing.
- Login success/failure.
- JWT validation.
- Refresh-token rotation.
- Logout.
- Password reset.
- Role claims.
- Database persistence.
- API error handling.
- Security tests.

---

## 12. Phase 1 Exit Criteria

- Auth Service builds independently.
- Unit tests pass.
- Integration tests pass.
- PostgreSQL migration succeeds.
- API endpoints work.
- Authentication tokens work.
- Security behavior is verified.
- Docker image starts successfully.

---

# Phase 2 — User Service

## 13. Objective

Implement customer profile and address management.

---

## 14. Scope

Implement:

- User profile.
- Address CRUD.
- Customer ownership.
- REST API.
- PostgreSQL persistence.
- Authentication integration.
- Tests.

---

## 15. User Flow

```text
Login
  |
  v
JWT
  |
  v
User Service
  |
  v
Customer Profile
```

---

## 16. Validation

Verify:

- Authenticated access.
- Own-resource access.
- Cross-user access rejection.
- Profile updates.
- Address management.
- Database constraints.
- API tests.

---

# Phase 3 — Product Service

## 17. Objective

Implement the product catalog.

---

## 18. Scope

Implement:

- Products.
- Categories.
- Product CRUD.
- Category CRUD.
- Product activation/deactivation.
- Pagination.
- Filtering.
- Sorting.
- Redis cache.
- Kafka product events.
- Admin authorization.
- Tests.

---

## 19. Product Architecture

```text
Client
  |
  v
Gateway
  |
  v
Product Service
  |
  +--> PostgreSQL
  |
  +--> Redis
  |
  +--> Kafka
```

---

## 20. Product Events

Initial events:

```text
ProductCreated
ProductUpdated
ProductDeactivated
```

These events will later support Search and Notification-related workflows where applicable.

---

## 21. Validation

Verify:

- CRUD.
- Admin authorization.
- Cache-aside behavior.
- Cache invalidation.
- Product events.
- Pagination.
- Filtering.
- Sorting.
- Integration tests.

---

# Phase 4 — Promotion Service

## 22. Objective

Implement simple promotional and coupon logic before checkout.

---

## 23. Scope

Implement:

- Promotion creation.
- Promotion activation/deactivation.
- Coupon codes.
- Percentage discounts.
- Fixed discounts.
- Expiration.
- Minimum order value.
- Validation.
- Discount calculation.
- REST administration.
- gRPC validation.
- Tests.

---

## 24. Promotion Architecture

```text
Order Service
      |
      | gRPC
      v
Promotion Service
      |
      v
PostgreSQL
```

---

## 25. Validation

Verify:

- Valid promotion.
- Expired promotion.
- Inactive promotion.
- Invalid coupon.
- Minimum order rule.
- Discount calculation.
- gRPC contract.
- Authorization.

---

# Phase 5 — Cart Service

## 26. Objective

Implement customer shopping carts using Redis.

---

## 27. Scope

Implement:

- Get cart.
- Add item.
- Update item.
- Remove item.
- Clear cart.
- Cart expiration.
- Redis persistence.
- Product reference validation.
- REST API.
- Tests.

---

## 28. Cart Architecture

```text
Client
  |
  v
Gateway
  |
  v
Cart Service
  |
  v
Redis
```

---

## 29. Validation

Verify:

- Empty cart.
- Multiple items.
- Quantity validation.
- Cart expiration.
- Redis failure behavior.
- Product references.
- Ownership through authenticated customer ID.

---

# Phase 6 — Inventory Service

## 30. Objective

Implement authoritative stock management.

Inventory is critical because checkout depends on correct reservation behavior.

---

## 31. Scope

Implement:

- Inventory creation.
- Stock adjustment.
- Stock retrieval.
- Reservation.
- Release.
- Idempotency.
- PostgreSQL persistence.
- gRPC API.
- Kafka events.
- Concurrency protection.
- Tests.

---

## 32. Inventory Architecture

```text
Order Service
      |
      | gRPC
      v
Inventory Service
      |
      +--> PostgreSQL
      |
      +--> Kafka
```

---

## 33. Inventory Events

```text
InventoryReserved
InventoryReleased
InventoryAdjusted
```

---

## 34. Validation

Critical scenarios:

```text
Stock = 1

Request A → reserve 1 → success
Request B → reserve 1 → failure
```

Also verify:

- No negative stock.
- Duplicate reservation.
- Duplicate release.
- Concurrent requests.
- Kafka events.
- gRPC failures.

---

# Phase 7 — Order Service

## 35. Objective

Implement the central commerce workflow.

Order Service becomes the primary orchestration point for checkout.

---

## 36. Scope

Implement:

- Create order.
- Retrieve order.
- List customer orders.
- Order items.
- Price snapshots.
- Address snapshots.
- Order status.
- Cancellation.
- Inventory reservation.
- Promotion validation.
- Payment processing.
- Kafka events.
- Idempotency.
- PostgreSQL persistence.
- Tests.

---

## 37. Checkout Architecture

Initial flow:

```text
Customer
   |
   v
Gateway
   |
   v
Order Service
   |
   +--> Cart
   |
   +--> Product
   |
   +--> Promotion
   |
   +--> Inventory
   |
   +--> Payment
   |
   v
Order Confirmed
   |
   v
Kafka
```

The exact synchronous/asynchronous boundary should follow Documents 09–11.

---

## 38. Order Lifecycle

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

Cancellation is possible from eligible states.

---

## 39. Checkout Validation

### Successful

```text
Cart
 ↓
Promotion
 ↓
Inventory
 ↓
Payment
 ↓
Order Confirmed
```

### Inventory Failure

```text
Inventory unavailable/insufficient
        ↓
Checkout fails
```

### Payment Failure

```text
Inventory Reserved
       ↓
Payment Failed
       ↓
Inventory Released
       ↓
Order Not Confirmed
```

---

## 40. Phase 7 Exit Criteria

- Successful checkout works.
- Inventory reservation works.
- Payment integration works.
- Promotion integration works.
- Order persistence works.
- Order lifecycle works.
- Failure compensation works.
- Kafka events work.
- Idempotency tests pass.
- End-to-end checkout test passes.

This is a major project milestone.

---

# Phase 8 — Payment Service

## 41. Objective

Implement simulated payment processing.

---

## 42. Scope

Implement:

- Payment record.
- Payment status.
- Payment processing.
- Success/failure simulation.
- Idempotency.
- gRPC API.
- PostgreSQL persistence.
- Kafka payment events.
- Tests.

---

## 43. Payment States

```text
PENDING
   |
   +--> SUCCEEDED
   |
   +--> FAILED
```

---

## 44. Validation

Verify:

- Successful payment.
- Failed payment.
- Duplicate request.
- Idempotency.
- Invalid amount.
- Invalid order.
- gRPC status mapping.
- Kafka events.

No real payment provider is required.

---

# Phase 9 — Shipping Service

## 45. Objective

Implement shipment creation and delivery lifecycle.

---

## 46. Scope

Implement:

- Shipment creation.
- Tracking information.
- Shipping status.
- Kafka event consumption.
- REST retrieval/update where appropriate.
- Kafka publishing.
- PostgreSQL persistence.
- Tests.

---

## 47. Shipping Lifecycle

```text
CREATED
   ↓
IN_TRANSIT
   ↓
OUT_FOR_DELIVERY
   ↓
DELIVERED
```

---

## 48. Shipping Flow

```text
OrderConfirmed
      |
      v
Kafka
      |
      v
Shipping Service
      |
      v
Shipment Created
```

Shipping should be independently recoverable if the service temporarily fails.

---

# Phase 10 — Notification Service

## 49. Objective

Implement asynchronous customer notification processing.

---

## 50. Scope

Implement:

- Kafka consumers.
- Notification records.
- Simulated email/in-app notifications.
- Retry behavior.
- Duplicate event handling.
- Dead-letter handling.
- PostgreSQL persistence where used.
- Tests.

---

## 51. Notification Architecture

```text
Kafka
  |
  v
Notification Consumer
  |
  v
Notification Application
  |
  v
Notification Store
```

---

## 52. Events

Consume relevant events such as:

```text
UserRegistered
OrderConfirmed
OrderCancelled
PaymentFailed
ShipmentCreated
ShipmentDelivered
```

The exact consumer matrix follows Document 11.

---

## 53. Validation

Verify:

- Event consumption.
- Notification generation.
- Duplicate events.
- Retry.
- DLQ.
- Service restart.
- Notification failure does not invalidate business state.

---

# Phase 11 — Review Service

## 54. Objective

Implement customer product reviews.

---

## 55. Scope

Implement:

- Create review.
- Retrieve reviews.
- Update own review.
- Delete own review.
- Rating validation.
- Customer ownership.
- Product reference.
- PostgreSQL persistence.
- REST API.
- Tests.

---

## 56. Validation

Verify:

- Rating 1–5.
- Customer ownership.
- Invalid rating rejection.
- Unauthorized modification rejection.
- Product reference handling.
- API behavior.

---

# Phase 12 — Search Service

## 57. Objective

Implement product search and demonstrate event-driven read synchronization.

Search is intentionally PostgreSQL-based for the initial project.

---

## 58. Scope

Implement:

- Search endpoint.
- Product indexing.
- Product update synchronization.
- Product deactivation.
- Filtering.
- Sorting.
- Pagination.
- Kafka consumer.
- PostgreSQL search storage.
- Tests.

---

## 59. Search Architecture

```text
Product Service
      |
      v
Kafka
      |
      v
Search Service
      |
      v
PostgreSQL
```

Product Service remains the source of truth.

---

## 60. Validation

Verify:

- ProductCreated updates search.
- ProductUpdated updates search.
- ProductDeactivated removes/deactivates search result.
- Search filtering works.
- Eventual consistency works.
- Replay/re-indexing is safe.

---

# Phase 13 — API Gateway Completion

## 61. Objective

Complete and harden the API Gateway after the service surface is known.

---

## 62. Scope

Implement/refine:

- YARP routing.
- JWT handling.
- Rate limiting.
- Request size limits.
- Correlation IDs.
- Trace propagation.
- Gateway error handling.
- Service routing.
- Health aggregation where useful.
- API documentation exposure.

---

## 63. Validation

Verify:

```text
Client
  |
  v
Gateway
  |
  +--> Auth
  +--> User
  +--> Product
  +--> Cart
  +--> Order
  +--> Review
  +--> Search
  ...
```

Also verify:

- Unauthorized access.
- Rate limiting.
- Invalid routes.
- Upstream failures.

---

# Phase 14 — Cross-Service Hardening

## 64. Objective

Stabilize the complete distributed system.

---

## 65. Scope

Review:

- Service boundaries.
- Database ownership.
- REST contracts.
- gRPC contracts.
- Kafka event contracts.
- Idempotency.
- Retry policies.
- Timeouts.
- Error handling.
- Transaction boundaries.
- Outbox implementation where required.

---

## 66. Reliability Validation

Test:

```text
Kafka unavailable
Redis unavailable
PostgreSQL unavailable
Inventory unavailable
Payment unavailable
Promotion unavailable
```

Verify service isolation and recovery behavior.

---

# Phase 15 — Testing Expansion

## 67. Objective

Bring the testing architecture from Document 17 into practice.

---

## 68. Scope

Implement:

- Unit test suites.
- Integration tests.
- API tests.
- gRPC tests.
- Kafka contract tests.
- Redis tests.
- PostgreSQL tests.
- Idempotency tests.
- Security tests.
- E2E checkout tests.
- Failure tests.

---

## 69. Testcontainers

Introduce Testcontainers for:

```text
PostgreSQL
Redis
Kafka
```

This should allow repeatable CI integration testing.

---

# Phase 16 — Observability

## 70. Objective

Implement the observability architecture.

---

## 71. Scope

Implement:

- Structured logging.
- OpenTelemetry.
- Distributed tracing.
- Metrics.
- Correlation IDs.
- Kafka metrics.
- Redis metrics.
- PostgreSQL metrics.
- Business metrics.
- Grafana dashboards.

---

## 72. Primary Trace

The main distributed trace should be:

```text
Gateway
  ↓
Order
  ↓
Promotion
  ↓
Inventory
  ↓
Payment
  ↓
Kafka
  ↓
Shipping
  ↓
Notification
```

This demonstrates the value of distributed tracing.

---

# Phase 17 — Dockerization

## 73. Objective

Containerize every application.

---

## 74. Scope

Create Docker images for:

- Gateway.
- 12 services.

Validate:

- Multi-stage builds.
- Configuration injection.
- Health checks.
- Non-root execution where practical.
- Image size.
- Runtime dependencies.

---

## 75. Full Docker Compose Environment

At this stage, support:

```text
Gateway
+
12 Services
+
PostgreSQL
+
Redis
+
Kafka
+
Schema Registry
```

The complete application should be runnable locally through Docker Compose.

---

# Phase 18 — Kubernetes / Minikube

## 76. Objective

Deploy CommerceX to Kubernetes.

---

## 77. Scope

Implement:

- Namespace.
- Deployments.
- Services.
- ConfigMaps.
- Secrets.
- PersistentVolumeClaims.
- Health probes.
- Resource requests/limits.
- NetworkPolicies.
- Service discovery.
- Gateway exposure.

---

## 78. Deployment Sequence

Recommended:

```text
Namespace
   ↓
PostgreSQL
   ↓
Redis
   ↓
Kafka
   ↓
Schema Registry
   ↓
Auth
   ↓
User
   ↓
Product
   ↓
Promotion
   ↓
Cart
   ↓
Inventory
   ↓
Order
   ↓
Payment
   ↓
Shipping
   ↓
Notification
   ↓
Review
   ↓
Search
   ↓
Gateway
```

Infrastructure should be ready before dependent applications.

---

# Phase 19 — CI/CD

## 79. Objective

Automate build, test, packaging, and deployment.

---

## 80. Scope

Implement GitHub Actions workflows for:

- Build.
- Unit tests.
- Integration tests.
- Contract tests.
- Security scanning.
- Docker builds.
- Image scanning.
- Image publishing.
- Kubernetes deployment.
- Smoke tests.

---

## 81. Pipeline

```text
Pull Request
     ↓
Build
     ↓
Tests
     ↓
Security
     ↓
Docker
     ↓
Registry
     ↓
Kubernetes
     ↓
Smoke Tests
```

---

# Phase 20 — Final System Validation

## 82. Objective

Validate CommerceX as a complete distributed system.

---

## 83. Full Business Scenarios

### Scenario 1 — Customer Registration

```text
Register
 ↓
Auth
 ↓
User Profile
```

### Scenario 2 — Browse Catalog

```text
Gateway
 ↓
Product/Search
 ↓
Product Results
```

### Scenario 3 — Shopping Cart

```text
Product
 ↓
Cart
 ↓
Redis
```

### Scenario 4 — Successful Checkout

```text
Cart
 ↓
Order
 ↓
Promotion
 ↓
Inventory
 ↓
Payment
 ↓
Order Confirmed
 ↓
Kafka
 ↓
Shipping + Notification
```

### Scenario 5 — Payment Failure

```text
Order
 ↓
Inventory Reservation
 ↓
Payment Failure
 ↓
Inventory Release
 ↓
Order Not Confirmed
```

### Scenario 6 — Product Update

```text
Product Update
 ↓
Kafka
 ↓
Search Update
```

### Scenario 7 — Shipping Completion

```text
Shipment
 ↓
Kafka
 ↓
Order Delivered
 ↓
Notification
```

---

# Phase 21 — Performance Validation

## 84. Objective

Measure whether the system meets the initial NFR targets.

---

## 85. Scope

Measure:

- Product read latency.
- Search latency.
- Cart latency.
- Order creation latency.
- Checkout latency.
- Kafka processing delay.
- Redis latency.
- PostgreSQL query performance.

Capture:

```text
p50
p95
p99
```

---

## 86. Load Scenarios

Initial tests:

```text
Product reads
Search
Cart operations
Order creation
Checkout
```

Do not optimize prematurely.

Measure first, then improve bottlenecks.

---

# Phase 22 — Security Validation

## 87. Objective

Perform a final security review.

---

## 88. Scope

Validate:

- Password hashing.
- JWT validation.
- Refresh-token protection.
- Role authorization.
- Resource ownership.
- Rate limiting.
- Input validation.
- Secret handling.
- Container security.
- Kubernetes security.
- Kafka/Redis exposure.
- Error disclosure.

---

# Phase 23 — Final Documentation

## 89. Objective

Complete the project documentation.

---

## 90. Deliverables

Finalize:

```text
Project Charter
System Requirements
Functional Requirements
Non-Functional Requirements
System Architecture
Microservices Architecture
Service Boundaries
Data Architecture
API Design
gRPC Design
Kafka Event Design
Redis Caching Strategy
Security Design
Docker Design
Kubernetes Design
Observability Design
Testing Strategy
CI/CD Design
Development Roadmap
Final Project Report
```

---

# 24. Milestone Structure

CommerceX should be considered to have the following major milestones:

| Milestone | Result |
|---|---|
| M0 | Infrastructure foundation |
| M1 | Authentication |
| M2 | Customer management |
| M3 | Product catalog |
| M4 | Promotions |
| M5 | Shopping cart |
| M6 | Inventory |
| M7 | Checkout/order workflow |
| M8 | Payment |
| M9 | Shipping |
| M10 | Notifications |
| M11 | Reviews |
| M12 | Search |
| M13 | Full API Gateway |
| M14 | Reliability hardening |
| M15 | Testing expansion |
| M16 | Observability |
| M17 | Docker |
| M18 | Kubernetes |
| M19 | CI/CD |
| M20 | Final validation |

---

# 25. Recommended Implementation Order

The recommended concrete implementation sequence is:

```text
1. Infrastructure
2. Auth
3. User
4. Product
5. Promotion
6. Cart
7. Inventory
8. Order
9. Payment
10. Shipping
11. Notification
12. Review
13. Search
14. Gateway hardening
15. Cross-service reliability
16. Testing expansion
17. Observability
18. Docker
19. Kubernetes
20. CI/CD
21. Final validation
```

The exact ordering can be adjusted when a dependency requires it, but unrelated services should not be implemented prematurely.

---

# 26. Phase Completion Rule

Every phase should follow:

```text
Plan
 ↓
Implement
 ↓
Build
 ↓
Unit Test
 ↓
Integration Test
 ↓
Review
 ↓
Document
 ↓
Commit
 ↓
Next Phase
```

Do not move forward simply because the code compiles.

---

# 27. Git Commit Strategy

Commits should represent meaningful completed units.

Examples:

```text
feat(auth): establish auth service foundation
feat(auth): add postgres persistence
feat(auth): implement registration
feat(auth): implement login and tokens
test(auth): add authentication integration tests
feat(product): add product catalog
feat(inventory): add stock reservation
feat(order): implement checkout orchestration
```

Avoid giant commits containing unrelated services.

---

# 28. Branch Strategy During Implementation

A service can use a dedicated feature branch:

```text
main
 |
 +-- feature/auth-foundation
 +-- feature/user-service
 +-- feature/product-service
 +-- feature/inventory-service
 +-- feature/order-service
```

Merge only after the relevant validation gates pass.

---

# 29. Documentation Discipline

When an implementation decision changes the architecture:

```text
Implementation Change
       ↓
Review Existing Document
       ↓
Update Relevant Document
       ↓
Record Decision
       ↓
Continue Implementation
```

Code and architecture documentation should not intentionally diverge.

---

# 30. Dependency-Aware Planning

The main dependencies are:

```text
Auth
  ↓
User

Product
  ↓
Cart

Promotion
  ↓
Order

Inventory
  ↓
Order

Payment
  ↓
Order

Order
  ↓
Shipping
  ↓
Notification

Product
  ↓
Search
```

This explains the recommended implementation order.

---

# 31. Critical Path

The most important implementation path is:

```text
Auth
  ↓
Product
  ↓
Cart
  ↓
Inventory
  ↓
Payment
  ↓
Order
  ↓
Shipping
  ↓
Notification
```

This path demonstrates the primary distributed commerce workflow.

---

# 32. Learning Objectives by Phase

| Phase | Main Learning |
|---|---|
| Infrastructure | Docker/Kafka/Redis/PostgreSQL |
| Auth | Security/JWT/EF Core |
| User | REST/service boundaries |
| Product | CRUD/caching/events |
| Promotion | gRPC/business rules |
| Cart | Redis |
| Inventory | concurrency/idempotency |
| Order | distributed orchestration |
| Payment | service isolation/gRPC |
| Shipping | Kafka workflows |
| Notification | event consumers |
| Review | authorization/data ownership |
| Search | eventual consistency |
| Testing | distributed testing |
| Observability | tracing/metrics/logging |
| Docker | containerization |
| Kubernetes | orchestration |
| CI/CD | automation/deployment |

---

# 33. Complexity Control

The following rules keep the project manageable:

### Do Not Initially Add

- Elasticsearch/OpenSearch.
- Real payment providers.
- Real email/SMS providers.
- Service mesh.
- Istio.
- Multi-region deployment.
- Multi-cloud.
- Advanced recommendation engines.
- Complex pricing engines.
- Event sourcing.
- Full CQRS.
- Distributed transactions.
- Complex workflow engines.

### Prefer

- PostgreSQL.
- Redis.
- Kafka.
- gRPC where justified.
- REST.
- Simple domain models.
- Explicit events.
- Simple Kubernetes manifests.
- GitHub Actions.
- Testcontainers.

---

# 34. Definition of Done — Service

A service is complete when:

- [ ] Domain model implemented.
- [ ] Application use cases implemented.
- [ ] Infrastructure implemented.
- [ ] Database/cache implemented.
- [ ] API implemented where required.
- [ ] gRPC implemented where required.
- [ ] Kafka events implemented where required.
- [ ] Authentication/authorization implemented.
- [ ] Unit tests pass.
- [ ] Integration tests pass.
- [ ] Failure behavior tested.
- [ ] Idempotency implemented where required.
- [ ] Health endpoints implemented.
- [ ] Docker image builds.
- [ ] Documentation updated.
- [ ] Changes committed.

---

# 35. Definition of Done — Phase

A phase is complete when:

```text
Implementation
     +
Tests
     +
Integration
     +
Documentation
     +
Build
     +
Deployment validation
```

all meet the phase's acceptance criteria.

---

# 36. Definition of Done — Entire Project

CommerceX is complete when:

- All 12 services are implemented.
- Gateway is operational.
- PostgreSQL service databases work.
- Redis cart/cache functionality works.
- Kafka workflows work.
- gRPC contracts work.
- REST APIs work.
- Authentication and authorization work.
- Checkout works successfully.
- Failure scenarios work correctly.
- Notifications work asynchronously.
- Search synchronization works.
- Reviews work.
- Automated tests pass.
- Docker deployment works.
- Kubernetes deployment works.
- Observability works.
- CI/CD works.
- Security validation passes.
- Documentation is complete.
- Final project report is produced.

---

# 37. Final Acceptance Scenario

The strongest final demonstration should be:

```text
1. Customer registers.
        ↓
2. Customer logs in.
        ↓
3. Customer browses/searches products.
        ↓
4. Customer adds products to cart.
        ↓
5. Customer applies a promotion.
        ↓
6. Customer creates an order.
        ↓
7. Inventory is reserved.
        ↓
8. Payment succeeds.
        ↓
9. Order becomes confirmed.
        ↓
10. OrderConfirmed event is published.
        ↓
11. Shipping creates a shipment.
        ↓
12. Notification is generated.
        ↓
13. Shipment progresses.
        ↓
14. Order becomes delivered.
        ↓
15. Customer submits a review.
```

This single scenario demonstrates most of the CommerceX architecture.

---

# 38. Failure Demonstration

A second demonstration should intentionally fail payment:

```text
Customer Checkout
       ↓
Inventory Reserved
       ↓
Payment Fails
       ↓
PaymentFailed
       ↓
Inventory Released
       ↓
Order Remains Unconfirmed
       ↓
No Shipment
```

This demonstrates distributed failure handling rather than only the happy path.

---

# 39. Final Architecture Validation

Before final submission, validate:

### Business

- [ ] All core workflows work.

### Architecture

- [ ] Service boundaries remain clear.
- [ ] No cross-service DB access exists.
- [ ] Communication choices match architecture.

### Data

- [ ] Each service owns its data.
- [ ] Historical snapshots are correct.
- [ ] Migrations work.

### Messaging

- [ ] Kafka events are versioned.
- [ ] Consumers are idempotent.
- [ ] Retry/DLQ behavior works.

### Caching

- [ ] Cart uses Redis.
- [ ] Product caching works.
- [ ] Cache failures are handled.

### Security

- [ ] Authentication works.
- [ ] Authorization works.
- [ ] Secrets are protected.

### Testing

- [ ] Critical tests pass.
- [ ] Checkout E2E passes.
- [ ] Failure scenarios pass.

### Infrastructure

- [ ] Docker works.
- [ ] Kubernetes works.
- [ ] CI/CD works.

### Observability

- [ ] Logs work.
- [ ] Metrics work.
- [ ] Traces work.
- [ ] Grafana dashboards work.

---

# 40. Roadmap Risk Management

Potential risks include:

### Too Much Infrastructure Too Early

Mitigation:

```text
Implement only what the current phase needs.
```

### Service Complexity

Mitigation:

```text
Keep domains intentionally small.
```

### Distributed Debugging Difficulty

Mitigation:

```text
Implement correlation IDs and tracing before the system becomes large.
```

### Kafka Complexity

Mitigation:

```text
Use Kafka only for meaningful asynchronous workflows.
```

### Kubernetes Complexity

Mitigation:

```text
Validate Docker deployment first.
```

### CI/CD Complexity

Mitigation:

```text
Automate the existing manual process rather than inventing a complex process.
```

---

# 41. Recommended Phase Execution Pattern

For every service:

```text
Step 1 — Define Domain
        ↓
Step 2 — Define Application Contracts
        ↓
Step 3 — Implement Infrastructure
        ↓
Step 4 — Implement API/gRPC
        ↓
Step 5 — Implement Messaging
        ↓
Step 6 — Unit Tests
        ↓
Step 7 — Integration Tests
        ↓
Step 8 — Docker
        ↓
Step 9 — Validate
        ↓
Step 10 — Commit
```

This pattern should be reused across services.

---

# 42. Current Project Progress

The roadmap should distinguish between planned and already completed work.

Based on the current CommerceX implementation baseline:

### Completed

- Project architecture/documentation baseline.
- Infrastructure foundation.
- Docker Compose infrastructure.
- PostgreSQL.
- Redis.
- Kafka.
- Schema Registry.
- Kubernetes infrastructure foundation.
- Auth Domain/Application foundations.
- Auth Infrastructure persistence foundation.
- Auth EF Core migration generation.

### Current Focus

**Auth Service completion and validation.**

The implementation should continue from the established Auth baseline rather than restarting earlier phases.

### Planned Next

After Auth is fully validated:

```text
User
 ↓
Product
 ↓
Promotion
 ↓
Cart
 ↓
Inventory
 ↓
Order
...
```

---

# 43. Implementation Discipline

The following rule is authoritative:

> Do not restart completed work. Do not jump ahead to later infrastructure concerns before the current phase is stable.

When a phase encounters a problem:

```text
Identify Problem
      ↓
Fix Smallest Correct Layer
      ↓
Build
      ↓
Test
      ↓
Continue
```

Avoid unrelated refactoring during phase implementation.

---

# 44. Project Milestone Timeline

No fixed calendar duration is required because CommerceX is a learning project.

Instead, progress should be measured by completed milestones:

```text
M0 → Infrastructure
M1 → Auth
M2 → User
M3 → Product
M4 → Promotion
M5 → Cart
M6 → Inventory
M7 → Order/Checkout
M8 → Payment
M9 → Shipping
M10 → Notification
M11 → Review
M12 → Search
M13 → Gateway
M14 → Reliability
M15 → Testing
M16 → Observability
M17 → Docker
M18 → Kubernetes
M19 → CI/CD
M20 → Final Validation
```

---

# 45. Final Deliverable Structure

The completed repository should contain:

```text
CommerceX/
├── src/
│   ├── Gateway/
│   ├── Services/
│   │   ├── Auth/
│   │   ├── User/
│   │   ├── Product/
│   │   ├── Inventory/
│   │   ├── Cart/
│   │   ├── Order/
│   │   ├── Payment/
│   │   ├── Shipping/
│   │   ├── Review/
│   │   ├── Notification/
│   │   ├── Promotion/
│   │   └── Search/
│   └── BuildingBlocks/
│
├── tests/
│
├── infra/
│   ├── docker/
│   └── kubernetes/
│
├── docs/
│   ├── architecture/
│   ├── requirements/
│   └── implementation/
│
├── .github/
│   └── workflows/
│
└── README.md
```

---

# 46. Final README Requirements

The final README should explain:

- What CommerceX is.
- Why it was built.
- Architecture.
- Service list.
- Technology stack.
- Local setup.
- Docker setup.
- Kubernetes setup.
- Testing.
- CI/CD.
- Observability.
- Main checkout flow.
- Repository structure.
- Documentation index.

---

# 47. Learning Outcomes

After completing CommerceX, the project should demonstrate practical understanding of:

### Backend

- ASP.NET Core.
- .NET 9.
- EF Core.
- REST APIs.
- Authentication.

### Distributed Systems

- Microservice boundaries.
- Service communication.
- Event-driven architecture.
- Eventual consistency.
- Idempotency.
- Failure handling.

### Infrastructure

- PostgreSQL.
- Redis.
- Kafka.
- Schema Registry.
- Docker.
- Kubernetes.

### Communication

- REST.
- gRPC.
- Kafka events.

### DevOps

- Git.
- GitHub.
- GitHub Actions.
- Container registries.
- Kubernetes deployment.

### Observability

- OpenTelemetry.
- Distributed tracing.
- Metrics.
- Structured logging.
- Grafana.

### Testing

- Unit testing.
- Integration testing.
- Contract testing.
- E2E testing.
- Failure testing.
- Performance testing.

---

# 48. Roadmap Summary

The complete CommerceX development strategy is:

```text
FOUNDATION
    |
    v
AUTHENTICATION
    |
    v
CUSTOMER + CATALOG
    |
    v
SHOPPING
    |
    v
INVENTORY + CHECKOUT
    |
    v
PAYMENT + FULFILLMENT
    |
    v
EVENT-DRIVEN FEATURES
    |
    v
TESTING + RELIABILITY
    |
    v
OBSERVABILITY
    |
    v
DOCKER
    |
    v
KUBERNETES
    |
    v
CI/CD
    |
    v
FINAL VALIDATION
```

This sequencing allows CommerceX to evolve from a small working backend into a complete distributed platform without introducing unnecessary complexity prematurely.

---

# 49. Architecture Baseline Decisions

The following roadmap decisions are established:

1. CommerceX is implemented incrementally.
2. Infrastructure is established before dependent services.
3. Auth is the first business service.
4. Services are implemented in dependency-aware order.
5. Each service must be independently buildable and testable.
6. Service boundaries defined in Documents 06 and 07 remain authoritative.
7. PostgreSQL remains the primary durable relational database.
8. Redis remains the primary Cart store and selective cache.
9. Kafka is introduced progressively for asynchronous workflows.
10. gRPC is introduced only where synchronous internal communication is justified.
11. REST remains the primary external API style.
12. Checkout is the primary distributed business workflow.
13. Payment remains simulated.
14. Search remains PostgreSQL-based initially.
15. Testing is introduced throughout development rather than only at the end.
16. Observability is introduced before final distributed-system validation.
17. Docker is validated before Kubernetes.
18. Kubernetes is validated before CI/CD deployment automation.
19. GitHub Actions is introduced after the build/test workflow is stable.
20. Advanced infrastructure is deferred until justified.
21. Completed work must not be unnecessarily restarted.
22. Implementation should proceed one phase at a time.
23. Every phase requires build, test, validation, documentation, and commit.
24. The final project must demonstrate both successful and failed distributed workflows.

---

# 50. Relationship With Other Documents

This roadmap operationalizes the decisions established in Documents 01–18.

The documentation sequence is now:

```text
01 Project Charter
02 System Requirements
03 Functional Requirements
04 Non-Functional Requirements
05 System Architecture
06 Microservices Architecture
07 Service Boundaries
08 Data Architecture
09 API Design
10 gRPC Design
11 Kafka Event Design
12 Redis Caching Strategy
13 Security Design
14 Docker Design
15 Kubernetes Design
16 Observability Design
17 Testing Strategy
18 CI/CD Design
19 Development Roadmap
20 Final Project Report
```

The final document will consolidate the project into a comprehensive report suitable for academic/project presentation and future portfolio use.

---

# 51. Baseline Status

This document establishes the **CommerceX Development Roadmap baseline**.

Future implementation should follow this roadmap unless a later architectural decision explicitly changes the sequence or scope.

**Next document:** Document 20 — Final Project Report
