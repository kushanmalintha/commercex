# CommerceX — Document 09: API Design

**Document ID:** COMMERceX-09  
**Project:** CommerceX  
**Document Type:** API Architecture / Interface Design  
**Status:** Baseline  
**Version:** 1.0  
**Date:** 2026-09-13

---

# 1. Purpose

This document defines the REST API architecture for CommerceX.

It establishes:

- API design principles
- API Gateway behavior
- Resource-oriented endpoint conventions
- API versioning
- Authentication and authorization expectations
- Request and response conventions
- Validation rules
- HTTP status code conventions
- Pagination, filtering, and sorting
- Error response structure
- Idempotency expectations
- API contracts for the twelve services
- Administrative APIs
- Client-facing versus internal APIs
- API security and operational considerations

This document builds on Documents 01–08.

It defines the **REST API surface and conventions**. Detailed gRPC contracts belong to Document 10, Kafka event contracts belong to Document 11, and Redis behavior belongs to Document 12.

---

# 2. API Architecture Goals

CommerceX APIs should be:

1. Resource-oriented.
2. Consistent across services.
3. Easy to test using REST clients.
4. Secure by default.
5. Explicit about validation and errors.
6. Independently owned by each service.
7. Suitable for browser/mobile/API clients.
8. Versionable without unnecessary complexity.
9. Observable through correlation IDs and tracing.
10. Simple enough for a learning-focused project.

---

# 3. API Entry Point

External clients should normally communicate through the API Gateway.

```text
Client
  │
  ▼
API Gateway
  │
  ├── Auth Service
  ├── User Service
  ├── Product Service
  ├── Inventory Service
  ├── Cart Service
  ├── Order Service
  ├── Payment Service
  ├── Shipping Service
  ├── Review Service
  ├── Notification Service
  ├── Promotion Service
  └── Search Service
```

The Gateway is the primary external entry point.

Direct service access may be allowed during local development or internal testing, but production-style client traffic should use the Gateway.

---

# 4. API Gateway Responsibilities

The Gateway should provide:

- Routing
- Authentication integration
- Request forwarding
- Correlation ID propagation
- Basic request metadata
- Rate limiting where implemented
- TLS termination where applicable
- Consistent external API exposure

The Gateway should not contain business rules.

For example, this is incorrect:

```text
Gateway
 └── Calculate order total
```

Instead:

```text
Gateway
 └── Forward order request
        ↓
     Order Service
```

---

# 5. API Versioning

The initial public API should use URL-based versioning.

Recommended:

```text
/api/v1/...
```

Examples:

```text
/api/v1/auth/login
/api/v1/products
/api/v1/orders
/api/v1/cart
```

The initial project only needs version `v1`.

A new major version should be introduced only when a breaking contract change is necessary.

---

# 6. Resource Naming

Use plural nouns for collections.

Preferred:

```text
/products
/orders
/reviews
/promotions
/addresses
```

Avoid:

```text
/getProducts
/createOrder
/deleteReview
```

HTTP methods should express the operation.

---

# 7. HTTP Method Conventions

| Method | Purpose |
|---|---|
| GET | Retrieve resource(s) |
| POST | Create resource or execute a non-idempotent command |
| PUT | Replace a resource |
| PATCH | Partially update a resource |
| DELETE | Remove/deactivate a resource where appropriate |

Examples:

```http
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/products
PATCH  /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

Not every domain operation must map mechanically to CRUD. Business commands may use action endpoints when appropriate.

---

# 8. Resource Identifiers

Resources should normally use UUID/GUID identifiers.

Example:

```text
GET /api/v1/products/7c6d...
```

Business identifiers remain separate.

For example:

```text
Product ID → UUID
SKU        → CX-TSHIRT-001
```

The UUID identifies the resource; the SKU is a business attribute.

---

# 9. Request Format

JSON is the default REST request format.

```http
Content-Type: application/json
```

Example:

```json
{
  "name": "CommerceX T-Shirt",
  "description": "Learning project product",
  "price": 2500.00,
  "categoryId": "..."
}
```

Requests should use camelCase JSON properties.

---

# 10. Response Format

Successful responses should return JSON resources.

Example:

```json
{
  "id": "7c6d...",
  "name": "CommerceX T-Shirt",
  "price": 2500.00,
  "status": "ACTIVE"
}
```

Collection responses should provide pagination metadata where pagination is applicable.

Example:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 145,
  "totalPages": 8
}
```

