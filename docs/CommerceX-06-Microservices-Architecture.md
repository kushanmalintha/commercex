# CommerceX — Microservices Architecture

**Document ID:** COMX-DOC-006  
**Document Version:** 1.0  
**Status:** Architecture Baseline  
**Project:** CommerceX  
**Related Documents:**
- `01-project-charter.md`
- `02-system-requirements.md`
- `03-functional-requirements.md`
- `04-non-functional-requirements.md`
- `05-system-architecture.md`

---

# 1. Introduction

## 1.1 Purpose

This document defines the microservices architecture of CommerceX.

The System Architecture document established the overall technical architecture. This document refines that architecture by defining:

- The 12 backend services
- The responsibility of each service
- Service boundaries
- Service ownership
- Major dependencies
- Communication responsibilities
- Data ownership
- Service interaction patterns
- Service-level architecture
- Distributed workflow responsibilities

This document intentionally remains at the **microservice architecture level**.

Detailed database schemas, REST endpoints, gRPC messages, Kafka event payloads, Kubernetes manifests, and implementation-specific classes will be defined in later documents.

---

# 2. Microservices Architecture Goals

The CommerceX microservices architecture has five primary goals:

1. Establish clear business boundaries.
2. Allow services to be developed and deployed independently.
3. Prevent direct database coupling between services.
4. Demonstrate multiple distributed communication patterns.
5. Keep each service small enough to remain manageable as a learning project.

The architecture should favor:

```text
Clear responsibility
        +
Low coupling
        +
High cohesion
        +
Independent deployment
```

over unnecessary architectural complexity.

---

# 3. Service Inventory

CommerceX consists of the following 12 backend services:

| # | Service | Primary Responsibility |
|---|---|---|
| 1 | Auth Service | Authentication, credentials, tokens, roles |
| 2 | User Service | Customer profiles and addresses |
| 3 | Product Service | Products and categories |
| 4 | Inventory Service | Stock and reservations |
| 5 | Cart Service | Shopping carts |
| 6 | Order Service | Orders and order lifecycle |
| 7 | Payment Service | Simulated payments |
| 8 | Shipping Service | Shipments and delivery status |
| 9 | Review Service | Product reviews and ratings |
| 10 | Notification Service | Asynchronous notification processing |
| 11 | Promotion Service | Coupons and discounts |
| 12 | Search Service | Product search and filtering |

The API Gateway is an architectural component and is not counted as one of the 12 business services.

---

# 4. Service Boundary Principles

Each service shall follow these principles.

## 4.1 Single Business Responsibility

A service should represent one cohesive business capability.

For example:

```text
Inventory Service
    |
    +--> Stock
    +--> Reservation
    +--> Release
```

rather than:

```text
Inventory Service
    |
    +--> Products
    +--> Orders
    +--> Payments
    +--> Users
```

## 4.2 Data Ownership

Each service owns its own data.

```text
Order Service
    |
    v
Order Database
```

Other services must communicate through APIs or events.

## 4.3 Independent Deployment

A service should be deployable without requiring the entire platform to be rebuilt or redeployed.

## 4.4 Independent Scaling

A service should be capable of running multiple instances where necessary.

## 4.5 Contract-Based Communication

Services communicate through:

```text
REST
gRPC
Kafka Events
```

rather than sharing internal classes or database tables.

---

# 5. Overall Service Architecture

```text
                             CLIENT
                                |
                                v
                       +----------------+
                       |  API Gateway   |
                       +-------+--------+
                               |
        +----------------------+----------------------+
        |             |               |              |
        v             v               v              v
      Auth          User           Product          Search
       |              |               |              |
       |              |               |              |
       +--------------+---------------+--------------+
                                      |
                    +-----------------+----------------+
                    |                 |                |
                    v                 v                v
                  Cart            Promotion        Inventory
                    |                 |                |
                    +-----------------+----------------+
                                      |
                                      v
                                    Order
                                      |
                         +------------+------------+
                         |                         |
                         v                         v
                     Payment                   Kafka Events
                                                   |
                              +--------------------+----------------+
                              |                    |                |
                              v                    v                v
                           Shipping           Notification        Other
```

