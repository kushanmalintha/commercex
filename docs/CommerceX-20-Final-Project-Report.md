# CommerceX — Final Project Report

**Project:** CommerceX — Distributed E-Commerce Backend Platform  
**Document:** 20 — Final Project Report  
**Status:** Final Documentation Baseline  
**Architecture Documents:** 01–19  
**Technology Stack:** C#, .NET 9, ASP.NET Core, PostgreSQL, Redis, Apache Kafka, gRPC, REST, Docker, Kubernetes, GitHub Actions, OpenTelemetry, Grafana

---

# 1. Executive Summary

CommerceX is a learning-focused distributed e-commerce backend platform designed to demonstrate practical software engineering concepts used in modern distributed systems.

The platform is implemented as a collection of independently deployable microservices behind an API Gateway. Each service owns a clearly defined business capability and its associated data. Services communicate using REST, gRPC, and Apache Kafka according to the communication requirements of each workflow.

The system uses PostgreSQL for durable relational data, Redis for shopping-cart storage and selective caching, Kafka for asynchronous event-driven communication, Docker for containerization, Kubernetes for orchestration, OpenTelemetry and Grafana for observability, and GitHub Actions for CI/CD automation.

The central business workflow is checkout:

```text
Customer
   ↓
API Gateway
   ↓
Order Service
   ↓
Promotion Validation
   ↓
Inventory Reservation
   ↓
Payment Processing
   ↓
Order Confirmation
   ↓
Kafka Events
   ↓
Shipping + Notification
```

The project deliberately balances realistic architecture with manageable implementation complexity. Advanced production technologies such as service meshes, multi-region infrastructure, real payment providers, and Elasticsearch are intentionally outside the initial scope.

---

# 2. Project Introduction

Modern e-commerce platforms provide a useful environment for learning distributed systems because they combine:

- Authentication.
- Customer management.
- Product catalogs.
- Inventory.
- Shopping carts.
- Promotions.
- Orders.
- Payments.
- Shipping.
- Reviews.
- Notifications.
- Search.
- Asynchronous processing.
- Caching.
- Observability.
- Deployment automation.

CommerceX uses this domain to create a practical environment in which these concepts can be implemented together.

The project is not intended to reproduce the scale or operational complexity of a commercial platform such as Amazon or Shopify. Instead, it provides a realistic architecture that can be implemented, tested, deployed, observed, and explained by a developer or student.

---

# 3. Problem Statement

A traditional monolithic e-commerce application can become difficult to scale and maintain as different business capabilities grow.

Common problems include:

- Tight coupling between business modules.
- Shared database dependencies.
- Difficult independent deployment.
- Limited failure isolation.
- Scaling the entire application when only one component needs more capacity.
- Increasing complexity around asynchronous workflows.
- Difficult distributed troubleshooting.

CommerceX addresses these challenges by separating business capabilities into independently deployable services.

The project also demonstrates the additional engineering challenges introduced by distributed architecture, including:

- Network failures.
- Eventual consistency.
- Idempotency.
- Service-to-service communication.
- Distributed tracing.
- Data ownership.
- Deployment coordination.

---

# 4. Project Vision

The vision of CommerceX is:

> Build a practical, observable, testable, and independently deployable e-commerce backend that demonstrates modern distributed-system engineering using a manageable technology stack.

The system should be understandable enough for learning while remaining realistic enough to demonstrate professional architecture and engineering practices.

---

# 5. Project Objectives

The main objectives are to:

1. Design a microservices-based e-commerce backend.
2. Implement independently deployable services.
3. Apply clear service boundaries.
4. Use PostgreSQL with database ownership per service.
5. Use Redis for appropriate high-speed data access.
6. Use Kafka for asynchronous business events.
7. Use gRPC for selected internal synchronous communication.
8. Expose client-facing functionality through REST APIs.
9. Implement authentication and authorization.
10. Implement reliable checkout processing.
11. Handle distributed failures and retries.
12. Apply idempotency to important operations.
13. Containerize services with Docker.
14. Deploy services with Kubernetes.
15. Implement observability using OpenTelemetry and Grafana.
16. Automate validation and delivery using GitHub Actions.
17. Develop automated tests at multiple levels.
18. Produce complete technical documentation.

---

# 6. Learning Objectives

CommerceX is specifically designed as a learning project.

The project provides practical experience with:

### Backend Development

- C#.
- .NET 9.
- ASP.NET Core.
- Entity Framework Core.
- REST APIs.
- Authentication.

### Distributed Systems

- Microservices.
- Service boundaries.
- Event-driven architecture.
- Eventual consistency.
- Idempotency.
- Failure handling.
- Distributed workflows.

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
- API testing.
- Contract testing.
- End-to-end testing.
- Failure testing.
- Performance testing.

---

# 7. Project Scope

## 7.1 In Scope

CommerceX includes:

- Customer registration and authentication.
- Customer profiles.
- Customer addresses.
- Product catalog.
- Categories.
- Product search.
- Shopping carts.
- Inventory.
- Stock reservations.
- Promotions.
- Orders.
- Simulated payments.
- Shipping.
- Reviews.
- Notifications.
- Event-driven communication.
- Caching.
- API Gateway.
- Docker.
- Kubernetes.
- Automated testing.
- Observability.
- CI/CD.

---

## 7.2 Out of Scope

The initial system intentionally excludes:

