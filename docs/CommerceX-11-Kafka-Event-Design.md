# CommerceX — Document 11: Kafka Event Design

**Document ID:** COMMERceX-11  
**Project:** CommerceX  
**Document Type:** Asynchronous Communication / Event Architecture  
**Status:** Baseline  
**Version:** 1.0  
**Date:** 2026-09-13

---

# 1. Purpose

This document defines the Apache Kafka event architecture for CommerceX.

It establishes:

- Why Kafka is used
- Event ownership
- Topic organization
- Producers and consumers
- Initial event catalog
- Event payload conventions
- Event metadata
- Delivery semantics
- Idempotency and duplicate handling
- Retry and failure handling
- Consumer groups
- Ordering considerations
- Partitioning
- Event versioning
- Schema evolution
- Dead-letter handling
- Observability
- Local development considerations
- Kafka security considerations
- Testing strategy

This document builds on Documents 01–10.

Kafka is used for **asynchronous business-event propagation**, not as a replacement for REST, gRPC, or service-owned databases.

---

# 2. Kafka's Role in CommerceX

CommerceX uses three primary communication styles:

```text
REST
 └── Client-facing request/response APIs

gRPC
 └── Selected internal synchronous operations

Kafka
 └── Asynchronous business-event propagation
```

Kafka is particularly useful when multiple services need to react independently to a business state change.

Example:

```text
Order Service
     │
     │ OrderConfirmed
     ▼
   Kafka
   /   \
  ▼     ▼
Shipping Notification
```

Order does not need to call both services synchronously.

---

# 3. Kafka Design Goals

The Kafka architecture should provide:

1. Loose coupling between services.
2. Asynchronous processing.
3. Independent consumer scaling.
4. Reliable event delivery.
5. Duplicate-safe processing.
6. Observable event flows.
7. Evolvable event contracts.
8. Clear ownership of business events.
9. Simple local development.
10. A realistic learning environment without unnecessary event-sourcing complexity.

---

# 4. Kafka Is Not the Source of Truth

Kafka transports events.

The service that owns the business state remains authoritative.

Example:

```text
Product Service
     │
     ├── PostgreSQL → Source of truth
     │
     └── ProductUpdated
             ↓
           Kafka
             ↓
        Search Service
```

Search is a derived consumer.

Kafka does not replace the Product database.

---

# 5. Event Ownership

The service that changes a business state owns the corresponding event.

Examples:

| Event | Owner |
|---|---|
| UserRegistered | Auth |
| PasswordResetCompleted | Auth |
| ProductCreated | Product |
| ProductUpdated | Product |
| ProductDeactivated | Product |
| InventoryReserved | Inventory |
| InventoryReleased | Inventory |
| InventoryAdjusted | Inventory |
| OrderCreated | Order |
| OrderConfirmed | Order |
| OrderCancelled | Order |
| OrderShipped | Order |
| OrderDelivered | Order |
| PaymentSucceeded | Payment |
| PaymentFailed | Payment |
| ShipmentCreated | Shipping |
| ShipmentInTransit | Shipping |
| ShipmentOutForDelivery | Shipping |
| ShipmentDelivered | Shipping |

Consumers react to these events but do not redefine their meaning.

---

# 6. Initial Event Catalog

CommerceX initially uses the following business events.

## Authentication

```text
UserRegistered
PasswordResetCompleted
```

## Product

```text
ProductCreated
ProductUpdated
ProductDeactivated
```

## Inventory

```text
InventoryReserved
InventoryReleased
InventoryAdjusted
```

## Order

```text
OrderCreated
OrderConfirmed
OrderCancelled
OrderShipped
OrderDelivered
```

## Payment

```text
PaymentSucceeded
PaymentFailed
```

## Shipping

```text
ShipmentCreated
ShipmentInTransit
ShipmentOutForDelivery
ShipmentDelivered
```

The event list may evolve as implementation reveals real integration requirements.

---

# 7. Event Naming

Event names should represent completed or meaningful business facts.

Preferred:

```text
OrderConfirmed
PaymentSucceeded
ProductUpdated
InventoryReserved
```