This is a logical architecture rather than a strict request sequence.

---

# 6. Service Classification

The services can be grouped into logical domains.

## 6.1 Identity Domain

```text
Auth Service
User Service
```

## 6.2 Catalog Domain

```text
Product Service
Search Service
Promotion Service
```

## 6.3 Shopping Domain

```text
Cart Service
Inventory Service
```

## 6.4 Transaction Domain

```text
Order Service
Payment Service
```

## 6.5 Fulfillment Domain

```text
Shipping Service
Notification Service
```

## 6.6 Customer Experience Domain

```text
Review Service
```

These groupings are logical and do not mean that services share databases.

---

# 7. Auth Service

## 7.1 Responsibility

The Auth Service owns authentication and credential-related functionality.

It is responsible for:

- Customer registration
- Credential management
- Password hashing
- Login
- JWT access tokens
- Refresh tokens
- Logout
- Password reset
- Role information

## 7.2 Owned Data

The Auth Service owns data such as:

```text
Credential
RefreshToken
PasswordResetToken
Authentication-related identity information
```

## 7.3 Database

```text
PostgreSQL
    |
    v
commercex_auth
```

## 7.4 Communication

External:

```text
REST
```

Internal:

```text
Kafka events where appropriate
```

## 7.5 Dependencies

The Auth Service should minimize dependencies.

It should not require:

```text
Product
Cart
Order
Inventory
Payment
Shipping
```

for normal authentication.

## 7.6 Events

Potential event:

```text
UserRegistered
```

## 7.7 Boundary

The Auth Service does not own:

- Customer profile
- Address
- Orders
- Product data

Those belong to other services.

---

# 8. User Service

## 8.1 Responsibility

The User Service owns customer profile information.

Responsibilities:

- Customer profiles
- Addresses
- Default address
- Basic profile management

## 8.2 Owned Data

```text
UserProfile
Address
```

## 8.3 Database

```text
PostgreSQL
    |
    v
commercex_users
```

## 8.4 Communication

External:

```text
REST
```

Internal:

```text
REST/gRPC where required
Kafka events where useful
```

## 8.5 Dependencies

The User Service may reference the identity identifier created by Auth Service but should not directly access Auth Service's database.

## 8.6 Boundary

The User Service does not own:

- Password credentials
- Tokens
- Orders
- Products
- Cart contents

---

# 9. Product Service

## 9.1 Responsibility

The Product Service owns the product catalog.

Responsibilities:

- Products
- Categories
- Product descriptions
- Prices
- Product status
- Basic product metadata

## 9.2 Owned Data

```text
Product
Category
```

## 9.3 Database

```text
PostgreSQL
    |
    v
commercex_products
```

## 9.4 Communication

External:

```text
REST
```

Internal:

```text
gRPC / REST where required
Kafka events
```

## 9.5 Redis

Product data may be cached using Redis.

Example:

```text
product:{productId}
```

## 9.6 Events

Potential events:

```text
ProductCreated
ProductUpdated
ProductDeactivated
```

## 9.7 Boundary

The Product Service does not own:

- Inventory quantities
- Shopping carts
- Orders
- Reviews
- Promotions

---

# 10. Inventory Service

## 10.1 Responsibility

The Inventory Service owns stock availability and reservations.

Responsibilities:

- Inventory records
- Available quantity
- Reserved quantity
- Stock adjustments
- Stock reservations
- Stock releases

## 10.2 Owned Data

```text
Inventory
StockReservation
```

## 10.3 Database

```text
PostgreSQL
    |
    v
commercex_inventory
```

## 10.4 Communication

External:

```text
REST for administrative operations
```

Internal:

```text
gRPC for synchronous reservation operations
Kafka for asynchronous events
```

## 10.5 Events

Potential events:

```text
InventoryReserved
InventoryReleased
InventoryAdjusted
```