- Real payment gateway integration.
- Real email/SMS provider integration.
- Elasticsearch/OpenSearch.
- Advanced recommendation engines.
- Multi-region deployment.
- Multi-cloud deployment.
- Service mesh.
- Istio.
- Complex warehouse management.
- Advanced pricing engines.
- Full event sourcing.
- Complex CQRS.
- Distributed two-phase transactions.

These may be future extensions.

---

# 8. System Overview

CommerceX consists of:

```text
Client
   |
   v
API Gateway
   |
   +---------------------------------------------------+
   |                                                   |
   v                                                   v
Business Services                              Infrastructure
   |                                                   |
   +--> Auth                                           +--> PostgreSQL
   +--> User                                           +--> Redis
   +--> Product                                        +--> Kafka
   +--> Inventory                                      +--> Schema Registry
   +--> Cart
   +--> Order
   +--> Payment
   +--> Shipping
   +--> Review
   +--> Notification
   +--> Promotion
   +--> Search
```

The API Gateway is an infrastructure/application entry point and is not counted among the 12 business services.

---

# 9. Service Landscape

CommerceX contains exactly 12 independently deployable backend business services.

| # | Service | Main Responsibility |
|---:|---|---|
| 1 | Auth | Authentication, credentials, tokens, roles |
| 2 | User | Profiles and addresses |
| 3 | Product | Products and categories |
| 4 | Inventory | Stock and reservations |
| 5 | Cart | Shopping carts |
| 6 | Order | Orders and checkout |
| 7 | Payment | Simulated payments |
| 8 | Shipping | Shipments and delivery |
| 9 | Review | Product reviews |
| 10 | Notification | Event-driven notifications |
| 11 | Promotion | Coupons and discounts |
| 12 | Search | Product search/read model |

---

# 10. Architectural Style

CommerceX follows a microservices architecture.

Each service has:

```text
API
Application
Domain
Infrastructure
```

Conceptually:

```text
                 Service
                    |
        +-----------+-----------+
        |           |           |
       API     Application    Domain
        |           |           |
        +-----------+-----------+
                    |
             Infrastructure
                    |
        +-----------+-----------+
        |           |           |
   PostgreSQL     Redis       Kafka/gRPC
```

Not every service uses every infrastructure technology.

---

# 11. Service Independence

Each service:

- Owns its business logic.
- Owns its data.
- Can be built independently.
- Can be tested independently.
- Can be containerized independently.
- Can be deployed independently.

A service must not directly access another service's database.

---

# 12. Database Architecture

PostgreSQL is the primary durable relational database.

The logical databases are:

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

Cart data is primarily stored in Redis.

For local development, multiple logical databases can run on one PostgreSQL server. The architecture still preserves service-level ownership.

---

# 13. Data Ownership

The ownership model is:

```text
Auth        → Identity credentials
User        → Customer profile/address
Product     → Product catalog
Inventory   → Stock
Cart        → Cart
Promotion   → Promotions
Order       → Orders
Payment     → Payments
Shipping    → Shipments
Review      → Reviews
Notification→ Notifications
Search      → Search read model
```

There are no cross-service foreign keys.

Services reference other business objects through identifiers.

---

# 14. Consistency Model

CommerceX uses:

### Strong/local consistency

Within a service database:

```text
Transaction
   ↓
Consistent local state
```

### Eventual consistency

Between services:

```text
Service A
   ↓
Kafka
   ↓
Service B
```

This is particularly important for:

- Search synchronization.
- Shipping creation.
- Notifications.
- Order state propagation.

---

# 15. Communication Architecture

CommerceX uses three primary communication styles.

| Communication | Purpose |
|---|---|
| REST | External/client-facing APIs |
| gRPC | Selected internal synchronous calls |
| Kafka | Asynchronous business events |

---

# 16. REST Architecture

External clients communicate through REST APIs.

The standard API structure is:

```text
/api/v1/...
```

Examples:

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
GET  /api/v1/products
GET  /api/v1/search/products
GET  /api/v1/cart
POST /api/v1/orders
GET  /api/v1/orders/{id}
POST /api/v1/reviews
```

The API Gateway is the preferred external entry point.

---

# 17. gRPC Architecture

gRPC is used selectively for internal synchronous operations.

Initial contracts include:

```text
Inventory
 ├── ReserveStock
 └── ReleaseStock

Promotion
 └── ValidatePromotion

Payment
 └── ProcessPayment
```

gRPC is not used for every service-to-service interaction.

---

# 18. Kafka Architecture

Kafka provides asynchronous event propagation.

Initial event categories include:

```text
UserRegistered
ProductCreated
ProductUpdated
ProductDeactivated
InventoryReserved
InventoryReleased
InventoryAdjusted
OrderCreated
OrderConfirmed
OrderCancelled
PaymentSucceeded
PaymentFailed
ShipmentCreated
ShipmentInTransit
ShipmentOutForDelivery
ShipmentDelivered
```

Events contain a standard envelope including:

```text
eventId
eventType
eventVersion
occurredAt
producer
correlationId
causationId
data
```

---

# 19. Kafka Topics

Initial topic structure:

```text
commercex.auth.events
commercex.product.events
commercex.inventory.events
commercex.order.events
commercex.payment.events
commercex.shipping.events
```

Consumers use independent consumer groups.

Examples:

```text
commercex.notification
commercex.search
commercex.shipping
commercex.order
```

---

# 20. Event Delivery Model

The initial system assumes:

> At-least-once event delivery.

Therefore, important consumers must be idempotent.

For important state-changing workflows, the Transactional Outbox Pattern is recommended.

The producing service owns its outbox records.

---

# 21. Redis Architecture

Redis is used selectively.

Primary uses:

1. Cart storage.
2. Product caching.
3. Category caching.

Example keys:

```text
commercex:cart:{customerId}
commercex:product:{productId}
commercex:category:{categoryId}
```

Redis is not the authoritative store for:

- Orders.
- Payments.
- Inventory.
- Customer profiles.

---

# 22. Caching Strategy

Product caching follows cache-aside:

```text
Request
  |
  v
