# CommerceX — Document 10: gRPC Design

**Document ID:** COMMERceX-10  
**Project:** CommerceX  
**Document Type:** Internal Communication / gRPC Design  
**Status:** Baseline  
**Version:** 1.0  
**Date:** 2026-09-13

---

# 1. Purpose

This document defines how CommerceX uses gRPC for selected internal synchronous communication.

The goal is not to replace REST or Kafka with gRPC. Instead, gRPC is used where a service needs a fast, strongly typed, request/response interaction with another service and an immediate result is required.

This document establishes:

- gRPC usage principles
- Internal service boundaries
- gRPC ownership
- Client/server responsibilities
- Initial gRPC contracts
- Protobuf conventions
- Error handling
- Deadlines and timeouts
- Authentication between services
- Idempotency
- Versioning
- Failure handling
- Observability
- Testing
- Implementation structure

Detailed REST contracts belong to Document 09. Kafka event contracts belong to Document 11.

---

# 2. gRPC Role in CommerceX

CommerceX uses three communication styles:

```text
REST
 └── Client-facing APIs

gRPC
 └── Selected internal synchronous operations

Kafka
 └── Asynchronous business events
```

The distinction is important.

Use REST when:

```text
Client → Gateway → Service
```

Use gRPC when:

```text
Service A → Service B
```

and Service A needs an immediate response.

Use Kafka when:

```text
Service A
   ↓
Business event
   ↓
Kafka
   ↓
One or more consumers
```

---

# 3. Why gRPC Is Used

gRPC provides several benefits for internal service communication:

- Strongly typed contracts
- Protobuf-based serialization
- Efficient binary communication
- Code generation for .NET clients/servers
- Explicit service methods
- Good support for deadlines and cancellation
- Natural fit for internal synchronous calls

CommerceX uses gRPC selectively to demonstrate these capabilities without turning the system into a collection of tightly coupled RPC services.

---

# 4. gRPC Design Principles

## 4.1 Internal Only by Default

gRPC endpoints are primarily internal service contracts.

External clients should normally use REST through the API Gateway.

## 4.2 Small Contracts

A gRPC service should expose focused business operations.

Good:

```text
ReserveStock
ReleaseStock
ValidatePromotion
ProcessPayment
```

Avoid exposing every repository operation as an RPC.

## 4.3 Business Capability, Not CRUD

Prefer:

```protobuf
rpc ReserveStock(...)
```

over:

```protobuf
rpc UpdateInventory(...)
```

when the operation represents a meaningful business capability.

## 4.4 Immediate Decisions Only

Use gRPC when the caller needs the result immediately.

If the caller can continue without waiting, Kafka is generally more appropriate.

## 4.5 No Database Coupling

gRPC clients communicate with service contracts.

They never use gRPC as a disguised mechanism for exposing another service's database.

---

# 5. Initial gRPC Service Map

The initial CommerceX design uses gRPC for these primary internal interactions:

```text
Order
 ├──→ Inventory : reserve/release stock
 ├──→ Promotion : validate/calculate promotion
 └──→ Payment   : process payment

```

Potential read/validation calls may be added where justified.

The initial design intentionally avoids introducing gRPC between every service.

---

# 6. gRPC Ownership Matrix

| gRPC Server | Primary Client | Purpose |
|---|---|---|
| Inventory | Order | Reserve/release stock |
| Promotion | Order | Validate/calculate promotion |
| Payment | Order | Process simulated payment |

The server owns the contract and business decision.

The client only requests the operation.

---

# 7. Inventory gRPC Boundary

## 7.1 Responsibility

Inventory Service exposes gRPC operations for stock reservations required during checkout.

The key business question is:

> Can the requested quantity be reserved for this order?

## 7.2 Initial Operations

```text
ReserveStock
ReleaseStock
```

Potential future operation:

```text
GetReservation
```

only if an immediate internal query is required.

---

# 8. ReserveStock Contract

Conceptual request:

```protobuf
message ReserveStockRequest {
  string order_id = 1;
  repeated StockReservationItem items = 2;
  string idempotency_key = 3;
}
```

Item:

```protobuf
message StockReservationItem {
  string product_id = 1;
  int32 quantity = 2;
}
```

Response:

