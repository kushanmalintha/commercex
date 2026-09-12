# CommerceX — Functional Requirements Specification

**Document ID:** COMX-DOC-003  
**Document Version:** 1.0  
**Status:** Baseline / Draft for Implementation Planning  
**Project:** CommerceX  
**Related Documents:**
- `01-project-charter.md`
- `02-system-requirements.md`

---

# 1. Introduction

## 1.1 Purpose

This document defines the detailed functional requirements for CommerceX.

The Project Charter establishes the purpose and scope of CommerceX, while the System Requirements Specification defines the system-level requirements. This document refines those requirements into concrete functional behavior for the API Gateway and the 12 backend services.

The requirements in this document will later be used to design:

- Service boundaries
- REST APIs
- gRPC contracts
- Kafka events
- Database schemas
- Application workflows
- Automated tests
- Kubernetes deployments

This document describes **what the system should do**, rather than prescribing the detailed implementation of how it should do it.

---

# 2. Functional Scope

CommerceX will provide the following functional areas:

```text
Authentication
     |
     v
User Management
     |
     v
Product Catalog
     |
     +----> Search
     |
     +----> Promotions
     |
     v
Shopping Cart
     |
     v
Inventory
     |
     v
Orders
     |
     +----> Payment
     |
     v
Shipping
     |
     +----> Notifications
     |
     v
Reviews
```

The platform will contain:

1. API Gateway
2. Auth Service
3. User Service
4. Product Service
5. Inventory Service
6. Cart Service
7. Order Service
8. Payment Service
9. Shipping Service
10. Review Service
11. Notification Service
12. Promotion Service
13. Search Service

The API Gateway is an infrastructure/API entry point and is not counted as one of the 12 business services.

---

# 3. Functional Requirement Conventions

Each requirement is assigned an identifier.

Examples:

```text
FR-AUTH-001
FR-USER-001
FR-PRODUCT-001
```

The naming convention is:

```text
FR-<SERVICE>-<NUMBER>
```

Requirement priority:

| Priority | Meaning |
|---|---|
| Must | Required for the initial implementation |
| Should | Important but can be implemented after core functionality |
| Could | Optional enhancement |
| Won't | Explicitly excluded from the initial implementation |

---

# 4. Actors

## 4.1 Customer

A customer can:

- Register
- Authenticate
- Manage their profile
- Manage addresses
- Browse products
- Search products
- Manage a shopping cart
- Apply promotions
- Place orders
- Make simulated payments
- View order history
- Track shipments
- Submit product reviews

## 4.2 Administrator

An administrator can perform selected management operations such as:

- Manage products
- Manage categories
- Manage inventory
- Manage promotions
- Manage operational data

## 4.3 Internal Service

An internal service is another CommerceX service communicating through:

- REST
- gRPC
- Kafka

depending on the workflow.

---

# 5. API Gateway Functional Requirements

## FR-GW-001 — Unified Entry Point

**Priority:** Must

The API Gateway shall provide the primary external entry point to CommerceX.

Example:

```text
Client
  |
  v
/api/products
/api/cart
/api/orders
```

Clients should not need to know individual internal service addresses.

## FR-GW-002 — Request Routing

**Priority:** Must

The gateway shall route requests to the appropriate backend service.

Example:

```text
/api/auth/*       -> Auth Service
/api/users/*      -> User Service
/api/products/*   -> Product Service
/api/cart/*       -> Cart Service
/api/orders/*     -> Order Service
```

## FR-GW-003 — Authentication Enforcement

**Priority:** Must

The gateway shall enforce authentication requirements for protected routes.

Public routes may include:

```text
POST /api/auth/register
POST /api/auth/login
```

Protected routes shall require valid authentication.

## FR-GW-004 — Authorization Support

**Priority:** Must

The gateway shall support route-level authorization where appropriate.

Administrative routes shall require an Admin role.

## FR-GW-005 — Request Correlation

**Priority:** Should

The gateway should generate or propagate a correlation/trace identifier.

## FR-GW-006 — Error Forwarding

**Priority:** Must

The gateway shall return appropriate client-facing errors when downstream services fail.

Internal service details shall not be exposed unnecessarily.

## FR-GW-007 — Health Endpoint

**Priority:** Must

The gateway shall expose a health endpoint.

Example:

```text
GET /health
```

---

# 6. Auth Service Functional Requirements

## 6.1 Account Registration

### FR-AUTH-001 — Register Customer

**Priority:** Must

The Auth Service shall allow a new customer to register.

Input should contain information such as:

```json
{
  "email": "customer@example.com",
  "password": "SecurePassword"
}
```