Redis
  |
  +--> Hit → Return
  |
  +--> Miss
          |
          v
      PostgreSQL
          |
          v
       Redis
```

Writes update PostgreSQL first and then invalidate relevant cache entries.

---

# 23. Cart Strategy

The Cart Service uses Redis as its primary operational store.

Example:

```text
Customer
   |
   v
Cart Service
   |
   v
Redis
```

Cart expiration is controlled through TTL.

The initial recommended TTL is approximately seven days and should remain configurable.

---

# 24. API Gateway

The API Gateway is implemented using ASP.NET Core with YARP.

Responsibilities include:

- Routing.
- Authentication handling.
- Rate limiting.
- Request policies.
- Correlation IDs.
- Trace propagation.
- External API boundary.

The Gateway should not contain core business logic.

---

# 25. Security Architecture

CommerceX uses:

- JWT access tokens.
- Refresh tokens.
- Secure password hashing.
- Role-based authorization.
- Resource ownership checks.
- Input validation.
- Rate limiting.
- Secret management.
- Secure error responses.

Roles initially include:

```text
Customer
Admin
```

---

# 26. Authentication

Auth Service owns:

- Credentials.
- Password hashing.
- Access tokens.
- Refresh tokens.
- Password reset tokens.
- Roles.

Passwords are never stored in plaintext.

Refresh tokens should be securely stored, revocable, expirable, and preferably represented in storage by a secure hash.

---

# 27. Authorization

Authorization exists at multiple boundaries.

Example:

```text
Gateway
   ↓
Service
   ↓
Application Authorization
   ↓
Resource Ownership
```

The Gateway must not be the only authorization enforcement point.

---

# 28. Main Business Workflow

The primary CommerceX workflow is checkout.

```text
Customer
   |
   v
API Gateway
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
   |
   +--> Shipping
   |
   +--> Notification
```

This workflow demonstrates:

- REST.
- gRPC.
- Kafka.
- Redis.
- PostgreSQL.
- Transactions.
- Idempotency.
- Eventual consistency.
- Distributed tracing.

---

# 29. Order Lifecycle

The order lifecycle is:

```text
PENDING
   ↓
PAYMENT_PENDING
   ↓
CONFIRMED
   ↓
PROCESSING
   ↓
SHIPPED
   ↓
DELIVERED
```

Cancellation is allowed from eligible states.

Invalid transitions must be rejected.

---

# 30. Payment Lifecycle

Payment uses:

```text
PENDING
   |
   +--> SUCCEEDED
   |
   +--> FAILED
```

Payment is intentionally simulated.

This allows the project to demonstrate distributed payment behavior without integrating with a real financial provider.

---

# 31. Shipping Lifecycle

Shipping follows:

```text
CREATED
   ↓
IN_TRANSIT
   ↓
OUT_FOR_DELIVERY
   ↓
DELIVERED
```

Shipping is primarily event-driven from Order events.

---

# 32. Inventory Management

Inventory is authoritative for stock.

Important rules:

- Stock cannot become negative.
- Reservation cannot exceed available stock.
- Reservation must be idempotent.
- Release must be idempotent where required.
- Concurrent reservation must be handled safely.

Example:

```text
Available = 1

Customer A → Reserve 1 → Success
Customer B → Reserve 1 → Failure
```

---

# 33. Distributed Failure Handling

CommerceX does not assume every dependency is always available.

Example:

```text
Inventory Reserved
       |
       v
Payment
       |
       X
    Failure
       |
       v
Release Inventory
       |
       v
Order Not Confirmed
```

Compensating actions are preferred over distributed transactions.

---

# 34. Idempotency

Idempotency is required for important operations.

Examples:

- Order creation.
- Payment.
- Inventory reservation.
- Inventory release.
- Kafka event consumers.

Example:

```text
Request A
   ↓
Payment succeeds
   ↓
Network timeout
   ↓
Request A retried
   ↓