```protobuf
message ReserveStockResponse {
  bool success = 1;
  string reservation_id = 2;
  string failure_code = 3;
  string failure_message = 4;
}
```

The exact protobuf schema may evolve during implementation.

---

# 9. Inventory Reservation Semantics

The Inventory Service is authoritative.

The caller does not decide whether stock is available.

Example:

```text
Order
  │
  │ ReserveStock
  ▼
Inventory
  │
  ├── sufficient stock → success
  │
  └── insufficient stock → failure
```

Inventory must atomically protect its stock invariant.

---

# 10. Inventory Idempotency

Reservation requests may be retried.

Therefore, Inventory should support an idempotency key.

Example:

```text
IdempotencyKey:
order-123-reservation
```

If the same request is received again:

```text
First request
  → Reservation created

Retry
  → Existing reservation returned
```

It must not create a second reservation.

---

# 11. ReleaseStock Contract

Conceptual request:

```protobuf
message ReleaseStockRequest {
  string reservation_id = 1;
  string order_id = 2;
  string idempotency_key = 3;
}
```

Response:

```protobuf
message ReleaseStockResponse {
  bool success = 1;
  string failure_code = 2;
  string failure_message = 3;
}
```

Release should be safe to retry.

---

# 12. Inventory gRPC Business Rules

Inventory must enforce:

- Product ID must be valid in the inventory domain.
- Quantity must be greater than zero.
- Reservation cannot exceed available stock.
- Reservation belongs to the requested order.
- A completed/released reservation cannot be reserved again accidentally.
- Duplicate requests must be idempotent.
- Stock must never become negative.

---

# 13. Promotion gRPC Boundary

## 13.1 Responsibility

Promotion Service exposes a synchronous validation/calculation capability for checkout.

The key question is:

> Is this promotion valid for the requested checkout, and what discount should be applied?

## 13.2 Initial Operation

```text
ValidatePromotion
```

The operation may return both validation and calculated discount information.

---

# 14. ValidatePromotion Contract

Conceptual request:

```protobuf
message ValidatePromotionRequest {
  string customer_id = 1;
  string promotion_code = 2;
  repeated PromotionItem items = 3;
  string currency = 4;
}
```

Item:

```protobuf
message PromotionItem {
  string product_id = 1;
  int32 quantity = 2;
  double unit_price = 3;
}
```

Response:

```protobuf
message ValidatePromotionResponse {
  bool valid = 1;
  string promotion_id = 2;
  double discount_amount = 3;
  string failure_code = 4;
  string failure_message = 5;
}
```

**Implementation note:** financial values should not use floating-point types in the final contract. A precise monetary representation should be selected when the `.proto` contract is finalized.

---

# 15. Promotion Business Rules

Promotion Service decides:

- Whether the promotion exists.
- Whether it is active.
- Whether it has expired.
- Whether eligibility rules are satisfied.
- What discount applies.

Order should not reproduce these rules.

Order only uses the result.

---

# 16. Payment gRPC Boundary

## 16.1 Responsibility

Payment Service exposes an internal operation for simulated payment processing.

The key question is:

> Can this payment attempt be accepted successfully?

## 16.2 Initial Operation

```text
ProcessPayment
```

Potential future operation:

```text
GetPaymentStatus
```

only when an immediate status query is needed.

---

# 17. ProcessPayment Contract

Conceptual request:

```protobuf
message ProcessPaymentRequest {
  string order_id = 1;
  string customer_id = 2;
  int64 amount_minor = 3;
  string currency = 4;
  string idempotency_key = 5;
}
```

Using minor currency units avoids floating-point money representation.

Example:

```text
LKR 2,500.00
→ 250000 minor units
```

Response:

```protobuf
message ProcessPaymentResponse {
  bool success = 1;
  string payment_id = 2;
  string failure_code = 3;
  string failure_message = 4;
}
```

---

# 18. Payment Idempotency

Payment processing must be idempotent.

Example:

```text
IdempotencyKey = order-123-payment
```

If the request is retried:

```text
First request
  → Payment succeeds

Retry
  → Existing payment result returned
```

The system must not create duplicate successful payments for one logical payment operation.

---

# 19. Payment Simulation

The initial Payment Service does not integrate with a real payment provider.

