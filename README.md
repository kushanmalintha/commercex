# CommerceX

### Distributed E-Commerce Backend Platform

CommerceX is a learning-focused **distributed e-commerce backend platform** built with modern .NET technologies and microservices architecture.

The project is designed to provide practical experience with **microservices, REST APIs, gRPC, Apache Kafka, Redis, PostgreSQL, Docker, Kubernetes, observability, automated testing, and CI/CD** while keeping the overall system realistic and manageable.

---

## 📌 Project Status

> 🚧 **Active Development**

CommerceX is being implemented incrementally according to the project's architecture and development roadmap.

The project is intentionally developed phase-by-phase rather than implementing all services and infrastructure simultaneously.

---

# 🎯 Project Goals

The main goal of CommerceX is to build a realistic distributed e-commerce backend while gaining practical experience in modern backend and distributed-system engineering.

The project focuses on:

* Microservices architecture
* Service boundaries and data ownership
* REST API development
* Internal gRPC communication
* Event-driven architecture with Apache Kafka
* Redis caching and cart storage
* PostgreSQL persistence
* Authentication and authorization
* Distributed checkout workflows
* Idempotency and failure handling
* Docker containerization
* Kubernetes deployment
* OpenTelemetry-based observability
* Grafana dashboards
* Automated testing
* GitHub Actions CI/CD

---

# 🏗️ Architecture Overview

CommerceX consists of an **API Gateway** and **12 independently deployable business services**.

The API Gateway is a separate infrastructure/application component and is **not counted among the 12 business services**.

```text
                         Client
                           |
                           v
                    +--------------+
                    | API Gateway  |
                    |    YARP      |
                    +------+-------+
                           |
       +-------------------+-------------------+
       |                   |                   |
       v                   v                   v
     Auth                User               Product
       |                   |                   |
       v                   v                   v
   PostgreSQL          PostgreSQL          PostgreSQL
                                               |
                                               v
                                             Kafka
                                               |
                                               v
                                             Search


                 +-----------------------+
                 |     CommerceX Core    |
                 +-----------------------+
                 | Cart                  |
                 | Inventory             |
                 | Order                 |
                 | Payment               |
                 | Promotion             |
                 | Shipping              |
                 | Notification          |
                 | Review                |
                 +-----------------------+
                           |
             +-------------+-------------+
             |             |             |
             v             v             v
         PostgreSQL      Redis          Kafka
```

The architecture follows clear service ownership:

```text
Auth         → Authentication
User         → Customer profiles and addresses
Product      → Product catalog
Inventory    → Stock and reservations
Cart         → Shopping carts
Order        → Orders and checkout
Payment      → Simulated payments
Shipping     → Shipments
Review       → Product reviews
Notification → Notifications
Promotion    → Promotions and discounts
Search       → Product search read model
```

Each service owns its business logic and data. Services do not directly access one another's databases.

---

# 🧩 Services

|  # | Service                  | Responsibility                             | Primary Storage    |
| -: | ------------------------ | ------------------------------------------ | ------------------ |
|  1 | **Auth Service**         | Authentication, credentials, tokens, roles | PostgreSQL         |
|  2 | **User Service**         | Customer profiles and addresses            | PostgreSQL         |
|  3 | **Product Service**      | Products and categories                    | PostgreSQL + Redis |
|  4 | **Inventory Service**    | Stock and reservations                     | PostgreSQL         |
|  5 | **Cart Service**         | Shopping carts                             | Redis              |
|  6 | **Order Service**        | Orders and checkout                        | PostgreSQL         |
|  7 | **Payment Service**      | Simulated payment processing               | PostgreSQL         |
|  8 | **Shipping Service**     | Shipments and delivery status              | PostgreSQL         |
|  9 | **Review Service**       | Customer product reviews                   | PostgreSQL         |
| 10 | **Notification Service** | Event-driven notifications                 | PostgreSQL         |
| 11 | **Promotion Service**    | Coupons and discounts                      | PostgreSQL         |
| 12 | **Search Service**       | Product search/read model                  | PostgreSQL         |

---

# 🔄 Main Business Workflow

