# CommerceX — Redis Caching Strategy

**Document:** 12 — Redis Caching Strategy  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 11 — Kafka Event Design  
**Next Document:** 13 — Security Design

---

## 1. Purpose

This document defines how CommerceX uses Redis for fast-access data, caching, temporary state, and cart storage.

The strategy intentionally keeps Redis usage focused. Redis is not treated as a replacement for PostgreSQL, and not every service or query is cached.

The initial design uses Redis primarily for:

- Shopping cart storage.
- Cache-aside caching for selected read-heavy product and category data.
- Short-lived temporary values where appropriate.
- Performance improvements for frequently accessed data.

The design prioritizes simplicity, clear ownership, predictable invalidation, and graceful degradation.

---

## 2. Redis Role in CommerceX

CommerceX uses Redis in two distinct ways:

| Use Case | Redis Role | Primary Authority |
|---|---|---|
| Shopping cart | Primary operational store | Redis |
| Product read cache | Cache | Product PostgreSQL database |
| Category read cache | Cache | Product PostgreSQL database |
| Temporary/cache values | Short-lived store | Owning service |

The distinction is important:

- A **cart** is intentionally stored in Redis because it is frequently accessed and modified.
- A **product cache** is disposable. PostgreSQL remains the source of truth.
- Cached data may be deleted at any time without losing authoritative business data.
- Services must not assume that a cache entry will always exist.

---

## 3. Redis Ownership

Redis data must have a clear owning service.

| Redis Data | Owner |
|---|---|
| Customer cart | Cart Service |
| Product cache | Product Service |
| Category cache | Product Service |
| Other service-specific cache | Owning service |

No service should directly manipulate another service's Redis keys.

The Cart Service owns cart structure and behavior. The Product Service owns product/category caching.

---

## 4. Shopping Cart Architecture

The Cart Service uses Redis as the primary persistence mechanism for active shopping carts.

A conceptual cart contains:

```text
Cart
 ├── CustomerId
 ├── Items
 │    ├── ProductId
 │    ├── Quantity
 │    └── AddedAt
 ├── CreatedAt
 └── UpdatedAt
```

The cart should contain references to products rather than copying the complete Product entity.

For example:

```json
{
  "customerId": "customer-id",
  "items": [
    {
      "productId": "product-id",
      "quantity": 2
    }
  ],
  "updatedAt": "2026-09-13T10:00:00Z"
}
```

Product name, description, price, and availability should be obtained from the Product Service when required.

This prevents the cart from becoming a second product database.

---

## 5. Cart Key Design

Cart keys use a predictable namespace.

Recommended format:

```text
commercex:cart:{customerId}
```

Example:

```text
commercex:cart:8f5c...
```

The customer ID is the natural key because a customer normally has one active cart.

For anonymous carts, if supported later, a separate session-based key can be introduced:

```text
commercex:cart:session:{sessionId}
```

Anonymous-cart support is not required for the initial implementation.

---

## 6. Cart Operations

The Cart Service should support operations such as:

| Operation | Redis Action |
|---|---|
| Get cart | GET |
| Create cart | SET |
| Add item | Read → modify → write |
| Update quantity | Read → modify → write |
| Remove item | Read → modify → write |
| Clear cart | DELETE |
| Checkout preparation | Read cart |

For the learning project, storing the serialized cart as a Redis value is acceptable.

A more granular Redis data structure can be introduced later if profiling demonstrates a need.

---

## 7. Cart TTL

Cart expiration should prevent abandoned carts from consuming Redis memory indefinitely.

An initial TTL can be configured, for example:

```text
Cart TTL: 7 days
```

The exact value should be configuration-driven rather than hard-coded.

Possible configuration:

```json
{
  "Redis": {
    "CartTtlDays": 7
  }
}
```

The TTL may be refreshed when an active cart is modified.

Important business consideration:

**Cart expiration must not affect completed orders.**

Once an order is created, the order data is persisted by the Order Service and is independent of the Redis cart.

---

## 8. Product Cache

The Product Service may cache frequently requested product data.

Example:

```text
commercex:product:{productId}
```

Category cache:

```text
commercex:category:{categoryId}
```

List/query caches can use a separate namespace:

```text
commercex:products:list:{hash}
```

However, query-result caching should initially be limited. Caching arbitrary combinations of filters and sorting can create excessive key cardinality and invalidation complexity.

The initial implementation should prioritize individual product/category caching.

