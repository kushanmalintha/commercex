# CommerceX — Document 08: Data Architecture

**Document ID:** COMMERceX-08  
**Project:** CommerceX  
**Document Type:** Architecture / Data Design  
**Status:** Baseline  
**Version:** 1.0  
**Date:** 2026-09-13

---

# 1. Purpose

This document defines the data architecture for CommerceX.

It establishes:

- Data ownership by service
- PostgreSQL database boundaries
- Core entities and relationships
- Redis data ownership
- Kafka-related persistence considerations
- Data consistency rules
- Transaction boundaries
- Historical snapshots
- Indexing and query considerations
- Data lifecycle and retention
- Backup and recovery expectations
- Migration strategy
- Data architecture decisions and constraints

This document builds on Documents 01–07.

It deliberately defines the **logical data model and ownership structure**, rather than detailed REST, gRPC, or Kafka contracts.

---

# 2. Data Architecture Goals

CommerceX data architecture has the following goals:

1. Every business service owns its authoritative data.
2. No service directly accesses another service's database.
3. Each service can evolve its persistence model independently.
4. PostgreSQL is the primary durable relational database.
5. Redis is used selectively for high-speed/temporary data.
6. Kafka events are used to propagate meaningful business changes.
7. Local transactions provide strong consistency within a service.
8. Cross-service workflows use eventual consistency where appropriate.
9. Historical order information remains immutable.
10. The design remains realistic but manageable for a learning project.

---

# 3. Core Data Ownership Model

CommerceX follows a **database-per-service ownership model**.

Conceptually:

```text
Auth Service
    └── commercex_auth

User Service
    └── commercex_users

Product Service
    └── commercex_products

Inventory Service
    └── commercex_inventory

Cart Service
    └── Redis

Order Service
    └── commercex_orders

Payment Service
    └── commercex_payments

Shipping Service
    └── commercex_shipping

Review Service
    └── commercex_reviews

Notification Service
    └── commercex_notifications

Promotion Service
    └── commercex_promotions

Search Service
    └── commercex_search
```

For local development, these logical databases may exist on the same PostgreSQL server.

This is a deployment simplification, not a violation of service ownership.

---

# 4. Database Ownership Rules

The following rules are mandatory.

## 4.1 No Cross-Service Database Access

Not allowed:

```text
Order Service
      ↓
Inventory PostgreSQL
```

Allowed:

```text
Order Service
      ↓
Inventory gRPC API
```

or:

```text
Inventory Service
      ↓
Kafka
      ↓
Order Service
```

## 4.2 No Cross-Service Foreign Keys

A foreign key must not reference another service's database.

For example, Order may contain:

```text
CustomerId
ProductId
PaymentId
```

but these are identifiers, not cross-database foreign keys.

## 4.3 Service-Local Transactions

Transactions should normally cover data owned by one service.

For example:

```text
Order transaction
 ├── Order
 ├── OrderItem
 └── OrderStatusHistory
```

Inventory reservation is a separate transaction owned by Inventory.

---

# 5. PostgreSQL Architecture

PostgreSQL is the primary persistent datastore for CommerceX.

Recommended logical structure:

```text
PostgreSQL Server
│
├── commercex_auth
├── commercex_users
├── commercex_products
├── commercex_inventory
├── commercex_orders
├── commercex_payments
├── commercex_shipping
├── commercex_reviews
├── commercex_notifications
├── commercex_promotions
└── commercex_search
```

Cart uses Redis as its primary active-state store.

The exact physical deployment can evolve later.

---

# 6. Auth Database

**Database:** `commercex_auth`

## 6.1 Core Entities

### Credential

Represents authentication credentials associated with a user identity.

Typical fields:

- Id
- UserId
- Email / normalized email
- PasswordHash
- PasswordChangedAt
- CreatedAt
- UpdatedAt
- Status

### RefreshToken

Represents a refresh-token/session record.

Typical fields:

- Id
- UserId
- TokenHash
- ExpiresAt
- RevokedAt
- CreatedAt
- ReplacedByTokenId where needed

### PasswordResetToken

Represents a password-reset operation.

Typical fields:

- Id
- UserId
- TokenHash
- ExpiresAt
- UsedAt
- CreatedAt

