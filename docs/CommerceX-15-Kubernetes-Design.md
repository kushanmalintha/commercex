# CommerceX — Kubernetes Design

**Document:** 15 — Kubernetes Design  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 14 — Docker Design  
**Next Document:** 16 — Observability Design

---

## 1. Purpose

This document defines the Kubernetes architecture for deploying and operating CommerceX.

CommerceX uses Kubernetes to demonstrate:

- Container orchestration.
- Service discovery.
- Independent service deployment.
- Horizontal scaling.
- Health checks.
- Configuration management.
- Secret management.
- Rolling updates.
- Resource management.
- Failure recovery.
- Internal networking.

The initial Kubernetes environment is **Minikube**.

The architecture should remain simple enough for local learning while following patterns that map reasonably well to production Kubernetes environments.

---

## 2. Kubernetes Objectives

The CommerceX Kubernetes design should allow the project to:

1. Deploy all application services independently.
2. Deploy required infrastructure components.
3. Provide Kubernetes service discovery.
4. Expose the API Gateway externally.
5. Keep databases and infrastructure internal.
6. Inject configuration at runtime.
7. Manage secrets separately from application images.
8. Perform health and readiness checks.
9. Restart failed application containers.
10. Support horizontal scaling for stateless services.
11. Perform rolling deployments.
12. Support local development through Minikube.
13. Provide a foundation for CI/CD.
14. Integrate with observability tooling.

---

## 3. Kubernetes Scope

The initial Kubernetes environment contains:

### Application Workloads

```text
API Gateway
Auth Service
User Service
Product Service
Inventory Service
Cart Service
Order Service
Payment Service
Shipping Service
Review Service
Notification Service
Promotion Service
Search Service
```

### Infrastructure

```text
PostgreSQL
Redis
Kafka
Schema Registry
```

Additional infrastructure can be introduced later if required.

---

## 4. High-Level Kubernetes Architecture

```text
                         Internet / Host
                               |
                               v
                       +---------------+
                       | API Gateway    |
                       | Service       |
                       +-------+-------+
                               |
                    Kubernetes Cluster
                               |
       +-----------------------+-----------------------+
       |                       |                       |
       v                       v                       v
+-------------+        +-------------+        +-------------+
| Auth        |        | Product     |        | Order       |
| Deployment  |        | Deployment  |        | Deployment  |
+------+------+        +------+------+        +------+------+
       |                       |                       |
       v                       v                       v
   Auth DB                Product DB               Order DB

       +-----------------------------------------------+
       | Redis | Kafka | Schema Registry | Other DBs   |
       +-----------------------------------------------+
```

Kubernetes provides the runtime and networking layer; individual services retain business ownership.

---

## 5. Minikube as Initial Cluster

The initial development cluster is Minikube.

Example:

```bash
minikube start
```

The exact CPU and memory allocation depends on the developer workstation.

Minikube is used because it provides a realistic Kubernetes environment without requiring a cloud provider.

---

## 6. Kubernetes Namespace

CommerceX should run inside a dedicated namespace.

Recommended:

```text
commercex
```

Conceptually:

```text
Kubernetes Cluster
        |
        +-- commercex namespace
              |
              +-- Gateway
              +-- Services
              +-- Infrastructure
```

This makes resource management and cleanup easier.

---

## 7. Namespace Isolation

The namespace provides logical isolation from unrelated workloads.

It can later be used for:

- Resource quotas.
- Network policies.
- Role-based access.
- Environment-specific resources.

The initial project does not require multiple namespaces for every service.

---

## 8. Deployment Model

Each stateless application service should have its own Kubernetes Deployment.

Example:

```text
auth-deployment
user-deployment
product-deployment
inventory-deployment
cart-deployment
order-deployment
payment-deployment
shipping-deployment
review-deployment
notification-deployment
promotion-deployment
search-deployment
gateway-deployment
```

A Deployment manages ReplicaSets and Pods.

---

## 9. Pod Model

The preferred initial model is:

```text
1 application container
        |
        v
1 Pod
```

For example:

```text
Order Deployment
       |
       +--> Order Pod
       +--> Order Pod
       +--> Order Pod
```

Sidecar containers are not required initially.

This keeps the architecture easy to understand.

---

## 10. Kubernetes Services

Each application requiring network access should have a Kubernetes Service.

Example:

```text
order-service
product-service
inventory-service
payment-service
```

The Service provides stable networking while Pods may be replaced.

---

## 11. Kubernetes DNS

Services should communicate using Kubernetes DNS.

Example:

```text
http://product-service:8080
```

or:

```text
http://product-service.commercex.svc.cluster.local:8080
```

The short service name is normally sufficient inside the same namespace.

