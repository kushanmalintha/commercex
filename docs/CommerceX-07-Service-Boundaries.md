# CommerceX — Document 07: Service Boundaries

**Document ID:** COMMERceX-07  
**Project:** CommerceX  
**Document Type:** Architecture / Design  
**Status:** Baseline  
**Version:** 1.0  
**Date:** 2026-09-12

---

## 1. Purpose

This document defines the business and technical boundaries of the twelve independently deployable backend services in CommerceX.

Its purpose is to make each service's responsibility explicit so implementation does not create overlapping ownership, shared-database dependencies, or a distributed monolith.

This document builds on Documents 01–06 and focuses specifically on **where responsibility starts and ends for each service**.

Detailed database schemas belong in Document 08. REST contracts belong in Document 09. gRPC contracts belong in Document 10. Kafka contracts belong in Document 11.

---

# 2. Boundary Principles

## 2.1 Single Business Ownership

Every important business concept has one authoritative service.

| Concept | Owner |
|---|---|
| Credentials | Auth |
| Customer profile | User |
| Product catalog | Product |
| Stock and reservations | Inventory |
| Shopping cart | Cart |
| Orders | Order |
| Payments | Payment |
| Shipments | Shipping |
| Reviews | Review |
| Notifications | Notification |
| Promotions | Promotion |
| Search read model | Search |

Other services may reference these concepts but do not become alternative owners.

## 2.2 Database Ownership

- A service may read and write its own database.
- A service must not directly query another service's database.
- Cross-service foreign keys are not allowed.
- One PostgreSQL server for local development does not mean shared ownership.
- Cross-service information is obtained through REST, gRPC, or Kafka.

## 2.3 Business Logic Ownership

Business rules belong to the service that owns the business concept.

Examples:

- Inventory decides whether stock can be reserved.
- Payment decides whether a simulated payment succeeds.
- Promotion decides whether a promotion is valid.
- Order owns the order lifecycle.
- Shipping owns shipment status transitions.

## 2.4 Reference, Do Not Duplicate Ownership

A service may store another service's identifier:

```text
Order
 ├── CustomerId
 ├── ProductId
 └── PaymentId
```

These references do not transfer ownership.

## 2.5 Historical Snapshots

Some information is intentionally copied to preserve history:

- Purchase-time item price
- Applied discount
- Shipping address used by an order
- Other order-specific historical values

These are historical snapshots, not shared ownership.

## 2.6 Synchronous vs Asynchronous Boundaries

Use synchronous communication when an immediate answer is required.

Examples:

```text
Order → Inventory
Order → Promotion
```

Use asynchronous communication when downstream work can continue independently.

Examples:

```text
Order → Kafka → Shipping
Order → Kafka → Notification
Product → Kafka → Search
```

---

# 3. Service Boundary Overview

| # | Service | Primary Responsibility | Main Owned Data |
|---|---|---|---|
| 1 | Auth | Authentication and credentials | Credentials, refresh tokens, reset tokens |
| 2 | User | Customer profile and addresses | Profiles, addresses |
| 3 | Product | Product catalog | Products, categories |
| 4 | Inventory | Stock and reservations | Inventory, reservations |
| 5 | Cart | Active shopping carts | Carts, cart items |
| 6 | Order | Orders and lifecycle | Orders, items, status history |
| 7 | Payment | Payment processing records | Payments, status |
| 8 | Shipping | Shipment fulfillment | Shipments, tracking, status |
| 9 | Review | Customer reviews | Reviews, review status |
| 10 | Notification | Notification processing | Notifications, delivery status |
| 11 | Promotion | Discounts and promotions | Promotions, rules |
| 12 | Search | Product search read model | Search documents/index data |

The API Gateway is separate and is not counted among the twelve business services.

---

# 4. Auth Service Boundary

### Responsibility

Auth owns authentication and credential-related identity operations.

> Can this user authenticate, and what authentication credentials/tokens are associated with the account?

### Owned concepts

- Credential
- RefreshToken
- PasswordResetToken
- Authentication/session state where required

### Commands