## 6.2 Relationships

Conceptually:

```text
User Identity
     │
     ├── Credential
     ├── RefreshToken
     └── PasswordResetToken
```

The user identity itself is not owned by User Service's profile data.

Auth owns authentication-related identity state.

---

# 7. User Database

**Database:** `commercex_users`

## 7.1 Core Entities

### UserProfile

Typical fields:

- Id
- UserId / IdentityId
- FirstName
- LastName
- Email/contact data where appropriate
- CreatedAt
- UpdatedAt
- Status

### Address

Typical fields:

- Id
- UserId
- Label
- RecipientName
- AddressLine1
- AddressLine2
- City
- PostalCode
- Country
- IsDefault
- CreatedAt
- UpdatedAt

## 7.2 Relationship

```text
UserProfile
    │
    └──< Address
```

User Service owns address records.

Order Service may store an immutable address snapshot when an order is created.

---

# 8. Product Database

**Database:** `commercex_products`

## 8.1 Core Entities

### Product

Typical fields:

- Id
- SKU
- Name
- Description
- Price
- CategoryId
- Status
- CreatedAt
- UpdatedAt

### Category

Typical fields:

- Id
- Name
- Description
- Status
- CreatedAt
- UpdatedAt

## 8.2 Relationship

```text
Category
    │
    └──< Product
```

## 8.3 Ownership

Product owns the current catalog price.

Inventory does not store the authoritative product price.

Order stores the historical purchase-time price.

---

# 9. Inventory Database

**Database:** `commercex_inventory`

## 9.1 Core Entities

### Inventory

Typical fields:

- Id
- ProductId
- AvailableQuantity
- ReservedQuantity
- CreatedAt
- UpdatedAt

### StockReservation

Typical fields:

- Id
- ProductId
- OrderId
- Quantity
- Status
- ExpiresAt where applicable
- CreatedAt
- ReleasedAt / ConfirmedAt where applicable

## 9.2 Relationship

```text
Inventory
    │
    └──< StockReservation
```

The ProductId and OrderId values are references only.

## 9.3 Invariant

Inventory must never allow:

```text
AvailableQuantity < 0
```

Reservations must not exceed available stock.

---

# 10. Cart Data Architecture

Cart is intentionally different from the relational services.

**Primary store:** Redis

Example:

```text
cart:{customerId}
```

Possible logical representation:

```json
{
  "customerId": "customer-id",
  "items": [
    {
      "productId": "product-id",
      "quantity": 2
    }
  ],
  "updatedAt": "timestamp"
}
```

The exact Redis serialization format will be finalized during implementation.

## 10.1 Cart Data Characteristics

Cart data is:

- Temporary
- Frequently accessed
- Frequently modified
- Customer-specific
- Suitable for Redis

## 10.2 Product Data in Cart

Cart should primarily store identifiers and quantities.

It should not become the authoritative source for:

- Product name
- Current product price
- Inventory quantity

Those belong to Product and Inventory respectively.

## 10.3 Cart Expiration

A reasonable TTL may be introduced to prevent abandoned carts from occupying Redis indefinitely.

The exact TTL will be defined in Document 12.

---

# 11. Promotion Database

**Database:** `commercex_promotions`

## 11.1 Core Entities

### Promotion

Typical fields:

- Id
- Code
- Name
- Description
- Type
- Value
- StartAt
- EndAt
- Status
- CreatedAt
- UpdatedAt

### PromotionRule

Typical fields:

- Id
- PromotionId
- RuleType
- RuleValue
- CreatedAt
- UpdatedAt

## 11.2 Relationship

```text
Promotion
    │
    └──< PromotionRule
```

Promotion Service owns discount calculation rules.

---

# 12. Order Database

**Database:** `commercex_orders`

Order is one of the most important transactional databases.

## 12.1 Core Entities

### Order

Typical fields:

- Id
- CustomerId
- Status
- Subtotal
- DiscountAmount
- TotalAmount
- Currency
- ShippingAddressSnapshot
- CreatedAt
- UpdatedAt

### OrderItem

Typical fields:

- Id
- OrderId
- ProductId
- SKU snapshot where useful
- ProductName snapshot where useful
- UnitPrice
- Quantity
- LineTotal

### OrderStatusHistory

