# CommerceX — System Architecture

**Document ID:** COMX-DOC-005  
**Document Version:** 1.0  
**Status:** Architecture Baseline  
**Project:** CommerceX  
**Related Documents:**
- `01-project-charter.md`
- `02-system-requirements.md`
- `03-functional-requirements.md`
- `04-non-functional-requirements.md`

---

# 1. Introduction

## 1.1 Purpose

This document defines the high-level system architecture for CommerceX.

The purpose is to establish how the requirements defined in the previous documents will be translated into a distributed technical architecture.

This document defines:

- Overall system structure
- Major architectural components
- Service interaction principles
- External and internal communication
- Data ownership
- API Gateway responsibilities
- Kafka integration
- Redis integration
- PostgreSQL integration
- Docker architecture
- Kubernetes architecture
- Observability architecture
- CI/CD architecture
- Security boundaries
- Deployment topology
- Important architectural decisions

Detailed service boundaries, database schemas, API contracts, gRPC contracts, Kafka event schemas, and infrastructure manifests will be documented separately.

---

# 2. Architecture Vision

CommerceX will use a **microservices-based distributed architecture** in which each business capability is implemented as an independently deployable service.

The overall architectural goal is:

> **Keep business services independently deployable while using REST, gRPC, Kafka, PostgreSQL, Redis, Docker, Kubernetes, and observability tooling to demonstrate practical distributed-system concepts.**

The architecture should be realistic but intentionally lightweight.

---

# 3. Architectural Style

CommerceX will primarily follow:

```text
Microservices Architecture
        +
Layered/Clean Architecture inside services
        +
API Gateway
        +
Event-Driven Architecture
        +
Database-per-Service Ownership
```

The architecture is therefore a hybrid of:

- Synchronous request/response communication
- Asynchronous event-driven communication
- Distributed data ownership
- Containerized deployment

---

# 4. High-Level Architecture

```text
                           CLIENTS
                 +-------------------------+
                 | Web / Mobile / REST     |
                 | API Clients             |
                 +------------+------------+
                              |
                              | HTTPS / HTTP
                              v
                    +---------------------+
                    |     API Gateway     |
                    | ASP.NET Core/YARP   |
                    +----------+----------+
                               |
            +------------------+------------------+
            |                  |                  |
            v                  v                  v
       +---------+        +---------+        +---------+
       |  Auth   |        | Product |        |  Order  |
       | Service |        | Service |        | Service |
       +----+----+        +----+----+        +----+----+
            |                  |                  |
            v                  v                  v
       +---------+        +---------+        +---------+
       | Auth DB |        |Product  |        | Order   |
       |         |        |   DB    |        |   DB    |
       +---------+        +---------+        +---------+

            Other Services
                 |
       +---------+----------+
       |                    |
       v                    v
   PostgreSQL             Redis
       |
       |
       v
     Kafka
       |
  +----+------+-------------+
  |           |             |
  v           v             v
Inventory  Shipping   Notification
Consumer    Consumer     Consumer


        +--------------------------------+
        |      Observability Layer       |
        | OpenTelemetry + Grafana        |
        +--------------------------------+

        +--------------------------------+
        |       Kubernetes / Minikube    |
        +--------------------------------+
```

This is a conceptual architecture. Detailed communication relationships will be defined in later documents.

---

# 5. Major Architectural Components

CommerceX consists of the following major components.

## 5.1 Client

The client represents any external consumer of CommerceX.

Examples:

- Web application
- Mobile application
- REST client
- Postman
- Automated test client

The initial project does not require a full frontend application.

---

## 5.2 API Gateway

The API Gateway is the external entry point.

Technology:

```text
ASP.NET Core
YARP
```

Responsibilities include:

- Request routing
- Authentication integration
- Authorization support
- Request correlation
- Basic gateway-level policies
- Service endpoint abstraction
- Error handling

The gateway should not contain core business logic.

---

## 5.3 Business Services

CommerceX contains 12 business services:

```text
1.  Auth Service
2.  User Service
3.  Product Service
4.  Inventory Service
5.  Cart Service
6.  Order Service
7.  Payment Service
8.  Shipping Service
9.  Review Service
10. Notification Service
11. Promotion Service
12. Search Service
```

Each service owns a specific business capability.

---

# 6. Service Architecture

Each service should follow a consistent internal architecture.

Recommended:

```text
+-----------------------------------+
|             API Layer             |
| REST / gRPC / messaging adapters  |
+----------------+------------------+
                 |
                 v
+-----------------------------------+
|        Application Layer          |
| Use cases / handlers / contracts  |
+----------------+------------------+
                 |
                 v
+-----------------------------------+
|           Domain Layer            |
| Entities / rules / value objects  |
+----------------+------------------+
                 |
                 v
+-----------------------------------+
|        Infrastructure Layer       |
| EF Core / Kafka / Redis / gRPC    |
+-----------------------------------+
```

This structure keeps business logic separate from infrastructure concerns.

---

# 7. Service Independence

Each service shall be:

- Independently buildable
- Independently testable
- Independently containerizable
- Independently deployable
- Responsible for its own data

For example:

```text
Order Service
    |
    +--> Order Database
    |
    +--> Order APIs
    |
    +--> Order Events
    |
    +--> Order Business Logic
```

The Order Service should not require direct access to Product Service's database.

---

# 8. Service Responsibility Overview

| Service | Main Responsibility | Primary Data Store |
|---|---|---|
| Auth | Authentication and credentials | PostgreSQL |
| User | Profiles and addresses | PostgreSQL |
| Product | Products and categories | PostgreSQL |
| Inventory | Stock and reservations | PostgreSQL |
| Cart | Shopping carts | Redis |
| Order | Orders and lifecycle | PostgreSQL |
| Payment | Simulated payments | PostgreSQL |
| Shipping | Shipments | PostgreSQL |
| Review | Reviews and ratings | PostgreSQL |
| Notification | Notification processing | PostgreSQL/optional simple store |
| Promotion | Coupons and discounts | PostgreSQL |
| Search | Product search/read model | PostgreSQL |

The exact persistence choices can be refined in the Data Architecture document.

---

# 9. Communication Architecture

CommerceX intentionally uses three communication mechanisms.

```text
                 Communication
                       |
        +--------------+--------------+
        |              |              |
        v              v              v
      REST            gRPC          Kafka
        |              |              |
   External/API     Internal       Async Events
```

Each mechanism has a specific purpose.

---

# 10. REST Communication

REST is the primary client-facing communication mechanism.

Example:

```text
Client
   |
   | REST
   v
API Gateway
   |
   | REST
   v
Product Service
```

REST is appropriate for:

- Product browsing
- Cart operations
- Order operations
- User operations
- Authentication
- Reviews
- Administrative APIs

The detailed REST API contract will be defined separately.

---

# 11. gRPC Communication

gRPC will be used selectively for internal synchronous communication.

Example:

```text
Order Service
      |
      | gRPC
      v
Inventory Service
```

Another possible example:

```text
Order Service
      |
      | gRPC
      v
Promotion Service
```

gRPC is intended to demonstrate:

- Strongly typed service contracts
- Protocol Buffers
- Efficient internal communication
- Synchronous service-to-service calls

gRPC should not be used for every service interaction.

---

# 12. Kafka Event Architecture

Apache Kafka provides asynchronous communication.

Conceptually:

```text
Order Service
     |
     | Publish
     v
   Kafka
     |
     +------------+-------------+
     |            |             |
     v            v             v
Inventory     Shipping     Notification
Consumer       Consumer        Consumer
```

Kafka is appropriate for events that do not require an immediate synchronous response.

Examples:

```text
OrderCreated
PaymentSucceeded
PaymentFailed
OrderConfirmed
OrderCancelled
ShipmentCreated
OrderDelivered
```

---

# 13. Event-Driven Design Principles

Kafka events shall follow these principles:

1. Events represent business facts.
2. Events are immutable after publication.
3. Consumers own their processing behavior.
4. Consumers should tolerate duplicate delivery.
5. Events should contain sufficient information for processing.
6. Services should not use Kafka as a replacement for their databases.
7. Event schemas should be documented.
8. Event processing should be observable.

---

# 14. Database Architecture

CommerceX follows the **database-per-service ownership principle**.

Conceptually:

```text
Auth Service
    |
    v
Auth Database

User Service
    |
    v
User Database

Product Service
    |
    v
Product Database

Order Service
    |
    v
Order Database
```

and so on.

For local development, a single PostgreSQL server may host multiple logical databases.

Example:

```text
PostgreSQL
|
+-- commercex_auth
+-- commercex_users
+-- commercex_products
+-- commercex_inventory
+-- commercex_orders
+-- commercex_payments
+-- commercex_shipping
+-- commercex_reviews
+-- commercex_notifications
+-- commercex_promotions
+-- commercex_search
```

This provides service ownership without requiring 12 PostgreSQL servers.

---

# 15. Database Access Rule

The following rule is mandatory:

> **A service must never directly access another service's database.**

Incorrect:

```text
Order Service
      |
      v
Product Database
```

Correct:

```text
Order Service
      |
      +--> Product API / gRPC
```

or:

```text
Product Service
      |
      +--> Kafka Product Event
```

This rule is essential for maintaining service independence.

---

# 16. Redis Architecture

Redis will provide low-latency access for selected data.

Primary use:

```text
Cart Service
      |
      v
    Redis
```

Example key:

```text
cart:{customerId}
```

Additional caching may include:

```text
product:{productId}
category:{categoryId}
```

Redis will follow a cache-aside approach for cacheable data where appropriate.

Conceptually:

```text
Application
    |
    v
Redis
    |
 Cache Hit ----> Return
    |
 Cache Miss
    |
    v
PostgreSQL
    |
    v
Update Redis
```

---

# 17. API Gateway Architecture

The API Gateway hides internal service topology from clients.

Without gateway:

```text
Client
  |
  +--> Auth Service
  +--> Product Service
  +--> Order Service
  +--> Cart Service
```

With gateway:

```text
Client
   |
   v
API Gateway
   |
   +--> Auth
   +--> Product
   +--> Order
   +--> Cart
```

Benefits:

- Single external endpoint
- Centralized routing
- Simplified client interaction
- Authentication integration
- Consistent API policies
- Easier service relocation

The gateway will not own business data.

---

# 18. Authentication Architecture

Authentication is primarily handled by Auth Service.

```text
                Login
                  |
                  v
             API Gateway
                  |
                  v
             Auth Service
                  |
                  v
             PostgreSQL
                  |
                  v
               JWT
                  |
                  v
               Client
```

For protected requests:

```text
Client
  |
  | JWT
  v
API Gateway
  |
  | Authenticated Request
  v
Backend Service
```

The exact token validation strategy will be documented in the Security Design.

---

# 19. Order Processing Architecture

The main distributed workflow is:

```text
Customer
   |
   v
API Gateway
   |
   v
Order Service
   |
   +------> Inventory Service
   |
   +------> Promotion Service
   |
   +------> Payment Service
   |
   v
 Kafka Events
   |
   +------> Shipping Service
   |
   +------> Notification Service
```

The system should not require a single distributed database transaction.

Instead, the workflow will use local transactions plus synchronous calls and asynchronous events.

---

# 20. Checkout Architecture

A simplified checkout workflow:

```text
1. Customer submits checkout
             |
             v
2. Order Service creates pending order
             |
             v
3. Validate/reserve inventory
             |
             v
4. Process simulated payment
             |
       +-----+-----+
       |           |
       v           v
    Success      Failure
       |           |
       v           v
5. Confirm     Release inventory
   order       / fail order
       |
       v
6. Publish OrderConfirmed
       |
       v
7. Kafka
       |
   +---+----------------+
   |                    |
   v                    v
Shipping            Notification
```

The exact workflow and failure semantics will be defined in later design documents.

---

# 21. Consistency Architecture

CommerceX will use a combination of:

### Strong consistency

Within a service's own database transaction.

Example:

```text
Order DB
   |
   +--> Order + OrderItems
```

### Eventual consistency

Between independent services.

Example:

```text
Order Confirmed
      |
      v
Kafka
      |
      v
Shipping Service
```

This distinction is important for learning distributed systems.

---

# 22. Failure Handling Architecture

Failures can occur at multiple levels.

```text
Client
  |
Gateway
  |
Service
  |
+-- Database
+-- Redis
+-- Kafka
+-- Other Services
```

CommerceX should use:

- Timeouts
- Selective retries
- Idempotency
- Health checks
- Graceful failure
- Asynchronous processing

The system should avoid blindly retrying operations that can produce duplicate business actions.

---

# 23. Container Architecture

Every service will be packaged as a Docker image.

Example:

```text
CommerceX
|
+-- auth-service:latest
+-- user-service:latest
+-- product-service:latest
+-- inventory-service:latest
+-- cart-service:latest
+-- order-service:latest
+-- payment-service:latest
+-- shipping-service:latest
+-- review-service:latest
+-- notification-service:latest
+-- promotion-service:latest
+-- search-service:latest
+-- api-gateway:latest
```