Avoid command-style names:

```text
ConfirmOrder
ProcessPayment
UpdateProduct
ReserveInventory
```

The difference is important:

```text
Command:
"Please do this."

Event:
"This happened."
```

---

# 8. Topic Strategy

CommerceX should initially use domain-oriented topics rather than creating a topic for every single event.

Recommended initial topics:

```text
commercex.auth.events
commercex.product.events
commercex.inventory.events
commercex.order.events
commercex.payment.events
commercex.shipping.events
```

Events from a service's domain can share the corresponding topic.

For example:

```text
commercex.product.events
 ├── ProductCreated
 ├── ProductUpdated
 └── ProductDeactivated
```

This keeps the initial Kafka setup manageable.

---

# 9. Topic Ownership

| Topic | Producer |
|---|---|
| `commercex.auth.events` | Auth |
| `commercex.product.events` | Product |
| `commercex.inventory.events` | Inventory |
| `commercex.order.events` | Order |
| `commercex.payment.events` | Payment |
| `commercex.shipping.events` | Shipping |

A service should normally own the topic(s) for its domain events.

Other services consume but do not publish another service's domain events.

---

# 10. Producer and Consumer Matrix

| Event | Producer | Main Consumers |
|---|---|---|
| UserRegistered | Auth | User, Notification |
| PasswordResetCompleted | Auth | Notification where required |
| ProductCreated | Product | Search |
| ProductUpdated | Product | Search |
| ProductDeactivated | Product | Search |
| InventoryReserved | Inventory | Order |
| InventoryReleased | Inventory | Order |
| InventoryAdjusted | Inventory | Optional reporting/monitoring |
| OrderCreated | Order | Notification/other consumers if required |
| OrderConfirmed | Order | Shipping, Notification |
| OrderCancelled | Order | Inventory, Notification |
| OrderShipped | Order/fulfillment flow | Notification |
| OrderDelivered | Order/fulfillment flow | Notification |
| PaymentSucceeded | Payment | Order, Notification |
| PaymentFailed | Payment | Order, Notification |
| ShipmentCreated | Shipping | Notification |
| ShipmentInTransit | Shipping | Notification |
| ShipmentOutForDelivery | Shipping | Notification |
| ShipmentDelivered | Shipping | Order, Notification |

The exact producer/consumer relationship for order and shipment lifecycle events should remain consistent with the authoritative state owner.

---

# 11. Consumer Groups

Each independent logical consumer should use its own consumer group.

Example:

```text
commercex.notification
commercex.search
commercex.order
commercex.shipping
```

If Notification consumes:

```text
commercex.order.events
```

it should use a Notification-specific consumer group.

Shipping should use a separate group.

This allows both services to receive the event independently.

---

# 12. Consumer Group Example

For:

```text
OrderConfirmed
```

Kafka can have:

```text
commercex.order.events
        │
        ├── Consumer Group: shipping-service
        │
        └── Consumer Group: notification-service
```

Both groups independently receive the event.

If Shipping is scaled:

```text
shipping-service
 ├── Pod 1
 ├── Pod 2
 └── Pod 3
```

the pods share the same consumer group.

---

# 13. Partitioning

Kafka topics should be partitioned according to the ordering/scaling requirements of the domain.

A useful initial key strategy is:

```text
Order events → OrderId
Product events → ProductId
Inventory events → ProductId
Payment events → OrderId
Shipping events → OrderId or ShipmentId
User events → UserId
```

This provides stable ordering for events associated with the same business entity within a topic.

---

# 14. Event Ordering

Kafka guarantees ordering within a partition, not globally across all partitions.

CommerceX should therefore avoid assuming global event ordering.

For example:

```text
ProductUpdated
ProductDeactivated
```

should use a consistent ProductId key if ordering for that product matters.

Consumers should still validate event state/version where necessary.

---

# 15. Event Envelope

Events should use a consistent envelope.

Conceptual JSON:

```json
{
  "eventId": "uuid",
  "eventType": "OrderConfirmed",
  "eventVersion": 1,
  "occurredAt": "2026-09-13T12:00:00Z",
  "producer": "order-service",
  "correlationId": "uuid",
  "causationId": "uuid",
  "data": {}
}
```