Never use Pod IP addresses for service-to-service communication.

---

## 12. API Gateway Exposure

The API Gateway is the primary external entry point.

For Minikube, possible exposure methods include:

- NodePort.
- LoadBalancer through Minikube.
- Ingress.

The preferred architecture is to use an Ingress when the project reaches the stage where routing and host-based access need to be demonstrated.

A simpler NodePort can be used during early development.

---

## 13. Gateway Traffic Flow

```text
External Client
      |
      v
Ingress / External Service
      |
      v
API Gateway
      |
      +--> Auth
      +--> User
      +--> Product
      +--> Search
      +--> Cart
      +--> Order
      +--> Review
      +--> Shipping
      ...
```

The Gateway remains the normal client-facing entry point.

---

## 14. Internal Services

Backend services should normally use:

```text
ClusterIP
```

Services.

This means they are accessible inside the Kubernetes cluster but are not directly exposed to the external network.

Examples:

```text
auth-service       ClusterIP
product-service    ClusterIP
order-service      ClusterIP
inventory-service  ClusterIP
```

---

## 15. Infrastructure Services

Infrastructure components should also normally remain internal.

Examples:

```text
postgres-service
redis-service
kafka-service
schema-registry-service
```

External exposure should only be enabled when required for local development/debugging.

---

## 16. Application Deployment Configuration

A Deployment should define:

- Container image.
- Replica count.
- Container ports.
- Environment variables.
- ConfigMap references.
- Secret references.
- Resource requests.
- Resource limits.
- Liveness probe.
- Readiness probe.
- Security context.
- Image pull policy.

Conceptually:

```yaml
Deployment
  |
  +-- replicas
  +-- image
  +-- ports
  +-- env
  +-- resources
  +-- probes
  +-- securityContext
```

---

## 17. Replica Strategy

Initial replica counts can be modest.

Example:

```text
Gateway      1
Auth         1
User         1
Product      1
Inventory    1
Cart         1
Order        1
Payment      1
Shipping     1
Review       1
Notification 1
Promotion    1
Search       1
```

This is appropriate for Minikube resource constraints.

Stateless services can later scale to:

```text
replicas: 2
```

or more.

---

## 18. Horizontal Scaling

Stateless API services should support horizontal scaling.

Example:

```text
Order Deployment
      |
      +--> Order Pod 1
      +--> Order Pod 2
      +--> Order Pod 3
```

The Kubernetes Service distributes traffic among available Pods.

The application must not depend on local in-memory session state.

---

## 19. Kafka Consumer Scaling

Kafka consumers scale differently.

Example:

```text
Notification Consumer Group
        |
        +--> Notification Pod 1
        +--> Notification Pod 2
        +--> Notification Pod 3
```

Kafka partitions determine how much parallel consumption is possible.

Multiple replicas in the same consumer group share partitions.

Consumer groups should be designed according to the Kafka Event Design.

---

## 20. Cart Service Scaling

Cart Service uses Redis as the primary cart store.

Therefore, Cart Pods should remain stateless:

```text
Cart Pod 1
    |
    +--> Redis

Cart Pod 2
    |
    +--> Redis
```

No cart should be stored only in Pod memory.

This allows multiple Cart replicas to access the same customer cart.

---

## 21. Database Deployment

For the initial learning environment, PostgreSQL can run inside Kubernetes.

A simple model is:

```text
PostgreSQL Deployment/StatefulSet
        |
        v
Persistent Volume
```

However, database workloads require stronger persistence considerations than stateless services.

For the learning environment, a single PostgreSQL instance is acceptable.

---

## 22. PostgreSQL Database Ownership

The same PostgreSQL server may contain separate logical databases:

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

This preserves the logical database-per-service model.

Services still must not access other services' databases.

---

## 23. PostgreSQL Stateful Workload

PostgreSQL should preferably use a StatefulSet when its Kubernetes lifecycle is explicitly managed inside the cluster.

Conceptually:

```text
StatefulSet
    |
    v
PostgreSQL Pod
    |
    v
PersistentVolumeClaim
    |
    v
PersistentVolume
```

For a learning environment, a Deployment with persistent storage can also work, but StatefulSet better represents the stateful nature of the workload.

---

## 24. Redis Deployment

Redis can initially run as a single instance.

Conceptually:

```text
redis-service
      |
      v
Redis Pod
      |
      v
Persistent storage if configured
```

High-availability Redis is outside the initial scope.

The Cart Service depends on Redis as a primary cart store.

---

## 25. Kafka Deployment

Kafka is infrastructure and should remain internal.

Initial Minikube deployment can use a single broker.

Conceptually:

```text
kafka-service
      |
      v
Kafka Pod
```