Each service should have an independently buildable image.

---

# 24. Local Development Architecture

Docker will be used for local infrastructure and optionally for all application services.

A simplified local environment:

```text
Developer Machine
       |
       +------------------------------+
       |                              |
       v                              v
   .NET Services                   Docker
                                      |
                       +--------------+--------------+
                       |              |              |
                       v              v              v
                  PostgreSQL       Redis          Kafka
```

A Docker Compose environment may be used to simplify local infrastructure.

Kubernetes is introduced after services work correctly in the local development environment.

---

# 25. Kubernetes Architecture

Minikube will provide the local Kubernetes cluster.

Conceptually:

```text
                         Minikube
                            |
                     +------+------+
                     |   Ingress   |
                     +------+------+
                            |
                            v
                      API Gateway
                            |
       +--------------------+--------------------+
       |         |          |          |         |
       v         v          v          v         v
      Auth     Product     Cart       Order    User
       |         |          |          |         |
       +---------+----------+----------+---------+
                            |
              +-------------+-------------+
              |             |             |
              v             v             v
          PostgreSQL      Redis         Kafka
```

The exact deployment topology will be documented separately.

---

# 26. Kubernetes Service Discovery

Services deployed in Kubernetes shall communicate using Kubernetes Service names rather than hard-coded container IP addresses.

Example:

```text
http://product-service
```

or:

```text
inventory-service:5000
```

depending on the protocol and configuration.

This allows pods to be replaced without requiring clients to know their individual IP addresses.

---

# 27. Kubernetes Configuration

Configuration shall be separated from container images.

Use:

```text
ConfigMap
    |
    +--> Non-sensitive configuration

Secret
    |
    +--> Sensitive configuration
```

Examples:

### ConfigMap

```text
Kafka endpoint
Redis endpoint
Service URLs
Environment name
```

### Secret

```text
Database passwords
JWT secrets
Other credentials
```

---

# 28. Kubernetes Health Architecture

Each service should expose:

```text
/health
```

and preferably:

```text
/ready
```

Kubernetes can use these endpoints for:

```text
Liveness Probe
Readiness Probe
```

Conceptually:

```text
Pod
 |
 +--> Liveness
 |
 +--> Readiness
 |
 v
Traffic
```

A service that is alive but not ready should not receive normal traffic.

---

# 29. Observability Architecture

CommerceX will use OpenTelemetry and Grafana.

Conceptually:

```text
                 Services
                    |
                    v
              OpenTelemetry
                    |
        +-----------+-----------+
        |           |           |
        v           v           v
     Metrics      Traces       Logs
        |           |           |
        +-----------+-----------+
                    |
                    v
                 Grafana
```

The exact telemetry backend components can be selected during the Observability Design phase.

---

# 30. Distributed Tracing

Trace context should flow across service boundaries.

Example:

```text
Trace ID: abc123

Gateway
   |
   v
Order Service
   |
   v
Inventory Service
   |
   v
Payment Service
   |
   v
Kafka
   |
   v
Notification Service
```

This allows a developer to understand where time was spent and where failures occurred.

---

# 31. Logging Architecture

Services will produce structured logs.

A useful log record should contain information such as:

```text
Timestamp
Service
Level
Message
Trace ID
Correlation ID
Operation
```

Sensitive information must not be included.

The initial project does not require an elaborate centralized logging stack unless it becomes useful for the observability learning objectives.

---

# 32. CI/CD Architecture

GitHub Actions will automate validation and packaging.

Basic pipeline:

```text
Developer
    |
    v
Git Push / Pull Request
    |
    v
GitHub Actions
    |
    +--> Restore
    |
    +--> Build
    |
    +--> Unit Tests
    |
    +--> Integration Tests
    |
    +--> Docker Build
    |
    +--> Publish Image
    |
    v
Container Registry
```

Deployment to Minikube can be added after the CI foundation is stable.

---

# 33. Source Control Architecture

CommerceX will use Git and GitHub.

Recommended:

```text
main
 |
 +-- feature/auth
 +-- feature/product
 +-- feature/order
 +-- feature/kafka
 +-- fix/...
 +-- chore/...
```

The `main` branch should remain in a stable state.