The primary distributed workflow in CommerceX is the checkout process.

```text
Customer
   |
   v
API Gateway
   |
   v
Order Service
   |
   +----> Cart
   |
   +----> Product
   |
   +----> Promotion
   |
   +----> Inventory
   |
   +----> Payment
   |
   v
Order Confirmed
   |
   v
Apache Kafka
   |
   +----> Shipping
   |
   +----> Notification
```

The order lifecycle is:

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

Cancellation is supported from appropriate states.

---

# 📡 Communication Architecture

CommerceX uses different communication mechanisms according to the type of interaction.

| Technology       | Usage                                    |
| ---------------- | ---------------------------------------- |
| **REST**         | Client-facing APIs                       |
| **gRPC**         | Selected internal synchronous operations |
| **Apache Kafka** | Asynchronous business events             |
| **Redis**        | Cart storage and selected caching        |

The project intentionally avoids using a single communication mechanism for everything.

---

## REST

External clients interact with CommerceX primarily through REST APIs.

Example:

```http
POST /api/v1/auth/login
GET  /api/v1/products
GET  /api/v1/search/products
GET  /api/v1/cart
POST /api/v1/orders
GET  /api/v1/orders/{id}
POST /api/v1/reviews
```

The API Gateway acts as the primary external entry point.

---

## gRPC

gRPC is used selectively for internal synchronous communication.

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

This allows services such as Order Service to obtain immediate responses where required.

---

## Apache Kafka

Kafka is used for asynchronous business-event propagation.

Examples include:

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

Example:

```text
Order Service
      |
      | OrderConfirmed
      v
    Kafka
    /   \
   v     v
Shipping Notification
```

Kafka events are designed as explicit integration contracts rather than direct database/entity dumps.

---

# 💾 Data Architecture

CommerceX follows a **database-per-service logical ownership model**.

PostgreSQL is the primary durable relational datastore.

For local development, multiple logical databases can run on a single PostgreSQL server.

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

The important rule is:

> A service must never directly access another service's database.

Cross-service information is obtained through APIs, gRPC, or Kafka events.

---

# ⚡ Redis

Redis is used selectively rather than as a replacement for PostgreSQL.

Primary uses:

### Shopping Cart

```text
commercex:cart:{customerId}
```

The Cart Service uses Redis as its primary active-cart store.

### Product and Category Caching

Product Service can use cache-aside caching for frequently accessed data.

```text
commercex:product:{productId}
commercex:category:{categoryId}
```

PostgreSQL remains the source of truth for product and category information.

If the product cache fails, the Product Service can fall back to PostgreSQL. Cart Redis availability is more critical because Redis is the cart's primary store.

---

# 🔐 Security

CommerceX applies security across multiple boundaries:

```text
Client
  ↓
API Gateway
  ↓
Services
  ↓
Databases / Redis / Kafka
```

Security features include:

* JWT-based authentication
* Access tokens
* Refresh tokens
* Refresh-token revocation
* Password hashing
* Password reset
* Role-based authorization
* Customer/Admin roles
* Resource ownership checks
* Input validation
* Rate limiting
* Secure error responses
* Secret management
* Service-to-service authentication
* Database access isolation
* Redis/Kafka access restrictions
* Secure container configuration

Passwords, tokens, credentials, and other sensitive information must never be logged or committed to source control.

---

# 🧱 Service Architecture

Business services follow a layered structure:

```text
Service
│
├── API
│
├── Application
│
├── Domain
│
└── Infrastructure
```

### API

Responsible for:

* HTTP endpoints
* Request/response DTOs
* API-level validation
* Authentication/authorization integration
* HTTP concerns

### Application

Responsible for:

* Use cases
* Application services/handlers
* Interfaces
* Orchestration
* Application validation

### Domain

Responsible for:

* Entities
* Business rules
* Domain behavior
* Domain invariants

### Infrastructure

Responsible for:

* EF Core
* PostgreSQL
* Redis
* Kafka
* gRPC clients
* External integrations
* Persistence implementations

---

# 🛠️ Technology Stack

## Backend

* **C#**
* **.NET 9**
* **ASP.NET Core**
* **Entity Framework Core**