A simple deterministic simulation may be used.

For example:

```text
Payment request
     ↓
Simulation rule
     ├── success
     └── failure
```

The simulation mechanism is an implementation detail and must not leak real-provider assumptions into the API contract.

---

# 20. Order's gRPC Client Role

Order Service acts primarily as the gRPC client during checkout.

Conceptually:

```text
Order
 │
 ├── InventoryClient
 │
 ├── PromotionClient
 │
 └── PaymentClient
```

These clients should be represented through application-level abstractions where appropriate.

For example:

```text
IInventoryClient
IPromotionClient
IPaymentClient
```

The application layer depends on contracts rather than directly embedding gRPC implementation details.

---

# 21. Recommended .NET Structure

A service using gRPC may use:

```text
CommerceX.Order
├── API
├── Application
│   └── Abstractions
│       └── Clients
├── Domain
└── Infrastructure
    └── Grpc
```

The generated protobuf client can remain an Infrastructure concern.

Example:

```text
Application
   ↓
IInventoryClient
   ↓
Infrastructure
   ↓
Generated Inventory gRPC client
```

This keeps transport concerns separate from business logic.

---

# 22. gRPC Server Structure

A gRPC server should map requests to application use cases.

Conceptually:

```text
gRPC Service
     ↓
Application Handler
     ↓
Domain
     ↓
Infrastructure
     ↓
Database
```

Avoid putting large amounts of business logic directly inside the gRPC service implementation.

---

# 23. Protobuf Package Naming

A consistent package structure is recommended.

Example:

```protobuf
syntax = "proto3";

package commercex.inventory.v1;
```

Other services:

```text
commercex.promotion.v1
commercex.payment.v1
```

This creates a clear namespace and supports future contract evolution.

---

# 24. Protobuf File Organization

Recommended structure:

```text
contracts/
└── proto/
    ├── inventory/
    │   └── v1/
    │       └── inventory.proto
    ├── promotion/
    │   └── v1/
    │       └── promotion.proto
    └── payment/
        └── v1/
            └── payment.proto
```

The exact repository organization may vary, but contracts should be clearly separated from service implementation.

---

# 25. Field Naming

Protobuf fields should use `snake_case`.

Example:

```protobuf
string order_id = 1;
string product_id = 2;
int32 quantity = 3;
```

Generated .NET properties will follow appropriate C# naming conventions.

---

# 26. Identifier Representation

UUID/GUID values can initially be represented as strings in protobuf.

Example:

```protobuf
string product_id = 1;
```

The application validates the UUID format.

A dedicated UUID wrapper can be introduced later if the project requires it.

---

# 27. Monetary Values

Avoid:

```protobuf
double amount
```

for financial values.

Preferred initial approach:

```protobuf
int64 amount_minor = 1;
string currency = 2;
```

Example:

```text
2500.00 LKR
→ amount_minor = 250000
currency = "LKR"
```

This avoids floating-point precision issues.

---

# 28. Enumerations

Enums should be used for stable states.

Example:

```protobuf
enum PaymentStatus {
  PAYMENT_STATUS_UNSPECIFIED = 0;
  PAYMENT_STATUS_PENDING = 1;
  PAYMENT_STATUS_SUCCEEDED = 2;
  PAYMENT_STATUS_FAILED = 3;
}
```

Using an explicit `UNSPECIFIED = 0` value is recommended.

---

# 29. gRPC Error Model

gRPC should use standard gRPC status codes.

| Code | Example |
|---|---|
| `OK` | Successful operation |
| `INVALID_ARGUMENT` | Invalid quantity |
| `UNAUTHENTICATED` | Missing/invalid service authentication |
| `PERMISSION_DENIED` | Not authorized |
| `NOT_FOUND` | Resource not found |
| `ALREADY_EXISTS` | Duplicate resource where applicable |
| `FAILED_PRECONDITION` | Invalid business state |
| `ABORTED` | Concurrency/state conflict |
| `DEADLINE_EXCEEDED` | Operation exceeded deadline |
| `UNAVAILABLE` | Dependency temporarily unavailable |
| `INTERNAL` | Unexpected server error |

Do not use `INTERNAL` for normal business failures.

Example:

```text
Insufficient stock
→ FAILED_PRECONDITION
```