The exact serialization format and schema strategy may be finalized during implementation.

---

# 16. Event Metadata

Recommended metadata:

### eventId

Unique identifier for the event.

Used for:

- Idempotency
- Logging
- Troubleshooting
- Deduplication

### eventType

Example:

```text
OrderConfirmed
```

### eventVersion

Example:

```text
1
```

### occurredAt

UTC timestamp representing when the business event occurred.

### producer

Identifies the service that produced the event.

### correlationId

Connects related operations across services.

### causationId

Identifies the event/request that directly caused this event where useful.

---

# 17. Event Payload Example

Example `OrderConfirmed`:

```json
{
  "eventId": "8a4...",
  "eventType": "OrderConfirmed",
  "eventVersion": 1,
  "occurredAt": "2026-09-13T12:00:00Z",
  "producer": "order-service",
  "correlationId": "c91...",
  "data": {
    "orderId": "ord-123",
    "customerId": "cus-456",
    "totalAmountMinor": 250000,
    "currency": "LKR"
  }
}
```

Consumers should receive only the information required for their responsibility.

---

# 18. Event Payload Design

Events should be:

- Explicit
- Stable
- Small enough to process efficiently
- Business meaningful
- Self-describing enough for consumers
- Independent from internal database schemas

Do not serialize EF Core entities directly into Kafka events.

Bad:

```text
Kafka event = Database entity dump
```

Prefer:

```text
Kafka event = Explicit integration contract
```

---

# 19. Event Data Ownership

An event may contain a value from the producing service's domain.

For example:

```text
OrderConfirmed
 ├── orderId
 ├── customerId
 ├── totalAmount
 └── currency
```

The consumer should not assume that receiving an event grants ownership of those values.

For example:

```text
Notification
 └── customerId
```

does not mean Notification owns customer information.

---

# 20. UserRegistered Flow

A typical registration flow:

```text
Client
  ↓
Auth
  ↓
Credential created
  ↓
UserRegistered
  ↓
Kafka
  ├── User Service
  └── Notification Service
```

User Service can create/initialize the corresponding customer profile.

Notification may generate a welcome notification.

Neither consumer needs direct access to Auth's database.

---

# 21. Product Synchronization Flow

```text
Product Service
      │
      ├── ProductCreated
      ├── ProductUpdated
      └── ProductDeactivated
              │
              ▼
            Kafka
              │
              ▼
        Search Service
              │
              ▼
       Search PostgreSQL
```

Search maintains a derived representation.

Product remains authoritative.

---

# 22. Order Confirmation Flow

```text
Order Service
      │
      └── OrderConfirmed
              │
              ▼
            Kafka
          /       \
         ▼         ▼
    Shipping   Notification
```

Shipping creates the shipment.

Notification generates an appropriate customer notification.

Order does not need to synchronously wait for either operation.

---

# 23. Payment Result Flow

```text
Order
  │
  └── gRPC → Payment
                │
                ├── PaymentSucceeded
                └── PaymentFailed
                        │
                        ▼
                      Kafka
                        │
                        ▼
                       Order
```

The Payment Service remains authoritative for payment status.

Order reacts to the payment result.

---

# 24. Order Cancellation Flow

An order cancellation may require inventory release.

Conceptually:

```text
Order
  │
  └── OrderCancelled
          │
          ▼
        Kafka
          │
          ▼
      Inventory
          │
          └── Release reservation
```

The exact mechanism may use a direct gRPC release operation when an immediate result is needed, or an event-driven release when asynchronous recovery is acceptable.

The chosen implementation must preserve inventory correctness.

---

# 25. Shipment Flow

Example:

```text
OrderConfirmed
      ↓
    Kafka
      ↓
Shipping
      ↓
ShipmentCreated
      ↓
Kafka
      ↓
Notification
```

Later:

```text
ShipmentInTransit
      ↓
Kafka
      ↓
Notification

ShipmentOutForDelivery
      ↓
Kafka
      ↓
Notification

ShipmentDelivered
      ↓
Kafka
   ┌──┴──────────────┐
   ▼                 ▼
Order            Notification
```