- Register account
- Login/authenticate
- Refresh access token
- Revoke refresh token
- Initiate password reset
- Complete password reset
- Change password
- Logout/revoke session

### Queries

- Authenticate credentials
- Validate authentication state
- Retrieve authentication-related identity information
- Retrieve roles required for authorization

### Communication

**Inbound:** REST through Gateway.

**Outbound:** authentication/account events through Kafka where useful.

### Belongs here

- Password hashing
- Credential validation
- Token generation and refresh
- Token revocation
- Authentication

### Does not belong here

- Customer profile
- Customer addresses
- Product catalog
- Cart
- Orders
- Payments
- Reviews

---

# 5. User Service Boundary

### Responsibility

User owns customer profile and address information.

### Owned concepts

- UserProfile
- Address

### Commands

- Create profile
- Update profile
- Delete/deactivate profile where supported
- Add address
- Update address
- Delete address
- Set default address

### Queries

- Get profile
- Get addresses
- Get a specific address
- Validate customer-owned address references

### Dependencies

User may consume `UserRegistered` from Auth.

It must not access the Auth database directly.

### Belongs here

- Name
- Contact information
- Customer profile preferences
- Saved addresses
- Address ownership

### Does not belong here

- Passwords
- Access tokens
- Refresh tokens
- Orders
- Cart contents
- Payments
- Product catalog

---

# 6. Product Service Boundary

### Responsibility

Product owns the authoritative product catalog.

> What products and categories does CommerceX sell, and what are their current catalog properties?

### Owned concepts

- Product
- Category

### Commands

- Create product
- Update product
- Activate product
- Deactivate product
- Create category
- Update category
- Deactivate category where supported

### Queries

- Get product
- List products
- Get category
- List categories
- Retrieve product details
- Determine whether a product is sellable

### Communication

**Inbound:** REST.

**Outbound:** product lifecycle events through Kafka; cache invalidation/update where applicable.

### Events

- `ProductCreated`
- `ProductUpdated`
- `ProductDeactivated`

### Belongs here

- Product name
- Description
- SKU
- Category relationship
- Current catalog price
- Product status
- Product metadata

### Does not belong here

- Stock quantity
- Stock reservations
- Shopping carts
- Orders
- Payments
- Reviews
- Search implementation

---

# 7. Inventory Service Boundary

### Responsibility

Inventory is the authoritative owner of stock availability and reservations.

> How much stock is available, and can the requested quantity be reserved?

### Owned concepts

- Inventory
- StockReservation

### Commands

- Create inventory
- Adjust stock
- Reserve stock
- Release reservation
- Confirm/reconcile reservation where required

### Queries

- Get stock
- Get available quantity
- Get reservation status
- Retrieve inventory for a product

### Communication

**Inbound:** gRPC for reservation/release operations; REST for administrative/read operations.

**Outbound:** inventory events through Kafka.

### Events

- `InventoryReserved`
- `InventoryReleased`
- `InventoryAdjusted`

### Belongs here

- Available stock
- Reserved stock
- Reservation state
- Stock adjustment rules
- Reservation idempotency

### Does not belong here

- Product descriptions
- Product prices
- Orders
- Payments
- Customer addresses
- Cart contents

---

# 8. Cart Service Boundary

### Responsibility

Cart owns the customer's current shopping cart.

> What is the customer currently intending to purchase?

### Owned concepts

- Cart
- CartItem

### Commands

- Create/get cart
- Add item
- Update quantity
- Remove item
- Clear cart

### Queries

- Get current cart
- Get cart item
- Retrieve current cart contents

### Storage

Redis is the primary storage for active carts.

Example logical key:

```text
cart:{customerId}
```

### Dependencies

Cart may call Product for validation where required.

It must not:

- Read the Product database directly.
- Read the Inventory database directly.
- Create orders as its own responsibility.
- Own authoritative product prices.

### Belongs here

- Current cart contents
- Cart item quantities
- Cart expiration policy
- Temporary cart state

### Does not belong here

- Completed orders
- Authoritative inventory
- Product catalog
- Payment records
- Purchase history