The configuration must correctly advertise a hostname reachable by Kafka clients inside the cluster.

Kafka's listener configuration is particularly important in Kubernetes.

---

## 26. Schema Registry

Schema Registry should run as a separate deployment/service.

```text
schema-registry-service
          |
          v
Schema Registry Pod
          |
          v
Kafka
```

It remains an internal infrastructure dependency.

---

## 27. ConfigMaps

Non-sensitive configuration should use ConfigMaps.

Examples:

```text
Kafka bootstrap server
Redis hostname
Database hostname
Service ports
Feature flags
Application environment
```

Example conceptual values:

```text
REDIS_HOST=redis-service
KAFKA_BOOTSTRAP_SERVERS=kafka-service:9092
```

---

## 28. Secrets

Sensitive values should use Kubernetes Secrets.

Examples:

```text
Database passwords
JWT signing key
Kafka credentials
Redis credentials
Internal service credentials
```

Application images must not contain these values.

---

## 29. Configuration Separation

A useful model is:

```text
Container Image
      |
      +--> Application binaries
      |
      v
Kubernetes Configuration
      |
      +--> ConfigMap
      +--> Secret
```

The same image can therefore be deployed with different configurations.

---

## 30. Environment Variables

Kubernetes can inject configuration as environment variables.

Example:

```yaml
env:
  - name: Redis__ConnectionString
    valueFrom:
      configMapKeyRef:
        name: commercex-config
        key: redis-connection
```

Sensitive values should use:

```yaml
secretKeyRef
```

instead.

---

## 31. Connection Strings

Application connection strings should use Kubernetes service names.

Examples:

```text
postgres-service:5432
redis-service:6379
kafka-service:9092
```

Do not use:

```text
localhost
```

for infrastructure located in other Pods.

---

## 32. Service-to-Service REST

For internal REST communication:

```text
Order Service
      |
      v
http://product-service:8080
```

The application configuration should define the internal service address rather than hard-coding it in source code.

---

## 33. Service-to-Service gRPC

For gRPC:

```text
Order Service
      |
      v
inventory-service:5001
```

The port should be defined in the Kubernetes Service.

The gRPC server must listen on an address reachable from other Pods.

---

## 34. Kafka Service Discovery

Kafka clients should connect using the Kubernetes service DNS name.

Example:

```text
kafka-service:9092
```

The broker's advertised listeners must be configured consistently.

A common failure is:

```text
Client connects to Kafka Service
        |
        v
Kafka returns unreachable advertised address
        |
        X
Client fails
```

Kafka networking must therefore be tested explicitly.

---

## 35. Health Probes

CommerceX should use three concepts where appropriate:

### Liveness

Determines whether the process should be restarted.

```text
/health/live
```

### Readiness

Determines whether the Pod should receive traffic.

```text
/health/ready
```

### Startup

Provides additional time for slow-starting services.

The exact probe combination depends on the service.

---

## 36. Liveness Probe

Liveness should answer:

> Is the application process functioning sufficiently that Kubernetes should keep it running?

A failed liveness probe may cause Kubernetes to restart the Pod.

Liveness should not depend on every external dependency.

For example, a temporary database outage should not automatically cause an endless application restart loop.

---

## 37. Readiness Probe

Readiness answers:

> Can this Pod safely receive traffic?

A service may be running but not ready.

Example:

```text
Order Pod
   |
   +--> Process running
   +--> PostgreSQL unavailable
   |
   v
Not Ready
```

Traffic should not be sent to an unavailable Pod.

---

## 38. Dependency Readiness

Readiness requirements depend on the service.

Examples:

### Product Service

```text
PostgreSQL = required
Redis = optional cache
```

Redis failure should not necessarily make Product unavailable.

### Cart Service

```text
Redis = required
```

Redis failure should make Cart unavailable for operations that require the cart store.

This follows the Redis Caching Strategy.

---

## 39. Resource Requests

Each container should define CPU and memory requests.

Example conceptual configuration:

```yaml
resources:
  requests:
    cpu: "100m"
    memory: "128Mi"
```

Actual values should be tuned using measurements and Minikube capacity.

Requests influence scheduling.

---

## 40. Resource Limits

Containers should also have reasonable limits.

Example:

```yaml
resources:
  limits:
    cpu: "500m"
    memory: "512Mi"
```

These values are illustrative and should not be treated as final production sizing.

---

## 41. Resource Quotas

A namespace-level ResourceQuota can prevent one workload from consuming all cluster resources.

For example:

```text
commercex namespace
       |
       +--> CPU quota
       +--> Memory quota
       +--> Pod quota
```

This is useful when running many services in Minikube.

---