---

# 26. At-Least-Once Delivery

CommerceX should initially assume Kafka processing is **at least once**.

This means an event may be delivered more than once.

Example:

```text
Event A
  ↓
Consumer
  ↓
Processing succeeds
  ↓
Offset commit fails
  ↓
Event A delivered again
```

Consumers must therefore be idempotent.

---

# 27. Idempotent Consumers

Consumers should record or otherwise detect processed event IDs when the operation is not naturally idempotent.

Example:

```text
eventId = abc123
```

Processing:

```text
Check eventId
   │
   ├── already processed → skip
   │
   └── new event → process
                    ↓
                record eventId
```

The exact storage mechanism is service-specific.

---

# 28. Idempotency Examples

## Search

Processing:

```text
ProductUpdated
```

multiple times should result in the same search document state.

## Notification

Processing the same:

```text
OrderConfirmed
```

must not generate unlimited duplicate notifications.

## Order

Processing the same:

```text
PaymentSucceeded
```

must not confirm an order multiple times.

## Inventory

Processing a release operation must not release the same reservation multiple times.

---

# 29. Transactional Outbox Consideration

A classic problem exists when a service updates its database and publishes an event separately.

Example:

```text
1. Save OrderConfirmed to DB
2. Publish Kafka event
```

If step 1 succeeds and step 2 fails:

```text
Database = confirmed
Kafka = no event
```

CommerceX should consider the **Transactional Outbox Pattern** for important event-producing workflows.

Conceptually:

```text
Service Transaction
 ├── Business data
 └── Outbox event
        ↓
     Commit
        ↓
 Outbox Publisher
        ↓
      Kafka
```

This provides stronger reliability without requiring distributed transactions.

The initial implementation may introduce the pattern first for critical workflows rather than every service.

---

# 30. Outbox Ownership

The service that owns the business transaction owns its outbox.

Example:

```text
Order Service
 ├── orders
 ├── order_items
 └── outbox_messages
```

The outbox belongs to Order Service.

Other services never write into it.

---

# 31. Outbox Publishing Flow

```text
Order Service
      │
      ├── DB transaction
      │    ├── Update Order
      │    └── Insert Outbox Message
      │
      ▼
PostgreSQL Commit
      │
      ▼
Outbox Publisher
      │
      ▼
Kafka
```

After successful Kafka publication, the outbox record can be marked as published.

---

# 32. Retry Strategy

Kafka consumers should retry transient processing failures.

Conceptually:

```text
Consume
  ↓
Process
  ├── success → commit
  │
  └── transient failure
          ↓
        retry
```

Retries must not become infinite.

---

# 33. Dead-Letter Topics

Messages that repeatedly fail should eventually be isolated.

Example:

```text
commercex.order.events
          ↓
     Order Consumer
          ↓
       retries
          ↓
commercex.order.events.dlq
```

A dead-letter topic should contain enough metadata to investigate the failure.

Example:

```text
eventId
eventType
originalTopic
originalPartition
originalOffset
failureReason
failedAt
```

---

# 34. Poison Messages

A poison message is an event that repeatedly fails because of invalid data or an incompatible consumer.

Example:

```text
OrderConfirmed
      ↓
Consumer
      X
Invalid payload
```

The consumer should avoid blocking the entire partition indefinitely.

After the configured retry policy, move the event to the dead-letter flow.

---

# 35. Consumer Failure Isolation

One consumer's failure should not prevent unrelated consumer groups from processing the same event.

Example:

```text
OrderConfirmed
      ↓
Kafka
   ├── Shipping group → healthy
   └── Notification group → failing
```

Shipping should continue processing.

Notification can recover independently.

---

# 36. Kafka Consumer Restart

Consumers should be able to restart safely.

Example:

```text
Notification Pod
      ↓
Crash
      ↓
New Pod
      ↓
Resume from committed offset
```

Combined with idempotency, this supports reliable recovery.

---

# 37. Consumer Scaling

Kafka consumer groups allow horizontal scaling.