rather than:

```text
→ INTERNAL
```

---

# 30. Business Error Details

Where useful, the service can provide structured error information.

Example conceptual result:

```text
FAILED_PRECONDITION
code = "INSUFFICIENT_STOCK"
message = "Requested quantity is unavailable"
```

Clients should use stable failure codes rather than parsing human-readable messages.

---

# 31. Deadlines

Every internal gRPC call should have a deadline.

Example conceptual flow:

```text
Order
  │
  └── Inventory
       deadline = configured timeout
```

Never allow an internal call to wait indefinitely.

Timeouts should be configuration-driven.

---

# 32. Cancellation

Cancellation tokens should propagate through the .NET call chain.

Conceptually:

```text
HTTP Request
    ↓
Order
    ↓
gRPC call
    ↓
Inventory
```

If the originating request is cancelled, downstream operations should stop when safe.

ASP.NET Core request cancellation tokens should be passed into application and gRPC calls.

---

# 33. Retry Policy

gRPC retries should be used carefully.

Potentially retryable:

- `UNAVAILABLE`
- transient network failures

Usually not retryable without explicit idempotency:

- `INVALID_ARGUMENT`
- `FAILED_PRECONDITION`

Payment and inventory operations must have idempotency protection before automatic retry is introduced.

---

# 34. Authentication Between Services

Internal gRPC communication should not assume that being inside the Kubernetes cluster is sufficient authorization.

The initial implementation can use service-to-service authentication based on controlled credentials/tokens.

The design should support:

```text
Order → authenticated gRPC → Inventory
```

rather than:

```text
Order → unauthenticated internal endpoint
```

The complete service security model is defined in Document 13.

---

# 35. TLS Considerations

For local development, plaintext gRPC may be used inside a controlled development environment if necessary.

For production-like deployment:

```text
gRPC over TLS
```

should be preferred.

mTLS/service mesh is intentionally not required for the initial learning scope.

---

# 36. Service Discovery

In Kubernetes, gRPC clients should connect using Kubernetes service DNS names.

Conceptual example:

```text
inventory-service.commercex.svc.cluster.local
```

The actual service naming convention will be finalized in Document 15.

Local development may use:

```text
localhost:<port>
```

or Docker/Kubernetes service names depending on the environment.

---

# 37. Load Balancing

A gRPC client may communicate with multiple replicas of a service.

Example:

```text
Order
 │
 ├── Inventory Pod 1
 ├── Inventory Pod 2
 └── Inventory Pod 3
```

Kubernetes Service provides the stable network endpoint.

Application code should not hard-code individual pod addresses.

---

# 38. gRPC Health Checking

gRPC services should support health checking where appropriate.

This can integrate with:

- Kubernetes readiness
- Kubernetes liveness
- Operational diagnostics

The service should distinguish:

```text
Process alive
```

from:

```text
Ready to handle requests
```

---

# 39. gRPC Observability

gRPC calls should be observable.

Capture:

- Trace ID
- Span ID
- Caller service
- Target service
- RPC method
- Duration
- Status
- Error category

Example trace:

```text
HTTP POST /orders
       │
       ▼
Order.CreateOrder
       │
       ├── Inventory.ReserveStock
       │
       ├── Promotion.ValidatePromotion
       │
       └── Payment.ProcessPayment
```

OpenTelemetry should connect these spans into a distributed trace.

---

# 40. gRPC Logging

Do not log sensitive request contents.

Avoid logging:

```text
Authorization tokens
Payment credentials
Passwords
Sensitive personal data
```

Useful fields include:

```text
traceId
rpcMethod
targetService
duration
status
failureCode
```

---

# 41. gRPC Contract Versioning

Initial contracts use:

```text
v1
```

Example:

```protobuf
package commercex.inventory.v1;
```

Breaking changes should result in a new contract version.

Example:

```text
commercex.inventory.v1
commercex.inventory.v2
```

Prefer additive, backward-compatible changes when possible.

---

# 42. Contract Evolution

Safe example:

```protobuf
message ReserveStockRequest {
  string order_id = 1;
  repeated StockReservationItem items = 2;
  string idempotency_key = 3;
  string correlation_id = 4; // new optional field
}
```