## 42. Security Context

Application Pods should use secure container settings where practical.

Examples:

```text
runAsNonRoot: true
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true where supported
```

Applications should request only the Linux capabilities they require.

This follows the Security Design.

---

## 43. Service Accounts

Services should not automatically receive broad Kubernetes API access.

Where a service does not need Kubernetes API access:

```text
automountServiceAccountToken = false
```

can be considered.

If a service needs Kubernetes API access in the future, a dedicated least-privilege ServiceAccount and RBAC policy should be created.

---

## 44. Network Policies

Kubernetes NetworkPolicies can restrict traffic between workloads.

Conceptual policy:

```text
Gateway
  |
  +--> Backend Services

Order
  |
  +--> Inventory
  +--> Promotion
  +--> Payment

Services
  |
  +--> Kafka

Cart/Product
  |
  +--> Redis
```

A service should not automatically have unrestricted network access.

Network policies can be introduced progressively after the service communication matrix is stable.

---

## 45. Database Network Policy

Databases should accept traffic only from required services.

For example:

```text
commercex_orders
       ^
       |
Order Service
```

rather than:

```text
Every Pod
       |
       v
PostgreSQL
```

This reinforces database ownership.

---

## 46. Redis Network Policy

Redis access should be limited to the services that use it.

Initial expected consumers:

```text
Cart Service
Product Service
```

Other services should not automatically have Redis access.

---

## 47. Kafka Network Policy

Kafka access should be limited to services that produce or consume events.

A service that has no Kafka responsibility should not require network access to Kafka.

This supports least privilege.

---

## 48. Ingress

Ingress provides a single HTTP entry point into the cluster.

Conceptually:

```text
Client
  |
  v
Ingress
  |
  v
Gateway
```

The Gateway remains the application's API boundary.

Ingress should not replace the Gateway's business/API responsibilities.

---

## 49. Ingress vs Gateway

The two components have different responsibilities.

| Component | Responsibility |
|---|---|
| Ingress | Kubernetes-level external routing |
| API Gateway | Application/API routing and policies |

Example:

```text
Internet
   |
   v
Ingress
   |
   v
API Gateway
   |
   +--> Auth
   +--> Product
   +--> Order
```

The Gateway remains responsible for API-level concerns such as authentication validation and rate limiting.

---

## 50. Rolling Updates

Deployments should use rolling updates.

Conceptually:

```text
Version 1
Pod 1
Pod 2

      |
      v

Version 1 + Version 2
Pod 1
Pod 2(new)

      |
      v

Version 2
Pod 1
Pod 2
```

Readiness probes help ensure that new Pods receive traffic only after becoming ready.

---

## 51. Deployment Strategy

A basic rolling update configuration can use:

```text
maxUnavailable: 0
maxSurge: 1
```

for critical services where local resource capacity allows it.

The exact settings should be adjusted for Minikube resource constraints.

---

## 52. Rollback

Kubernetes Deployments support rollback to previous revisions.

Example:

```text
Version 5
   |
   X problem
   |
   v
Rollback
   |
   v
Version 4
```

Image tags should be traceable so the previous known-good version can be identified.

---

## 53. Graceful Shutdown

Kubernetes sends termination signals before forcefully stopping a container.

Applications should:

- Stop accepting new work.
- Finish/cancel active requests appropriately.
- Stop Kafka consumers gracefully.
- Dispose database/Redis connections.
- Complete required cleanup.

This is especially important for Kafka consumers.

---

## 54. Pod Disruption

Application services should tolerate Pod replacement.

For stateless services:

```text
Pod disappears
    |
    v
Deployment creates replacement
    |
    v
Service routes traffic to ready Pods
```

This is one of the primary benefits of Kubernetes.

---

## 55. Persistent Storage

Stateful infrastructure requires persistent storage.

Initial components:

```text
PostgreSQL
Redis where persistence is required
Kafka where persistence is required
```

Kubernetes PersistentVolumeClaims should be used rather than depending on Pod-local filesystem state.

---

## 56. StorageClass

Minikube normally provides a default storage class.

The initial project can use the default StorageClass for local persistent volumes.

Cloud-specific storage configuration is outside the initial scope.

---

## 57. PostgreSQL Backup Considerations

Kubernetes persistent volumes do not automatically constitute a complete backup strategy.

The project should distinguish:

```text
Persistent storage
```

from:

```text
Backup
```

A later operational design can introduce:

- `pg_dump`.
- Scheduled backups.
- Backup storage.
- Restore testing.

For the learning environment, backup/restore can be demonstrated manually.

---

## 58. Redis Persistence in Kubernetes

Redis is primarily used for:

- Cart storage.
- Product/category cache.