Typical fields:

- Id
- OrderId
- PreviousStatus
- NewStatus
- ChangedAt
- Reason

## 12.2 Relationship

```text
Order
 ├──< OrderItem
 └──< OrderStatusHistory
```

## 12.3 Historical Data

OrderItem must preserve the price used when the order was placed.

Example:

```text
Current Product Price = $120
Order Unit Price      = $100
```

The order remains $100 for historical purposes.

The Product Service may later change the current price without changing the historical order.

---

# 13. Payment Database

**Database:** `commercex_payments`

## 13.1 Core Entity

### Payment

Typical fields:

- Id
- OrderId
- Amount
- Currency
- Status
- PaymentReference
- IdempotencyKey
- AttemptedAt
- CompletedAt
- FailureReason where appropriate

## 13.2 Relationship

Conceptually:

```text
Order
  │
  └── Payment
```

The relationship is represented through identifiers rather than a cross-database foreign key.

## 13.3 Initial Scope

Payment is simulated.

The architecture intentionally does not require:

- Real payment provider credentials
- PCI-oriented card storage
- External payment gateway integration
- Storing raw card details

---

# 14. Shipping Database

**Database:** `commercex_shipping`

## 14.1 Core Entity

### Shipment

Typical fields:

- Id
- OrderId
- TrackingNumber
- Carrier
- Status
- ShippingAddressSnapshot where required
- CreatedAt
- UpdatedAt
- ShippedAt
- DeliveredAt

## 14.2 Relationship

```text
Order
  │
  └── Shipment
```

OrderId is a reference.

Shipping owns the shipment lifecycle.

---

# 15. Review Database

**Database:** `commercex_reviews`

## 15.1 Core Entity

### Review

Typical fields:

- Id
- ProductId
- CustomerId
- OrderId where eligibility requires it
- Rating
- Title
- Content
- Status
- CreatedAt
- UpdatedAt

## 15.2 Constraints

Rating must satisfy:

```text
1 ≤ Rating ≤ 5
```

A review belongs to one customer and one product.

ProductId and CustomerId are references, not cross-service foreign keys.

---

# 16. Notification Database

**Database:** `commercex_notifications`

## 16.1 Core Entity

### Notification

Typical fields:

- Id
- CustomerId
- EventId
- Type
- Channel
- Subject
- Content
- Status
- CreatedAt
- ProcessedAt
- FailureReason where applicable

## 16.2 Idempotency

EventId should be usable to prevent duplicate processing where appropriate.

Example:

```text
Kafka event
      ↓
Notification Service
      ↓
Check EventId
      ↓
Already processed? → skip
      ↓
Otherwise process
```

---

# 17. Search Database

**Database:** `commercex_search`

Search owns a derived read model.

## 17.1 Core Entity

### ProductSearchDocument

Typical fields:

- ProductId
- SKU
- Name
- Description
- CategoryId
- CategoryName
- Price
- Status
- Searchable text fields
- UpdatedAt

## 17.2 Data Flow

```text
Product Service
      │
      └── ProductCreated/Updated/Deactivated
                    │
                    ▼
                  Kafka
                    │
                    ▼
             Search Service
                    │
                    ▼
             Search Database
```

Search data is not authoritative.

If the Search database is lost, it can be rebuilt from Product data/events or a controlled re-indexing process.

---

# 18. Entity Ownership Matrix

| Entity | Owning Service | Primary Store |
|---|---|---|
| Credential | Auth | PostgreSQL |
| RefreshToken | Auth | PostgreSQL |
| PasswordResetToken | Auth | PostgreSQL |
| UserProfile | User | PostgreSQL |
| Address | User | PostgreSQL |
| Product | Product | PostgreSQL |
| Category | Product | PostgreSQL |
| Inventory | Inventory | PostgreSQL |
| StockReservation | Inventory | PostgreSQL |
| Cart | Cart | Redis |
| CartItem | Cart | Redis |
| Promotion | Promotion | PostgreSQL |
| PromotionRule | Promotion | PostgreSQL |
| Order | Order | PostgreSQL |
| OrderItem | Order | PostgreSQL |
| OrderStatusHistory | Order | PostgreSQL |
| Payment | Payment | PostgreSQL |
| Shipment | Shipping | PostgreSQL |
| Review | Review | PostgreSQL |
| Notification | Notification | PostgreSQL |
| ProductSearchDocument | Search | PostgreSQL |