## 10.6 Important Boundary

The Inventory Service owns the truth about stock.

The Product Service must not contain authoritative inventory quantities.

---

# 11. Cart Service

## 11.1 Responsibility

The Cart Service owns active shopping carts.

Responsibilities:

- Create/retrieve cart
- Add item
- Update quantity
- Remove item
- Clear cart

## 11.2 Owned Data

```text
Cart
CartItem
```

## 11.3 Storage

Primary initial storage:

```text
Redis
```

Example:

```text
cart:{customerId}
```

## 11.4 Communication

External:

```text
REST
```

Internal:

```text
REST/gRPC where appropriate
```

## 11.5 Boundary

The Cart Service does not own:

- Product master data
- Inventory truth
- Orders
- Payments

It may temporarily contain product references and snapshots required for cart operations.

---

# 12. Promotion Service

## 12.1 Responsibility

The Promotion Service owns discount and coupon rules.

Responsibilities:

- Promotion creation
- Promotion activation/deactivation
- Promotion validation
- Discount calculation

## 12.2 Owned Data

```text
Promotion
PromotionRule
```

For the initial implementation, a separate complex rule engine is unnecessary.

## 12.3 Database

```text
PostgreSQL
    |
    v
commercex_promotions
```

## 12.4 Communication

External:

```text
REST
```

Internal:

```text
gRPC for synchronous validation/calculation
```

## 12.5 Boundary

The Promotion Service does not own:

- Product catalog
- Orders
- Customer accounts
- Payment

---

# 13. Order Service

## 13.1 Responsibility

The Order Service is the central transaction-oriented business service.

Responsibilities:

- Order creation
- Order items
- Order totals
- Order status
- Order cancellation
- Order history

## 13.2 Owned Data

```text
Order
OrderItem
OrderStatusHistory
```

## 13.3 Database

```text
PostgreSQL
    |
    v
commercex_orders
```

## 13.4 Communication

External:

```text
REST
```

Internal:

```text
gRPC
Kafka
```

## 13.5 Dependencies

The Order Service may interact with:

```text
Cart Service
Product Service
Inventory Service
Promotion Service
Payment Service
```

However, the architecture should avoid creating a long synchronous chain.

## 13.6 Events

Potential events:

```text
OrderCreated
OrderConfirmed
OrderCancelled
OrderShipped
OrderDelivered
```

## 13.7 Boundary

The Order Service owns order state.

Other services should not directly modify the Order database.

---

# 14. Payment Service

## 14.1 Responsibility

The Payment Service owns simulated payment processing.

Responsibilities:

- Payment initiation
- Payment result
- Payment status
- Payment record
- Idempotency

## 14.2 Owned Data

```text
Payment
PaymentStatus
```

## 14.3 Database

```text
PostgreSQL
    |
    v
commercex_payments
```

## 14.4 Communication

External:

```text
REST where needed
```

Internal:

```text
gRPC / Kafka
```

## 14.5 Simulation

The service may support controlled outcomes:

```text
SUCCESS
FAILURE
```

This allows distributed failure scenarios to be tested.

## 14.6 Boundary

The Payment Service shall not store real payment credentials.

It does not own:

- Orders
- Customer credentials
- Bank information

---

# 15. Shipping Service

## 15.1 Responsibility

The Shipping Service owns shipment information and delivery status.

Responsibilities:

- Shipment creation
- Tracking number generation
- Shipment status
- Delivery status

## 15.2 Owned Data

```text
Shipment
TrackingInformation
ShipmentStatus
```

## 15.3 Database

```text
PostgreSQL
    |
    v
commercex_shipping
```

## 15.4 Communication

External:

```text
REST
```

Internal:

```text
Kafka
REST/gRPC where appropriate
```

## 15.5 Events

Potential events:

```text
ShipmentCreated
ShipmentInTransit
ShipmentOutForDelivery
ShipmentDelivered
```

## 15.6 Boundary

Shipping owns shipment state, not order state.

The Order Service remains the owner of order lifecycle.