Same operation/result
```

No duplicate payment should be created.

---

# 35. Observability

CommerceX implements the three observability pillars:

```text
Logs
Metrics
Traces
```

OpenTelemetry provides the application telemetry framework.

Grafana is used for visualization.

---

# 36. Distributed Tracing

The primary trace is the checkout workflow.

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

W3C trace context is used for propagation.

---

# 37. Correlation IDs

Requests can use:

```text
X-Correlation-ID
```

The correlation ID is propagated through relevant service interactions and logs.

Trace IDs remain the primary distributed tracing identifier.

---

# 38. Metrics

Important metrics include:

### API

- Request count.
- Request duration.
- Status codes.
- p50/p95/p99 latency.

### Kafka

- Messages produced.
- Messages consumed.
- Consumer lag.
- Processing failures.
- DLQ volume.

### Redis

- Cache hits.
- Cache misses.
- Hit ratio.
- Latency.
- Memory usage.

### PostgreSQL

- Connection usage.
- Query latency.
- Failures.
- Timeouts.

### Business

- Orders created.
- Orders confirmed.
- Orders cancelled.
- Payments succeeded/failed.
- Inventory reservations.
- Checkout success/failure.

---

# 39. Logging

Services use structured logs.

Logs should include useful operational context such as:

```text
timestamp
service
level
event
traceId
correlationId
operation
```

Sensitive information must never be logged.

Examples of prohibited log data:

- Passwords.
- Password hashes.
- Access tokens.
- Refresh tokens.
- JWT signing keys.
- Database passwords.
- Redis credentials.
- Kafka credentials.
- Payment credentials.

---

# 40. Testing Strategy

CommerceX follows a testing pyramid:

```text
             E2E
              /             /         API/Contract
          /            Integration
        /               /  Unit          /____________```

Most tests should be fast unit tests.

Integration and E2E tests are used where real infrastructure or distributed behavior matters.

---

# 41. Testing Levels

The project includes:

- Unit tests.
- Application/domain tests.
- Integration tests.
- REST API tests.
- gRPC tests.
- Kafka contract/event tests.
- Component tests.
- End-to-end tests.
- Security tests.
- Failure tests.
- Performance tests.
- Docker tests.
- Kubernetes smoke tests.

---

# 42. Test Infrastructure

Testcontainers is recommended for integration tests.

Example:

```text
xUnit
 |
 +--> PostgreSQL
 +--> Redis
 +--> Kafka
```

This creates disposable infrastructure for repeatable testing.

---

# 43. Critical End-to-End Test

The primary E2E scenario is:

```text
Register
 ↓
Login
 ↓
Browse Product
 ↓
Add to Cart
 ↓
Apply Promotion
 ↓
Create Order
 ↓
Reserve Inventory
 ↓
Process Payment
 ↓
Confirm Order
 ↓
Create Shipment
 ↓
Generate Notification
 ↓
Deliver Order
 ↓
Submit Review
```

---

# 44. Security Testing

Security tests cover:

- Authentication.
- Authorization.
- Resource ownership.
- Token validation.
- Token expiration.
- Rate limiting.
- Input validation.
- Secret protection.
- Error disclosure.
- Container security.
- Kubernetes security.

---

# 45. Docker Architecture

Every application is packaged independently.

Images include:

```text
commercex-gateway
commercex-auth
commercex-user
commercex-product
commercex-inventory
commercex-cart
commercex-order
commercex-payment
commercex-shipping
commercex-review
commercex-notification
commercex-promotion
commercex-search
```

Each application uses a multi-stage .NET Docker build.

---

# 46. Docker Compose

Docker Compose provides the primary local environment.

Infrastructure includes:

```text
PostgreSQL
Redis
Kafka
Schema Registry
```

As the application matures, the complete CommerceX stack can be executed with:

```text
Gateway
+
12 Services
+
Infrastructure
```

---

# 47. Kubernetes Architecture

The initial Kubernetes environment is Minikube.

CommerceX uses:

```text
Namespace:
commercex
```

Application workloads use:

- Deployments.
- ClusterIP Services.
- ConfigMaps.
- Secrets.
- Health probes.
- Resource limits.
- Persistent volumes for stateful infrastructure.

---

# 48. Kubernetes Service Discovery

Internal services communicate through Kubernetes DNS.

Examples:

```text
postgres-service
redis-service
kafka-service
```

Application services should not depend on hard-coded Pod IP addresses.

---

# 49. Kubernetes Health

Applications expose:

```text
/health/live
/health/ready
```

The probes distinguish:

- Process liveness.
- Dependency readiness.

This prevents unhealthy workloads from receiving traffic.

---

# 50. Kubernetes Security

The deployment should use:

- Non-root containers where practical.
- Restricted privilege escalation.
- Kubernetes Secrets.
- Least-privilege ServiceAccounts.
- NetworkPolicies where appropriate.
- Resource limits.
- Internal services that are not unnecessarily exposed externally.

---

# 51. CI/CD Architecture

GitHub Actions is the primary CI/CD platform.

The pipeline follows:

```text
Pull Request
      ↓
Build
      ↓
Unit Tests
      ↓
Integration Tests
      ↓
Contract Tests
      ↓
Security Checks
      ↓
Docker Build
      ↓
Image Scan
      ↓
Registry
      ↓
Kubernetes
      ↓
Smoke Tests
```

---

# 52. Container Registry

Images should be stored in an OCI-compatible registry.

GitHub Container Registry is a natural initial option because the source code is hosted on GitHub.

Images should be traceable to Git commits.

Example:

```text
commercex-order:<git-sha>
```

---

# 53. CI/CD Quality Gates

A change should not proceed if:

- Build fails.
- Required tests fail.
- Contract validation fails.
- Critical security checks fail.
- Docker build fails.
- Kubernetes rollout fails.
- Critical smoke tests fail.

---

# 54. Deployment Strategy

The initial deployment strategy is Kubernetes rolling update.

The deployment process is:

```text
Validated Code
     ↓
Docker Image
     ↓
Registry
     ↓
Kubernetes Deployment
     ↓
Rollout
     ↓