---

## 9. Cache-Aside Pattern

Product caching follows the cache-aside pattern.

### Read Flow

```text
Client
  |
  v
Product Service
  |
  v
Redis
  |
  +-- HIT --> Return cached value
  |
  +-- MISS
       |
       v
   PostgreSQL
       |
       v
   Store in Redis
       |
       v
   Return value
```

The service controls the cache and remains responsible for retrieving authoritative data from PostgreSQL.

### Write Flow

```text
Client
  |
  v
Product Service
  |
  v
PostgreSQL
  |
  v
Invalidate Redis key
```

The database update should succeed before the corresponding cache entry is invalidated.

---

## 10. Cache TTL Strategy

Caching should use TTLs appropriate to the data.

Initial recommendations:

| Data | Suggested TTL |
|---|---:|
| Product by ID | 5–15 minutes |
| Category by ID | 15–30 minutes |
| Product list/query | Avoid initially or use short TTL |
| Cart | 7 days |
| Temporary values | Application-specific |

These are starting values, not strict performance requirements.

TTL values should be configurable.

Example:

```json
{
  "Redis": {
    "ProductCacheMinutes": 10,
    "CategoryCacheMinutes": 20,
    "CartTtlDays": 7
  }
}
```

---

## 11. Cache Invalidation

Cache invalidation is required whenever authoritative data changes.

For a product update:

```text
Update PostgreSQL
      |
      v
Delete commercex:product:{productId}
```

For a category update:

```text
Update PostgreSQL
      |
      v
Delete commercex:category:{categoryId}
```

The next read will repopulate the cache.

This approach is simple and suitable for the initial system.

---

## 12. Kafka and Cache Invalidation

Kafka can also support cache synchronization when product changes need to affect other services.

For example:

```text
Product Service
     |
     +--> PostgreSQL
     |
     +--> ProductUpdated
              |
              v
         Search Service
```

The Product Service itself does not need Kafka to invalidate its own local Redis cache because it can invalidate the cache immediately after its database operation.

Other services can use Product events to update their own derived data.

Redis invalidation and Kafka event publishing should remain separate responsibilities.

---

## 13. Cache Consistency

Redis cache entries are not authoritative.

The consistency model is:

```text
PostgreSQL = Source of Truth
Redis      = Derived/Accelerated Data
```

Therefore:

- Cache misses are normal.
- Expired keys are normal.
- Cache loss must not cause permanent data loss for PostgreSQL-backed entities.
- Stale values should be minimized through invalidation and TTL.
- Business-critical decisions must not rely only on cached product data when authoritative validation is required.

For checkout, current product price and inventory availability should be validated by the appropriate authoritative services rather than trusting an old cache entry.

---

## 14. Checkout and Redis

Redis participates in checkout through the Cart Service.

A simplified flow is:

```text
Client
  |
  v
Order Service
  |
  v
Cart Service
  |
  v
Redis
```

The Order Service obtains the current cart contents and then performs the required product, promotion, inventory, and payment operations.

The cart should not be treated as the final order record.

After successful order creation:

```text
Order persisted
      |
      v
Cart cleared
```

If cart clearing fails after the order has been successfully created, the operation must not create a second order. Order creation therefore requires its own idempotency protection.

---

## 15. Serialization

For the initial implementation, JSON serialization is acceptable for Redis values.

Advantages:

- Easy to inspect during development.
- Easy to debug.
- Simple .NET integration.
- Human-readable.

Example:

```json
{
  "customerId": "123",
  "items": [
    {
      "productId": "456",
      "quantity": 2
    }
  ]
}
```

The serialized representation should be treated as an internal implementation detail.

Redis data should not be exposed directly as a public API contract.

---

## 16. Serialization Compatibility

When changing a Redis-stored object:

- Prefer backward-compatible changes.
- Treat missing fields as optional/defaultable where appropriate.
- Consider versioning if the structure becomes complex.
- Expire old incompatible values rather than attempting complicated migrations.

For example, a namespace version can be introduced:

```text
commercex:cart:v1:{customerId}
```

A version change can use:

```text
commercex:cart:v2:{customerId}
```

This is useful if a future cart structure changes significantly.

Versioned keys are not required for every minor code change.

---

## 17. Cache Stampede Protection

A cache miss can cause many requests to query PostgreSQL simultaneously.

For example:

```text
100 requests
     |
     v
Redis MISS
     |
     +--> PostgreSQL
     +--> PostgreSQL
     +--> PostgreSQL
     ...
```

For the initial implementation, basic protection is sufficient.

Recommended approaches:

1. Use short randomized TTL jitter where appropriate.
2. Avoid caching extremely short-lived values unnecessarily.
3. Use application-level request coordination only if measurements show a real stampede problem.
4. Keep database queries efficient.

The project should not introduce distributed locking solely for theoretical cache stampede prevention.

---

## 18. Redis Failure Behavior

Redis is a dependency, so failure behavior must be explicit.

### Product Cache Failure

If Redis is unavailable:

```text
Product Request
      |
      v
Redis unavailable
      |
      v
PostgreSQL
      |
      v
Return product
```

Product reads should continue using PostgreSQL where practical.

This is a major advantage of treating product Redis data as a cache.

### Cart Failure

Cart behavior is different because Redis is the primary cart store.

If Redis is unavailable:

- Cart reads/writes cannot reliably be completed.
- The service should return an appropriate temporary-service/dependency error.
- It must not silently claim that cart operations succeeded.

The application should recover when Redis becomes available again.

---

## 19. Redis Availability and Application Startup

Services using Redis should expose health information for Redis connectivity.

Recommended health categories:

- Liveness: process is running.
- Readiness: required dependencies are available according to service requirements.
- Dependency health: Redis connectivity can be monitored separately.

The Cart Service should consider Redis availability a critical dependency.

The Product Service can treat Redis as a non-critical cache dependency if PostgreSQL remains available.

---

## 20. Redis Connection Management

Each service should use a managed Redis client connection rather than creating a new connection for every request.

For .NET, the application can use:

```text
StackExchange.Redis
```

A shared connection/multiplexer should be registered through dependency injection.

Conceptually:

```text
Application
    |
    v
Redis abstraction
    |
    v
StackExchange.Redis
    |
    v
Redis
```

Application code should depend on an abstraction where that improves testability and separation.

---

## 21. Configuration

Redis connection information must be configuration-driven.

Example:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "commercex:"
  }
}
```

Production credentials or sensitive connection details must not be committed to source control.

Recommended configuration sources:

- Local development configuration.
- Environment variables.
- Kubernetes ConfigMaps for non-sensitive settings.
- Kubernetes Secrets for sensitive settings.

---

## 22. Docker Architecture

Local development should run Redis as an infrastructure container.

Conceptual architecture:

```text
+-----------------------+
| CommerceX Application  |
+-----------+-----------+
            |
            v
+-----------------------+
| Redis Container       |
| Port: 6379            |
+-----------------------+
```

Redis should be defined in the infrastructure Docker Compose configuration used for local development.

The application should connect using the Docker service name when both components run inside the same Compose network.

Example:

```text
redis:6379
```

When the application runs directly on the host, the local mapped port can be used instead.

---

## 23. Kubernetes Architecture

In Minikube, Redis should be deployed as an infrastructure dependency.

Conceptually:

```text
Cart Service
     |
     v
commercex-redis Service
     |
     v