---

# 16. Review Service

## 16.1 Responsibility

The Review Service owns product reviews and ratings.

Responsibilities:

- Create reviews
- Update reviews
- Delete reviews
- Retrieve reviews
- Rating validation
- Basic moderation status

## 16.2 Owned Data

```text
Review
ReviewStatus
```

## 16.3 Database

```text
PostgreSQL
    |
    v
commercex_reviews
```

## 16.4 Communication

External:

```text
REST
```

Internal:

```text
REST/gRPC
Kafka where appropriate
```

## 16.5 Boundary

The Review Service owns review data.

The Product Service owns product information.

A review contains a product identifier but does not own the product.

---

# 17. Notification Service

## 17.1 Responsibility

The Notification Service processes notification-related events asynchronously.

Responsibilities:

- Consume Kafka events
- Generate notification records
- Simulate email/notification delivery
- Handle notification failures

## 17.2 Owned Data

The initial implementation may store:

```text
Notification
NotificationStatus
```

Alternatively, a minimal implementation may rely primarily on structured logs.

## 17.3 Database

Optional PostgreSQL database:

```text
commercex_notifications
```

A database is not mandatory if the project only needs simulated output.

## 17.4 Communication

Primary:

```text
Kafka
```

External REST APIs are not required for the core notification workflow.

## 17.5 Boundary

The Notification Service should not become part of the synchronous checkout path.

---

# 18. Search Service

## 18.1 Responsibility

The Search Service provides product search and filtering.

Responsibilities:

- Text search
- Filtering
- Sorting
- Pagination

## 18.2 Data Model

The Search Service may maintain a simplified product read model.

Possible data:

```text
ProductSearchDocument
```

## 18.3 Database

Initial implementation:

```text
PostgreSQL
    |
    v
commercex_search
```

## 18.4 Synchronization

Product information can be updated using product events.

Example:

```text
Product Service
      |
      v
ProductUpdated
      |
      v
Kafka
      |
      v
Search Service
      |
      v
Search Read Model
```

## 18.5 Boundary

The Search Service does not become the authoritative owner of product data.

Product Service remains the source of truth.

---

# 19. Service Dependency Model

A simplified dependency model is:

```text
                   Auth
                    |
                    v
                   User


                 Product
                    |
          +---------+---------+
          |                   |
          v                   v
        Search            Cart
                              |
                              v
                         Order Service
                         /     |                              /      |                              v       v        v
                 Inventory  Payment  Promotion
                       |
                       |
                     Kafka
                       |
              +--------+--------+
              |                 |
              v                 v
          Shipping        Notification

                Review
                  |
                  v
             Product Reference
```

This is a conceptual dependency map, not a strict request sequence.

---

# 20. Synchronous Dependency Rules

Synchronous communication should be used only when the caller needs an immediate answer.

Examples:

```text
Order -> Inventory
```

Question:

```text
Can this stock be reserved?
```

Response:

```text
Success / Failure
```

Another example:

```text
Order -> Promotion
```

Question:

```text
Is this promotion valid and what discount applies?
```

The architecture should avoid chains such as:

```text
Gateway
  -> Order
      -> Product
          -> Inventory
              -> Promotion
                  -> User
```

because long synchronous chains increase latency and failure propagation.

---

# 21. Asynchronous Dependency Rules

Kafka should be used when a service needs to communicate a business fact without requiring an immediate response.

Example:

```text
Order Service
     |
     v
OrderConfirmed
     |
     v
Kafka
     |
     +--> Shipping
     |
     +--> Notification
```

Shipping and Notification can process the event independently.

---

# 22. Service-to-Service Communication Matrix

The following is the initial architectural communication model.