## API & Communication

* REST
* JSON
* gRPC
* Protocol Buffers
* Apache Kafka

## Data

* PostgreSQL
* Redis
* Kafka
* Schema Registry

## Infrastructure

* Docker
* Docker Compose
* Kubernetes
* Minikube

## Observability

* OpenTelemetry
* Grafana
* Structured logging
* Distributed tracing
* Metrics

## Testing

* xUnit
* Integration testing
* Contract testing
* Testcontainers
* End-to-end testing
* Failure testing

## CI/CD

* Git
* GitHub
* GitHub Actions
* Container Registry

---

# 🐳 Docker

Every deployable application has its own container image.

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

The project uses multi-stage .NET Docker builds:

```text
.NET SDK
   |
   +--> Restore
   +--> Build
   +--> Publish
   |
   v
.NET ASP.NET Runtime
   |
   v
Application Container
```

Docker Compose is used for local infrastructure and integration environments.

---

# ☸️ Kubernetes

The initial Kubernetes deployment target is **Minikube**.

CommerceX uses a dedicated namespace:

```text
commercex
```

Each application is independently deployable.

Conceptually:

```text
commercex namespace
│
├── gateway
├── auth
├── user
├── product
├── inventory
├── cart
├── order
├── payment
├── shipping
├── review
├── notification
├── promotion
├── search
│
├── postgres
├── redis
├── kafka
└── schema-registry
```

Kubernetes provides:

* Service discovery
* Health checks
* Configuration
* Secrets
* Restart/recovery
* Horizontal scaling
* Rolling updates
* Resource management

The initial environment intentionally uses modest replica counts suitable for local Minikube development.

---

# 📊 Observability

CommerceX uses the three primary observability signals:

```text
Logs
Metrics
Traces
```

OpenTelemetry provides the common telemetry instrumentation model.

Grafana is used for visualization.

The system tracks areas such as:

* HTTP request rate
* API latency
* HTTP errors
* Database performance
* Redis cache hit/miss behavior
* Kafka consumer lag
* Kafka processing failures
* Service health
* Kubernetes resource usage
* Checkout success/failure
* Payment success/failure
* Inventory reservation failures

The primary distributed trace is the checkout workflow.

```text
Gateway
   ↓
Order
   ├── Promotion
   ├── Inventory
   ├── Payment
   └── Kafka
          ├── Shipping
          └── Notification
```

---

# 🧪 Testing Strategy

CommerceX follows a layered testing approach:

```text
Unit Tests
    ↓
Integration Tests
    ↓
API / Contract Tests
    ↓
Component Tests
    ↓
End-to-End Tests
    ↓
Infrastructure Tests
```

Testing includes:

* Domain tests
* Application tests
* PostgreSQL integration tests
* Redis integration tests
* Kafka integration tests
* gRPC tests
* REST API tests
* Contract tests
* Authentication tests
* Authorization tests
* Idempotency tests
* Failure tests
* Concurrency tests
* Security tests
* End-to-end checkout tests
* Docker tests
* Kubernetes smoke tests

Testcontainers will be used where appropriate to provide disposable infrastructure for integration testing.

---

# 🚀 CI/CD

GitHub Actions is used for CI/CD automation.

The intended pipeline is:

```text
Pull Request
      |
      v
Restore
      |
      v
Build
      |
      v
Unit Tests
      |
      v
Integration / Contract Tests
      |
      v
Security Checks
      |
      v
Docker Build
      |
      v
Image Scan
      |
      v
Container Registry
      |
      v
Kubernetes Deployment
      |
      v
Smoke Tests
```

Images are tagged using traceable identifiers such as Git commit SHA.

Example:

```text
commercex-order:5f5e622
```

This allows deployments to be traced back to the exact source revision.

---

# 💻 Local Development

CommerceX is developed using **Visual Studio Code**.

Recommended development tools:

* Visual Studio Code
* C# Dev Kit
* C# extension
* Docker
* Docker Compose
* Git
* REST Client
* YAML support
* GitLens
* kubectl
* Minikube

---

# 📋 Prerequisites

Install the following before developing CommerceX:

```text
.NET 9 SDK
Git
Docker
Docker Compose
kubectl
Minikube
```

Verify the installation:

```powershell
dotnet --version
dotnet --list-sdks

git --version

docker --version
docker compose version

kubectl version --client
minikube version
```

PostgreSQL, Redis, Kafka, and Schema Registry are intended to run through the project's containerized infrastructure rather than requiring separate native installations for normal development.

---

# 📁 Repository Structure

The repository follows a monorepo structure.

```text
CommerceX/
│
├── src/
│   ├── Gateway/
│   │
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
│   │
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
├── .github/
│   └── workflows/
│
├── .gitignore
├── .dockerignore
└── README.md
```

---

# 🔧 Development Approach

CommerceX is implemented incrementally.

The project does **not** attempt to implement all services simultaneously.

Each phase follows:

```text
Plan
  ↓
Implement
  ↓
Build
  ↓
Test
  ↓
Integrate
  ↓
Verify
  ↓
Commit
  ↓
Next Phase
```

Each completed phase should leave the repository in a working state.

---

# 🗺️ Development Roadmap

## Phase 0 — Infrastructure Foundation

Establish:

* Repository structure
* Docker
* Docker Compose
* PostgreSQL
* Redis
* Kafka
* Schema Registry
* Local infrastructure connectivity
* Initial Kubernetes foundation

---

## Phase 1 — Auth Service

Implement:

* Registration
* Login
* Password hashing
* JWT access tokens
* Refresh tokens
* Logout/revocation
* Password reset
* Roles
* PostgreSQL persistence
* EF Core migrations
* REST APIs
* Tests

---

## Phase 2 — User Service

Implement:

* Customer profiles
* Addresses
* Default address
* Ownership checks
* REST APIs
* PostgreSQL
* Tests

---

## Phase 3 — Product Service

Implement:

* Products
* Categories
* CRUD operations
* Product status
* Redis caching
* Kafka product events
* Pagination
* Filtering
* Sorting
* Tests

---

## Phase 4 — Promotion Service

Implement:

* Coupons
* Promotions
* Percentage discounts
* Fixed discounts
* Expiration
* Validation
* Discount calculation
* gRPC contract
* Tests

---

## Phase 5 — Cart Service

Implement:

* Cart retrieval
* Add item
* Update quantity
* Remove item
* Clear cart
* Redis storage
* TTL
* Ownership
* Tests

---

## Phase 6 — Inventory Service

Implement:

* Inventory
* Stock adjustment
* Stock reservation
* Stock release
* Idempotency
* Concurrency protection
* PostgreSQL
* gRPC
* Kafka events
* Tests

---

## Phase 7 — Order Service

Implement:

* Orders
* Order items
* Order status history
* Checkout
* Product integration
* Promotion integration
* Inventory integration
* Payment integration
* Historical price snapshots
* Shipping address snapshots
* Kafka events
* Idempotency
* Tests

This is the central business workflow of CommerceX.

---

## Phase 8 — Payment Service

Implement:

* Simulated payment processing
* Payment status
* Success/failure simulation
* Idempotency
* gRPC
* Kafka events
* PostgreSQL
* Tests

No real payment provider is required.

---

## Phase 9 — Shipping Service

Implement:

* Shipments
* Tracking information
* Shipping status
* Kafka consumers
* Kafka producers
* PostgreSQL
* Tests

---

## Phase 10 — Notification Service

Implement:

* Kafka consumers
* Notification records
* Simulated delivery
* Duplicate-event handling
* Failure handling
* Tests

Notification failures must not invalidate confirmed orders.

---

## Phase 11 — Review Service

Implement:

* Review creation
* Ratings
* Review updates
* Review deletion
* Customer ownership
* Rating validation
* PostgreSQL
* REST API
* Tests

---

## Phase 12 — Search Service

Implement:

* Product search read model
* Product event consumption
* Keyword search
* Filtering
* Sorting
* Pagination
* PostgreSQL
* REST API
* Tests

The initial implementation intentionally uses **PostgreSQL-based search** rather than Elasticsearch/OpenSearch.

---

## Phase 13 — API Gateway