The persistence requirement differs by use case.

Product cache can be rebuilt.

Cart data has greater operational value and may justify Redis persistence.

The selected Redis configuration should reflect this distinction.

---

## 59. Kafka Persistence in Kubernetes

Kafka requires persistent storage for broker data if events should survive Pod replacement.

For the single-broker learning deployment:

```text
Kafka Pod
   |
   v
PVC
```

is sufficient as an initial model.

High availability and multi-broker replication are deferred.

---

## 60. Infrastructure Startup

Infrastructure should become ready before dependent services attempt normal operation.

However, services must also implement retry/reconnection behavior.

Kubernetes startup order should not be treated as a transaction.

Example:

```text
Kafka Pod starts
      |
      v
Kafka initializing
      |
      v
Order Pod starts
      |
      +--> Retry
      |
      v
Kafka ready
      |
      v
Order operates normally
```

---

## 61. Dependency Failure

Kubernetes should not automatically restart every service merely because a downstream dependency is temporarily unavailable.

Example:

```text
Redis unavailable
       |
       +--> Product Service
       |      |
       |      +--> Fall back to PostgreSQL
       |
       +--> Cart Service
              |
              +--> Not Ready / dependency error
```

This matches service-specific resilience requirements.

---

## 62. Kubernetes and Eventual Consistency

Kubernetes does not change CommerceX's data consistency model.

The architecture remains:

```text
Local service transaction
        =
Strong consistency

Cross-service event propagation
        =
Eventual consistency
```

Kubernetes only provides the runtime environment.

---

## 63. Kubernetes and Service Discovery

Service discovery should be dynamic.

Example:

```text
Order Service
    |
    v
inventory-service
```

If the Inventory Pod changes from:

```text
10.0.0.21
```

to:

```text
10.0.0.45
```

the Order Service should not need configuration changes.

The Kubernetes Service provides stable discovery.

---

## 64. Kubernetes Labels

Resources should use consistent labels.

Example:

```text
app: commercex
service: order
component: api
```

Useful labels support:

- Resource selection.
- Monitoring.
- Debugging.
- Policy application.
- Deployment management.

---

## 65. Kubernetes Resource Organization

A possible repository structure is:

```text
infra/
└── kubernetes/
    ├── namespace.yaml
    ├── config/
    │   ├── configmap.yaml
    │   └── secrets.example.yaml
    ├── gateway/
    ├── services/
    │   ├── auth/
    │   ├── user/
    │   ├── product/
    │   ├── inventory/
    │   ├── cart/
    │   ├── order/
    │   ├── payment/
    │   ├── shipping/
    │   ├── review/
    │   ├── notification/
    │   ├── promotion/
    │   └── search/
    └── infrastructure/
        ├── postgres/
        ├── redis/
        ├── kafka/
        └── schema-registry/
```

The exact structure can be refined with Kustomize.

---

## 66. Kustomize

Kustomize can be used to manage Kubernetes configuration without introducing a separate templating system.

Potential structure:

```text
infra/kubernetes/
├── base/
└── overlays/
    ├── minikube/
    └── development/
```

For the initial implementation, plain manifests are acceptable.

Kustomize should be introduced when environment variation becomes meaningful.

---

## 67. Helm

Helm is not required for the initial CommerceX implementation.

A Helm chart can be introduced later if:

- Deployment configuration becomes repetitive.
- Multiple environments require parameterization.
- The project wants to demonstrate Helm.

The initial goal is to understand native Kubernetes resources first.

---

## 68. ConfigMap and Secret Naming

Consistent naming is recommended.

Examples:

```text
commercex-config
commercex-secrets
```

Service-specific configuration can use:

```text
commercex-order-config
commercex-order-secrets
```

The final approach should balance reuse with least privilege.

---

## 69. Secret Scope

A service should receive only the secrets it requires.

For example:

```text
Order Service
   |
   +--> Order DB password
   +--> Required internal service credential
```

It should not receive:

```text
Auth DB password
Payment DB password
Redis credential
```

unless explicitly required.

---

## 70. API Gateway Configuration

The Gateway requires:

- Backend service addresses.
- Authentication configuration.
- Rate-limit configuration.
- Gateway port.
- Observability configuration.

Backend service addresses should reference Kubernetes Services.

Example conceptual routing:

```text
/api/v1/auth/*       -> auth-service
/api/v1/products/*  -> product-service
/api/v1/orders/*    -> order-service
/api/v1/cart/*      -> cart-service
```

---

## 71. gRPC Service Ports

Internal gRPC services should expose dedicated ports through ClusterIP Services.

Example:

```text
inventory-service:5001
promotion-service:5002
payment-service:5003
```