| Source | Target | Mechanism | Purpose |
|---|---|---|---|
| Gateway | Auth | REST | Authentication |
| Gateway | User | REST | Profile operations |
| Gateway | Product | REST | Product operations |
| Gateway | Search | REST | Search |
| Gateway | Cart | REST | Cart operations |
| Gateway | Order | REST | Order operations |
| Gateway | Review | REST | Reviews |
| Gateway | Promotion | REST | Admin promotion operations |
| Order | Inventory | gRPC | Stock reservation |
| Order | Promotion | gRPC | Promotion validation |
| Order | Payment | gRPC/REST | Payment initiation |
| Product | Search | Kafka | Product synchronization |
| Order | Kafka | Kafka | Order events |
| Payment | Kafka | Kafka | Payment events |
| Inventory | Kafka | Kafka | Inventory events |
| Kafka | Shipping | Kafka | Shipment creation |
| Kafka | Notification | Kafka | Notifications |
| Shipping | Kafka | Kafka | Shipment events |
| Review | Kafka | Kafka | Review events |

This matrix is an initial baseline. Specific interactions may change during detailed design.

---

# 23. Data Ownership Matrix

| Service | Owned Data | Primary Store |
|---|---|---|
| Auth | Credentials, refresh tokens, reset tokens | PostgreSQL |
| User | Profiles, addresses | PostgreSQL |
| Product | Products, categories | PostgreSQL |
| Inventory | Stock, reservations | PostgreSQL |
| Cart | Carts, cart items | Redis |
| Order | Orders, order items, status history | PostgreSQL |
| Payment | Payment records/status | PostgreSQL |
| Shipping | Shipments/tracking/status | PostgreSQL |
| Review | Reviews/ratings | PostgreSQL |
| Notification | Notifications/status | PostgreSQL or logs |
| Promotion | Promotions/discount rules | PostgreSQL |
| Search | Search read model | PostgreSQL |

---

# 24. Shared Data Rules

Services may share identifiers but must not share ownership.

Example:

```text
Order
 |
 +--> ProductId
 +--> CustomerId
 +--> AddressId
```

These identifiers reference concepts owned by other services.

The Order Service may store snapshots required for historical correctness.

For example:

```text
OrderItem
    ProductId
    ProductNameSnapshot
    UnitPriceSnapshot
```

This prevents historical orders from changing when the Product Service changes current product information.

---

# 25. Service API Boundary

Each service exposes only the operations required by its responsibilities.

For example:

```text
Product Service
    |
    +--> Product CRUD
    +--> Category CRUD

Inventory Service
    |
    +--> Stock lookup
    +--> Reserve
    +--> Release
    +--> Adjust

Order Service
    |
    +--> Create order
    +--> Get order
    +--> List orders
    +--> Cancel order
```

Internal implementation details should remain private.

---

# 26. Service Internal Architecture

Each service should use a consistent structure:

```text
CommerceX.<Service>.API
CommerceX.<Service>.Application
CommerceX.<Service>.Domain
CommerceX.<Service>.Infrastructure
```

Example:

```text
CommerceX.Order/
|
+-- CommerceX.Order.API
+-- CommerceX.Order.Application
+-- CommerceX.Order.Domain
+-- CommerceX.Order.Infrastructure
```

---

# 27. Domain Layer

The Domain layer should contain:

- Entities
- Value objects
- Domain rules
- Domain behavior
- Domain events where appropriate

It should not depend on:

```text
PostgreSQL
Redis
Kafka
HTTP
gRPC
ASP.NET Core
```

where avoidable.

---

# 28. Application Layer

The Application layer should contain:

- Use cases
- Commands
- Queries
- Application services/handlers
- Validation
- Interfaces for infrastructure dependencies

Example:

```text
CreateOrder
ReserveInventory
ConfirmPayment
```

---

# 29. Infrastructure Layer

The Infrastructure layer should implement technical concerns such as:

- EF Core
- PostgreSQL
- Redis
- Kafka
- gRPC clients
- External integrations
- Repository implementations

---

# 30. API Layer

The API layer should expose:

- REST controllers/endpoints
- gRPC endpoints where the service provides them
- Authentication configuration
- Request/response mapping

The API layer should not contain substantial business logic.

---

# 31. BuildingBlocks

CommerceX may contain shared technical BuildingBlocks.