---

# 19. Cross-Service References

CommerceX uses logical identifiers instead of shared relational relationships.

Example:

```text
Order
 ├── CustomerId
 ├── ProductId
 └── PaymentId
```

This means:

```text
Order.CustomerId
    → identity owned by Auth/User

Order.ProductId
    → Product-owned identifier

Order.PaymentId
    → Payment-owned identifier
```

No cross-service foreign keys are created.

---

# 20. Data Consistency Model

CommerceX uses a combination of strong local consistency and eventual consistency.

## 20.1 Strong Local Consistency

Within a service transaction:

```text
Order
 + OrderItems
 + StatusHistory
```

should be committed atomically.

Similarly:

```text
Inventory
 + Reservation
```

should be handled transactionally within Inventory.

## 20.2 Eventual Consistency

Across services:

```text
Product
   ↓
Kafka
   ↓
Search
```

Search may temporarily lag behind Product.

Likewise:

```text
OrderConfirmed
   ↓
Kafka
   ├── Shipping
   └── Notification
```

Shipping and Notification may process the event slightly later.

---

# 21. Distributed Transaction Strategy

CommerceX does not use distributed database transactions.

Avoid:

```text
Two-phase commit
Distributed SQL transaction
Cross-service database transaction
```

Instead:

- Keep local transactions small.
- Use service APIs for synchronous decisions.
- Use Kafka for asynchronous propagation.
- Use idempotency for repeated operations.
- Design workflows so partial failure can be recovered.

The checkout process is therefore a distributed workflow rather than one global database transaction.

---

# 22. Checkout Data Flow

A simplified data flow is:

```text
1. Cart
   │
   ▼
2. Order
   │
   ├── Product validation
   │
   ├── Promotion validation
   │
   ├── Inventory reservation
   │
   └── Payment processing
          │
          ▼
3. Order confirmation
          │
          ├── Kafka → Shipping
          └── Kafka → Notification
```

Each service persists only the information it owns.

---

# 23. Historical Snapshot Strategy

Historical data must not depend on mutable current data.

## 23.1 Order Item Snapshot

Store:

```text
ProductId
ProductName
SKU
UnitPrice
Quantity
```

where required.

## 23.2 Shipping Address Snapshot

An order should preserve the address used at checkout.

Example:

```text
Order
 └── ShippingAddressSnapshot
      ├── RecipientName
      ├── AddressLine1
      ├── AddressLine2
      ├── City
      ├── PostalCode
      └── Country
```

Changing the customer's saved address later must not rewrite historical orders.

---

# 24. Timestamps

Persistent entities should generally include:

- `CreatedAt`
- `UpdatedAt`

Additional lifecycle timestamps may be used where meaningful:

- `RevokedAt`
- `ExpiresAt`
- `UsedAt`
- `ConfirmedAt`
- `CancelledAt`
- `ShippedAt`
- `DeliveredAt`
- `ProcessedAt`

Timestamps should use UTC.

---

# 25. Identifier Strategy

Services should use globally unique identifiers where practical.

Recommended default:

```text
UUID / Guid
```

Advantages:

- Avoids central ID coordination.
- Works well across service boundaries.
- Makes references portable between services.
- Fits .NET and PostgreSQL naturally.

Human-readable values such as SKU or tracking number should remain separate business identifiers.

---

# 26. PostgreSQL Data Types

The implementation should use PostgreSQL-native types where appropriate.

Examples:

| Data | Suggested Type |
|---|---|
| UUID | `uuid` |
| Money | `numeric` |
| Timestamp | `timestamptz` |
| Boolean | `boolean` |
| Long text | `text` |
| Short text | `varchar` |
| Quantity | `integer` |
| Status | PostgreSQL text/string or controlled enum representation |
| JSON metadata | `jsonb` where justified |

Money should not be represented using floating-point types.

---

# 27. Nullability and Constraints

Database constraints should protect important invariants.

Examples:

- Required identifiers are `NOT NULL`.
- Quantity must be positive where applicable.
- Rating must be between 1 and 5.
- Product SKU should be unique.
- Promotion codes should be unique where required.
- Token hashes should be indexed/unique where appropriate.
- Order totals should not be nullable when an order is finalized.

Application validation and database constraints should complement each other.

---

# 28. Indexing Strategy

Indexes should be created around actual query patterns.

Initial examples:

## Auth

- Normalized email
- UserId
- Token hash
- Expiration/status fields where useful

## User

- UserId
- Address UserId

## Product

- SKU
- CategoryId
- Status
- Common catalog query fields

## Inventory

- ProductId
- Reservation OrderId
- Reservation status

## Order

- CustomerId
- CreatedAt
- Status
- OrderId

## Payment

- OrderId
- IdempotencyKey
- Status

## Shipping

- OrderId
- TrackingNumber
- Status

## Review

- ProductId
- CustomerId
- Status

## Notification

- CustomerId
- EventId
- Status

## Promotion

- Code
- Status
- StartAt/EndAt where useful

Search indexes will be designed around search/filter behavior.

Indexes should be added based on measured query needs rather than indexing every column.

---

# 29. Uniqueness Rules

Likely uniqueness constraints include:

```text
Product.SKU
Promotion.Code
Auth.Credential normalized email
Payment.IdempotencyKey
Shipping.TrackingNumber
```

Exact constraints will be finalized during schema implementation.

Business uniqueness should be enforced both at the application level and, where appropriate, at the database level.

---

# 30. Soft Delete vs Hard Delete

CommerceX should use the simplest appropriate deletion strategy.

## Use status/deactivation when history matters

Examples:

- Product
- Promotion
- User profile where required

Instead of physically deleting historical business information, use:

```text
ACTIVE
INACTIVE
```

or an equivalent state.

## Hard delete may be appropriate for

Temporary records where historical retention is unnecessary.

The implementation should avoid introducing soft-delete infrastructure globally unless a real requirement exists.

---

# 31. Data Retention

Initial retention should remain simple.

### Long-lived business records

Retain:

- Orders
- Order history
- Payment records
- Shipment records
- Reviews where applicable

### Temporary records

May expire or be cleaned:

- Password reset tokens
- Expired refresh tokens
- Old cart data
- Processed notification records where policy permits

Exact retention periods can be configured later.

---

# 32. Redis Data Architecture

Redis is not a replacement for PostgreSQL.

CommerceX initially uses Redis for:

1. Cart storage
2. Selected read caching

Potential future examples:

```text
product:{productId}
category:{categoryId}
```

The cache should always be treated as disposable.

If Redis is cleared:

```text
PostgreSQL → authoritative data
Redis      → rebuildable cache
```

Cart data is an exception because Redis is the primary store for active carts; therefore cart durability/expiration expectations must be explicit.

---

# 33. Cache Consistency

For cached Product data, a cache-aside model is recommended:

```text
Request
  │
  ▼
Redis?
 ├── Hit → return cached data
 │
 └── Miss
       │
       ▼
   Product DB
       │
       ▼
   Store cache
       │
       ▼
     Return
```

When Product data changes:

```text
Product updated
      ↓
Invalidate/update cache
      ↓
Publish ProductUpdated
```

The complete strategy is defined in Document 12.

---

# 34. Kafka and Data Synchronization

Kafka is used to propagate business changes without creating database coupling.

Example:

```text
Product DB
    ↓
ProductUpdated
    ↓
Kafka
    ↓
Search DB
```

Kafka is not itself the authoritative database for these services.

Events communicate state changes; the owning service remains the source of truth.

---

# 35. Eventual Consistency Considerations

Consumers must tolerate temporary stale data.

Example:

```text
Product updated
     ↓
Kafka delay
     ↓
Search still has old document
```

The system should not assume that every read model is updated instantly.

Consumers should support:

- Retry
- Duplicate handling
- Out-of-order considerations where required
- Reprocessing
- Rebuilding derived data

---

# 36. Data Migration Strategy

Each service owns its schema migrations.

For Entity Framework Core services:

```text
Service
  └── Infrastructure
       └── EF Core migrations
```

Example:

```text
CommerceX.Auth.Infrastructure
    └── Migrations
```

A migration must be generated and reviewed as part of that service's development.