Redis Pod
```

Applications should use Kubernetes DNS rather than hard-coded Pod IP addresses.

Example conceptual connection:

```text
commercex-redis:6379
```

The exact Kubernetes resource names should be established consistently in the Kubernetes design document.

---

## 24. Redis Persistence

For the initial CommerceX learning environment, Redis persistence is not the primary durability mechanism.

### Product Cache

No durability requirement exists because PostgreSQL is authoritative.

### Cart

Cart persistence is more important because Redis is the primary cart store.

The deployment should therefore use an appropriate Redis persistence configuration for the selected environment.

However, Redis persistence should not be confused with order durability.

Orders, payments, inventory reservations, and other critical business records remain PostgreSQL responsibilities.

---

## 25. Memory Management

Redis is memory-oriented, so key growth must be controlled.

Controls include:

- TTLs for temporary/cache data.
- Cart expiration.
- Avoiding unnecessary large values.
- Avoiding unlimited query-result caching.
- Monitoring Redis memory usage.
- Using appropriate eviction behavior for cache-only data.

The system should distinguish between:

```text
Cache data
```

and:

```text
Primary operational data
```

An eviction policy suitable for cache data must not be selected without considering the fact that carts are stored primarily in Redis.

---

## 26. Key Naming Convention

CommerceX Redis keys should follow a consistent namespace.

Recommended:

```text
commercex:{domain}:{resource}:{identifier}
```

Examples:

```text
commercex:cart:{customerId}
commercex:product:{productId}
commercex:category:{categoryId}
```

Versioned key example:

```text
commercex:cart:v2:{customerId}
```

Avoid ambiguous keys such as:

```text
cart1
product123
data:456
```

A consistent namespace makes debugging, monitoring, and future multi-application Redis usage easier.

---

## 27. Key Ownership Matrix

| Key Pattern | Owner | Data Type | Source of Truth |
|---|---|---|---|
| `commercex:cart:{customerId}` | Cart | Serialized cart | Redis |
| `commercex:product:{productId}` | Product | Serialized product | PostgreSQL |
| `commercex:category:{categoryId}` | Product | Serialized category | PostgreSQL |
| `commercex:products:list:{hash}` | Product | Cached query result | PostgreSQL |

The list/query key is optional and should not be implemented until there is a clear performance reason.

---

## 28. Security Considerations

Redis must not be treated as a public-facing endpoint.

Requirements:

- Do not expose Redis directly to external clients.
- Restrict network access.
- Use authentication where appropriate.
- Protect Redis credentials.
- Do not store plaintext passwords or authentication secrets as normal cache values.
- Avoid storing unnecessary personal information.
- Do not log full cart contents unnecessarily.
- Use TLS for Redis connections where required by the deployment environment.

Redis access should be limited to the services that need it.

---

## 29. Personal Data Considerations

Carts may contain customer-related information.

The initial cart representation should therefore minimize personal data.

Prefer:

```text
CustomerId
ProductId
Quantity
timestamps
```

Avoid storing unnecessary:

- Passwords.
- Access tokens.
- Payment card information.
- Sensitive authentication data.
- Full customer profile information.

Customer identity and authentication remain responsibilities of Auth/User services.

---

## 30. Observability

Redis usage should be observable.

Important metrics include:

- Cache hits.
- Cache misses.
- Hit ratio.
- Redis command latency.
- Connection failures.
- Connection count.
- Memory usage.
- Key expiration rate.
- Cart operation latency.
- Redis availability.

Example derived metric:

```text
Cache Hit Ratio =
Cache Hits / (Cache Hits + Cache Misses)
```

A low hit ratio may indicate:

- TTL is too short.
- Cache keys are poorly designed.
- Data is not reused.
- Invalidations occur too frequently.
- Caching is not useful for the selected query.

---

## 31. Logging

Redis-related logs should focus on operational information.

Useful logs:

```text
Product cache miss for productId={id}
Product cache invalidated for productId={id}
Redis connection failure
Cart loaded for customerId={id}
Cart updated for customerId={id}
```

Do not log:

- Passwords.
- Access tokens.
- Payment credentials.
- Complete sensitive customer data.

Correlation IDs should be included when Redis operations participate in a distributed request.

---

## 32. Distributed Tracing

Redis calls should participate in OpenTelemetry tracing where supported by the application's instrumentation.

Example:

```text
HTTP Request
   |
   v
Product Service
   |
   +--> Redis GET
   |
   +--> PostgreSQL SELECT
```

Tracing should make it possible to determine whether latency comes from:

- API processing.
- Redis.
- PostgreSQL.
- Another downstream dependency.

---

## 33. Cache Warming

The initial system should not require complex cache warming.

Caches should normally populate lazily:

```text
First request
    |
    v
Cache miss
    |
    v
PostgreSQL
    |
    v
Redis
```

Optional preloading can be introduced later if profiling shows that certain product/category data is frequently requested immediately after deployment.

---

## 34. Cache Invalidation Failure

A possible sequence is:

```text
PostgreSQL UPDATE succeeds
        |
        X
Redis DELETE fails
```

This can temporarily leave stale cache data.

Mitigations include:

- Short TTLs.
- Retryable invalidation.
- Explicit cache invalidation on subsequent updates.
- Event-driven invalidation where justified.
- Monitoring invalidation failures.

For the initial implementation, short TTL + retry/logging is sufficient.

The system should not introduce distributed transactions between PostgreSQL and Redis.

---

## 35. Redis and Transaction Boundaries

Redis and PostgreSQL should not be treated as a single ACID transaction.

Avoid designs such as:

```text
BEGIN PostgreSQL transaction
    |
    +--> UPDATE database
    +--> UPDATE Redis
COMMIT
```

Instead:

```text
PostgreSQL transaction
       |
       v