---

# 9. Promotion Service Boundary

### Responsibility

Promotion owns promotion definitions and discount rules.

> Is this promotion valid, and what discount does it provide?

### Owned concepts

- Promotion
- PromotionRule

### Commands

- Create promotion
- Update promotion
- Activate/deactivate promotion
- Configure promotion rules

### Queries

- Validate promotion
- Calculate discount
- Retrieve active promotions
- Retrieve promotion details

### Communication

**Inbound:** REST for administration; gRPC for checkout validation/calculation.

**Outbound:** promotion lifecycle events where useful.

### Belongs here

- Coupon codes
- Promotion activation
- Start/end dates
- Eligibility rules
- Discount calculation

### Does not belong here

- Final order totals
- Payments
- Product ownership
- Inventory
- Customer account ownership

Order records the final applied discount as historical order data.

---

# 10. Order Service Boundary

### Responsibility

Order owns customer orders and the order lifecycle.

> What has the customer ordered, what state is it in, and what should happen next?

### Owned concepts

- Order
- OrderItem
- OrderStatusHistory

### Commands

- Create order
- Initiate checkout
- Confirm order
- Cancel order
- Advance order lifecycle where applicable
- Record downstream outcomes
- Request required downstream operations

### Queries

- Get order
- List customer orders
- Get order items
- Get status/history

### Dependencies

Order is the main checkout orchestration point, but it does not own other services' data.

Potential synchronous dependencies include:

```text
Order → Product
Order → Inventory
Order → Promotion
Order → Payment
```

Only dependencies requiring an immediate decision should be synchronous.

### Events

- `OrderCreated`
- `OrderConfirmed`
- `OrderCancelled`
- `OrderShipped`
- `OrderDelivered`

### Historical ownership

Order owns historical purchase information such as:

- Purchase-time item price
- Ordered quantity
- Applied discount
- Order total
- Shipping address snapshot

### Belongs here

- Order lifecycle
- Order items
- Checkout orchestration
- Historical purchase information
- Status history

### Does not belong here

- Authoritative stock
- Payment implementation
- Customer credentials
- Product catalog ownership
- Shipment tracking implementation

---

# 11. Payment Service Boundary

### Responsibility

Payment owns payment attempts and payment status.

> What happened to the payment attempt for this order?

### Owned concepts

- Payment
- PaymentStatus
- Payment attempt/idempotency state

### Commands

- Initiate payment
- Process simulated payment
- Retry eligible payment
- Record payment result

### Queries

- Get payment status
- Retrieve payment by order
- Retrieve payment history where applicable

### Communication

**Inbound:** internal synchronous request from Order; controlled REST operations for administration/testing if needed.

**Outbound:** payment result events.

### Events

- `PaymentSucceeded`
- `PaymentFailed`

### Belongs here

- Payment state
- Payment attempts
- Simulated payment decision
- Payment idempotency
- Payment result

### Does not belong here

- Order lifecycle
- Product pricing
- Inventory
- Customer credentials
- Real external payment provider integration in the initial scope

---

# 12. Shipping Service Boundary

### Responsibility

Shipping owns shipment creation and delivery status.

> Where is the shipment in its fulfillment lifecycle?

### Owned concepts

- Shipment
- Tracking information
- Shipping status

### Commands

- Create shipment
- Update shipment status
- Mark in transit
- Mark out for delivery
- Mark delivered

### Queries

- Get shipment
- Get tracking information
- Get shipment status

### Communication

Shipping primarily receives fulfillment information asynchronously.

```text
OrderConfirmed
      ↓
    Kafka
      ↓
Shipping Service
```

### Events

- `ShipmentCreated`
- `ShipmentInTransit`
- `ShipmentOutForDelivery`
- `ShipmentDelivered`

### Belongs here

- Shipment record
- Tracking identifier
- Delivery status
- Shipping lifecycle

### Does not belong here

- Order ownership
- Payment processing
- Product catalog
- Inventory ownership
- Authentication

---

# 13. Review Service Boundary

### Responsibility