No central migration should modify all service schemas as one business transaction.

---

# 37. Schema Evolution

Schema changes should be backward-compatible where services communicate through events or APIs.

Prefer:

1. Add new field.
2. Deploy consumers that tolerate old/new versions.
3. Start producing the new field.
4. Migrate/remove old behavior later.

Avoid breaking all consumers simultaneously.

Kafka event evolution will be defined more precisely in Document 11.

---

# 38. Backup and Recovery

For local development:

- PostgreSQL volumes should be persistent.
- Important databases should be exportable using PostgreSQL backup tools.
- Redis should be considered recoverable/replaceable according to its role.

For a more production-like deployment:

- Scheduled PostgreSQL backups
- Backup verification
- Restore testing
- Persistent volumes
- Defined recovery objectives

The learning project should not initially introduce complex distributed backup infrastructure.

---

# 39. Data Security

Sensitive data requires additional protection.

## Auth

Never store:

- Plain-text passwords
- Plain-text refresh tokens where hashing is practical
- Plain-text password-reset tokens where hashing is practical

## Payment

Do not store raw:

- Card numbers
- CVV
- Full payment credentials

The initial payment system is simulated.

## General

- Use least-privilege database users.
- Protect connection strings.
- Keep secrets outside source control.
- Avoid logging credentials, tokens, or sensitive personal data.

---

# 40. Data Access Architecture

Each .NET service should generally use:

```text
API
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
 ↓
Database
```

The Infrastructure layer owns persistence implementation.

For Entity Framework Core:

```text
Application
 └── Repository abstraction

Infrastructure
 └── EF Core implementation
      └── DbContext
```

The domain layer must not depend on PostgreSQL or EF Core.

---

# 41. Service Data Flow Examples

## Product

```text
Client
  ↓
Gateway
  ↓
Product API
  ↓
Application
  ↓
Domain
  ↓
PostgreSQL
```

Then:

```text
Product change
  ↓
Kafka
  ├── Search
  └── other interested consumers
```

## Order

```text
Client
  ↓
Gateway
  ↓
Order
  ├── Product
  ├── Promotion
  ├── Inventory
  └── Payment
       ↓
PostgreSQL
```

Then:

```text
OrderConfirmed
      ↓
    Kafka
   ├── Shipping
   └── Notification
```

---

# 42. Data Architecture Anti-Patterns

CommerceX must avoid:

## Shared business database

```text
All services → commercex
```

Not allowed.

## Cross-service joins

```sql
SELECT ...
FROM orders
JOIN inventory ...
```

Not allowed across service boundaries.

## Shared EF DbContext

A single DbContext spanning multiple business services is not allowed.

## Shared domain entities

Do not put business entities into BuildingBlocks.

## Cache as authoritative source

Except where explicitly designed, Redis should not become the source of truth.

## Uncontrolled denormalization

Copies of data should have a clear purpose:

- Cache
- Search projection
- Historical snapshot
- Read model

---

# 43. Data Architecture Decision Matrix

| Decision | Choice | Reason |
|---|---|---|
| Primary relational DB | PostgreSQL | Strong relational support and project requirement |
| ORM | EF Core | Native .NET integration and learning value |
| Database ownership | Database per service | Independent service boundaries |
| Local DB deployment | One PostgreSQL server, separate logical DBs | Simple local development |
| Cart store | Redis | High-frequency temporary state |
| Search store | PostgreSQL initially | Avoid unnecessary search infrastructure |
| Async propagation | Kafka | Event-driven integration |
| Cross-service joins | Not allowed | Preserve service independence |
| Distributed transactions | Not used | Reduce complexity |
| IDs | UUID/Guid | Distributed-friendly identifiers |
| Money | Numeric/decimal | Avoid floating-point errors |
| Timestamps | UTC | Consistent distributed timestamps |
| Historical order data | Immutable snapshots | Preserve purchase history |

---

# 44. Data Architecture and Service Boundaries

The data model directly reinforces Document 07.

```text
Auth       → Authentication data
User       → Customer data
Product    → Catalog data
Inventory  → Stock data
Cart       → Active cart data
Promotion  → Promotion data
Order      → Order data
Payment    → Payment data
Shipping   → Shipment data
Review     → Review data
Notification → Notification data
Search     → Derived search data
```