The service shall:

1. Validate the input.
2. Check whether the account already exists.
3. Hash the password.
4. Create authentication credentials.
5. Create the associated user identity where required.
6. Return an appropriate response.

### FR-AUTH-002 — Duplicate Account Prevention

**Priority:** Must

The service shall reject registration when the identity already exists.

The response should indicate a conflict without exposing unnecessary account information.

---

## 6.2 Authentication

### FR-AUTH-003 — Login

**Priority:** Must

A registered customer shall be able to authenticate using valid credentials.

Successful authentication shall return:

```text
Access Token
Refresh Token
```

### FR-AUTH-004 — Invalid Credentials

**Priority:** Must

Invalid credentials shall result in an authentication failure.

The response shall not reveal whether the email or password was specifically incorrect.

### FR-AUTH-005 — Token Refresh

**Priority:** Must

The service shall accept a valid refresh token and issue a new access token.

### FR-AUTH-006 — Refresh Token Rotation

**Priority:** Should

The service should support refresh-token rotation or equivalent replay protection.

### FR-AUTH-007 — Logout

**Priority:** Must

The service shall invalidate the relevant refresh token.

---

## 6.3 Password Management

### FR-AUTH-008 — Password Reset Request

**Priority:** Must

A customer shall be able to request a password reset.

The service shall generate a secure, time-limited reset token.

### FR-AUTH-009 — Password Reset Completion

**Priority:** Must

A customer shall be able to reset their password using a valid reset token.

The reset token shall:

- Expire after a defined period.
- Be single-use.
- Become invalid after successful use.

### FR-AUTH-010 — Password Change

**Priority:** Should

An authenticated customer should be able to change their password.

---

## 6.4 Roles

### FR-AUTH-011 — Customer Role

**Priority:** Must

New customer accounts shall receive the Customer role.

### FR-AUTH-012 — Admin Role

**Priority:** Must

The system shall support an Admin role.

The initial assignment of administrator accounts may be performed through controlled administrative configuration rather than a public registration flow.

---

# 7. User Service Functional Requirements

## 7.1 Profile

### FR-USER-001 — Create Profile

**Priority:** Must

The User Service shall maintain a customer profile associated with the authenticated identity.

### FR-USER-002 — Retrieve Profile

**Priority:** Must

A customer shall be able to retrieve their profile.

### FR-USER-003 — Update Profile

**Priority:** Must

A customer shall be able to update permitted profile fields.

### FR-USER-004 — Profile Ownership

**Priority:** Must

A customer shall only be able to modify their own profile unless the operation is explicitly administrative.

---

## 7.2 Address Management

### FR-USER-005 — Add Address

**Priority:** Must

A customer shall be able to add a delivery address.

### FR-USER-006 — List Addresses

**Priority:** Must

A customer shall be able to retrieve their saved addresses.

### FR-USER-007 — Update Address

**Priority:** Must

A customer shall be able to update their address.

### FR-USER-008 — Delete Address

**Priority:** Must

A customer shall be able to delete an address that is no longer required, subject to business rules.

### FR-USER-009 — Default Address

**Priority:** Must

A customer shall be able to designate one address as the default address.

Only one address should be the active default address at a time.

---

# 8. Product Service Functional Requirements

## 8.1 Product Management

### FR-PRODUCT-001 — Create Product

**Priority:** Must

An Admin shall be able to create a product.

Minimum product information:

```text
Product ID
Name
Description
Price
Category ID
Status
Created At
Updated At
```

### FR-PRODUCT-002 — Retrieve Product

**Priority:** Must

The system shall allow clients to retrieve a product by ID.

### FR-PRODUCT-003 — List Products

**Priority:** Must

The system shall provide a paginated product list.

### FR-PRODUCT-004 — Update Product

**Priority:** Must

An Admin shall be able to update permitted product information.

### FR-PRODUCT-005 — Deactivate Product

**Priority:** Must

An Admin shall be able to deactivate a product.

Deactivation should preserve historical references.

### FR-PRODUCT-006 — Product Status

**Priority:** Must

A product shall have a status indicating whether it can currently be sold.

Example:

```text
ACTIVE
INACTIVE
```

---

## 8.2 Category Management

### FR-PRODUCT-007 — Create Category

**Priority:** Must

An Admin shall be able to create a product category.

### FR-PRODUCT-008 — List Categories

**Priority:** Must

Clients shall be able to retrieve available categories.

### FR-PRODUCT-009 — Update Category

**Priority:** Should

An Admin should be able to update category information.

### FR-PRODUCT-010 — Deactivate Category

**Priority:** Should