Review owns customer reviews and product ratings.

### Owned concepts

- Review
- ReviewStatus

### Commands

- Create review
- Update review
- Delete review
- Moderate/approve review where implemented

### Queries

- Get product reviews
- Get customer's reviews
- Get rating summary

### Dependencies

Review may need to determine whether a customer is eligible to review a product.

A review may reference:

- CustomerId
- ProductId
- OrderId

These are references, not ownership.

### Belongs here

- Review content
- Rating 1–5
- Review status
- Review ownership
- Review timestamps

### Does not belong here

- Product catalog
- Order lifecycle
- Credentials
- Inventory

---

# 14. Notification Service Boundary

### Responsibility

Notification processes notification requests generated by business events.

> What notifications should be generated, and what is their delivery status?

### Owned concepts

- Notification
- NotificationStatus
- Notification type/channel

### Commands

- Create notification
- Process notification
- Mark delivered
- Mark failed

### Queries

- Retrieve notification status
- Retrieve notification history where required

### Communication

Notification is primarily event-driven.

```text
OrderConfirmed
      ↓
    Kafka
      ↓
Notification Service
```

### Events consumed

Examples:

- `UserRegistered`
- `OrderConfirmed`
- `OrderCancelled`
- `PaymentSucceeded`
- `PaymentFailed`
- `ShipmentDelivered`

### Failure boundary

Notification failure must not invalidate the underlying business transaction.

```text
Order confirmed
      ↓
Notification fails
      ↓
Order remains confirmed
```

### Belongs here

- Notification generation
- Notification status
- Delivery simulation
- Event-to-notification mapping

### Does not belong here

- Order decisions
- Payment decisions
- Authentication
- Product management

---

# 15. Search Service Boundary

### Responsibility

Search owns the optimized read model used to search and filter products.

> Which products match this search/filter request?

### Owned concepts

- ProductSearchDocument
- Search/index metadata

### Commands

- Index product
- Update search document
- Remove/deactivate search document
- Rebuild index where required

### Queries

- Search products
- Filter products
- Sort results
- Paginate results

### Source of truth

Product Service remains authoritative.

```text
Product Service
      ↓
Product events
      ↓
    Kafka
      ↓
Search Service
      ↓
Search read model
```

### Belongs here

- Search indexing
- Search filters
- Search-specific denormalization
- Search ranking within initial scope

### Does not belong here

- Product creation/update rules
- Product catalog ownership
- Inventory ownership
- Order management

---

# 16. Cross-Service Dependency Rules

## 16.1 Contract, Not Database

Allowed:

```text
Order
  └── Inventory gRPC contract
```

Not allowed:

```text
Order
  └── Direct access to Inventory PostgreSQL
```

## 16.2 Avoid Long Synchronous Chains

Avoid:

```text
Gateway
  ↓
Order
  ↓
Inventory
  ↓
Product
  ↓
Promotion
  ↓
User
```

Long chains increase latency and failure propagation.

Use direct dependencies only when the immediate result is required, and events for downstream work.

---

# 17. Ownership Matrix

| Concept | Owner | Other Services May |
|---|---|---|
| Credentials | Auth | Reference identity |
| Refresh tokens | Auth | No direct ownership |
| Password reset tokens | Auth | No direct ownership |
| Customer profile | User | Read through contract |
| Customer addresses | User | Receive snapshots/references |
| Product | Product | Reference |
| Category | Product | Reference |
| Stock | Inventory | Request/read through contract |
| Stock reservation | Inventory | Reference reservation ID |
| Cart | Cart | Request cart operations |
| Promotion | Promotion | Validate through contract |
| Order | Order | Reference order ID |
| Order item | Order | No ownership |
| Payment | Payment | Reference payment ID/status |
| Shipment | Shipping | Reference shipment ID/status |
| Review | Review | Query review information |
| Notification | Notification | Publish triggering events |
| Search document | Search | Consume Product events |

---

# 18. Command Ownership