Example:

```text
Notification Service
 ├── Consumer Pod 1
 ├── Consumer Pod 2
 └── Consumer Pod 3
```

Partitions are distributed across the group.

The number of useful consumers is limited by the number of partitions.

---

# 38. Partition Count

The initial project should use a modest number of partitions.

For example:

```text
2–3 partitions per high-volume topic
```

This is only a starting point.

The actual number should be selected based on:

- Expected workload
- Ordering requirements
- Local resource constraints
- Learning goals

Do not create dozens of partitions unnecessarily in Minikube.

---

# 39. Replication

For a production Kafka cluster, replication protects against broker failure.

For local Minikube development, a single-broker Kafka deployment may be acceptable.

The architecture should therefore distinguish:

```text
Learning/local environment
→ simple Kafka deployment

Production-like environment
→ multiple brokers + replication
```

The initial project should prioritize understanding the concepts rather than creating an oversized local cluster.

---

# 40. Kafka Retention

Kafka topic retention should be configured based on event usefulness.

Initial learning configuration can use a manageable retention period.

Retention allows:

- Consumer recovery
- Debugging
- Reprocessing
- Temporary consumer downtime

Kafka retention is not a substitute for long-term business storage.

---

# 41. Event Replay

Some events may need replay.

Example:

```text
Search database lost
      ↓
Replay Product events
      ↓
Rebuild Search
```

This is particularly useful for derived data.

Consumers should therefore avoid destructive assumptions that make legitimate replay impossible.

---

# 42. Search Rebuild

Search is a good example of event replay.

```text
Product database
      ↓
Product events
      ↓
Kafka
      ↓
Search consumer
      ↓
Search database
```

If Search loses its database, the service can be rebuilt through controlled re-indexing.

A full production-grade event-sourced system is not required.

---

# 43. Event Versioning

Events should include a version.

Example:

```text
OrderConfirmed v1
```

A future incompatible contract can become:

```text
OrderConfirmed v2
```

Prefer backward-compatible additions whenever possible.

---

# 44. Schema Evolution

Safe changes:

```text
Add optional field
Add metadata
```

Risky changes:

```text
Rename existing field
Change field meaning
Remove required field
Change type incompatibly
```

Consumers should be deployed in a way that supports rolling upgrades.

---

# 45. Schema Registry

CommerceX includes a Schema Registry in its infrastructure.

It can be used to manage and validate event schemas.

Conceptually:

```text
Producer
   ↓
Schema
   ↓
Schema Registry
   ↓
Kafka
   ↓
Consumer
```

The exact serializer/format can be selected during implementation.

The project already reserves Schema Registry infrastructure for this purpose.

---

# 46. Event Serialization

Possible approaches include:

- JSON for simplicity
- Avro with Schema Registry
- Protobuf with Schema Registry

For the initial learning project, the selected format should balance:

- Schema evolution
- Tooling
- Debuggability
- Implementation complexity

The event contract must remain explicit regardless of serialization format.

---

# 47. Kafka Headers

Useful Kafka headers may include:

```text
eventType
eventVersion
correlationId
traceparent
```

Headers should be used for metadata, not to hide required business payload fields.

---

# 48. Correlation and Tracing

Kafka messages should carry correlation information.

Example:

```text
HTTP Request
   │
   ▼
Order
   │
   └── correlationId = abc
          │
          ▼
     OrderConfirmed
          │
          ▼
        Kafka
          │
       Shipping
          │
          └── correlationId = abc
```

This allows distributed tracing and troubleshooting.

OpenTelemetry integration is defined in Document 16.

---

# 49. Kafka Security

Kafka should be protected from arbitrary clients.

The initial production-like design should consider:

- Authentication
- Authorization
- Topic-level access
- Consumer group permissions
- Secret management
- TLS where appropriate

For local development, security may be simplified, but the service architecture should not assume unrestricted access forever.

---

# 50. Kafka Topic Access

A useful conceptual model:

```text
Order Service
 ├── WRITE → commercex.order.events
 └── READ  → payment/inventory topics where required

Shipping Service
 ├── READ  → commercex.order.events
 └── WRITE → commercex.shipping.events
```