An Admin should be able to deactivate a category.

---

# 9. Search Service Functional Requirements

## FR-SEARCH-001 — Search Products

**Priority:** Must

The Search Service shall allow customers to search products by text.

Example:

```text
GET /api/search/products?q=laptop
```

The search should consider relevant product fields such as:

- Name
- Description

## FR-SEARCH-002 — Filter Products

**Priority:** Must

Search results shall support basic filters.

Supported initial filters:

```text
Category
Minimum Price
Maximum Price
Availability
```

## FR-SEARCH-003 — Sort Results

**Priority:** Should

Search results should support simple sorting.

Examples:

```text
price-ascending
price-descending
name
newest
```

## FR-SEARCH-004 — Pagination

**Priority:** Must

Search results shall support pagination.

## FR-SEARCH-005 — Empty Results

**Priority:** Must

A valid search with no matching products shall return an empty result set rather than an error.

## FR-SEARCH-006 — Search Data Synchronization

**Priority:** Must

The Search Service shall obtain product information through defined service communication or events rather than directly accessing the Product Service database.

---

# 10. Inventory Service Functional Requirements

## FR-INVENTORY-001 — Inventory Creation

**Priority:** Must

An inventory record shall exist for a sellable product.

## FR-INVENTORY-002 — Retrieve Inventory

**Priority:** Must

The system shall provide current inventory information for authorized operations.

## FR-INVENTORY-003 — Stock Adjustment

**Priority:** Must

An Admin shall be able to adjust stock quantities.

## FR-INVENTORY-004 — Stock Reservation

**Priority:** Must

The Inventory Service shall support reservation of available stock.

Input should include:

```text
Order ID
Product ID
Quantity
```

## FR-INVENTORY-005 — Prevent Overselling

**Priority:** Must

The system shall reject a reservation when available stock is insufficient.

## FR-INVENTORY-006 — Stock Release

**Priority:** Must

Reserved stock shall be released when the related order is cancelled or reservation is otherwise invalidated.

## FR-INVENTORY-007 — Reservation Idempotency

**Priority:** Must

Repeated processing of the same reservation request shall not reserve the same stock multiple times.

## FR-INVENTORY-008 — Inventory Events

**Priority:** Must

The Inventory Service shall publish relevant events after significant inventory state changes.

Potential events:

```text
InventoryReserved
InventoryReleased
InventoryAdjusted
```

---

# 11. Cart Service Functional Requirements

## FR-CART-001 — Retrieve Cart

**Priority:** Must

An authenticated customer shall be able to retrieve their current cart.

## FR-CART-002 — Add Item

**Priority:** Must

A customer shall be able to add a product to their cart.

The service shall validate:

- Product identifier
- Quantity
- Basic product availability information

## FR-CART-003 — Update Item

**Priority:** Must

A customer shall be able to change the quantity of an existing cart item.

## FR-CART-004 — Remove Item

**Priority:** Must

A customer shall be able to remove an item from their cart.

## FR-CART-005 — Clear Cart

**Priority:** Must

A customer shall be able to remove all items from the cart.

## FR-CART-006 — Duplicate Item Handling

**Priority:** Must

Adding an already-existing product shall update the quantity rather than creating duplicate cart lines.

## FR-CART-007 — Empty Cart

**Priority:** Must

The system shall prevent order creation from an empty cart.

## FR-CART-008 — Redis Storage

**Priority:** Must

The initial Cart Service implementation shall use Redis for cart storage.

## FR-CART-009 — Cart Expiration

**Priority:** Should

Inactive carts may expire after a configured period.

---

# 12. Promotion Service Functional Requirements

## FR-PROMO-001 — Create Promotion

**Priority:** Must

An Admin shall be able to create a promotion.

A promotion shall contain:

```text
Promotion Code
Discount Type
Discount Value
Start Date
End Date
Active Status
```

## FR-PROMO-002 — Promotion Types

**Priority:** Must

The initial implementation shall support:

```text
Percentage Discount
Fixed Amount Discount
```

## FR-PROMO-003 — Validate Promotion

**Priority:** Must

The system shall validate a promotion code against the configured rules.

## FR-PROMO-004 — Expiration

**Priority:** Must

An expired promotion shall not be accepted.

## FR-PROMO-005 — Inactive Promotion

**Priority:** Must

An inactive promotion shall not be accepted.

## FR-PROMO-006 — Unknown Promotion

**Priority:** Must

An unknown promotion code shall be rejected.

## FR-PROMO-007 — Calculate Discount

**Priority:** Must

The Promotion Service shall calculate the discount applicable to the provided order information.