The exact DTO structure may be refined during implementation.

---

# 11. HTTP Status Code Conventions

| Status | Meaning |
|---|---|
| 200 | Successful request |
| 201 | Resource created |
| 202 | Accepted for asynchronous processing where appropriate |
| 204 | Successful request with no response body |
| 400 | Invalid request |
| 401 | Authentication required/failed |
| 403 | Authenticated but not authorized |
| 404 | Resource not found |
| 409 | Conflict/business state conflict |
| 422 | Semantically invalid request where used |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |
| 502 | Upstream service failure |
| 503 | Service temporarily unavailable |

The API should avoid returning `500` for normal business validation failures.

---

# 12. Error Response Contract

CommerceX should use a consistent problem-details style response.

Example:

```json
{
  "type": "https://commercex/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/products",
  "traceId": "00-abc123...",
  "errors": {
    "price": [
      "Price must be greater than zero."
    ]
  }
}
```

Sensitive internal details must not be exposed.

Do not return:

```json
{
  "exception": "Npgsql.PostgresException ...",
  "connectionString": "...",
  "stackTrace": "..."
}
```

---

# 13. Validation

Validation should occur at multiple appropriate layers.

## API layer

Validate:

- Required fields
- Format
- Length
- Basic ranges
- JSON structure

## Application/domain layer

Validate:

- Business rules
- State transitions
- Ownership
- Domain invariants

## Database layer

Protect:

- Uniqueness
- Non-null constraints
- Numeric/data integrity
- Other important invariants

Example:

```text
POST /orders
      ↓
API validation
      ↓
Application validation
      ↓
Domain rules
      ↓
Persistence
```

---

# 14. Authentication

Protected APIs use the authenticated user's access token.

Recommended:

```http
Authorization: Bearer <access-token>
```

The token is issued by Auth Service.

The Gateway can validate authentication before routing, but individual services should still enforce authorization for their own resources.

---

# 15. Authorization

Roles are initially:

- Customer
- Admin

Examples:

```text
Customer
 ├── manage own profile
 ├── manage own cart
 ├── create own orders
 ├── view own orders
 └── manage own reviews

Admin
 ├── manage products
 ├── manage inventory
 └── manage promotions
```

Authorization must be checked based on both:

1. Role
2. Resource ownership

Example:

```text
Customer A
    ↓
GET /orders/{OrderOfCustomerB}
    ↓
403 Forbidden
```

---

# 16. Auth Service API

Base path:

```text
/api/v1/auth
```

## Public endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/register` | Register account |
| POST | `/login` | Authenticate |
| POST | `/refresh` | Refresh access token |
| POST | `/forgot-password` | Start password reset |
| POST | `/reset-password` | Complete password reset |

## Authenticated endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/logout` | Revoke current refresh/session token |
| POST | `/change-password` | Change password |

Example registration:

```http
POST /api/v1/auth/register
```

```json
{
  "email": "customer@example.com",
  "password": "StrongPassword123!"
}
```

Response:

```http
201 Created
```

---

# 17. User Service API

Base path:

```text
/api/v1/users
```

The current user's profile can use:

```text
GET   /api/v1/users/me
PATCH /api/v1/users/me
```

Addresses:

```text
GET    /api/v1/users/me/addresses
POST   /api/v1/users/me/addresses
GET    /api/v1/users/me/addresses/{id}
PATCH  /api/v1/users/me/addresses/{id}
DELETE /api/v1/users/me/addresses/{id}
```