Complete and harden the Gateway:

* YARP routing
* Authentication
* Authorization support
* Rate limiting
* Request limits
* Correlation IDs
* Error handling
* Observability
* API version routing

The Gateway must remain free of core business logic.

---

## Phase 14 — Cross-Service Hardening

Focus on:

* Idempotency
* Timeouts
* Retries
* Failure handling
* Compensation
* Duplicate Kafka events
* Concurrency
* Service isolation
* Authorization
* Distributed consistency

---

## Phase 15 — Testing Expansion

Expand:

* Unit tests
* Integration tests
* Contract tests
* API tests
* gRPC tests
* Kafka tests
* Redis tests
* E2E tests
* Security tests
* Failure tests
* Concurrency tests

---

## Phase 16 — Observability

Implement:

* OpenTelemetry
* Distributed tracing
* Structured logging
* Metrics
* Grafana dashboards
* Kafka observability
* Redis observability
* PostgreSQL observability
* Kubernetes observability

---

## Phase 17 — Dockerization

Finalize:

* Service Dockerfiles
* Multi-stage builds
* Docker Compose
* Health checks
* Runtime configuration
* Image tagging
* Image security scanning

---

## Phase 18 — Kubernetes / Minikube

Deploy CommerceX to Minikube.

Implement:

* Namespace
* Deployments
* Services
* ConfigMaps
* Secrets
* Persistent storage
* Health probes
* Resource limits
* Security contexts
* Service discovery
* Network policies where appropriate

---

## Phase 19 — CI/CD

Implement GitHub Actions for:

* Build
* Test
* Security checks
* Docker builds
* Image scanning
* Image publishing
* Kubernetes deployment
* Smoke testing

---

## Phase 20 — Final Validation

Perform complete system validation.

Test:

```text
Registration
     ↓
Authentication
     ↓
Profile
     ↓
Products
     ↓
Search
     ↓
Cart
     ↓
Promotion
     ↓
Inventory
     ↓
Checkout
     ↓
Payment
     ↓
Order
     ↓
Shipping
     ↓
Notification
     ↓
Review
```

Also validate:

* Kafka event propagation
* Redis behavior
* Failure handling
* Distributed tracing
* Docker deployment
* Kubernetes deployment
* CI/CD pipeline

The development roadmap follows the project's documented dependency-aware implementation sequence.

---

# 📚 Documentation

CommerceX maintains a dedicated architecture and design document for each major area.

|  # | Document                    |
| -: | --------------------------- |
| 01 | Project Charter             |
| 02 | System Requirements         |
| 03 | Functional Requirements     |
| 04 | Non-Functional Requirements |
| 05 | System Architecture         |
| 06 | Microservices Architecture  |
| 07 | Service Boundaries          |
| 08 | Data Architecture           |
| 09 | API Design                  |
| 10 | gRPC Design                 |
| 11 | Kafka Event Design          |
| 12 | Redis Caching Strategy      |
| 13 | Security Design             |
| 14 | Docker Design               |
| 15 | Kubernetes Design           |
| 16 | Observability Design        |
| 17 | Testing Strategy            |
| 18 | CI/CD Design                |
| 19 | Development Roadmap         |
| 20 | Final Project Report        |

These documents form the **authoritative architecture baseline** for implementation.

---

# 🧠 Key Architectural Decisions

CommerceX intentionally makes the following decisions:

### Microservices

Business capabilities are separated into independently deployable services.

### Database Ownership

Each service owns its data.

### PostgreSQL

PostgreSQL is the primary relational datastore.

### Redis

Redis is used selectively for carts and caching rather than replacing PostgreSQL.

### Kafka

Kafka is used for asynchronous business events.

### gRPC

gRPC is used selectively for internal synchronous operations.

### REST

REST is the primary external API style.

### Simulated Payment

Payment processing is simulated to avoid unnecessary external provider complexity.

### PostgreSQL Search

The initial search implementation uses PostgreSQL rather than introducing Elasticsearch/OpenSearch.

### Docker

All applications are independently containerized.

### Kubernetes

Minikube is the initial deployment environment.

### OpenTelemetry