Smoke Test
```

Rollback should use Kubernetes deployment history and previously validated images.

---

# 55. Configuration Management

Configuration is externalized from application images.

Examples:

```text
Database connection strings
Kafka endpoints
Redis endpoints
JWT configuration
Internal service URLs
```

Environment-specific values are injected during deployment.

---

# 56. Secret Management

Secrets are stored outside source control.

Development:

```text
.NET User Secrets
Environment Variables
```

CI:

```text
GitHub Secrets
```

Kubernetes:

```text
Kubernetes Secrets
```

No secret should be embedded in:

- Git.
- Dockerfiles.
- Docker images.
- CI logs.

---

# 57. Development Roadmap

CommerceX is implemented progressively.

The major sequence is:

```text
Infrastructure
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
Gateway Hardening
    ↓
Reliability
    ↓
Testing
    ↓
Observability
    ↓
Docker
    ↓
Kubernetes
    ↓
CI/CD
    ↓
Final Validation
```

---

# 58. Development Milestones

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
| M13 | Gateway |
| M14 | Reliability |
| M15 | Testing |
| M16 | Observability |
| M17 | Docker |
| M18 | Kubernetes |
| M19 | CI/CD |
| M20 | Final validation |

---

# 59. Current Implementation Status

The project has progressed beyond the architecture-planning stage.

The following foundation has been established during implementation:

### Infrastructure

- Docker Compose infrastructure.
- PostgreSQL.
- Redis.
- Kafka.
- Schema Registry.
- Kubernetes infrastructure foundation.
- Local cluster validation.

### Auth

- Domain foundation.
- Application contracts.
- Persistence abstractions.
- Infrastructure persistence foundation.
- EF Core configurations.
- Initial Auth database migration generation.

The current implementation work should continue from the established Auth baseline rather than restarting completed work.

---

# 60. Auth Implementation Baseline

The Auth service has an established foundation.

The authoritative implementation baseline is the previously established Auth foundation commit:

```text
5f5e622097941ad906c56879b236e941d34277fa
```

Commit:

```text
feat(auth): establish auth service foundation
```

Future Auth work should build incrementally from this baseline.

---

# 61. Repository Structure

The recommended repository structure is:

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
│
└── .github/
    └── workflows/
```

---

# 62. Internal Service Structure

A typical service follows:

```text
Service/
├── CommerceX.Service.Api/
├── CommerceX.Service.Application/
├── CommerceX.Service.Domain/
└── CommerceX.Service.Infrastructure/
```

Tests are maintained separately.

---

# 63. BuildingBlocks

Shared BuildingBlocks should contain only technical cross-cutting concerns.

Examples:

- Common abstractions.
- Messaging infrastructure.
- Observability.
- Error handling.
- Authentication infrastructure.
- Shared technical utilities.

BuildingBlocks should not contain shared business-domain entities.

---

# 64. Architecture Decisions

The major architectural decisions are summarized below.

### Microservices

Chosen to demonstrate independent service boundaries and deployment.

### PostgreSQL

Chosen for mature relational capabilities and strong support in .NET.

### Redis

Chosen for fast cart access and selective caching.

### Kafka

Chosen for asynchronous event-driven communication.

### gRPC

Chosen for selected internal synchronous calls.

### REST

Chosen for client-facing APIs.

### Docker

Chosen for repeatable application packaging.

### Kubernetes

Chosen for orchestration and deployment learning.

### GitHub Actions

Chosen for accessible CI/CD automation.

### OpenTelemetry

Chosen for standardized telemetry.

### Grafana

Chosen for visualization and operational dashboards.

---

# 65. Architectural Trade-Offs

## 65.1 Microservices vs Monolith

Microservices increase operational complexity but provide valuable distributed-system learning.

## 65.2 Kafka vs Direct Calls

Kafka is used only where asynchronous behavior provides a meaningful benefit.

## 65.3 One PostgreSQL Server vs Multiple Servers

One PostgreSQL server is acceptable for local development while maintaining logical database ownership.

## 65.4 Redis Everywhere vs Selective Redis

Redis is used only where it provides clear value.

## 65.5 Simulated Payment vs Real Provider

Simulated payment avoids external financial dependencies while preserving payment workflow complexity.

## 65.6 PostgreSQL Search vs Elasticsearch

PostgreSQL search keeps the initial project manageable. A dedicated search engine can be introduced later.

---

# 66. Reliability Architecture

CommerceX is designed around:

```text
Timeouts
Retries
Idempotency
Compensation
Health Checks
Eventual Consistency
Failure Isolation
```

The architecture avoids attempting to provide global ACID transactions across services.

---

# 67. Distributed Transaction Strategy

CommerceX uses local transactions and compensating actions.

Example:

```text
Order
  ↓
Reserve Inventory
  ↓
Process Payment
  ↓
Payment Failed
  ↓
Release Inventory
```

This is preferred over distributed two-phase commit.

---

# 68. Failure Isolation

A failure in one non-critical asynchronous service should not unnecessarily invalidate completed business state.

Example:

```text
Order Confirmed
      |
      v
Notification Service
      |
      X
    Failure
```

The order remains confirmed.

Notification processing can retry independently.

---

# 69. Performance Objectives

Initial engineering targets include:

```text
Majority of normal API requests:
< approximately 500ms

Gateway overhead:
< approximately 100ms

Redis:
Low-millisecond local access expected

Kafka:
Manageable consumer lag under expected workload
```