The exact port allocation should be finalized during implementation.

HTTP and gRPC endpoints can be exposed through the same Pod using separate container ports where appropriate.

---

## 72. REST Service Ports

Application services should expose their HTTP port consistently.

For example:

```text
8080
```

for containerized HTTP APIs.

Consistency reduces configuration complexity.

---

## 73. Environment Variables by Service

Each service should receive only the configuration it needs.

Example:

```text
Product Service:
  ProductDatabase
  Redis
  Kafka
  OpenTelemetry

Order Service:
  OrderDatabase
  Inventory gRPC endpoint
  Promotion gRPC endpoint
  Payment gRPC endpoint
  Kafka
  OpenTelemetry
```

This prevents unnecessary configuration exposure.

---

## 74. Kubernetes Security

The Kubernetes deployment should follow the Security Design.

Minimum baseline:

- Dedicated namespace.
- Secrets for sensitive configuration.
- Non-root containers where practical.
- Restricted privileges.
- No unnecessary host networking.
- Limited exposed services.
- Service accounts with minimal privileges.
- Network policies where practical.
- Resource limits.
- Updated images.

---

## 75. Kubernetes Observability

Every application should expose:

- Health endpoints.
- Metrics.
- Logs.
- Distributed traces where supported.

Kubernetes provides runtime information such as:

- Pod restarts.
- Resource usage.
- Deployment status.
- Readiness state.

Application-level observability is defined in Document 16.

---

## 76. Minikube Development Workflow

A typical workflow is:

```text
1. Start Minikube
2. Create commercex namespace
3. Deploy infrastructure
4. Verify infrastructure
5. Deploy application services
6. Verify Pods
7. Verify Services
8. Verify readiness
9. Deploy Gateway
10. Access Gateway
11. Run end-to-end tests
```

---

## 77. Example Minikube Commands

Typical commands include:

```bash
minikube start
kubectl create namespace commercex
kubectl get pods -n commercex
kubectl get svc -n commercex
kubectl get deployments -n commercex
```

For debugging:

```bash
kubectl logs -n commercex <pod>
kubectl describe pod -n commercex <pod>
kubectl describe deployment -n commercex <deployment>
```

---

## 78. Local Image Loading

When application images are built locally, they can be made available to Minikube using an appropriate local image workflow.

Possible approach:

```bash
minikube image load commercex/auth:<tag>
```

This avoids pushing every development image to a remote registry.

The exact command depends on the local Minikube/container runtime setup.

---

## 79. Image Pull Policy

When using locally loaded images, Kubernetes should not unnecessarily attempt to pull the image from a remote registry.

For example, a development environment may use:

```text
IfNotPresent
```

Production-like CI/CD deployments should use immutable registry tags.

---

## 80. Kubernetes and CI/CD

The eventual deployment pipeline will be:

```text
Git Push
   |
   v
GitHub Actions
   |
   +--> Test
   +--> Build
   +--> Docker Image
   +--> Security Scan
   +--> Push Registry
   |
   v
Kubernetes
   |
   v
Rolling Deployment
```

The detailed pipeline is defined later in the CI/CD Design.

---

## 81. Deployment Ordering

A practical deployment order is:

### Infrastructure

```text
Namespace
Config/Secrets
PostgreSQL
Redis
Kafka
Schema Registry
```

### Core Services

```text
Auth
User
Product
Promotion
Cart
Inventory
```

### Transaction Services

```text
Order
Payment
Shipping
```

### Event/Customer Services

```text
Notification
Review
Search
```

### Gateway

```text
API Gateway
```

However, Kubernetes manifests should not rely exclusively on deployment order. Applications must handle dependency readiness.

---

## 82. Database Migration Strategy

Each service owns its EF Core migrations.

A migration should be applied to its own database.

Example:

```text
Auth Migration
      |
      v
commercex_auth

Order Migration
      |
      v
commercex_orders
```

Migrations should not be executed against another service's database.

---

## 83. Migration Execution

For Kubernetes, preferred approaches include:

- Explicit migration Job.
- CI/CD migration step.
- Controlled operational command.

A migration should not automatically modify all databases merely because a service starts.

For the learning environment, explicit migration Jobs or controlled commands are preferred because they make deployment behavior clear.

---

## 84. Kubernetes Jobs

Jobs can be used for one-time operational tasks.

Examples:

```text
Auth DB Migration Job
Order DB Migration Job
Seed Product Data Job
```

Jobs are different from long-running Deployments.

---

## 85. Scheduled Jobs

CronJobs are not required initially.

They may be introduced later for:

- Cleanup.
- Data maintenance.
- Scheduled reporting.
- Backup.

Do not introduce CronJobs simply because Kubernetes supports them.