Commit authoritative state
       |
       v
Cache invalidation/update
```

If cache synchronization fails, the system should rely on TTL, retry, and subsequent cache refresh.

---

## 36. Testing Strategy

Redis behavior should be tested at multiple levels.

### Unit Tests

Test:

- Key generation.
- Serialization/deserialization.
- TTL calculation.
- Cache hit behavior.
- Cache miss behavior.
- Invalidation behavior.
- Cart business rules.

### Integration Tests

Test against a real Redis instance:

- Create cart.
- Retrieve cart.
- Add/update/remove items.
- Clear cart.
- Verify TTL.
- Verify product cache population.
- Verify invalidation.

### Failure Tests

Test:

- Redis unavailable.
- Redis timeout.
- Invalid cached data.
- Expired key.
- Cache miss.
- Repeated requests after cache invalidation.

---

## 37. Testcontainers

For integration testing, a containerized Redis instance can be used.

Conceptually:

```text
xUnit
  |
  v
Redis Test Container
```

This avoids depending on a developer's local Redis installation.

The exact test-container implementation can be introduced when the testing infrastructure is established.

---

## 38. Performance Expectations

Redis should reduce latency for repeated read-heavy operations.

Expected pattern:

```text
First product request:
API → Redis MISS → PostgreSQL → Redis → API

Repeated request:
API → Redis HIT → API
```

Performance targets should be validated through measurements rather than assumptions.

The project should track:

- Redis GET latency.
- PostgreSQL query latency.
- End-to-end API latency.
- Cache hit ratio.

---

## 39. Scaling

For the initial learning environment:

```text
One Redis instance
```

is sufficient.

The architecture should keep Redis access behind service boundaries so that more advanced deployments can be introduced later.

Possible future evolution:

```text
Redis replication
Redis Sentinel
Redis Cluster
Managed Redis
```

These are not required for the initial CommerceX implementation.

---

## 40. Failure Isolation

Redis failures should affect services differently according to Redis's role.

| Service | Redis Role | Failure Impact |
|---|---|---|
| Cart | Primary store | Cart operations temporarily unavailable |
| Product | Cache | Fall back to PostgreSQL |
| Other services | Optional cache | Prefer authoritative datastore |

This distinction is important for graceful degradation.

---

## 41. Anti-Patterns to Avoid

CommerceX should avoid:

### 41.1 Using Redis as a universal database

Do not move all service data into Redis.

### 41.2 Caching everything

Only cache data with a meaningful performance benefit.

### 41.3 Direct cross-service Redis access

A service must not read another service's Redis keys.

### 41.4 Storing authoritative order/payment data only in Redis

Critical business records belong in PostgreSQL.

### 41.5 No TTLs

Temporary/cache values should have controlled lifetimes.

### 41.6 Caching sensitive authentication data unnecessarily

Avoid using Redis as a general secret store.

### 41.7 Complex distributed locking without a demonstrated need

Do not introduce RedLock or similar mechanisms simply because Redis supports them.

### 41.8 Hard-coded connection strings

Use configuration and environment-specific settings.

### 41.9 Treating cache hits as guaranteed

Every cache access must be prepared for a miss or failure.

### 41.10 Large unbounded query caches

Avoid generating thousands of unique keys from arbitrary filters.

---

## 42. Recommended .NET Abstraction

Where useful, the application layer should depend on an abstraction rather than directly depending on Redis implementation details.

For example:

```csharp
public interface ICartStore
{
    Task<Cart?> GetAsync(Guid customerId, CancellationToken cancellationToken);
    Task SaveAsync(
        Guid customerId,
        Cart cart,
        TimeSpan ttl,
        CancellationToken cancellationToken);
    Task DeleteAsync(
        Guid customerId,
        CancellationToken cancellationToken);
}
```

The Infrastructure layer can implement this interface using StackExchange.Redis.

For product caching:

```csharp
public interface IProductCache
{
    Task<ProductDto?> GetAsync(
        Guid productId,
        CancellationToken cancellationToken);

    Task SetAsync(
        Guid productId,
        ProductDto product,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid productId,
        CancellationToken cancellationToken);
}
```

The exact interfaces can evolve during implementation.

---

## 43. Layering

Redis implementation should remain in Infrastructure.

Recommended service structure:

```text
API
 |
Application
 |
Domain
 |
Infrastructure
    |
    +-- Redis