| Command | Owning Service |
|---|---|
| Register account | Auth |
| Login | Auth |
| Refresh token | Auth |
| Reset password | Auth |
| Update profile | User |
| Add address | User |
| Create product | Product |
| Update product | Product |
| Adjust stock | Inventory |
| Reserve stock | Inventory |
| Add cart item | Cart |
| Validate promotion | Promotion |
| Create order | Order |
| Cancel order | Order |
| Process payment | Payment |
| Create shipment | Shipping |
| Update shipment status | Shipping |
| Submit review | Review |
| Process notification | Notification |
| Index product | Search |
| Search products | Search |

---

# 19. Query Ownership

Queries should normally be answered by the service that owns the relevant data.

```text
Get product details      → Product
Get available stock     → Inventory
Get my cart             → Cart
Get my orders           → Order
Get payment status      → Payment
Get shipment tracking   → Shipping
Get product reviews     → Review
Search products         → Search
```

A service should not expose another service's database through a disguised endpoint.

---

# 20. Checkout Boundary

Checkout crosses several service boundaries.

```text
Cart
  │
  │ cart contents
  ▼
Order
  │
  ├── Product → validation/current information
  │
  ├── Promotion → validation/discount
  │
  ├── Inventory → stock reservation
  │
  └── Payment → simulated payment
         │
         ▼
       Order
         │
         ├── Kafka → Shipping
         │
         └── Kafka → Notification
```

**Boundary rule:** Order coordinates checkout but does not absorb ownership of Inventory, Promotion, Payment, Shipping, or Notification.

---

# 21. Failure Isolation Boundaries

### Notification failure

```text
Order → CONFIRMED
Notification → FAILED
```

The order remains confirmed.

### Search failure

```text
Product → remains authoritative
Search → temporarily unavailable
```

### Shipping failure

```text
Order → CONFIRMED
Shipping → temporarily unavailable
```

The event can be retried/reprocessed.

### Inventory failure

Inventory is part of the checkout decision. Order must not confirm an order when the required reservation cannot be successfully established.

---

# 22. Idempotency Boundaries

Idempotency is owned by the service responsible for the operation.

- Inventory owns reservation idempotency.
- Payment owns payment idempotency.
- Notification owns duplicate event/notification handling.
- Order owns order command/idempotency behavior.

A caller should not implement another service's internal idempotency rules.

---

# 23. Authorization Boundaries

Auth owns authentication.

Services enforce authorization for resources they own.

Examples:

```text
Customer → update own profile
Customer → update own cart
Customer → view own orders
Customer → submit own review
Admin    → create/update products
Admin    → adjust inventory
Admin    → manage promotions
```

The Gateway may perform coarse authentication/routing checks, but business authorization must not exist only in the Gateway.

---

# 24. API Gateway Boundary

The Gateway is not one of the twelve business services.

It is responsible for:

- Client entry point
- Routing
- Authentication integration
- Request forwarding
- Cross-cutting concerns
- Correlation/request metadata
- Rate limiting where introduced
- Deliberate API composition where required

It must not own:

- Product logic
- Order logic
- Inventory logic
- Payment logic
- Promotion rules
- Business state

---

# 25. Shared BuildingBlocks Boundary

Shared libraries are allowed for technical concerns:

- Logging abstractions
- OpenTelemetry setup
- Common middleware
- Error response infrastructure
- Authentication/token helpers
- Kafka infrastructure helpers
- gRPC infrastructure helpers
- Configuration utilities

They must not contain shared business-domain ownership.

Avoid:

```text
BuildingBlocks
 ├── Product.cs
 ├── Order.cs
 └── Inventory.cs
```

Prefer:

```text
BuildingBlocks
 ├── Observability
 ├── Messaging
 ├── Web
 └── Security
```

Each service owns its domain models.

---

# 26. Data Duplication Rules

Duplication is acceptable when it has a clear purpose.

### Historical snapshot

```text
Order
 └── ShippingAddressSnapshot
```

### Search projection

```text
SearchDocument
 ├── ProductId
 ├── ProductName
 ├── Category
 └── SearchableFields
```

### Cache

```text
Redis
 └── CachedProduct
```