Services should receive only the permissions they need.

---

# 51. Kafka and Database Ownership

Kafka consumers must not bypass their own service persistence layer.

Bad:

```text
Shipping Consumer
    ↓
Write directly into another service DB
```

Good:

```text
Kafka Consumer
    ↓
Shipping Application
    ↓
Shipping Domain
    ↓
Shipping PostgreSQL
```

---

# 52. Kafka Consumer Architecture in .NET

A typical service may use:

```text
Hosted Background Service
        ↓
Kafka Consumer
        ↓
Deserialize Event
        ↓
Application Handler
        ↓
Domain
        ↓
Infrastructure
        ↓
Database
```

The consumer should remain thin.

Business behavior belongs in the Application/Domain layers.

---

# 53. Consumer Error Handling

Errors should be classified.

### Permanent errors

Examples:

- Invalid event schema
- Unsupported event version
- Impossible business data

Move to DLQ after appropriate handling.

### Transient errors

Examples:

- Database temporarily unavailable
- Network timeout
- Temporary downstream failure

Retry.

---

# 54. Event Processing Transaction

Where practical:

```text
Consume event
    ↓
Begin local transaction
    ├── Apply business change
    └── Record processed event
    ↓
Commit
    ↓
Commit Kafka offset
```

This reduces duplicate processing.

The exact implementation depends on the Kafka client and persistence strategy.

---

# 55. Event Delivery Guarantees

Initial target:

```text
At-least-once delivery
+
Idempotent consumers
```

This is preferred over attempting to implement complex end-to-end exactly-once semantics throughout the entire application.

---

# 56. Exactly-Once Consideration

Kafka supports advanced exactly-once mechanisms, but CommerceX does not require end-to-end exactly-once processing initially.

Reason:

- More complexity
- More operational configuration
- Harder debugging
- Not necessary for the learning objectives

Correct idempotent business operations are more important.

---

# 57. Event-Driven vs Synchronous Decision

Use Kafka when:

```text
The consumer can process later
```

Examples:

- Create notification
- Update search index
- Create shipment after order confirmation
- Propagate lifecycle information

Use gRPC when:

```text
The caller cannot make the immediate business decision without the response
```

Examples:

- Reserve stock
- Validate promotion
- Process payment

---

# 58. Events That Should Not Be Published

Avoid events for meaningless internal changes.

For example:

```text
RepositoryMethodCalled
EntityMapped
DatabaseRowUpdated
```

These are implementation details.

Publish business facts:

```text
OrderConfirmed
PaymentSucceeded
ProductUpdated
```

---

# 59. Event Payload Anti-Patterns

Avoid:

## Database dumps

```json
{
  "allColumns": "...",
  "internalFields": "..."
}
```

## Sensitive data

Do not publish:

- Passwords
- Password hashes
- Access tokens
- Refresh tokens
- Payment credentials
- Secrets

## Excessive payloads

Do not publish entire databases or huge object graphs.

---

# 60. Event Naming and Domain Language

Events should use consistent business language.

Examples:

```text
OrderConfirmed
PaymentSucceeded
ShipmentDelivered
```

Avoid technical terminology:

```text
OrderRowUpdated
PaymentTableChanged
ShipmentEntitySaved
```

This makes contracts meaningful to both developers and system designers.

---

# 61. Event Testing Strategy

Each event should be tested for:

- Schema validity
- Required fields
- Version compatibility
- Serialization/deserialization
- Producer behavior
- Consumer behavior
- Duplicate processing
- Retry behavior
- DLQ behavior
- Ordering assumptions

---

# 62. Producer Contract Testing

A producer test should verify:

```text
Business operation
    ↓
Expected event
    ↓
Correct event type
    ↓
Correct version
    ↓
Required payload
```

Example:

```text
Confirm Order
     ↓
OrderConfirmed v1
```

---

# 63. Consumer Contract Testing

A consumer should be tested with:

```text
Valid event
Old compatible event
Duplicate event
Invalid event
Unknown event version
```

The consumer should fail safely.

---