## FR-PROMO-008 — Promotion Ownership

**Priority:** Must

Promotion management operations shall require Admin authorization.

---

# 13. Order Service Functional Requirements

## 13.1 Order Creation

### FR-ORDER-001 — Create Order

**Priority:** Must

An authenticated customer shall be able to create an order from a non-empty cart.

The system shall:

1. Retrieve cart information.
2. Validate applicable product information.
3. Validate the selected shipping address.
4. Validate any promotion.
5. Calculate the order total.
6. Create the order.
7. Initiate inventory reservation.
8. Initiate payment processing.
9. Update the order according to resulting events.

### FR-ORDER-002 — Order Snapshot

**Priority:** Must

The order shall store the relevant product name, quantity, and price at order creation time.

Historical orders shall not change when the current product changes.

### FR-ORDER-003 — Shipping Address Snapshot

**Priority:** Must

The order shall retain the shipping address used at checkout.

Later changes to the customer's saved address shall not modify the historical order address.

---

## 13.2 Order Lifecycle

### FR-ORDER-004 — Initial Status

**Priority:** Must

A newly created order shall enter an initial pending state.

Example:

```text
PENDING
```

### FR-ORDER-005 — Payment Pending

**Priority:** Must

The order shall be able to represent that payment is being processed.

```text
PAYMENT_PENDING
```

### FR-ORDER-006 — Confirmation

**Priority:** Must

A successfully paid and otherwise valid order shall transition to:

```text
CONFIRMED
```

### FR-ORDER-007 — Processing

**Priority:** Must

A confirmed order may transition to:

```text
PROCESSING
```

### FR-ORDER-008 — Shipment

**Priority:** Must

The order shall transition to:

```text
SHIPPED
```

when the relevant shipment state is established.

### FR-ORDER-009 — Delivery

**Priority:** Must

The order shall transition to:

```text
DELIVERED
```

after successful delivery.

### FR-ORDER-010 — Cancellation

**Priority:** Must

An eligible order shall be cancellable.

The cancellation process shall release reserved inventory where required.

---

## 13.3 Order Retrieval

### FR-ORDER-011 — Get Order

**Priority:** Must

A customer shall be able to retrieve an order by ID if the order belongs to that customer.

### FR-ORDER-012 — Order History

**Priority:** Must

A customer shall be able to retrieve their order history.

### FR-ORDER-013 — Administrative Order Access

**Priority:** Should

An Admin should be able to retrieve orders for operational purposes.

---

## 13.4 Order Events

### FR-ORDER-014 — Order Created Event

**Priority:** Must

The Order Service shall publish an event when an order is created.

### FR-ORDER-015 — Order State Events

**Priority:** Must

The Order Service shall publish events for important lifecycle transitions.

Potential events:

```text
OrderCreated
OrderConfirmed
OrderCancelled
OrderShipped
OrderDelivered
```

---

# 14. Payment Service Functional Requirements

## FR-PAYMENT-001 — Initiate Payment

**Priority:** Must

The Payment Service shall support simulated payment initiation for an order.

## FR-PAYMENT-002 — Simulated Success

**Priority:** Must

The system shall support a successful payment outcome.

## FR-PAYMENT-003 — Simulated Failure

**Priority:** Must

The system shall support a failed payment outcome.

The failure can be intentionally triggered during testing or development.

## FR-PAYMENT-004 — Payment Record

**Priority:** Must

The service shall maintain a payment record containing information such as:

```text
Payment ID
Order ID
Amount
Status
Created At
Updated At
```

## FR-PAYMENT-005 — Payment Status

**Priority:** Must

The system shall support statuses such as:

```text
PENDING
SUCCEEDED
FAILED
```

## FR-PAYMENT-006 — Idempotent Payment

**Priority:** Must

The same logical payment request shall not create multiple successful payment transactions.

## FR-PAYMENT-007 — No Real Financial Data

**Priority:** Must

The initial system shall not process or persist real card or banking credentials.

---

# 15. Shipping Service Functional Requirements

## FR-SHIPPING-001 — Create Shipment

**Priority:** Must

The Shipping Service shall create a shipment for an eligible confirmed order.

## FR-SHIPPING-002 — Tracking Number

**Priority:** Must

Each shipment shall have a unique simulated tracking number.

## FR-SHIPPING-003 — Shipment Status

**Priority:** Must

The service shall support:

```text
CREATED
IN_TRANSIT
OUT_FOR_DELIVERY
DELIVERED
CANCELLED
```

## FR-SHIPPING-004 — Update Shipment Status

**Priority:** Must

Authorized internal or administrative operations shall be able to update shipment status.