These are initial development targets rather than production SLAs.

---

# 70. Scalability

Services should be horizontally scalable where practical.

Example:

```text
Product Service
   |
   +--> Pod 1
   +--> Pod 2
   +--> Pod 3
```

Stateless APIs make this possible.

Kafka consumers scale through consumer groups and partitions.

---

# 71. Availability

The system should support:

- Health checks.
- Pod restart recovery.
- Service isolation.
- Controlled retries.
- Graceful shutdown.
- Kubernetes restart behavior.

Initial Minikube deployments use modest replica counts because the project is primarily educational.

---

# 72. Data Integrity

Important rules include:

- Inventory cannot become negative.
- Order prices are immutable historical values.
- Shipping addresses used by orders are stored as historical snapshots.
- Promotion validity is enforced.
- Duplicate payment operations are prevented.
- Customer resources are protected by ownership checks.

---

# 73. API Design Principles

The external API follows:

- Versioned routes.
- Resource-oriented naming.
- Standard HTTP methods.
- JSON.
- Pagination.
- Validation.
- Problem-details-style errors.
- Bearer authentication.
- Correlation IDs.
- OpenAPI documentation.

---

# 74. API Error Handling

Errors should provide useful but safe information.

Example conceptual response:

```json
{
  "type": "https://commercex/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "traceId": "..."
}
```

Internal stack traces and sensitive information should not be exposed.

---

# 75. Event-Driven Architecture Benefits

Kafka allows services to react independently.

Example:

```text
OrderConfirmed
       |
       +--> Shipping
       |
       +--> Notification
```

Shipping and Notification do not need a direct synchronous dependency on each other.

---

# 76. Search Architecture

Search is implemented as a read-oriented service.

```text
Product Service
      |
      v
Product Events
      |
      v
Kafka
      |
      v
Search Service
      |
      v
Search PostgreSQL
```

Product Service remains the source of truth.

This demonstrates eventual consistency.

---

# 77. Notification Architecture

Notification is asynchronous.

It consumes relevant events and creates simulated notifications.

This demonstrates:

- Kafka consumers.
- Consumer groups.
- Retry behavior.
- Duplicate event handling.
- Dead-letter processing.
- Failure isolation.

---

# 78. Promotion Architecture

Promotion provides simple business rules rather than a large pricing engine.

Initial capabilities:

- Coupon codes.
- Percentage discount.
- Fixed discount.
- Expiration.
- Minimum order value.

Validation is available through gRPC for checkout.

---

# 79. Review Architecture

Review Service owns product reviews.

Important rules:

- Rating is between 1 and 5.
- Customer can manage their own review.
- Product reference is stored without owning the Product entity.

---

# 80. Search and Product Separation

Search does not own Product data.

Instead:

```text
Product = Source of Truth
Search = Read Model
```

This prevents duplicate business ownership.

---

# 81. Observability Dashboards

Initial Grafana dashboards should include:

### System Overview

- Service availability.
- Error rate.
- Request rate.

### API Performance

- Latency.
- Throughput.
- HTTP status codes.

### Checkout

- Checkout attempts.
- Success/failure.
- Payment failures.
- Inventory failures.

### Kafka

- Throughput.
- Consumer lag.
- DLQ.

### Redis

- Hit ratio.
- Memory.
- Errors.

### PostgreSQL

- Connections.
- Query latency.
- Errors.

### Kubernetes

- Pod status.
- CPU.
- Memory.
- Restarts.

---

# 82. Operational Health

Each service should expose health endpoints.

At minimum:

```text
/health/live
/health/ready
```

Health checks should be designed carefully so that telemetry failure does not automatically make the business application unavailable.

---

# 83. Project Testing Matrix

| Area | Testing |
|---|---|
| Domain | Unit |
| Application | Unit |
| PostgreSQL | Integration |
| Redis | Integration |
| Kafka | Integration/Contract |
| REST | API |
| gRPC | Contract/Integration |
| Security | Security tests |
| Checkout | E2E |
| Docker | Smoke |
| Kubernetes | Smoke/Recovery |
| Observability | Integration/Validation |
| Performance | Load/benchmark |

---

# 84. Final Validation Scenarios

## Scenario A — Successful Purchase

```text
Customer
 ↓
Login
 ↓
Product
 ↓
Cart
 ↓
Promotion
 ↓
Order
 ↓
Inventory
 ↓
Payment
 ↓
Order Confirmed
 ↓
Shipping
 ↓
Notification
```

Expected result:

```text
Order = Confirmed
Payment = Succeeded
Inventory = Reserved
Shipment = Created
Notification = Generated
```

---

## Scenario B — Payment Failure

```text
Order
 ↓
Inventory Reserved
 ↓
Payment Failed
 ↓
Inventory Released
```

Expected result:

```text
Payment = Failed
Order ≠ Confirmed
Inventory reservation = Released
Shipment = Not Created
```

---

## Scenario C — Duplicate Payment

```text
Payment Request
       ↓
Success
       ↓
Retry
       ↓
Same Idempotency Key
```

Expected:

```text
One Payment
One Business Effect
```

---

## Scenario D — Duplicate Order Event

```text
OrderConfirmed
OrderConfirmed
```

Expected:

```text
One Shipment
```

---

## Scenario E — Product Synchronization

```text
ProductUpdated
      ↓
Kafka
      ↓
Search
```

Expected:

```text
Search eventually reflects updated Product data.
```

---

# 85. Project Definition of Done

The final project is complete when:

### Services

- All 12 services are implemented.
- Each service builds independently.
- Service boundaries are respected.

### APIs

- REST APIs work.
- gRPC contracts work.
- Authentication works.
- Authorization works.

### Data

- PostgreSQL persistence works.
- Redis cart/cache works.
- No cross-service DB access exists.

### Messaging

- Kafka events work.
- Consumers are idempotent.
- Retry/DLQ behavior works where required.

### Business

- Checkout works.
- Payment failure is handled.
- Inventory reservation works.
- Shipping works.
- Notifications work.
- Reviews work.
- Search synchronization works.

### Testing

- Unit tests pass.
- Integration tests pass.
- Contract tests pass.
- Security tests pass.
- Critical E2E tests pass.

### Infrastructure

- Docker works.
- Kubernetes works.
- CI/CD works.

### Observability

- Logs work.
- Metrics work.
- Traces work.
- Grafana dashboards work.

### Documentation

- Architecture documents are complete.
- Final project report is complete.

---

# 86. Future Enhancements

CommerceX can evolve beyond the initial scope.

Potential extensions include:

### Search

- Elasticsearch/OpenSearch.
- Advanced ranking.
- Faceted search.

### Payments

- Stripe or another provider.
- Payment webhooks.
- Refunds.

### Authentication

- OAuth 2.0.
- OpenID Connect.
- Identity provider integration.

### Infrastructure

- Managed Kubernetes.
- Cloud deployment.
- Infrastructure as Code.

### Messaging

- Advanced schema management.
- More sophisticated event versioning.
- Event replay tooling.

### Observability

- OpenTelemetry Collector.
- Advanced alerting.
- Centralized log storage.

### Deployment

- GitOps.
- Argo CD.
- Canary deployments.

### Distributed Systems

- Advanced resilience testing.
- Chaos engineering.
- Multi-region architecture.

These are deliberately future enhancements.

---

# 87. Academic/Professional Value

CommerceX demonstrates more than CRUD development.

The project demonstrates understanding of:

- Domain-driven service boundaries.
- Distributed communication.
- Database ownership.
- Event-driven architecture.
- Caching.
- Authentication.
- Idempotency.
- Distributed failure handling.
- Containerization.
- Kubernetes.
- CI/CD.
- Observability.
- Automated testing.

This makes the project suitable as a portfolio or academic software-engineering project.

---

# 88. Example CV Description

A concise professional description can be derived from the project:

> **CommerceX — Distributed E-Commerce Backend Platform**  
> Designed and developed a microservices-based e-commerce backend using C#/.NET 9, ASP.NET Core, PostgreSQL, Redis, Apache Kafka, gRPC, Docker, Kubernetes, GitHub Actions, OpenTelemetry, and Grafana. Implemented independently deployable services for authentication, users, products, inventory, carts, orders, payments, shipping, reviews, notifications, promotions, and search. Designed event-driven checkout workflows with idempotency, inventory reservation, simulated payments, eventual consistency, distributed tracing, automated testing, containerization, Kubernetes deployment, and CI/CD automation.

---

# 89. Technology Stack

| Category | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 9 |
| Web Framework | ASP.NET Core |
| API Gateway | YARP / ASP.NET Core |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| Cache / Cart | Redis |
| Messaging | Apache Kafka |
| Schema Management | Schema Registry |
| Internal RPC | gRPC |
| External API | REST/JSON |
| Containers | Docker |
| Orchestration | Kubernetes |
| Local Kubernetes | Minikube |
| CI/CD | GitHub Actions |
| Testing | xUnit |
| Integration Testing | Testcontainers |
| Telemetry | OpenTelemetry |
| Visualization | Grafana |
| Source Control | Git/GitHub |

---

# 90. Documentation Set

The complete CommerceX architecture documentation consists of:

```text
01 — Project Charter
02 — System Requirements
03 — Functional Requirements
04 — Non-Functional Requirements
05 — System Architecture
06 — Microservices Architecture
07 — Service Boundaries
08 — Data Architecture
09 — API Design
10 — gRPC Design
11 — Kafka Event Design
12 — Redis Caching Strategy
13 — Security Design
14 — Docker Design
15 — Kubernetes Design
16 — Observability Design
17 — Testing Strategy
18 — CI/CD Design
19 — Development Roadmap
20 — Final Project Report
```

These documents together form the project's architecture and implementation baseline.

---

# 91. Final Architecture Diagram

The complete conceptual architecture is:

```text
                         +----------------+
                         |    Customer    |
                         +-------+--------+
                                 |
                                 v
                         +---------------+
                         | API Gateway   |
                         | ASP.NET/YARP  |
                         +-------+-------+
                                 |
       +-------------------------+--------------------------+
       |            |            |           |              |
       v            v            v           v              v
    Auth          User       Product       Cart          Search
       |            |            |           |              |
       v            v            v           v              v
   PostgreSQL   PostgreSQL   PostgreSQL    Redis       PostgreSQL
                              |
                              v
                             Kafka
                              |
              +---------------+----------------+
              |               |                |
              v               v                v
         Inventory         Order          Promotion
              |               |                |
        PostgreSQL       PostgreSQL       PostgreSQL
                              |
                              +------ gRPC ------+
                              |                  |
                              v                  v
                           Payment           Inventory
                              |
                         PostgreSQL
                              |
                              v
                            Kafka
                              |
                    +---------+---------+
                    |                   |
                    v                   v
                 Shipping          Notification
                    |                   |
               PostgreSQL          PostgreSQL

                    +-------------------+
                    |
                    v
                  Review
                    |
               PostgreSQL
```