OpenTelemetry provides the application's observability foundation.

### Grafana

Grafana provides dashboards and operational visibility.

### GitHub Actions

GitHub Actions automates CI/CD.

---

# 🚫 Intentionally Deferred

To keep the learning scope manageable, CommerceX does not initially implement:

* Real payment gateways
* Real email/SMS providers
* Elasticsearch
* OpenSearch
* Service mesh
* Istio
* Multi-region deployment
* Multi-cloud deployment
* Advanced recommendation systems
* Complex warehouse management
* Full event sourcing
* Complex CQRS
* Distributed two-phase transactions

These may be considered as future extensions.

---

# 🔒 Important Architecture Rules

The following rules should not be violated during implementation.

### Rule 1 — No Cross-Service Database Access

```text
❌ Order → Inventory Database

✅ Order → Inventory gRPC/API
```

### Rule 2 — No Cross-Service Foreign Keys

References between services use identifiers rather than database-level foreign keys.

### Rule 3 — No Business Logic in the Gateway

```text
Gateway → Routing/policies
Service → Business logic
```

### Rule 4 — Do Not Use Kafka for Everything

Kafka is used when asynchronous communication is appropriate.

### Rule 5 — Do Not Use gRPC for Everything

gRPC is used where immediate internal responses are required.

### Rule 6 — Do Not Treat Redis as PostgreSQL

Redis is authoritative for active carts but is a cache for product/category data.

### Rule 7 — Preserve Historical Order Data

Order prices and shipping information required for historical accuracy must be stored as order-owned snapshots.

### Rule 8 — Handle Duplicate Events

Kafka uses at-least-once delivery assumptions, so important consumers must be idempotent.

### Rule 9 — Avoid Distributed Transactions

CommerceX uses local transactions, events, and compensation rather than distributed two-phase transactions.

### Rule 10 — Keep Services Independently Deployable

A service should be buildable, testable, containerizable, and deployable without requiring another service's internal implementation.

---

# 📈 Project Success Criteria

CommerceX will be considered successful when the system can demonstrate:

* Independent microservices
* Clear service boundaries
* Secure authentication
* Customer management
* Product management
* Product search
* Redis-backed shopping carts
* Inventory reservation
* Promotion validation
* Successful checkout
* Payment failure handling
* Order lifecycle management
* Shipping workflow
* Event-driven notifications
* Product reviews
* Kafka event propagation
* Idempotent distributed operations
* Automated tests
* Docker deployment
* Kubernetes deployment
* Distributed tracing
* Grafana dashboards
* GitHub Actions CI/CD

---

# 🎓 Learning Outcomes

By completing CommerceX, the project demonstrates practical experience with:

```text
Modern .NET
     ↓
REST APIs
     ↓
Microservices
     ↓
PostgreSQL
     ↓
Redis
     ↓
gRPC
     ↓
Kafka
     ↓
Distributed Workflows
     ↓
Docker
     ↓
Kubernetes
     ↓
OpenTelemetry
     ↓
Grafana
     ↓
Automated Testing
     ↓
CI/CD
```

The project therefore serves both as a practical engineering exercise and as a portfolio project demonstrating distributed backend development.

---

# 🤝 Development Philosophy

CommerceX is primarily a **learning project**.

The implementation prioritizes:

* Understanding over shortcuts
* Clear architecture over unnecessary abstraction
* Incremental development over large rewrites
* Testability over premature optimization
* Practical distributed-system patterns over enterprise complexity

Every architectural decision should have a clear reason.

---

# 📄 License

This project is intended primarily for educational and portfolio purposes.

Add the project's chosen license here if/when a license is selected.

---

# 👤 Author

**CommerceX**

Distributed E-Commerce Backend Platform

Built with:

```text
C# • .NET 9 • ASP.NET Core • PostgreSQL • Redis
Kafka • gRPC • Docker • Kubernetes
OpenTelemetry • Grafana • GitHub Actions
```

---

## ⭐ Project Direction

CommerceX is being developed progressively from infrastructure foundations toward a complete distributed e-commerce platform.

The repository's implementation should always remain aligned with the documented architecture baseline.