## FR-SHIPPING-005 — Retrieve Shipment

**Priority:** Must

A customer shall be able to retrieve shipment information for their own order.

## FR-SHIPPING-006 — Shipment Events

**Priority:** Must

The Shipping Service shall publish events for important shipment changes.

Potential events:

```text
ShipmentCreated
ShipmentInTransit
ShipmentOutForDelivery
ShipmentDelivered
```

---

# 16. Review Service Functional Requirements

## FR-REVIEW-001 — Create Review

**Priority:** Must

An authenticated customer shall be able to submit a review for an eligible purchased product.

## FR-REVIEW-002 — Purchase Eligibility

**Priority:** Should

The system should verify that the customer has purchased the product before allowing a review.

## FR-REVIEW-003 — Rating

**Priority:** Must

The rating shall be an integer between:

```text
1
2
3
4
5
```

## FR-REVIEW-004 — Review Text

**Priority:** Must

A customer may provide optional review text.

## FR-REVIEW-005 — Retrieve Reviews

**Priority:** Must

Clients shall be able to retrieve reviews for a product.

## FR-REVIEW-006 — Update Own Review

**Priority:** Must

A customer shall be able to modify their own review.

## FR-REVIEW-007 — Delete Own Review

**Priority:** Must

A customer shall be able to remove their own review where permitted.

## FR-REVIEW-008 — Review Status

**Priority:** Should

Reviews may have a status such as:

```text
PUBLISHED
HIDDEN
```

This allows basic administrative moderation without implementing a complex moderation system.

---

# 17. Notification Service Functional Requirements

## FR-NOTIFY-001 — Consume Events

**Priority:** Must

The Notification Service shall consume relevant Kafka events.

## FR-NOTIFY-002 — Registration Notification

**Priority:** Should

The service should process user-registration events and generate a simulated welcome notification.

## FR-NOTIFY-003 — Order Notification

**Priority:** Must

The service shall generate simulated notifications for important order events.

Examples:

```text
OrderConfirmed
OrderCancelled
OrderShipped
OrderDelivered
```

## FR-NOTIFY-004 — Payment Notification

**Priority:** Should

The service should process payment success/failure events where notifications are appropriate.

## FR-NOTIFY-005 — Simulated Delivery

**Priority:** Must

Notifications shall initially be simulated through logs, stored records, or another local mechanism.

No external email provider is required.

## FR-NOTIFY-006 — Asynchronous Processing

**Priority:** Must

Notification processing shall occur asynchronously through Kafka events where applicable.

## FR-NOTIFY-007 — Duplicate Event Handling

**Priority:** Must

The service shall prevent duplicate processing from generating unintended duplicate notifications where practical.

---

# 18. Cross-Service Functional Requirements

## FR-CROSS-001 — Service Isolation

**Priority:** Must

A service shall not directly access another service's database.

## FR-CROSS-002 — Explicit Contracts

**Priority:** Must

Service communication shall use explicitly defined contracts.

These may include:

```text
REST API
gRPC contract
Kafka event schema
```

## FR-CROSS-003 — Independent Deployment

**Priority:** Must

Each business service shall be independently deployable.

## FR-CROSS-004 — Service Failure Isolation

**Priority:** Must

Failure of a non-critical asynchronous service shall not automatically fail the core transaction.

Example:

```text
Order Confirmed
      |
      v
Kafka
      |
      +----> Notification Service fails
      |
      v
Order remains confirmed
```

## FR-CROSS-005 — Eventual Consistency

**Priority:** Must

Cross-service workflows may use eventual consistency.

## FR-CROSS-006 — Idempotency

**Priority:** Must

Important operations that may be retried shall support appropriate idempotency.

---

# 19. Customer Checkout Functional Flow

The following is the primary functional workflow.

## Step 1 — Authenticate

```text
Customer
   |
   v
API Gateway
   |
   v
Auth Service
   |
   v
JWT
```

## Step 2 — Browse

Customer retrieves products from the Product Service.

## Step 3 — Search

Customer searches using the Search Service.

## Step 4 — Add to Cart

Customer adds products to their Redis-backed cart.

## Step 5 — Apply Promotion

Customer provides an optional promotion code.

The Promotion Service validates the code and calculates the discount.

## Step 6 — Checkout

Customer submits the cart for order creation.

## Step 7 — Inventory Reservation

The Order Service requests inventory reservation.

If stock is unavailable:

```text
Order -> Rejected / Cancelled
```

## Step 8 — Payment

The Payment Service performs simulated payment processing.

If payment fails:

```text
Payment Failed
      |
      v
Release Inventory
      |
      v
Order Failed/Cancelled
```