Do not maintain competing authoritative product records.

---

# 27. Service Boundary Validation Questions

Before adding functionality, ask:

1. Which service owns this business concept?
2. Is this operation changing authoritative state?
3. If yes, which service should own that state?
4. Am I directly accessing another service's database?
5. Am I duplicating another service's business rule?
6. Could this interaction be asynchronous?
7. Does the caller actually require an immediate response?
8. Is copied data a legitimate snapshot or projection?
9. Could this create a distributed-monolith dependency chain?
10. Is this genuinely technical shared functionality, or business-domain logic?

If question 4 is yes, the design should normally be changed.

---

# 28. Boundary Anti-Patterns

## 28.1 Shared Database

```text
Order → Inventory DB
```

Not allowed.

## 28.2 Shared Domain Entity

```text
BuildingBlocks.Product
```

Not allowed as a shared business entity.

## 28.3 Business Logic in Gateway

```text
Gateway calculates order total
```

Not allowed.

## 28.4 Duplicate Ownership

```text
Product Service → Current product price
Order Service   → Historical purchase-time price
```

This is valid because Order owns the historical snapshot, while Product owns the current catalog price.

## 28.5 Excessive Synchronous Communication

```text
Order → User → Product → Inventory → Promotion → Payment
```

Avoid unnecessarily long synchronous chains.

## 28.6 Event for Every CRUD Operation

Not every CRUD update needs Kafka.

Events should represent meaningful integration/business changes.

## 28.7 Search Becoming Product Owner

Search is a derived read model. Product remains authoritative.

---

# 29. Service Definition of Done

A service boundary is well-defined when:

- Its primary business responsibility is clear.
- Its owned entities are identified.
- Its commands and queries are identified.
- Its database ownership is explicit.
- Its dependencies are documented.
- Its events are identified.
- Its authorization responsibility is clear.
- Its failure boundary is understood.
- It does not access another service's database.
- It does not duplicate another service's authoritative business logic.
- It can be built, tested, containerized, and deployed independently.

---

# 30. Final Boundary Model

```text
Auth
 └── Identity credentials/authentication

User
 └── Customer profile/address

Product
 └── Product catalog

Inventory
 └── Stock/reservations

Cart
 └── Active shopping cart

Promotion
 └── Promotions/discount rules

Order
 └── Orders/checkout lifecycle

Payment
 └── Payment processing records

Shipping
 └── Shipment/delivery lifecycle

Review
 └── Product reviews/ratings

Notification
 └── Event-driven notifications

Search
 └── Product search read model
```

The core CommerceX rule is:

> **Each service owns a clear business capability and its authoritative state, communicates through explicit contracts, and never depends on another service's database for normal operation.**

---

# 31. Relationship to Other Documents

This document defines service boundaries at the business/domain level.

Next:

- **Document 08 — Data Architecture:** database ownership, entities, relationships, PostgreSQL/Redis structures, and persistence decisions.
- **Document 09 — API Design:** REST endpoints, request/response contracts, validation, status codes, and versioning.
- **Document 10 — gRPC Design:** internal synchronous contracts and protobuf definitions.
- **Document 11 — Kafka Event Design:** topics, schemas, producers, consumers, delivery and idempotency strategy.
- **Document 12 — Redis Caching Strategy:** cache ownership, keys, TTLs, invalidation, and cart storage.
- **Document 13 — Security Design:** authentication, authorization, secrets, tokens, and service security.
- **Documents 14–19:** Docker, Kubernetes, observability, testing, CI/CD, and roadmap.

---

# 32. Baseline Decision

This document establishes the initial CommerceX service-boundary baseline.

The twelve services remain independently deployable:

1. Auth
2. User
3. Product
4. Inventory
5. Cart
6. Order
7. Payment
8. Shipping
9. Review
10. Notification
11. Promotion
12. Search

The API Gateway remains an infrastructure/edge component and is not counted as a business service.

Any future boundary change should be intentional and reflected in the architecture documentation before implementation.

**Next document:** `CommerceX-08-Data-Architecture.md`