No service should become a general-purpose data repository for other services.

---

# 45. Implementation Guidance

Implementation should proceed incrementally.

Recommended order:

1. Auth database and EF Core model
2. User database
3. Product database
4. Promotion database
5. Cart Redis model
6. Inventory database
7. Order database
8. Payment database
9. Shipping database
10. Notification database
11. Review database
12. Search database

For each service:

```text
Define entities
    ↓
Define configurations
    ↓
Create DbContext
    ↓
Create migration
    ↓
Review migration
    ↓
Apply migration
    ↓
Implement repositories
    ↓
Test persistence
```

Do not implement every database at once.

---

# 46. Data Architecture Validation Checklist

Before considering a service's data model complete:

- [ ] All entities have an identified owner.
- [ ] No cross-service database access exists.
- [ ] No cross-service foreign keys exist.
- [ ] Local transactions are clearly defined.
- [ ] Required constraints are identified.
- [ ] Important indexes are identified.
- [ ] Historical snapshots are explicit.
- [ ] Redis usage is justified.
- [ ] Derived data is clearly marked as non-authoritative.
- [ ] Sensitive data handling is defined.
- [ ] Migration ownership is clear.
- [ ] Data retention requirements are understood.
- [ ] Backup/recovery expectations are documented.
- [ ] The model remains simple enough for the learning project.

---

# 47. Final Data Ownership Model

```text
                    CommerceX Data Architecture

                           ┌───────────┐
                           │  Gateway  │
                           └─────┬─────┘
                                 │
        ┌────────────────────────┼────────────────────────┐
        │                        │                        │
        ▼                        ▼                        ▼
   Auth / User              Product / Search          Order Flow
        │                        │                        │
        ▼                        ▼                        ▼
   PostgreSQL               PostgreSQL                 PostgreSQL
                                                        │
                         ┌──────────────────────────────┼─────────────┐
                         │              │               │             │
                         ▼              ▼               ▼             ▼
                    Inventory       Promotion        Payment      Shipping
                    PostgreSQL      PostgreSQL       PostgreSQL   PostgreSQL

                         Cart
                          │
                          ▼
                         Redis

                  Review / Notification
                          │
                          ▼
                      PostgreSQL
```

The architecture preserves a simple but meaningful rule:

> **Durable business data is owned by the service responsible for that business capability; derived, cached, or historical copies exist only for an explicit purpose.**

---

# 48. Relationship to Next Documents

This document establishes the logical data architecture.

The next documents refine different technical contracts:

- **Document 09 — API Design:** external REST API structure and resource contracts.
- **Document 10 — gRPC Design:** internal synchronous contracts.
- **Document 11 — Kafka Event Design:** event topics, schemas, producers, consumers, and delivery semantics.
- **Document 12 — Redis Caching Strategy:** cache keys, TTLs, cache-aside behavior, invalidation, and cart storage.
- **Document 13 — Security Design:** protection of data, identities, tokens, secrets, and service communication.
- **Document 14 — Docker Design:** container-level persistence/configuration considerations.
- **Document 15 — Kubernetes Design:** persistent volumes, database deployment, configuration, and service discovery.
- **Document 16 — Observability Design:** database, Redis, and messaging telemetry.
- **Document 17 — Testing Strategy:** persistence and integration testing.
- **Document 18 — CI/CD Design:** migration/deployment considerations.
- **Document 19 — Development Roadmap:** implementation sequencing.

---

# 49. Baseline Decision

This document establishes the initial CommerceX data architecture baseline.

The key decisions are:

1. PostgreSQL is the primary relational datastore.
2. Each business service owns its own logical database.
3. One PostgreSQL server may host multiple service databases locally.
4. Cart uses Redis as its primary active-state store.
5. Search uses a PostgreSQL-derived read model initially.
6. Services never directly access another service's database.
7. Cross-service relationships use identifiers and contracts, not foreign keys.
8. Local transactions provide strong consistency.
9. Kafka supports eventual consistency across service boundaries.
10. Order data preserves historical snapshots.
11. EF Core migrations are owned by individual services.
12. The architecture intentionally avoids distributed database transactions.

**Next document:** `CommerceX-09-API-Design.md`