---

## 86. Seed Data

Development seed data may be applied through:

- EF Core seed logic.
- Migration scripts.
- Explicit initialization Jobs.

Production-like deployments should not depend on arbitrary startup seed behavior.

---

## 87. Kubernetes Failure Recovery

Kubernetes should automatically recover from common Pod failures.

Example:

```text
Order Pod
   |
   X Crash
   |
   v
Kubernetes
   |
   v
Replacement Pod
```

Persistent state remains in PostgreSQL.

Kafka consumer state remains controlled through Kafka offsets.

Cart state remains in Redis.

---

## 88. Application Resilience vs Kubernetes Resilience

Kubernetes cannot solve all application failures.

For example:

```text
Kubernetes
  |
  +--> Restart crashed process
```

but:

```text
Bad business logic
Duplicate event
Incorrect transaction
Invalid authorization
```

must be handled by the application.

CommerceX therefore separates:

```text
Platform resilience
```

from:

```text
Application resilience
```

---

## 89. Distributed Transaction Handling

Kubernetes does not provide distributed transactions.

CommerceX continues to use:

- Local database transactions.
- gRPC for synchronous operations.
- Kafka for asynchronous propagation.
- Idempotency.
- Compensating actions where required.

This follows the architecture established in earlier documents.

---

## 90. Kubernetes Anti-Patterns

CommerceX should avoid:

### 90.1 One Deployment for All Services

This removes independent deployment.

### 90.2 Direct Pod IP Communication

Use Kubernetes Services.

### 90.3 Publicly Exposing Databases

Keep infrastructure internal.

### 90.4 Secrets in YAML

Do not commit real secret values.

### 90.5 No Resource Limits

One service can consume the entire Minikube cluster.

### 90.6 Using `latest` Everywhere

Use traceable image tags.

### 90.7 Treating `depends_on`-style startup ordering as sufficient

Applications must handle readiness.

### 90.8 Running Everything as Root

Follow least privilege.

### 90.9 Putting Business Logic in Ingress

Ingress is not a business layer.

### 90.10 Introducing Helm/Service Mesh Too Early

Understand native Kubernetes first.

---

## 91. Initial Kubernetes Implementation Scope

### Implement Initially

- Minikube cluster.
- `commercex` namespace.
- Deployments for Gateway and 12 services.
- ClusterIP Services.
- PostgreSQL deployment/stateful workload.
- Redis deployment.
- Kafka deployment.
- Schema Registry deployment.
- ConfigMaps.
- Kubernetes Secrets.
- PersistentVolumeClaims.
- Health probes.
- Resource requests/limits.
- Secure Pod/container settings.
- Internal DNS/service discovery.
- Gateway external exposure.
- Basic NetworkPolicies.
- Rolling updates.
- Local image loading.
- Database migration Jobs/commands.
- Basic operational troubleshooting.

### Defer

- Multi-node production cluster.
- Cloud-managed Kubernetes.
- Helm charts.
- Service mesh.
- Advanced autoscaling.
- Multi-zone deployment.
- Multi-region deployment.
- Operator-managed databases.
- Advanced storage systems.
- Complex admission policies.

---

## 92. Implementation Checklist

### Cluster

- [ ] Create Minikube cluster.
- [ ] Create `commercex` namespace.
- [ ] Verify kubectl access.
- [ ] Verify cluster resources.

### Infrastructure

- [ ] Deploy PostgreSQL.
- [ ] Configure persistent storage.
- [ ] Deploy Redis.
- [ ] Deploy Kafka.
- [ ] Deploy Schema Registry.
- [ ] Verify infrastructure health.

### Application Services

- [ ] Deploy Auth.
- [ ] Deploy User.
- [ ] Deploy Product.
- [ ] Deploy Inventory.
- [ ] Deploy Cart.
- [ ] Deploy Order.
- [ ] Deploy Payment.
- [ ] Deploy Shipping.
- [ ] Deploy Review.
- [ ] Deploy Notification.
- [ ] Deploy Promotion.
- [ ] Deploy Search.
- [ ] Deploy Gateway.

### Networking

- [ ] Create ClusterIP Services.
- [ ] Verify Kubernetes DNS.
- [ ] Configure gRPC service addresses.
- [ ] Configure Kafka advertised listeners.
- [ ] Configure Gateway routing.
- [ ] Configure external Gateway access.

### Configuration

- [ ] Create ConfigMaps.
- [ ] Create Secrets.
- [ ] Inject service-specific configuration.
- [ ] Verify no secrets are inside images.

### Health and Resources