```

Application logic should not depend directly on:

```text
StackExchange.Redis
```

unless there is a specific architectural reason.

This maintains separation between business logic and infrastructure technology.

---

## 44. Initial Implementation Scope

The first implementation should be deliberately small.

### Implement Now

- Redis Docker infrastructure.
- Redis connection configuration.
- Cart storage.
- Cart key convention.
- Cart TTL.
- Product cache by ID.
- Category cache by ID.
- Cache-aside reads.
- Cache invalidation after product/category updates.
- Basic Redis health checks.
- Redis logging/metrics.
- Integration tests.

### Defer

- Redis Cluster.
- Sentinel.
- Distributed locks.
- Advanced cache stampede coordination.
- Complex query-result caching.
- Distributed session storage.
- Full Redis Streams architecture.
- Lua-heavy business logic.
- Advanced eviction strategies.
- Multi-region Redis.

---

## 45. Implementation Checklist

### Infrastructure

- [ ] Add Redis to Docker Compose.
- [ ] Verify Redis connectivity.
- [ ] Add Redis configuration.
- [ ] Add Kubernetes Redis deployment/service later.

### Cart Service

- [ ] Define cart model.
- [ ] Define `ICartStore`.
- [ ] Implement Redis cart store.
- [ ] Implement key convention.
- [ ] Implement TTL.
- [ ] Implement CRUD operations.
- [ ] Validate quantities.
- [ ] Handle Redis failures.

### Product Service

- [ ] Define `IProductCache`.
- [ ] Implement cache-aside reads.
- [ ] Cache product by ID.
- [ ] Cache category by ID.
- [ ] Invalidate cache on updates.
- [ ] Invalidate cache on deactivation.

### Observability

- [ ] Track cache hits/misses.
- [ ] Track Redis errors.
- [ ] Track Redis latency.
- [ ] Monitor Redis memory.
- [ ] Add tracing.

### Testing

- [ ] Unit test key generation.
- [ ] Unit test cache behavior.
- [ ] Integration test Redis.
- [ ] Test TTL expiration.
- [ ] Test invalidation.
- [ ] Test Redis failure.
- [ ] Test cart lifecycle.

---

## 46. Redis Architecture Summary

The final initial Redis architecture is:

```text
                    +----------------------+
                    |       Redis          |
                    +----------+-----------+
                               |
                +--------------+--------------+
                |                             |
                v                             v
       +----------------+             +----------------+
       |  Cart Service  |             | Product Service|
       +-------+--------+             +--------+-------+
               |                               |
               | Cart                          | Cache
               v                               v
        commercex:cart:*                commercex:product:*
                                         commercex:category:*
                                                |
                                                v
                                      +------------------+
                                      |    PostgreSQL    |
                                      | Product DB       |
                                      +------------------+
```

The Cart Service treats Redis as its primary cart store.

The Product Service treats Redis as a disposable cache over PostgreSQL.

---

## 47. Architectural Decisions

The following decisions are established for the CommerceX baseline:

1. Redis is used selectively rather than universally.
2. Cart data is primarily stored in Redis.
3. Product/category caching uses the cache-aside pattern.
4. PostgreSQL remains authoritative for product/catalog data.
5. Redis keys use a CommerceX namespace.
6. Cart data has a configurable TTL.
7. Product/category cache entries have configurable TTLs.
8. Product/category cache entries are invalidated after successful writes.
9. Redis failure should not normally prevent product reads when PostgreSQL is available.
10. Redis failure prevents reliable cart operations because Redis is the cart's primary store.
11. Services must not directly access another service's Redis keys.
12. JSON is acceptable for initial Redis serialization.
13. Redis is not used as the authoritative store for orders, payments, inventory, or other durable business records.
14. Advanced Redis clustering and distributed locking are deferred.
15. Redis observability is part of the platform observability design.

---

## 48. Relationship With Other Documents

This document depends on:

- Project Charter
- System Requirements
- Functional Requirements
- Non-Functional Requirements
- System Architecture
- Microservices Architecture
- Service Boundaries
- Data Architecture
- API Design
- gRPC Design
- Kafka Event Design

It provides Redis requirements for future:

- Security Design
- Docker Design
- Kubernetes Design
- Observability Design
- Testing Strategy
- CI/CD Design

---

## 49. Baseline Status

This document establishes the **CommerceX Redis Caching Strategy baseline**.

Any future implementation should follow these decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 13 — Security Design