Potential contents:

```text
CommerceX.BuildingBlocks
|
+-- Messaging
+-- Observability
+-- Persistence
+-- Common technical abstractions
```

The BuildingBlocks project must not become a shared domain model.

Avoid:

```text
BuildingBlocks
    |
    +-- Order Entity
    +-- Product Entity
    +-- Customer Entity
```

---

# 32. Service Communication Rules

The following rules apply:

### Rule 1

Use REST for client-facing APIs.

### Rule 2

Use gRPC when an internal synchronous call needs a strongly typed contract.

### Rule 3

Use Kafka when asynchronous processing or event propagation is appropriate.

### Rule 4

Do not use Kafka simply to replace every API call.

### Rule 5

Do not use gRPC simply because it is available.

### Rule 6

Do not directly access another service's database.

### Rule 7

Do not expose internal implementation classes as service contracts.

---

# 33. Distributed Transaction Strategy

CommerceX will not use a traditional distributed database transaction across services.

Instead:

```text
Local Transaction
       +
Synchronous Validation
       +
Asynchronous Events
       +
Compensation
```

Example:

```text
Order Created
      |
      v
Inventory Reserved
      |
      v
Payment Failed
      |
      v
Inventory Released
      |
      v
Order Cancelled/Failed
```

This provides a simple introduction to distributed transaction concepts.

---

# 34. Failure Isolation

Service boundaries should reduce failure propagation.

Example:

```text
Notification Service
        |
       DOWN
        |
        X
        |
Order Service
        |
       OK
```

Because notifications are asynchronous, the order should remain valid.

Another example:

```text
Inventory Service
        |
       DOWN
        |
        v
Checkout cannot complete
```

This is acceptable because inventory is a required dependency for checkout.

---

# 35. Idempotency Boundaries

Idempotency is particularly important for:

```text
Inventory Reservation
Inventory Release
Payment
Order Processing
Kafka Consumers
```

Example:

```text
ReserveInventory(Order123)
```

repeated twice should not reserve twice.

The same principle applies to:

```text
ProcessPayment(Order123)
```

---

# 36. Event Ownership

A service should publish events about state changes it owns.

Examples:

```text
Product Service
    -> ProductUpdated

Order Service
    -> OrderConfirmed

Payment Service
    -> PaymentSucceeded

Inventory Service
    -> InventoryReserved

Shipping Service
    -> ShipmentDelivered
```

A service should not publish an event claiming another service's state has changed.

For example:

```text
Order Service
```

should not publish:

```text
PaymentSucceeded
```

because Payment Service owns payment state.

---

# 37. Event Consumer Independence

Consumers should process events independently.

Example:

```text
OrderConfirmed
       |
       v
     Kafka
       |
 +-----+-----+
 |           |
 v           v
Shipping  Notification
```

Shipping failure should not prevent Notification Service from processing the same event.

---

# 38. Service Scaling Model

The architecture allows independent scaling.

Example:

```text
Product Service
   |
   +-- Pod 1
   +-- Pod 2
   +-- Pod 3
```

while:

```text
Review Service
   |
   +-- Pod 1
```

This is a core advantage of the architecture.

Kafka consumers can also scale through consumer groups.

---

# 39. Service Security Boundaries

Each service shall enforce appropriate authorization for its operations.

Example:

```text
Customer
   |
   +--> Own Cart       ALLOWED
   +--> Own Orders     ALLOWED
   +--> Any Product    READ
   +--> Other Cart     DENIED
   +--> Admin Product  MANAGEMENT DENIED
```

Admin operations require the Admin role.

---

# 40. Main Distributed Workflow

The primary end-to-end workflow is:

```text
Customer
   |
   v
API Gateway
   |
   v
Cart
   |
   v
Order
   |
   +----> Promotion
   |
   +----> Inventory
   |
   +----> Payment
   |
   v
Kafka
   |
   +----> Shipping
   |
   +----> Notification
```

The Product and User services provide supporting information.

---