- [ ] Add liveness probes.
- [ ] Add readiness probes.
- [ ] Add startup probes where required.
- [ ] Add CPU requests.
- [ ] Add memory requests.
- [ ] Add CPU limits.
- [ ] Add memory limits.

### Security

- [ ] Configure security contexts.
- [ ] Use non-root containers where practical.
- [ ] Restrict service accounts.
- [ ] Add NetworkPolicies.
- [ ] Keep databases internal.
- [ ] Verify external exposure.

### Deployment

- [ ] Configure rolling updates.
- [ ] Verify rollback.
- [ ] Verify Pod recovery.
- [ ] Verify graceful shutdown.
- [ ] Verify local image loading.
- [ ] Verify migrations.

---

## 93. Validation Scenarios

The Kubernetes environment should prove the following.

### Scenario 1 — Service Discovery

```text
Order → Inventory
```

Order should reach Inventory through Kubernetes DNS.

### Scenario 2 — Pod Restart

Delete an Order Pod:

```bash
kubectl delete pod <pod> -n commercex
```

Kubernetes should create a replacement.

### Scenario 3 — Scaling

Scale Order:

```bash
kubectl scale deployment order --replicas=2 -n commercex
```

Two Pods should become ready.

### Scenario 4 — Readiness

Make a dependency unavailable and verify that the affected service behaves according to its readiness policy.

### Scenario 5 — Rolling Update

Deploy a new image and verify that old Pods are replaced progressively.

### Scenario 6 — Rollback

Deploy a deliberately invalid version in a controlled test and verify rollback.

### Scenario 7 — Persistence

Restart PostgreSQL and verify persisted data remains available through the configured volume.

### Scenario 8 — Gateway

Send a client request to the Gateway and verify routing to the appropriate service.

---

## 94. Kubernetes Architecture Summary

The final initial Kubernetes architecture is:

```text
                         External Client
                               |
                               v
                         +-----------+
                         | Ingress / |
                         | NodePort  |
                         +-----+-----+
                               |
                               v
                         +-----------+
                         | API       |
                         | Gateway   |
                         +-----+-----+
                               |
                  +------------+-------------+
                  |                          |
                  v                          v
          +---------------+          +---------------+
          | Backend       |          | Kafka         |
          | Services      |          | Consumers     |
          +-------+-------+          +---------------+
                  |
        +---------+----------+
        |         |          |
        v         v          v
    PostgreSQL  Redis      Kafka
        |
        v
 Persistent Storage
```

Each application service has:

```text
Deployment
    +
ClusterIP Service
    +
Config/Secrets
    +
Health Probes
    +
Resource Controls
```

---

## 95. Architectural Decisions

The following decisions are established for the CommerceX Kubernetes baseline:

1. Minikube is the initial Kubernetes environment.
2. CommerceX uses a dedicated `commercex` namespace.
3. Each application service has an independent Deployment.
4. Each network-accessible service has a Kubernetes Service.
5. Backend services use ClusterIP by default.
6. The API Gateway is the primary external application entry point.
7. Ingress may be used as the Kubernetes-level external routing layer.
8. Databases, Redis, Kafka, and Schema Registry remain internal infrastructure.
9. Kubernetes DNS is used for service discovery.
10. Pod IP addresses are never used as stable service endpoints.
11. Stateless services can scale horizontally.
12. Kafka consumers scale through consumer groups and partitions.
13. PostgreSQL is initially deployed as a single stateful workload.
14. Redis is initially deployed as a single instance.
15. Kafka is initially deployed as a single broker for learning.
16. Schema Registry is deployed separately from Kafka clients/services.
17. ConfigMaps hold non-sensitive configuration.
18. Secrets hold sensitive configuration.
19. Application images remain environment-independent.
20. Health probes distinguish liveness from readiness.
21. Resource requests and limits are required.
22. Application containers should run with restricted privileges where practical.
23. Persistent storage is used for stateful infrastructure.
24. Application Pods should remain stateless.
25. Rolling updates are the default deployment strategy.
26. Database migrations are controlled operations owned by each service.
27. Kubernetes is responsible for workload orchestration, not business logic.
28. Kubernetes does not replace application-level resilience, idempotency, or authorization.
29. Advanced Kubernetes technologies are deferred until the basic architecture is stable.
30. The same Docker images should be reusable across local and Kubernetes environments.

---

## 96. Relationship With Other Documents

This document builds on:

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
- Redis Caching Strategy
- Security Design
- Docker Design

It provides Kubernetes requirements for:

- Observability Design
- Testing Strategy
- CI/CD Design
- Development Roadmap
- Final Project Report

---

## 97. Baseline Status

This document establishes the **CommerceX Kubernetes Design baseline**.

Future Kubernetes implementation should follow these decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 16 — Observability Design