If payment succeeds:

```text
Payment Succeeded
      |
      v
Order Confirmed
```

## Step 9 — Shipment

A confirmed order results in shipment creation.

## Step 10 — Notification

Relevant events are consumed by the Notification Service.

## Step 11 — Tracking

Customer retrieves shipment information using the tracking number.

---

# 20. Order Failure Scenarios

## FR-FLOW-001 — Insufficient Inventory

When requested inventory is unavailable:

1. Reservation fails.
2. Order shall not be confirmed.
3. Customer shall receive an appropriate response.
4. No successful payment should be finalized for an order that cannot proceed.

## FR-FLOW-002 — Payment Failure

When simulated payment fails:

1. Payment status becomes FAILED.
2. Order shall not become CONFIRMED.
3. Reserved inventory shall be released.
4. Appropriate events shall be published.

## FR-FLOW-003 — Notification Failure

When Notification Service fails:

1. Core order state shall remain valid.
2. Kafka should retain the event for later processing according to configured broker/consumer behavior.
3. Notification failure shall be observable.

## FR-FLOW-004 — Shipping Failure

A temporary Shipping Service failure shall not corrupt the confirmed order.

The shipment creation operation should be retried or processed again according to the defined event workflow.

---

# 21. Functional API Surface

The exact API contracts will be defined in the API Design document. The initial functional surface is expected to include:

## Authentication

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
POST /api/auth/password-reset
POST /api/auth/password-reset/confirm
```

## Users

```text
GET    /api/users/me
PUT    /api/users/me
GET    /api/users/me/addresses
POST   /api/users/me/addresses
PUT    /api/users/me/addresses/{id}
DELETE /api/users/me/addresses/{id}
```

## Products

```text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}

GET    /api/categories
POST   /api/categories
PUT    /api/categories/{id}
DELETE /api/categories/{id}
```

## Search

```text
GET /api/search/products
```

## Inventory

```text
GET  /api/inventory/{productId}
POST /api/inventory
POST /api/inventory/reserve
POST /api/inventory/release
```

## Cart

```text
GET    /api/cart
POST   /api/cart/items
PUT    /api/cart/items/{productId}
DELETE /api/cart/items/{productId}
DELETE /api/cart
```

## Promotions

```text
POST /api/promotions
GET  /api/promotions/{code}
POST /api/promotions/validate
```

## Orders

```text
POST /api/orders
GET  /api/orders
GET  /api/orders/{id}
POST /api/orders/{id}/cancel
```

## Payments

```text
POST /api/payments
GET  /api/payments/{id}
```

## Shipping

```text
GET /api/shipments/{id}
GET /api/shipments/order/{orderId}
```

## Reviews

```text
GET    /api/products/{productId}/reviews
POST   /api/products/{productId}/reviews
PUT    /api/reviews/{id}
DELETE /api/reviews/{id}
```

The actual endpoint ownership and routing will be finalized in the API Design document.

---

# 22. Functional Business Rules

## BR-001 — Product Must Exist

An order or cart item must reference an existing product.

## BR-002 — Product Must Be Sellable

Inactive products shall not be newly added to carts or orders.

## BR-003 — Quantity Must Be Positive

Cart and order quantities must be greater than zero.

## BR-004 — Stock Cannot Be Negative

Available inventory must never become negative.

## BR-005 — Reservation Cannot Exceed Available Stock

A reservation must not exceed currently available stock.

## BR-006 — Empty Cart Cannot Be Ordered

An order cannot be created from an empty cart.

## BR-007 — Historical Order Prices Are Immutable

Changing the current product price shall not modify an existing order.

## BR-008 — Historical Shipping Address Is Immutable

Changing a customer's saved address shall not modify an existing order's shipping address.

## BR-009 — Promotion Must Be Valid

Only active, non-expired promotions satisfying their rules may be applied.

## BR-010 — Payment Must Succeed Before Confirmation

An order shall not enter CONFIRMED state unless payment succeeds and required inventory conditions are satisfied.

## BR-011 — Cancelled Orders Cannot Be Reconfirmed

A cancelled order shall not return to a normal processing state.

## BR-012 — Customer Resource Ownership

Customers may access only their own protected resources unless explicitly authorized.

## BR-013 — Admin Operations Require Admin Role

Administrative management operations require the Admin role.

## BR-014 — Review Rating Range

Review ratings must be between 1 and 5.

## BR-015 — Review Ownership

Customers may modify or delete only their own reviews unless administrative moderation rules allow otherwise.

---

# 23. State Models

## 23.1 Order State

Initial order state model:

```text
              +---------+
              | PENDING |
              +----+----+
                   |
                   v
        +-------------------+
        | PAYMENT_PENDING   |
        +---------+---------+
                  |
        +---------+---------+
        |                   |
        v                   v
   PAYMENT FAILED     PAYMENT SUCCESS
        |                   |
        v                   v
    CANCELLED           CONFIRMED
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