Administrative/user-management endpoints may be added later if required.

---

# 18. Product Service API

Base path:

```text
/api/v1/products
```

## Public/customer operations

```text
GET /api/v1/products
GET /api/v1/products/{id}
```

## Administrative operations

```text
POST   /api/v1/products
PATCH  /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

Categories:

```text
GET    /api/v1/categories
GET    /api/v1/categories/{id}
POST   /api/v1/categories
PATCH  /api/v1/categories/{id}
DELETE /api/v1/categories/{id}
```

Product creation example:

```json
{
  "sku": "CX-TSHIRT-001",
  "name": "CommerceX T-Shirt",
  "description": "Demo product",
  "price": 2500.00,
  "categoryId": "..."
}
```

---

# 19. Search Service API

Base path:

```text
/api/v1/search
```

Primary endpoint:

```text
GET /api/v1/search/products
```

Example:

```text
GET /api/v1/search/products?q=tshirt&categoryId=...&minPrice=1000&maxPrice=5000&page=1&pageSize=20
```

Supported concepts:

- Keyword
- Category
- Price range
- Status
- Sorting
- Pagination

Search returns product search representations, not ownership of Product data.

---

# 20. Inventory Service API

Base path:

```text
/api/v1/inventory
```

Administrative/read endpoints:

```text
GET   /api/v1/inventory/{productId}
POST  /api/v1/inventory
PATCH /api/v1/inventory/{productId}
```

Internal reservation operations are preferably exposed through gRPC rather than public REST.

Conceptually:

```text
Order
  ↓
Inventory gRPC
  ↓
Reserve / Release
```

This keeps internal transactional operations distinct from public APIs.

---

# 21. Cart Service API

Base path:

```text
/api/v1/cart
```

```text
GET    /api/v1/cart
POST   /api/v1/cart/items
PATCH  /api/v1/cart/items/{productId}
DELETE /api/v1/cart/items/{productId}
DELETE /api/v1/cart
```

Example:

```json
{
  "productId": "...",
  "quantity": 2
}
```

The cart belongs to the authenticated customer.

A client must not supply another customer's ID to access a cart.

---

# 22. Promotion Service API

Base path:

```text
/api/v1/promotions
```

Administrative:

```text
GET    /api/v1/promotions
GET    /api/v1/promotions/{id}
POST   /api/v1/promotions
PATCH  /api/v1/promotions/{id}
DELETE /api/v1/promotions/{id}
```

Validation/calculation during checkout should preferably use an internal gRPC contract.

Public promotion browsing can remain REST.

---

# 23. Order Service API

Base path:

```text
/api/v1/orders
```

Customer operations:

```text
POST /api/v1/orders
GET  /api/v1/orders
GET  /api/v1/orders/{id}
POST /api/v1/orders/{id}/cancel
```

Order creation example:

```json
{
  "shippingAddressId": "...",
  "promotionCode": "WELCOME10"
}
```

The server obtains cart contents from the Cart Service rather than trusting arbitrary client-supplied line items.

---

# 24. Order Creation Semantics

A simplified request flow is:

```text
POST /orders
       ↓
Authenticate customer
       ↓
Retrieve customer's cart
       ↓
Validate products
       ↓
Validate promotion
       ↓
Reserve inventory
       ↓
Process payment
       ↓
Create/confirm order
       ↓