---

# 92. Final System Flow

The complete platform can be understood as four major layers:

```text
                    CLIENT LAYER
                         |
                         v
                  API GATEWAY LAYER
                         |
                         v
                BUSINESS SERVICE LAYER
                         |
          +--------------+--------------+
          |              |              |
          v              v              v
      PostgreSQL       Redis          Kafka
          |                             |
          +--------------+--------------+
                         |
                         v
               INFRASTRUCTURE LAYER
                         |
                         v
              Docker + Kubernetes
                         |
                         v
              OpenTelemetry + Grafana
                         |
                         v
                  GitHub Actions
```

---

# 93. Final Project Assessment

CommerceX provides a practical demonstration of modern distributed backend engineering.

The project combines:

```text
Microservices
+
REST
+
gRPC
+
Kafka
+
Redis
+
PostgreSQL
+
Docker
+
Kubernetes
+
Testing
+
Observability
+
CI/CD
+
Security
```

The most important learning outcome is not simply the number of technologies used.

It is understanding **why each technology is used, where its boundary belongs, what failure modes it introduces, and how the complete system remains testable and observable.**

---

# 94. Final Baseline Decisions

The complete CommerceX baseline is:

1. The system contains exactly 12 independently deployable business services.
2. The API Gateway is separate and is not counted as a business service.
3. Each service owns its business logic and data.
4. Services never directly access another service's database.
5. PostgreSQL is the primary durable relational datastore.
6. Redis is used primarily for Cart and selective caching.
7. Kafka provides asynchronous business-event communication.
8. gRPC is used selectively for internal synchronous communication.
9. REST is the primary external API style.
10. The Gateway does not contain core business logic.
11. Auth owns credentials and tokens.
12. User owns customer profiles and addresses.
13. Product owns the product catalog.
14. Inventory is authoritative for stock.
15. Cart uses Redis as its primary operational store.
16. Promotion owns coupon and discount rules.
17. Order owns order state and historical snapshots.
18. Payment is simulated.
19. Shipping owns shipment state.
20. Review owns product reviews.
21. Notification is event-driven.
22. Search is an eventually consistent Product read model.
23. Distributed transactions are avoided.
24. Local transactions and compensating actions are preferred.
25. Important operations are idempotent.
26. Kafka delivery is initially treated as at-least-once.
27. Important event producers may use the Transactional Outbox Pattern.
28. Authentication uses JWT access tokens and refresh tokens.
29. Customer/Admin roles are initially supported.
30. Docker packages each deployable application independently.
31. Kubernetes is the deployment platform.
32. Minikube is the initial local Kubernetes environment.
33. OpenTelemetry provides application telemetry.
34. Grafana provides observability visualization.
35. GitHub Actions provides CI/CD.
36. Automated testing is layered.
37. Testcontainers is recommended for integration infrastructure.
38. Checkout is the primary end-to-end workflow.
39. Successful and failed checkout paths must both be tested.
40. Security is treated as a cross-cutting requirement.
41. Configuration is externalized from application images.
42. Secrets are never committed to source control.
43. Database migrations are controlled explicitly.
44. Rolling deployment is the initial Kubernetes deployment strategy.
45. Advanced enterprise infrastructure is deferred until justified.
46. Documentation is maintained alongside implementation.
47. Development proceeds incrementally without unnecessarily restarting completed work.
48. Every major implementation phase requires build, testing, validation, documentation, and a meaningful Git commit.

---

# 95. Final Project Status

The CommerceX project has an established architecture, requirements, data model, API design, communication strategy, security model, infrastructure design, testing strategy, CI/CD design, and development roadmap.

The architecture documentation set is complete through Document 20.

Implementation should continue according to the Development Roadmap, beginning from the established implementation baseline and progressing incrementally through the remaining services and platform capabilities.

---

# 96. Conclusion

CommerceX is designed as a practical bridge between traditional backend development and distributed-system engineering.

The project demonstrates how an e-commerce platform can be decomposed into independently owned services while maintaining reliable business workflows.

The architecture deliberately uses:

- REST where external simplicity is important.
- gRPC where low-latency internal synchronous communication is useful.
- Kafka where asynchronous decoupling is valuable.
- Redis where low-latency state access provides clear benefits.
- PostgreSQL where durable relational consistency is required.
- Docker for reproducible packaging.
- Kubernetes for orchestration.
- OpenTelemetry and Grafana for operational visibility.
- GitHub Actions for automated delivery.

The resulting system provides a strong learning platform for understanding not only how to build individual services, but also how those services behave when connected into a distributed system.

The central engineering lesson of CommerceX is:

> A distributed system is not simply a collection of microservices. It is a collection of independently owned components that must communicate reliably, handle failure deliberately, maintain clear data ownership, and remain observable and testable as a whole.

---

# 97. Final Documentation Baseline

This document establishes the **CommerceX Final Project Report baseline** and completes the planned 20-document architecture and planning set.

The complete documentation sequence is:

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

**Documentation set status: COMPLETE**

**Implementation status: Continue incrementally according to Document 19 — Development Roadmap.**