Cancellation may be possible from selected intermediate states according to business rules.

## 23.2 Payment State

```text
PENDING
  |
  +----> SUCCEEDED
  |
  +----> FAILED
```

## 23.3 Shipment State

```text
CREATED
   |
   v
IN_TRANSIT
   |
   v
OUT_FOR_DELIVERY
   |
   v
DELIVERED
```

Cancellation may be supported before delivery.

## 23.4 Product State

```text
ACTIVE
  |
  v
INACTIVE
```

---

# 24. Event-Driven Functional Requirements

CommerceX will use Kafka to propagate business events.

Initial event categories:

```text
User Events
Product Events
Inventory Events
Order Events
Payment Events
Shipping Events
Review Events
```

Potential event names:

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
OrderShipped
OrderDelivered

PaymentSucceeded
PaymentFailed

ShipmentCreated
ShipmentDelivered

ReviewCreated
ReviewUpdated
```

Events are functional contracts, not database records.

Detailed event payloads will be defined in the Kafka Event Design document.

---

# 25. gRPC Functional Usage

gRPC shall be used selectively for internal synchronous operations.

Potential use cases:

```text
Order Service
      |
      | gRPC
      v
Inventory Service
```

and:

```text
Order Service
      |
      | gRPC
      v
Promotion Service
```

The exact gRPC boundaries will be established later.

The system shall not introduce gRPC merely for the sake of using the technology.

---

# 26. Redis Functional Usage

Redis will initially provide:

### Cart Storage

```text
cart:{customerId}
```

### Product Cache

Potentially:

```text
product:{productId}
```

### Category Cache

Potentially:

```text
category:{categoryId}
```

Cache behavior shall include:

```text
Cache Hit
Cache Miss
Cache Population
Cache Invalidation
Expiration
```

Detailed cache strategy will be defined separately.

---

# 27. Administrative Functions

The initial Admin functional scope shall remain intentionally small.

An Admin shall be able to:

### Product Management

- Create product
- Update product
- Deactivate product
- View products

### Category Management

- Create category
- Update category
- Deactivate category

### Inventory Management

- View inventory
- Adjust inventory

### Promotion Management

- Create promotion
- Update promotion
- Activate/deactivate promotion

### Operational Visibility

The Admin may retrieve selected order, payment, shipment, and review information as required by later service designs.

---

# 28. Functional Security Rules

### FR-SEC-001

Unauthenticated users shall only access explicitly public operations.

### FR-SEC-002

Authenticated customers shall be restricted to permitted customer operations.

### FR-SEC-003

Administrative operations shall require the Admin role.

### FR-SEC-004

A customer shall not be able to retrieve another customer's private profile information.

### FR-SEC-005

A customer shall not be able to retrieve another customer's private orders.

### FR-SEC-006

A customer shall not be able to modify another customer's cart.

### FR-SEC-007

A customer shall not be able to modify another customer's review.

---

# 29. Validation Requirements

The system shall validate input at service boundaries.

Validation shall cover:

- Required fields
- String lengths
- Valid identifiers
- Positive quantities
- Valid prices
- Valid dates
- Valid promotion values
- Valid rating values
- Valid status transitions

Invalid input shall produce an appropriate client-facing validation response.

---

# 30. Functional Error Handling

The system shall distinguish common functional errors.

Examples:

```text
RESOURCE_NOT_FOUND
INVALID_REQUEST
VALIDATION_FAILED
UNAUTHORIZED
FORBIDDEN
DUPLICATE_RESOURCE
INSUFFICIENT_STOCK
INVALID_PROMOTION
PAYMENT_FAILED
INVALID_ORDER_STATE
```

Exact error codes will be standardized in the API Design document.

---

# 31. Functional Acceptance Criteria

A functional implementation shall be considered acceptable when the following scenarios work.

## Acceptance Scenario 1 — Customer Registration

```text
Register
  ->
Account created
  ->
Credentials stored securely
```

## Acceptance Scenario 2 — Login

```text
Valid Credentials
  ->
Access Token
  ->
Refresh Token
```

## Acceptance Scenario 3 — Product Browsing

```text
Customer
  ->
Product List
  ->
Product Details
```

## Acceptance Scenario 4 — Cart

```text
Product
  ->