Publish OrderConfirmed
```

The exact orchestration and failure handling are defined by the architecture and later gRPC/Kafka documents.

---

# 25. Payment Service API

Payment is primarily an internal service.

A public customer-facing payment API should be minimal.

Internal conceptual operations:

```text
ProcessPayment
GetPaymentStatus
```

For learning/testing, a controlled administrative or test endpoint may be exposed if needed.

The initial implementation must not accept or persist real card information.

---

# 26. Shipping Service API

Base path:

```text
/api/v1/shipments
```

Customer:

```text
GET /api/v1/shipments/{id}
```

Administrative/internal operations:

```text
POST  /api/v1/shipments
PATCH /api/v1/shipments/{id}
```

Normal shipment creation should be triggered from order fulfillment events rather than directly by customers.

---

# 27. Review Service API

Base path:

```text
/api/v1/reviews
```

Customer:

```text
POST   /api/v1/reviews
GET    /api/v1/reviews/{id}
PATCH  /api/v1/reviews/{id}
DELETE /api/v1/reviews/{id}
```

Product reviews:

```text
GET /api/v1/products/{productId}/reviews
```

The Gateway may route this to Review Service.

Example:

```json
{
  "productId": "...",
  "orderId": "...",
  "rating": 5,
  "title": "Great product",
  "content": "Good quality."
}
```

---

# 28. Notification Service API

Notification is primarily event-driven.

A public API is not required for normal business flows.

Optional administrative/read operations:

```text
GET /api/v1/notifications
GET /api/v1/notifications/{id}
```

Notifications should normally be created from Kafka events.

---

# 29. API Surface Summary

| Service | Primary External API |
|---|---|
| Auth | `/api/v1/auth/*` |
| User | `/api/v1/users/*` |
| Product | `/api/v1/products/*` |
| Inventory | `/api/v1/inventory/*` |
| Cart | `/api/v1/cart/*` |
| Order | `/api/v1/orders/*` |
| Payment | Limited/internal |
| Shipping | `/api/v1/shipments/*` |
| Review | `/api/v1/reviews/*` |
| Notification | Limited/read-oriented |
| Promotion | `/api/v1/promotions/*` |
| Search | `/api/v1/search/*` |

---

# 30. Pagination

Collection endpoints should use pagination when result size can grow.

Recommended query parameters:

```text
page
pageSize
```

Example:

```text
GET /api/v1/products?page=2&pageSize=20
```

Reasonable initial limits:

```text
pageSize minimum = 1
pageSize maximum = 100
```

The service should enforce the maximum.

---

# 31. Filtering

Use query parameters.

Example:

```text
GET /api/v1/products?categoryId=...&status=ACTIVE
```

Search:

```text
GET /api/v1/search/products?q=laptop&minPrice=50000&maxPrice=150000
```

Avoid creating a separate endpoint for every filter combination.

---

# 32. Sorting

Use explicit query parameters.

Example:

```text
GET /api/v1/products?sort=price&direction=asc
```

Only supported sortable fields should be accepted.

Do not pass raw SQL/order expressions from clients.

---

# 33. Resource Relationships

Nested resources should be used when the relationship is natural and bounded.

Good:

```text
/users/me/addresses
/products/{productId}/reviews
```

Avoid deeply nested URLs such as:

```text
/users/{userId}/orders/{orderId}/items/{itemId}/reviews/...
```

Keep API paths readable.

---

# 34. Idempotency

Operations that may be retried should support idempotency where appropriate.

Especially:

- Order creation
- Payment processing
- Inventory reservation
- Important state-changing commands

A client may provide:

```http
Idempotency-Key: <unique-key>
```

The owning service is responsible for interpreting and enforcing idempotency.

The exact implementation is service-specific.

---

# 35. Optimistic Concurrency

For resources that may be modified concurrently, optimistic concurrency can be introduced where useful.

Possible mechanisms:

- Version number
- Row version
- ETag/If-Match

The initial project should use this selectively rather than introducing complex concurrency infrastructure everywhere.

Inventory reservation and order state transitions should prioritize correct domain invariants.

---

# 36. API Timeouts

Internal calls should have explicit timeouts.

For example:

```text
Order → Inventory
Order → Promotion
Order → Payment
```

must not wait indefinitely.

Timeout values should be configurable.

The service should return an appropriate failure response rather than hanging.

---

# 37. Retries

Retries should be selective.

Safe candidates:

- Temporary network failure
- Transient service unavailable
- Certain Kafka operations

Unsafe retries:

```text
POST payment
```

unless idempotency is guaranteed.

A retry without idempotency can create duplicate business operations.

---

# 38. API Correlation

Every incoming request should have a correlation/trace identifier.

Example:

```http
X-Correlation-ID: abc-123
```

If OpenTelemetry tracing is enabled, the trace context should propagate through downstream calls.

Example:

```text
Client
 ↓
Gateway
 ↓
Order
 ↓
Inventory
```

The same trace should make the distributed request observable.

---

# 39. API Security

The API layer should protect against:

- Missing authentication
- Broken authorization
- Excessive request size
- Malformed input
- Injection attempts
- Brute-force authentication attempts
- Sensitive data leakage
- Abuse of administrative endpoints

Additional security belongs in Document 13.

---

# 40. API Rate Limiting

Rate limiting should be considered primarily at the Gateway.

High-value initial candidates:

```text
POST /auth/login
POST /auth/register
POST /auth/forgot-password
```

The exact limits can be configured later.

Internal service-to-service calls should not rely only on external Gateway rate limits.

---

# 41. API Documentation

Each service should expose OpenAPI documentation during development.

Recommended:

```text
Swagger / OpenAPI
```

For each endpoint document:

- HTTP method
- Path
- Authentication requirement
- Authorization requirement
- Request schema
- Response schema
- Error responses
- Example requests/responses

OpenAPI should describe the public REST contracts.

---

# 42. API Health Endpoints

Every HTTP service should expose health endpoints.

Example:

```text
GET /health
GET /health/ready
GET /health/live
```

Conceptually:

```text
/health/live
    → process is alive

/health/ready
    → service can accept traffic
```

Dependency checks should be used carefully so that a temporary downstream failure does not unnecessarily make every service appear unhealthy.

---

# 43. Administrative API Boundaries

Administrative operations require the Admin role.

Examples:

```text
POST   /products
PATCH  /products/{id}
POST   /inventory
PATCH  /inventory/{productId}
POST   /promotions
PATCH  /promotions/{id}
```

Customers should not be able to invoke administrative operations by simply knowing the endpoint.

---

# 44. API Gateway Routing Model

Conceptually:

```text
/api/v1/auth/*          → Auth
/api/v1/users/*         → User
/api/v1/products/*      → Product
/api/v1/inventory/*     → Inventory
/api/v1/cart/*          → Cart
/api/v1/orders/*        → Order
/api/v1/shipments/*     → Shipping
/api/v1/reviews/*       → Review
/api/v1/notifications/*→ Notification
/api/v1/promotions/*    → Promotion
/api/v1/search/*        → Search
```

Payment may remain primarily internal.

The exact YARP configuration is an implementation concern.

---

# 45. Internal vs External API Boundary

Not every service endpoint should be publicly exposed.

### External-facing

- Auth
- User
- Product
- Cart
- Order
- Review
- Search
- Selected Promotion/Product/Shipping reads

### Primarily internal

- Payment processing
- Inventory reservation
- Promotion calculation
- Notification processing
- Some Shipping operations

This reduces unnecessary external attack surface.

---

# 46. API Compatibility

Backward compatibility should be preserved where possible.

Safe change:

```text
Add optional response field
```

Potential breaking change:

```text
Rename productId → id
```

Breaking changes should normally require a new API version.

---

# 47. API Anti-Patterns

CommerceX should avoid:

## RPC-style CRUD URLs

```text
POST /getProducts
POST /createOrder
```

Prefer resource-oriented endpoints.

## Leaking database models

Do not expose EF Core entities directly as public API contracts.

Use DTOs.

## Cross-service aggregation everywhere

The Gateway should not become a universal data aggregation layer.

## Client-controlled sensitive values

Do not trust client-provided:

- CustomerId
- Order total
- Payment status
- Inventory availability
- Discount amount

The service owning the data determines authoritative values.

## Huge response payloads

Use pagination and focused resource representations.

---

# 48. DTO Boundary

Each service should maintain API DTOs separate from domain entities.

Recommended:

```text
API DTO
   ↓
Application Command/Query
   ↓
Domain Model
```

Do not expose domain entities directly.

This allows:

- Domain evolution
- API compatibility
- Security filtering
- Explicit contracts

---

# 49. API Testing Strategy

Each API should be tested at multiple levels.

### Unit tests

- Validation
- Application behavior
- Domain rules

### Integration tests

- HTTP endpoint
- Database interaction
- Authentication/authorization

### Contract tests

- REST contract compatibility
- gRPC contract compatibility where applicable
- Event compatibility for Kafka consumers

### End-to-end tests

Examples:

```text
Register
 → Login
 → Product
 → Cart
 → Order
 → Payment
 → Shipping
```

The testing strategy is detailed in Document 17.

---

# 50. API Design Validation Checklist

Before adding an endpoint:

- [ ] Is the resource owned by this service?
- [ ] Is the endpoint REST-oriented?
- [ ] Is authentication required?
- [ ] Is authorization required?
- [ ] Is resource ownership checked?
- [ ] Is request validation defined?
- [ ] Is the response contract defined?
- [ ] Are expected errors defined?
- [ ] Is pagination required?
- [ ] Is filtering/sorting required?
- [ ] Is idempotency required?
- [ ] Is the operation public or internal?
- [ ] Does the endpoint expose another service's data ownership?
- [ ] Does the endpoint contain business logic that belongs elsewhere?

---

# 51. Initial API Contract Summary

The initial CommerceX REST API is intentionally manageable.

```text
Auth
 ├── register
 ├── login
 ├── refresh
 ├── logout
 └── password reset

User
 ├── profile
 └── addresses

Product
 ├── products
 └── categories

Inventory
 └── inventory reads/admin
     + internal reservation through gRPC

Cart
 └── current cart/items

Promotion
 └── promotion management
     + internal validation through gRPC

Order
 ├── create
 ├── list
 ├── get
 └── cancel

Payment
 └── primarily internal

Shipping
 └── shipment/tracking reads + controlled operations

Review
 └── review CRUD

Notification
 └── primarily event-driven

Search
 └── product search
```

---

# 52. Relationship to Other Documents

This document defines REST API boundaries and conventions.

The next documents refine other communication/data concerns:

- **Document 10 — gRPC Design:** internal synchronous service contracts.
- **Document 11 — Kafka Event Design:** asynchronous event contracts.
- **Document 12 — Redis Caching Strategy:** cache and cart behavior.
- **Document 13 — Security Design:** authentication, authorization, secrets, tokens, and API security.
- **Document 14 — Docker Design:** container/API runtime considerations.
- **Document 15 — Kubernetes Design:** service exposure, ingress, health probes, and deployment.
- **Document 16 — Observability Design:** API metrics, traces, and logging.
- **Document 17 — Testing Strategy:** API and contract testing.
- **Document 18 — CI/CD Design:** automated API validation and deployment.
- **Document 19 — Development Roadmap:** implementation sequencing.

---

# 53. Baseline Decision

This document establishes the initial CommerceX API architecture.

The key decisions are:

1. External APIs use REST/JSON.
2. Public endpoints use `/api/v1/...`.
3. Resources use plural nouns and standard HTTP methods.
4. DTOs are separated from domain entities.
5. Authentication uses Bearer access tokens.
6. Authorization is enforced by the owning service.
7. The Gateway is the normal external entry point.
8. Internal transactional operations may use gRPC instead of public REST.
9. Kafka is used for asynchronous business events.
10. Pagination is required for potentially large collections.
11. Consistent problem-details-style errors are used.
12. Idempotency is required for appropriate retry-sensitive operations.
13. Business logic does not belong in the Gateway.
14. Client-provided values never override authoritative service-owned values.
15. API contracts should remain simple and independently evolvable.

**Next document:** `CommerceX-10-gRPC-Design.md`