Avoid reusing field numbers after removing fields.

Do not silently change the meaning of an existing field.

---

# 43. gRPC and Kafka Boundary

gRPC and Kafka have different purposes.

Example:

```text
Order
 │
 ├── gRPC → Inventory
 │           "Can I reserve this?"
 │
 └── Kafka → Shipping
             "Order was confirmed."
```

Do not use Kafka when an immediate reservation decision is required.

Do not use gRPC when a downstream consumer can process independently.

---

# 44. Checkout Communication Model

The initial checkout communication can be represented as:

```text
Client
  │
  ▼
Gateway
  │
  ▼
Order
  │
  ├── gRPC → Product/validation where required
  │
  ├── gRPC → Promotion
  │
  ├── gRPC → Inventory
  │
  └── gRPC → Payment
  │
  ▼
Order Confirmation
  │
  ├── Kafka → Shipping
  └── Kafka → Notification
```

The exact sequence and failure compensation are implementation concerns, but the communication roles remain distinct.

---

# 45. Failure Scenarios

## 45.1 Inventory unavailable

```text
Order → Inventory
         X
     unavailable
```

Result:

```text
Order cannot be confirmed
```

The API should return a controlled service/dependency error.

## 45.2 Insufficient stock

```text
Inventory → FAILED_PRECONDITION
```

Order should not confirm.

## 45.3 Promotion invalid

```text
Promotion → invalid
```

Order should reject the promotion or follow the defined checkout rule.

## 45.4 Payment failure

```text
Payment → FAILED
```

Order should not become `CONFIRMED`.

## 45.5 Timeout

```text
Order → gRPC
       ↓
DEADLINE_EXCEEDED
```

The operation should be handled without leaving an uncontrolled state.

---

# 46. Partial Failure Considerations

A gRPC call can succeed at the server while the client experiences a network failure.

Example:

```text
Order → Payment
         │
         ├── payment succeeded
         │
         X response lost
```

The client may retry.

Therefore, payment idempotency is mandatory.

The same principle applies to inventory reservation.

---

# 47. gRPC Contract Testing

Each gRPC contract should be tested independently.

Tests should cover:

- Valid request
- Invalid request
- Authentication failure
- Authorization failure
- Resource not found
- Business rule failure
- Successful operation
- Duplicate request
- Timeout
- Cancellation
- Service unavailable

---

# 48. Integration Testing

Example Inventory test:

```text
Order test client
      ↓
gRPC
      ↓
Inventory
      ↓
Test PostgreSQL
```

Verify:

- Reservation created
- Available stock updated correctly
- Duplicate reservation does not create duplicate state
- Release works
- Invalid quantities fail

---

# 49. Local Development

A local environment may expose gRPC ports separately.

Example conceptual layout:

```text
Inventory HTTP   → 5101
Inventory gRPC   → 6101

Promotion HTTP   → 5102
Promotion gRPC   → 6102

Payment HTTP     → 5103
Payment gRPC     → 6103
```

These are examples only; actual ports should be standardized during implementation.

---

# 50. Docker Considerations

Each gRPC-enabled service should expose its gRPC endpoint inside its container.

Conceptually:

```text
Container
 ├── HTTP endpoint
 └── gRPC endpoint
```

The container should not require a fixed external host port for service-to-service communication.

Docker/Kubernetes service discovery should provide the stable endpoint.

---

# 51. Kubernetes Considerations

Kubernetes Services should provide stable endpoints for gRPC-enabled services.

Example:

```text
order-service
inventory-service
promotion-service
payment-service
```

Each Kubernetes Service can route traffic to multiple replicas.

Readiness probes should prevent traffic from reaching unready pods.

---

# 52. Security Boundary

The gRPC server must assume the caller may be untrusted.

It should validate:

- Caller identity
- Authentication
- Authorization
- Request fields
- Resource ownership where applicable
- Business invariants

Do not rely exclusively on Order to send valid inventory quantities.

Inventory remains authoritative.

---

# 53. gRPC Anti-Patterns

CommerceX should avoid:

## gRPC Everywhere

Not every service interaction requires gRPC.

## CRUD RPCs

Avoid:

```text
CreateProduct
UpdateProduct
DeleteProduct
GetProduct
```