Add to Cart
  ->
Update Quantity
  ->
Remove Item
```

## Acceptance Scenario 5 — Successful Checkout

```text
Cart
  ->
Order Created
  ->
Inventory Reserved
  ->
Payment Succeeded
  ->
Order Confirmed
  ->
Shipment Created
  ->
Notification Generated
```

## Acceptance Scenario 6 — Payment Failure

```text
Cart
  ->
Order Created
  ->
Inventory Reserved
  ->
Payment Failed
  ->
Inventory Released
  ->
Order Cancelled/Failed
```

## Acceptance Scenario 7 — Insufficient Stock

```text
Cart
  ->
Order Created
  ->
Inventory Reservation Failed
  ->
Order Not Confirmed
```

## Acceptance Scenario 8 — Review

```text
Delivered/Purchased Product
  ->
Create Review
  ->
Retrieve Review
```

---

# 32. Functional Requirement Traceability

The following relationship should be maintained:

```text
Project Charter
       |
       v
System Requirements
       |
       v
Functional Requirements
       |
       +----> Service Design
       |
       +----> API Design
       |
       +----> Database Design
       |
       +----> gRPC Design
       |
       +----> Kafka Event Design
       |
       +----> Test Cases
```

Every major functional requirement should eventually have corresponding implementation and test coverage.

---

# 33. Implementation Priorities

## Priority 1 — Core Platform

Implement first:

```text
Auth
User
Product
Inventory
Cart
Order
Payment
```

## Priority 2 — Supporting Services

Then:

```text
Shipping
Notification
Promotion
Search
Review
```

## Priority 3 — Distributed Features

Then strengthen:

```text
Kafka workflows
gRPC communication
Redis caching
Failure handling
Idempotency
```

## Priority 4 — Operational Features

Finally:

```text
Observability
Kubernetes
CI/CD
Performance testing
```

This ordering may be adjusted during development as long as dependencies are respected.

---

# 34. Functional Scope Boundaries

The initial implementation deliberately avoids:

- Real payment processing
- Real email delivery
- Complex product recommendation
- Advanced search engines
- Multi-vendor marketplace behavior
- Complex shipping providers
- Advanced discount engines
- Complex inventory warehouses
- Subscription billing
- Shopping wishlists
- Product image management infrastructure
- Loyalty programs
- Gift cards
- Tax engines

These may be added later only as explicit scope extensions.

---

# 35. Definition of Functional Completion

A functional feature is considered complete when:

```text
Requirement Defined
       |
       v
Business Rule Defined
       |
       v
API / Event Contract Defined
       |
       v
Implementation Completed
       |
       v
Unit Tests
       |
       v
Integration Tests where applicable
       |
       v
Failure Cases Tested
       |
       v
Documentation Updated
```

A service is not considered functionally complete merely because its API returns a successful response for the happy path.

---

# 36. Initial Functional Baseline

The following defines the functional baseline for CommerceX:

1. Customers can register and authenticate.
2. Customers can manage their profiles and addresses.
3. Administrators can manage products and categories.
4. Customers can browse and search products.
5. Customers can maintain Redis-backed shopping carts.
6. Administrators can manage inventory.
7. Orders can be created from carts.
8. Inventory can be reserved and released.
9. Promotions can be validated and applied.
10. Payments are simulated.
11. Payment processing supports success and failure.
12. Orders have a defined lifecycle.
13. Shipments can be created and tracked.
14. Customers can submit product reviews.
15. Notifications are generated asynchronously.
16. Kafka is used for appropriate business events.
17. gRPC is used for selected internal synchronous interactions.
18. Services do not directly access each other's databases.
19. Customers can access only authorized resources.
20. Administrators can perform administrative operations.
21. The primary customer checkout workflow is supported end-to-end.
22. Important failure scenarios are handled.
23. Functional behavior is testable and documented.

---

# 37. Relationship to Future Documents

This document defines the functional behavior of CommerceX.

The next documents should refine the implementation details:

```text
01 Project Charter
        |
        v
02 System Requirements
        |
        v
03 Functional Requirements       <-- This document
        |
        v
04 Non-Functional Requirements
        |
        v
05 System Architecture
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

# 38. Approval / Baseline Record

| Item | Value |
|---|---|
| Project | CommerceX |
| Document | Functional Requirements Specification |
| Document ID | COMX-DOC-003 |
| Version | 1.0 |
| Status | Initial Baseline |
| Previous Document | System Requirements |
| Next Document | Non-Functional Requirements |
| Purpose | Define detailed functional behavior of the CommerceX platform and its services |

---

**End of Document**