# 41. Product Information Flow

```text
Admin
  |
  v
API Gateway
  |
  v
Product Service
  |
  v
PostgreSQL
  |
  +--> ProductCreated
  +--> ProductUpdated
  +--> ProductDeactivated
           |
           v
         Kafka
           |
           v
     Search Service
```

Search maintains a read-oriented representation without becoming the product system of record.

---

# 42. Cart Information Flow

```text
Customer
   |
   v
API Gateway
   |
   v
Cart Service
   |
   v
Redis
```

The Cart Service may validate product references through appropriate service communication.

The Cart Service does not own product prices as authoritative values.

---

# 43. Order Information Flow

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
   +----> Promotion
   |
   +----> Inventory
   |
   +----> Payment
   |
   v
Order DB
```

The Order Service maintains the authoritative order state.

---

# 44. Fulfillment Information Flow

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
       |
       v
ShipmentCreated
       |
       v
     Kafka
       |
       v
Notification Service
```

The exact event choreography will be finalized in the Kafka Event Design.

---

# 45. Service Boundary Validation Questions

Before implementing each service, the following questions should be answered:

1. What business capability does this service own?
2. What data does it own?
3. What data must it never own?
4. Which operations does it expose?
5. Which services does it need to communicate with?
6. Which communication mechanism should be used?
7. Which events does it publish?
8. Which events does it consume?
9. Can it operate independently?
10. What happens if one of its dependencies fails?

These questions will guide detailed service design.

---

# 46. Service-Level Definition of Done

A service architecture is considered ready for implementation when:

```text
Service Responsibility Defined
          |
          v
Boundary Defined
          |
          v
Owned Data Defined
          |
          v
Dependencies Defined
          |
          v
Communication Defined
          |
          v
Events Defined
          |
          v
Security Boundary Defined
          |
          v
Failure Scenarios Identified
```

Detailed implementation can then begin.

---

# 47. Architecture Anti-Patterns

CommerceX should avoid the following.

## 47.1 Shared Database

```text
Service A ----+
Service B ----+--> Shared Database
Service C ----+
```

## 47.2 Shared Domain Library

```text
All Services
      |
      v
Shared Business Entities
```

## 47.3 Distributed Monolith

```text
A -> B -> C -> D -> E
```

where every operation requires a long synchronous chain.

## 47.4 Chatty Services

Avoid many small network calls for a single simple operation.

## 47.5 Gateway Business Logic

Do not put:

```text
Order Rules
Inventory Rules
Payment Rules
```

inside the gateway.

## 47.6 Event Everything

Not every CRUD operation needs to become an event.

---

# 48. Technology Mapping

| Concern | Technology |
|---|---|
| Service Implementation | C# / .NET 9 |
| HTTP APIs | ASP.NET Core |
| External API | REST |
| Internal RPC | gRPC |
| Messaging | Apache Kafka |
| Persistent Data | PostgreSQL |
| ORM | Entity Framework Core |
| Fast State/Cache | Redis |
| Gateway | ASP.NET Core + YARP |
| Containers | Docker |
| Orchestration | Kubernetes |
| Local Cluster | Minikube |
| CI/CD | GitHub Actions |
| Telemetry | OpenTelemetry |
| Dashboards | Grafana |

---

# 49. Microservices Architecture Decision Summary

| Decision | Choice |
|---|---|
| Architecture | Microservices |
| Service Count | 12 |
| Repository | Monorepo |
| Data Ownership | Database per service |
| Local DB Infrastructure | One PostgreSQL server with logical databases |
| Client Communication | REST |
| Internal Synchronous | gRPC selectively |
| Asynchronous | Kafka |
| Cart Storage | Redis |
| Product Cache | Redis selectively |
| Search | PostgreSQL-based initially |
| Payment | Simulated |
| Gateway | YARP |
| Containers | Docker |
| Orchestration | Kubernetes / Minikube |
| Observability | OpenTelemetry + Grafana |
| CI/CD | GitHub Actions |

---

# 50. Implementation Boundaries