unless there is a genuine internal contract requirement.

## Database RPC

Do not expose:

```text
ExecuteSql
QueryInventoryTable
```

## Large Chatty Calls

Avoid many sequential calls when one meaningful business operation can return the required result.

## Business Logic in Client

Order should not calculate inventory rules.

Inventory owns them.

## Infinite Retries

Never retry indefinitely.

## Unversioned Contracts

Do not expose permanent unversioned protobuf packages.

---

# 54. gRPC Implementation Checklist

For each gRPC service:

- [ ] Define business capability.
- [ ] Define request message.
- [ ] Define response message.
- [ ] Define stable error codes.
- [ ] Define authentication.
- [ ] Define authorization.
- [ ] Define timeout/deadline.
- [ ] Define retry behavior.
- [ ] Define idempotency behavior.
- [ ] Define observability.
- [ ] Define protobuf version.
- [ ] Add server implementation.
- [ ] Add generated client.
- [ ] Add integration tests.
- [ ] Verify Kubernetes connectivity.

---

# 55. Initial gRPC Contract Summary

The initial CommerceX gRPC surface remains intentionally small:

```text
Inventory
 ├── ReserveStock
 └── ReleaseStock

Promotion
 └── ValidatePromotion

Payment
 └── ProcessPayment
```

Order acts as the primary client.

The contracts can be expanded only when a real synchronous business requirement appears.

---

# 56. REST vs gRPC vs Kafka Decision Table

| Requirement | Technology |
|---|---|
| Browser/mobile/client API | REST |
| Public product API | REST |
| Public order API | REST |
| Stock reservation decision | gRPC |
| Promotion validation during checkout | gRPC |
| Simulated payment processing | gRPC |
| Order confirmed notification | Kafka |
| Product update to Search | Kafka |
| Order confirmed to Shipping | Kafka |
| Customer registration event | Kafka |
| Long-running/asynchronous work | Kafka |

---

# 57. Final Architecture

```text
                       CommerceX

                         Client
                           │
                           ▼
                     API Gateway
                           │
                    REST / JSON
                           │
                           ▼
                         Order
                       /   |   \
                      /    |    \
                     ▼     ▼     ▼
              Inventory Promotion Payment
                  │         │       │
                  └──── gRPC ──────┘

                         Order
                           │
                        Kafka
                      /       \
                     ▼         ▼
                 Shipping  Notification

Product
   │
   └── Kafka → Search
```

This architecture deliberately keeps:

- REST at the external boundary.
- gRPC for selected synchronous decisions.
- Kafka for asynchronous propagation.

---

# 58. Relationship to Other Documents

This document establishes the internal gRPC communication baseline.

Related documents:

- **Document 09 — API Design:** external REST APIs.
- **Document 11 — Kafka Event Design:** asynchronous event contracts.
- **Document 12 — Redis Caching Strategy:** cache and cart communication/storage.
- **Document 13 — Security Design:** service authentication and authorization.
- **Document 15 — Kubernetes Design:** service discovery and gRPC deployment.
- **Document 16 — Observability Design:** distributed tracing and gRPC telemetry.
- **Document 17 — Testing Strategy:** gRPC integration and contract testing.
- **Document 18 — CI/CD Design:** contract validation and deployment.
- **Document 19 — Development Roadmap:** implementation sequencing.

---

# 59. Baseline Decision

This document establishes the initial CommerceX gRPC architecture.

Key decisions:

1. gRPC is primarily for internal synchronous communication.
2. REST remains the primary external API technology.
3. Kafka remains the primary asynchronous integration mechanism.
4. Inventory exposes `ReserveStock` and `ReleaseStock`.
5. Promotion exposes `ValidatePromotion`.
6. Payment exposes `ProcessPayment`.
7. Order is the primary gRPC client for checkout operations.
8. gRPC contracts are versioned using protobuf packages.
9. Monetary values use precise representations rather than floating-point values.
10. Internal calls use deadlines and cancellation.
11. Retry-sensitive operations require idempotency.
12. gRPC errors use standard status codes plus stable business failure codes where useful.
13. gRPC services are independently observable and secured.
14. The initial gRPC surface remains intentionally small.

**Next document:** `CommerceX-11-Kafka-Event-Design.md`