---

# 34. Security Architecture

Security boundaries:

```text
                 External Client
                       |
                       v
                API Gateway
                       |
                 JWT Validation
                       |
                       v
                 Backend APIs
                       |
             +---------+---------+
             |                   |
             v                   v
        Service Data        Internal APIs
```

Security responsibilities are distributed:

### Gateway

- External request authentication
- Route protection
- Basic authorization policies

### Services

- Authorization enforcement for business operations
- Resource ownership validation
- Input validation
- Business security rules

### Infrastructure

- Secret protection
- Database credentials
- Kafka credentials if applicable
- Kubernetes Secrets

---

# 35. Data Flow Categories

CommerceX has three primary data-flow categories.

## 35.1 Request/Response

```text
Client
  ->
Gateway
  ->
Service
  ->
Database
  ->
Service
  ->
Gateway
  ->
Client
```

Used for immediate operations.

## 35.2 Internal Synchronous

```text
Service A
   |
   | gRPC
   v
Service B
```

Used when Service A requires an immediate response.

## 35.3 Asynchronous

```text
Service A
   |
   v
Kafka
   |
   +--> Service B
   +--> Service C
```

Used for events and decoupled processing.

---

# 36. Architecture Decision: Database-per-Service

### Decision

Each service owns its own logical database.

### Reason

This demonstrates:

- Service autonomy
- Data ownership
- Reduced database coupling
- Independent schema evolution

### Local simplification

A single PostgreSQL server may host all logical service databases.

This provides the learning benefit without the operational overhead of running many database servers.

---

# 37. Architecture Decision: REST + gRPC + Kafka

### Decision

CommerceX will deliberately use all three communication mechanisms.

### REST

For external/client-facing APIs.

### gRPC

For selected internal synchronous calls.

### Kafka

For asynchronous business events.

### Reason

Using each technology for an appropriate purpose demonstrates the differences between communication models without forcing unnecessary complexity.

---

# 38. Architecture Decision: Redis

### Decision

Redis will primarily support:

- Shopping cart storage
- Cacheable product data
- Other selected high-read temporary data

### Reason

This provides practical learning opportunities around:

- Cache-aside
- TTL
- Cache invalidation
- Low-latency reads
- Distributed state

---

# 39. Architecture Decision: Simulated Payment

### Decision

Payment processing will be simulated.

### Reason

A real payment provider introduces:

- External dependencies
- Security concerns
- Financial compliance
- Credential management
- Additional failure modes

These are outside the initial learning scope.

The simulated Payment Service is sufficient to demonstrate distributed payment workflows.

---

# 40. Architecture Decision: PostgreSQL-Based Search

### Decision

The initial Search Service will use PostgreSQL rather than Elasticsearch/OpenSearch.

### Reason

The project needs search functionality but does not need a dedicated search cluster.

This keeps the infrastructure manageable while still allowing the project to demonstrate:

- Search queries
- Filtering
- Pagination
- Indexing
- Caching

A dedicated search engine can be introduced as a future extension.

---

# 41. Architecture Decision: Monorepo

### Decision

CommerceX will initially use a monorepo.

Example:

```text
CommerceX/
|
+-- src/
|   +-- ApiGateway/
|   +-- Services/
|   +-- BuildingBlocks/
|
+-- tests/
|
+-- deploy/
|
+-- docs/
|
+-- .github/
```

### Reason

A monorepo is easier for a single developer to:

- Navigate
- Build
- Test
- Version
- Document
- Configure

Independent deployment does not require separate Git repositories.

---

# 42. Architecture Decision: Shared BuildingBlocks

A small shared BuildingBlocks area may contain technical components such as:

```text
Messaging
Observability
Common infrastructure abstractions
Shared technical utilities
```

It must not become a shared business-domain library.

Avoid:

```text
CommerceX.Common
    |
    +--> Product Entity
    +--> Order Entity
    +--> Customer Entity
```

because this creates tight coupling between services.

---

# 43. Security Boundaries

The architecture shall enforce the following boundaries:

```text
External
   |
   v
API Gateway
   |
   v
Service Boundary
   |
   +--> Own Database
   +--> Own Business Logic
```

Services shall not bypass another service's boundary by directly accessing its database.

---

# 44. Main Deployment Topology

The target local deployment will resemble:

```text
                         Minikube
                            |
                      +-----+-----+
                      |  Ingress  |
                      +-----+-----+
                            |
                      API Gateway
                            |
        +-------------------+-------------------+
        |                   |                   |
        v                   v                   v
     Auth/User          Catalog              Order
     Services           Services             Services
        |                   |                   |
        v                   v                   v
     PostgreSQL          PostgreSQL          PostgreSQL

                     Shared Infrastructure
                 +----------+----------+
                 |          |          |
                 v          v          v
              PostgreSQL  Redis      Kafka
                 |
                 v
             Observability
                 |
                 v
               Grafana
```

This is a logical representation. Physical pod and node placement will depend on Minikube resource availability.

---

# 45. Environment Strategy

CommerceX should support at least:

```text
Development
Test
Kubernetes/Local
```

### Development

Services can run directly from .NET tooling.

### Test

Automated tests use controlled infrastructure.

### Kubernetes/Local

Containerized services run on Minikube.

Environment-specific values shall be externalized.

---

# 46. Request Lifecycle

A typical external request follows:

```text
1. Client sends request
          |
          v
2. API Gateway receives request
          |
          v
3. Authentication/authorization
          |
          v
4. Gateway routes request
          |
          v
5. Service validates request
          |
          v
6. Application executes use case
          |
          v
7. Domain logic executes
          |
          v
8. Infrastructure accesses data
          |
          v
9. Response generated
          |
          v
10. Gateway returns response
          |
          v
11. Client receives response
```

Telemetry should be captured across the lifecycle.

---

# 47. Event Lifecycle

A typical Kafka event follows:

```text
Business Operation
       |
       v
Local Database Transaction
       |
       v
Publish Event
       |
       v
Kafka Topic
       |
       +--------+---------+
       |        |         |
       v        v         v
 Consumer    Consumer   Consumer
       |
       v
Local Processing
       |
       v
Consumer Database
```

Detailed event publication reliability patterns will be addressed in the Kafka design document.

---

# 48. Architecture Constraints

The architecture is constrained by the learning-project goals.

It must:

- Use the specified technology stack.
- Support all 12 services.
- Remain locally deployable.
- Avoid unnecessary infrastructure.
- Support independent service deployment.
- Demonstrate distributed communication.
- Demonstrate event-driven processing.
- Demonstrate caching.
- Demonstrate container orchestration.
- Demonstrate observability.
- Demonstrate CI/CD.

---

# 49. Architecture Trade-Offs

## Microservices vs Monolith

### Chosen

Microservices.

### Benefit

Demonstrates:

- Service boundaries
- Distributed communication
- Independent deployment
- Failure isolation

### Cost

- More infrastructure
- More deployment complexity
- More testing complexity

The project accepts this cost because learning microservices is a primary objective.

---

## Kafka vs Direct Calls

### Chosen

Both.

Kafka is used where asynchronous processing is valuable, while direct REST/gRPC calls are used when an immediate response is required.

---

## Multiple PostgreSQL Servers vs One PostgreSQL Server

### Chosen

One PostgreSQL server with logical service databases for local development.

### Reason

This preserves database ownership while reducing local resource consumption.

---

## Redis Everywhere vs Selective Redis

### Chosen

Selective Redis.

### Reason

Using Redis everywhere would add complexity and obscure the purpose of caching.

---

# 50. Architecture Quality Goals

The architecture should achieve:

```text
Independent Services
        +
Clear Data Ownership
        +
Appropriate Communication
        +
Failure Isolation
        +
Observability
        +
Simple Deployment
```

The architecture should not optimize for maximum theoretical scalability.

It should optimize for:

> **Learning value + correctness + manageable complexity.**

---

# 51. Architecture Validation

The architecture will be validated progressively.

## Level 1 — Local Service

```text
Service
  |
  v
PostgreSQL
```

## Level 2 — Service Integration

```text
Service A
   |
   v
Service B
```

## Level 3 — Messaging

```text
Service
   |
   v
Kafka
   |
   v
Consumer
```

## Level 4 — Containerization

```text
Docker
```

## Level 5 — Kubernetes

```text
Minikube
```

## Level 6 — Observability

```text
OpenTelemetry
      |
      v
Grafana
```

## Level 7 — CI/CD

```text
GitHub Actions
```

This progressive approach prevents infrastructure complexity from blocking application development.

---

# 52. Architecture Implementation Sequence

The recommended implementation sequence is:

```text
1. Repository and solution foundation
            |
            v
2. BuildingBlocks foundation
            |
            v
3. PostgreSQL
            |
            v
4. Auth Service
            |
            v
5. API Gateway
            |
            v
6. Product/User Services
            |
            v
7. Redis / Cart
            |
            v
8. Inventory
            |
            v
9. Order / Payment
            |
            v
10. Kafka workflows
            |
            v
11. Shipping / Notification
            |
            v
12. Review / Promotion / Search
            |
            v
13. Observability
            |
            v
14. Docker
            |
            v
15. Kubernetes / Minikube
            |
            v
16. GitHub Actions
```

The exact implementation order can be refined in the Development Roadmap.

---

# 53. Architecture Anti-Patterns to Avoid

CommerceX should explicitly avoid:

### Shared Database

```text
All Services
     |
     v
One Shared Schema
```

### Distributed Monolith

```text
Service A
  |
  +--> Service B
          |
          +--> Service C
                  |
                  +--> Service D
```

where every request requires a long synchronous chain.

### Shared Business Model

```text
All Services
     |
     v
Common Domain Entities
```

### Excessive Kafka

Publishing every small operation as an event without a business reason.

### Excessive gRPC

Using synchronous gRPC for every service interaction.

### Business Logic in Gateway

The gateway should route and enforce cross-cutting policies, not implement order or inventory rules.

### Infrastructure Before Business Logic

Kubernetes and observability should support working services rather than becoming blockers to basic development.

---

# 54. Architectural Success Criteria

The system architecture will be considered successfully implemented when:

1. All 12 services have clear responsibilities.
2. The API Gateway provides a unified external entry point.
3. Services own their data.
4. Services do not directly access other service databases.
5. REST is used for appropriate external APIs.
6. gRPC is used for selected internal synchronous calls.
7. Kafka is used for asynchronous business events.
8. Redis provides meaningful caching/cart functionality.
9. PostgreSQL provides durable service-owned persistence.
10. Services can run independently.
11. Services can be containerized independently.
12. Services can be deployed to Minikube.
13. Health checks work in Kubernetes.
14. Distributed workflows can be observed.
15. Grafana provides useful system visibility.
16. GitHub Actions can validate and package the services.
17. The overall system remains manageable as a learning project.

---

# 55. Architectural Baseline

The CommerceX architecture baseline is:

```text
                    CLIENT
                       |
                       v
                 API GATEWAY
                       |
       +---------------+---------------+
       |               |               |
       v               v               v
    SERVICES        SERVICES        SERVICES
       |               |               |
       +---------------+---------------+
                       |
          +------------+------------+
          |            |            |
          v            v            v
     PostgreSQL      Redis        Kafka
                                      |
                          +-----------+-----------+
                          |           |           |
                          v           v           v
                       Service     Service     Service

               Kubernetes / Minikube
                         |
                         v
                 OpenTelemetry
                         |
                         v
                      Grafana

                 GitHub Actions
                         |
                         v
                    CI/CD Pipeline
```

This architecture is the authoritative high-level technical baseline for the next design documents.

---

# 56. Relationship to Future Documents

This document defines **the overall technical architecture**.

The next documents will progressively define the architecture in greater detail:

```text
01 Project Charter
        |
        v
02 System Requirements
        |
        v
03 Functional Requirements
        |
        v
04 Non-Functional Requirements
        |
        v
05 System Architecture          <-- This document
        |
        v
06 Microservices Architecture
        |
        v
07 Service Boundaries
        |
        v
08 Data Architecture
        |
        v
09 API Design
        |
        v
10 gRPC Design
        |
        v
11 Kafka Event Design
        |
        v
12 Redis Caching Strategy
        |
        v
13 Security Design
        |
        v
14 Docker Design
        |
        v
15 Kubernetes Design
        |
        v
16 Observability Design
        |
        v
17 Testing Strategy
        |
        v
18 CI/CD Design
        |
        v
19 Development Roadmap
```

---

# 57. Approval / Baseline Record

| Item | Value |
|---|---|
| Project | CommerceX |
| Document | System Architecture |
| Document ID | COMX-DOC-005 |
| Version | 1.0 |
| Status | Architecture Baseline |
| Previous Document | Non-Functional Requirements |
| Next Document | Microservices Architecture |
| Purpose | Define the overall technical architecture and interaction model of CommerceX |

---

**End of Document**