The following should be implemented only after their boundaries are agreed:

```text
Auth
User
Product
Inventory
Cart
Order
Payment
Shipping
Review
Notification
Promotion
Search
```

Implementation should proceed service by service.

A service should not be considered complete merely because its basic CRUD APIs work. Its communication, persistence, testing, observability, and deployment requirements must eventually be addressed.

---

# 51. Recommended Initial Implementation Order

The service implementation order should follow business dependencies and learning progression:

```text
1. Auth Service
       |
       v
2. User Service
       |
       v
3. Product Service
       |
       v
4. Promotion Service
       |
       v
5. Cart Service
       |
       v
6. Inventory Service
       |
       v
7. Order Service
       |
       v
8. Payment Service
       |
       v
9. Shipping Service
       |
       v
10. Notification Service
       |
       v
11. Review Service
       |
       v
12. Search Service
```

The API Gateway should be introduced early enough to validate external routing, but detailed gateway policies can be refined as services become available.

---

# 52. Architectural Baseline

The CommerceX microservices baseline is:

```text
                    API GATEWAY
                         |
       +-----------------+-----------------+
       |                 |                 |
      Auth              User            Product
       |                 |                 |
       |                 |                 +----> Search
       |                 |
       |                 |
       +-----------------+-----------------+
                         |
                        Cart
                         |
                    +----+----+
                    |         |
               Promotion   Inventory
                    |         |
                    +----+----+
                         |
                        Order
                     /    |                        /     |                        v      v      v
              Inventory Payment  Kafka
                                  |
                        +---------+---------+
                        |                   |
                        v                   v
                    Shipping          Notification

                         |
                       Review
```

The diagram represents ownership and major interaction relationships, not every possible request path.

---

# 53. Future Refinement

The following documents will refine this architecture:

### Service Boundaries

Will define each service's domain boundaries in greater detail.

### Data Architecture

Will define:

- Tables
- Entities
- Relationships
- Indexes
- Database boundaries
- Persistence strategies

### API Design

Will define:

- REST endpoints
- Request models
- Response models
- HTTP status codes
- Error contracts

### gRPC Design

Will define:

- `.proto` files
- Services
- RPC methods
- Messages
- Error handling

### Kafka Event Design

Will define:

- Topics
- Event schemas
- Producers
- Consumers
- Consumer groups
- Delivery behavior

### Redis Caching Strategy

Will define:

- Keys
- TTLs
- Serialization
- Cache-aside behavior
- Invalidation

---

# 54. Final Microservices Architecture Principles

The CommerceX architecture shall follow these principles:

1. **One service owns one cohesive business capability.**
2. **Each service owns its data.**
3. **No direct cross-service database access.**
4. **Services communicate through explicit contracts.**
5. **REST is primarily client-facing.**
6. **gRPC is used selectively for synchronous internal communication.**
7. **Kafka is used for meaningful asynchronous events.**
8. **Redis is used where caching or fast state provides a benefit.**
9. **Business logic stays within the owning service.**
10. **The API Gateway does not contain core business logic.**
11. **Important distributed operations are idempotent.**
12. **Failures should be isolated where practical.**
13. **Services should be independently deployable.**
14. **Services should be independently scalable.**
15. **Observability should cross service boundaries.**
16. **The architecture should remain simple enough for a learning project.**
17. **New technologies should only be introduced when they provide a clear learning or functional benefit.**

---

# 55. Relationship to Future Documents

The documentation sequence is:

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
05 System Architecture
        |
        v
06 Microservices Architecture      <-- This document
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

# 56. Approval / Baseline Record

| Item | Value |
|---|---|
| Project | CommerceX |
| Document | Microservices Architecture |
| Document ID | COMX-DOC-006 |
| Version | 1.0 |
| Status | Architecture Baseline |
| Previous Document | System Architecture |
| Next Document | Service Boundaries |
| Purpose | Define the 12 microservices, their responsibilities, ownership, dependencies, and communication model |

---

**End of Document**