# 64. Local Development Architecture

CommerceX local infrastructure may contain:

```text
Kafka
 ├── Broker
 ├── Schema Registry
 └── Kafka UI/tooling where desired
```

Services connect through configured endpoints.

Example:

```text
Order Service
    ↓
Kafka

Product Service
    ↓
Kafka

Notification Service
    ↓
Kafka
```

The existing infrastructure is intentionally reusable across the service implementations.

---

# 65. Kubernetes Architecture

In Minikube:

```text
                  Kafka Service
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
       Order        Product      Payment
       Service      Service      Service
          │            │            │
          └────────────┼────────────┘
                       ▼
                    Kafka
                       │
             ┌─────────┴─────────┐
             ▼                   ▼
          Shipping          Notification
```

Consumer groups allow services to scale independently.

---

# 66. Operational Metrics

Important Kafka metrics include:

- Consumer lag
- Messages produced
- Messages consumed
- Processing failures
- Retry count
- DLQ count
- Processing latency
- Partition distribution
- Broker health

Consumer lag is especially important.

Example:

```text
Produced: 10,000
Consumed: 9,200
Lag: 800
```

A growing lag may indicate that consumers cannot keep up.

---

# 67. Alerting Considerations

In a production-like environment, monitor:

- Sustained consumer lag
- Repeated consumer failures
- Increasing DLQ messages
- Kafka broker failures
- Under-replicated partitions
- High processing latency

The initial learning project can visualize these metrics in Grafana without building a complex alerting platform.

---

# 68. Kafka Resource Naming

Use a consistent naming convention.

Recommended:

```text
commercex.<domain>.events
```

Examples:

```text
commercex.auth.events
commercex.product.events
commercex.inventory.events
commercex.order.events
commercex.payment.events
commercex.shipping.events
```

Dead-letter topics:

```text
commercex.<domain>.events.dlq
```

---

# 69. Recommended Initial Topic Matrix

| Topic | Main Events | Primary Consumers |
|---|---|---|
| `commercex.auth.events` | UserRegistered, PasswordResetCompleted | User, Notification |
| `commercex.product.events` | ProductCreated, ProductUpdated, ProductDeactivated | Search |
| `commercex.inventory.events` | InventoryReserved, InventoryReleased, InventoryAdjusted | Order |
| `commercex.order.events` | OrderCreated, OrderConfirmed, OrderCancelled, OrderShipped, OrderDelivered | Inventory, Shipping, Notification |
| `commercex.payment.events` | PaymentSucceeded, PaymentFailed | Order, Notification |
| `commercex.shipping.events` | ShipmentCreated, ShipmentInTransit, ShipmentOutForDelivery, ShipmentDelivered | Order, Notification |

---

# 70. Event Flow Summary

## Account

```text
Auth
 ↓
UserRegistered
 ↓
Kafka
 ├── User
 └── Notification
```

## Catalog

```text
Product
 ↓
ProductUpdated
 ↓
Kafka
 ↓
Search
```

## Checkout

```text
Order
 ├── gRPC → Inventory
 ├── gRPC → Promotion
 └── gRPC → Payment

Payment
 ↓
PaymentSucceeded
 ↓
Kafka
 ↓
Order
 ↓
OrderConfirmed
 ↓
Kafka
 ├── Shipping
 └── Notification
```

## Fulfillment

```text
Shipping
 ↓
ShipmentDelivered
 ↓
Kafka
 ├── Order
 └── Notification
```

---

# 71. Kafka Boundary Rules

The following rules are mandatory:

1. Producers own their events.
2. Consumers never write to producer databases.
3. Consumers use independent consumer groups.
4. Events represent business facts.
5. Events are versioned.
6. Event IDs are unique.
7. Consumers are idempotent.
8. Transient failures are retried.
9. Poison messages are isolated.
10. Sensitive information is never published.
11. Kafka does not replace service-owned persistence.
12. Event payloads are explicit integration contracts.
13. The initial architecture assumes at-least-once delivery.
14. Distributed transactions are not required.
15. Important event-producing transactions should consider the Outbox Pattern.

---

# 72. Kafka Anti-Patterns

CommerceX should avoid:

## Event Everything

Do not publish every internal method call.

## Kafka as Database

Do not use Kafka as the primary long-term business database.

## Shared Consumer Database

Each consumer owns its own persistence.

## Synchronous Waiting on Kafka

Do not publish an event and immediately require the same request to wait synchronously for all consumers.

## Non-Idempotent Consumers

Assume duplicate delivery.

## Giant Events

Keep event payloads focused.

## Sensitive Events

Never publish passwords, tokens, or payment credentials.

## Uncontrolled Topic Explosion

Do not create a topic for every field or CRUD method.

---

# 73. Implementation Checklist

Before implementing an event:

- [ ] Identify the event owner.
- [ ] Define the business fact.
- [ ] Define topic.
- [ ] Define event type.
- [ ] Define event version.
- [ ] Define event ID.
- [ ] Define payload.
- [ ] Define partition key.
- [ ] Identify consumers.
- [ ] Define consumer groups.
- [ ] Define idempotency behavior.
- [ ] Define retry behavior.
- [ ] Define DLQ behavior.
- [ ] Define schema evolution rules.
- [ ] Define observability fields.
- [ ] Determine whether an outbox is required.
- [ ] Add producer tests.
- [ ] Add consumer tests.

---

# 74. Final Kafka Architecture

```text
                         CommerceX

                              Kafka
                               │
       ┌───────────────────────┼────────────────────────┐
       │                       │                        │
       ▼                       ▼                        ▼
 commercex.auth.events   commercex.product.events   commercex.order.events
       │                       │                        │
   ┌───┴───┐                   ▼                  ┌─────┼─────┐
   ▼       ▼                 Search               ▼     ▼     ▼
 User  Notification                         Inventory Shipping Notification

       commercex.inventory.events
                  │
                  ▼
                Order

       commercex.payment.events
                  │
             ┌────┴────┐
             ▼         ▼
           Order   Notification

       commercex.shipping.events
                  │
             ┌────┴────┐
             ▼         ▼
           Order   Notification
```

The architecture keeps Kafka focused on asynchronous business integration.

---

# 75. Relationship to Other Documents

This document establishes the Kafka event architecture.

Related documents:

- **Document 08 — Data Architecture:** service-owned persistence and outbox data.
- **Document 09 — API Design:** external REST contracts.
- **Document 10 — gRPC Design:** synchronous internal contracts.
- **Document 12 — Redis Caching Strategy:** Redis usage and caching.
- **Document 13 — Security Design:** Kafka authentication, authorization, and secrets.
- **Document 14 — Docker Design:** Kafka/service container configuration.
- **Document 15 — Kubernetes Design:** Kafka deployment and service discovery.
- **Document 16 — Observability Design:** Kafka metrics, tracing, and consumer lag.
- **Document 17 — Testing Strategy:** producer/consumer and event contract testing.
- **Document 18 — CI/CD Design:** schema validation and deployment.
- **Document 19 — Development Roadmap:** incremental Kafka implementation.

---

# 76. Baseline Decision

This document establishes the initial CommerceX Kafka architecture.

Key decisions:

1. Kafka is used for asynchronous business-event propagation.
2. Kafka does not replace service-owned databases.
3. Events are owned by the service that owns the corresponding business state.
4. Initial topics are domain-oriented rather than one-topic-per-event.
5. Consumers use independent consumer groups.
6. Partition keys should preserve ordering for related entities.
7. Event envelopes contain event ID, type, version, timestamp, producer, and correlation information.
8. Events use explicit integration contracts rather than database entities.
9. The initial delivery model assumes at-least-once processing.
10. Consumers must be idempotent.
11. Transient failures are retried and poison messages are isolated through DLQ handling.
12. Important event-producing workflows should consider the Transactional Outbox Pattern.
13. Sensitive information must never be published.
14. Schema Registry is part of the infrastructure and can support event contract management.
15. REST remains external, gRPC remains selected synchronous internal communication, and Kafka remains asynchronous.

**Next document:** `CommerceX-12-Redis-Caching-Strategy.md`
