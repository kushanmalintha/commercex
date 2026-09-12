# CommerceX — Docker Design

**Document:** 14 — Docker Design  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 13 — Security Design  
**Next Document:** 15 — Kubernetes Design

---

## 1. Purpose

This document defines the Docker architecture and containerization strategy for CommerceX.

CommerceX contains an API Gateway, 12 independently deployable backend services, and several infrastructure components. Docker provides a consistent way to build, run, test, and package these components.

The Docker design prioritizes:

- Independent service containerization.
- Reproducible builds.
- Small runtime images.
- Clear service boundaries.
- Secure configuration.
- Local development through Docker Compose.
- Compatibility with Kubernetes.
- Simple CI/CD integration.

The design intentionally avoids unnecessary container orchestration complexity because Kubernetes is defined separately in Document 15.

---

## 2. Docker Objectives

The CommerceX Docker strategy should allow developers to:

1. Build every service independently.
2. Run the complete local platform with infrastructure dependencies.
3. Run individual services when developing.
4. Create reproducible application images.
5. Keep secrets outside container images.
6. Use the same application images in Kubernetes.
7. Perform container health checks.
8. Support GitHub Actions image builds.
9. Diagnose container failures easily.
10. Maintain a consistent image structure across services.

---

## 3. Containerization Scope

The following components are expected to be containerized.

### Application Components

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

### Infrastructure Components

```text
PostgreSQL
Redis
Kafka
Kafka-compatible supporting components as required
Schema Registry
```

The API Gateway is a separate deployable container but is not one of the 12 business services.

---

## 4. Container Architecture

The high-level Docker architecture is:

```text
+---------------------------------------------------+
|              CommerceX Docker Environment         |
|                                                   |
|  +-------------+       +----------------------+   |
|  | API Gateway | ----> | Backend Containers   |   |
|  +-------------+       +----------------------+   |
|                                                   |
|  +-------------+  +-------+  +-------+           |
|  | PostgreSQL  |  | Redis |  | Kafka |           |
|  +-------------+  +-------+  +-------+           |
|                              |                    |
|                       +---------------+            |
|                       | Schema Registry|           |
|                       +---------------+            |
+---------------------------------------------------+
```

All containers communicate through an internal Docker network.

---

## 5. One Container Per Deployable Application

Each application should have its own image.

For example:

```text
commercex-gateway
commercex-auth
commercex-users
commercex-products
commercex-inventory
commercex-cart
commercex-orders
commercex-payments
commercex-shipping
commercex-reviews
commercex-notifications
commercex-promotions
commercex-search
```

This allows:

- Independent deployment.
- Independent scaling.
- Independent versioning.
- Failure isolation.
- Service-specific configuration.

A single image containing all business services should not be used.

---

## 6. Dockerfile Strategy

Each .NET application should use a multi-stage Dockerfile.

Conceptually:

```text
SDK Image
    |
    +--> Restore
    +--> Build
    +--> Publish
    |
    v
Runtime Image
    |
    v
Final Application Image
```

The SDK image is used only during build.

The final runtime image should contain only the files required to execute the application.

---

## 7. Multi-Stage Build

A typical structure is:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CommerceX.Auth.Api.dll"]
```

The exact project paths and DLL names vary by service.

The important architectural rule is that the runtime image should not contain the full SDK.

---

## 8. Build Context

The repository is organized as a monorepo.

A representative structure is:

```text
CommerceX/
├── src/
│   ├── Gateway/
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
│   └── BuildingBlocks/
├── tests/
├── infra/
│   ├── docker/
│   └── kubernetes/
└── .github/
    └── workflows/
```

Dockerfiles should use an appropriate repository build context so shared BuildingBlocks can be restored and compiled.

---

## 9. Shared BuildingBlocks

CommerceX may contain shared technical BuildingBlocks.

Examples:

```text
CommerceX.BuildingBlocks.Contracts
CommerceX.BuildingBlocks.Observability
CommerceX.BuildingBlocks.Infrastructure
```

A service Docker build must include only the BuildingBlocks projects that the service actually references.

Business-domain entities must not be moved into shared BuildingBlocks merely to simplify Docker builds.

---

## 10. Docker Ignore File

The repository should include a `.dockerignore`.

It should exclude unnecessary content such as:

```text
.git
.gitignore
.vs
.vscode
bin
obj
TestResults
*.user
*.suo
README* if not required by build
```

The exact list should match the repository.

The objective is to:

- Reduce build context.
- Improve build speed.
- Avoid leaking local files.
- Improve reproducibility.

---

## 11. Runtime Image Principles

Runtime images should be:

- Small.
- Minimal.
- Free of development tools where possible.
- Free of source code when not required.
- Free of secrets.
- Regularly updated.

The image should contain:

```text
.NET runtime
Application binaries
Required configuration defaults
Required certificates/assets
```

It should not contain:

```text
Git credentials
Database passwords
JWT signing keys
Developer files
Source-control metadata
```

---

## 12. Base Images

CommerceX should use official Microsoft .NET images for .NET services.

Conceptually:

```text
Build:
mcr.microsoft.com/dotnet/sdk:9.0

Runtime:
mcr.microsoft.com/dotnet/aspnet:9.0
```

Exact patch versions can be pinned or centrally managed according to the project's dependency strategy.

Infrastructure images should come from trusted upstream projects.

---

## 13. Non-Root Containers

Application containers should preferably run as a non-root user.

The objective is:

```text
Container compromise
        |
        v
Limited process privileges
```

rather than:

```text
Container compromise
        |
        v
Root privileges
```

The exact user configuration depends on the selected .NET base image.

This should be verified through container tests.

---

## 14. Configuration

Container images must not contain environment-specific configuration.

Configuration should be injected at runtime.

Example:

```text
Container Image
      |
      +--> Application binaries
      |
      v
Runtime Configuration
      |
      +--> Database connection
      +--> Redis connection
      +--> Kafka connection
      +--> JWT configuration
```

This allows the same image to run in:

```text
Development
Testing
Minikube
CI
```

with different configuration.

---

## 15. Environment Variables

Environment variables are suitable for container configuration.

Example:

```text
ConnectionStrings__AuthDatabase
Redis__ConnectionString
Kafka__BootstrapServers
Jwt__Issuer
Jwt__Audience
```

Sensitive values should be supplied through secure mechanisms rather than committed to source control.

---

## 16. Secrets

Docker images must never contain production secrets.

Do not place secrets in:

```dockerfile
ENV JWT_SECRET=...
ENV DB_PASSWORD=...
```

Do not copy secret files into images.

For local development, `.env` or Docker Compose environment configuration may be used carefully.

For Kubernetes, Secrets should be used.

---

## 17. Docker Compose Role

Docker Compose is the primary local development orchestration mechanism before Kubernetes is introduced.

It should be used to run:

- PostgreSQL.
- Redis.
- Kafka.
- Schema Registry.
- Selected application services.
- API Gateway when required.

The initial Compose setup should make infrastructure available without requiring the full Kubernetes environment.

---

## 18. Infrastructure-First Compose

A practical local development sequence is:

```text
Docker Compose
      |
      +--> PostgreSQL
      +--> Redis
      +--> Kafka
      +--> Schema Registry
```

Then applications can be started.

This supports development of individual services without requiring all 12 services to run simultaneously.

---

## 19. Full Local Compose

A full environment can eventually run:

```text
Gateway
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
PostgreSQL
Redis
Kafka
Schema Registry
```

This is useful for:

- End-to-end testing.
- Demonstrating the complete architecture.
- Integration testing.
- Architecture validation.

However, developers should not be required to run every application container for every code change.

---

## 20. Docker Compose Networking

All CommerceX containers should communicate over a dedicated Docker network.

Conceptually:

```text
commercex-network
       |
       +-- gateway
       +-- auth
       +-- users
       +-- products
       +-- inventory
       +-- cart
       +-- orders
       +-- payments
       +-- shipping
       +-- reviews
       +-- notifications
       +-- promotions
       +-- search
       +-- postgres
       +-- redis
       +-- kafka
       +-- schema-registry
```

Containers should use service names for internal communication.

Example:

```text
postgres:5432
redis:6379
kafka:9092
```

Do not use container IP addresses.

---

## 21. Port Strategy

Only ports that need host access should be exposed.

Example local infrastructure:

| Component | Container Port | Example Host Port |
|---|---:|---:|
| PostgreSQL | 5432 | 5432 |
| Redis | 6379 | 6379 |
| Kafka | 9092 | 9092 |
| Schema Registry | 8081/internal | 9081/local |

The exact host-port mapping can vary.

The important principle is:

```text
Container-to-container communication
        |
        v
Internal Docker network

Host access
        |
        v
Only explicitly exposed ports
```

The Schema Registry host port should remain configurable because local port conflicts can occur.

---

## 22. Kafka Docker Considerations

Kafka requires special attention because clients can receive broker-advertised addresses.

The Docker environment must distinguish between:

```text
Container clients
```

and:

```text
Host clients
```

Advertised listeners should therefore be configured so that:

- Containers can reach Kafka using the Docker service name.
- Host-based development tools can reach Kafka through the appropriate host address/port.

Incorrect advertised listeners are a common source of local Kafka connection failures.

---

## 23. PostgreSQL Docker Architecture

For local development, one PostgreSQL container can host multiple logical CommerceX databases.

Example:

```text
PostgreSQL Container
       |
       +--> commercex_auth
       +--> commercex_users
       +--> commercex_products
       +--> commercex_inventory
       +--> commercex_orders
       +--> commercex_payments
       +--> commercex_shipping
       +--> commercex_reviews
       +--> commercex_notifications
       +--> commercex_promotions
       +--> commercex_search
```

This preserves logical database ownership while keeping local infrastructure manageable.

---

## 24. PostgreSQL Data Persistence

PostgreSQL should use a Docker volume in local development.

Conceptually:

```text
PostgreSQL Container
        |
        v
postgres-data volume
```

Without a volume, recreating the container would remove local database data.

Application database migrations remain the responsibility of each service.

---

## 25. Redis Docker Architecture

Redis should use a named volume if the selected local persistence configuration requires it.

Conceptually:

```text
Redis Container
      |
      v
redis-data
```

However, cache data does not require the same durability guarantees as PostgreSQL data.

Cart persistence requirements should be considered when selecting the final Redis configuration.

---

## 26. Kafka Data Persistence

Kafka should use persistent volumes in the local development environment when retaining data across container recreation is useful.

Conceptually:

```text
Kafka
 |
 +--> broker-data
```

The exact persistence and replication configuration should remain appropriate for a single-node learning environment.

---

## 27. Schema Registry

Schema Registry should run as a separate infrastructure container.

Its responsibilities include:

- Managing event schema versions.
- Supporting Kafka event contract evolution.
- Providing schema compatibility checks where configured.

It should not be embedded into application containers.

---

## 28. Health Checks

Containers should expose application health information.

For ASP.NET Core services, health endpoints should be available such as:

```text
/health/live
/health/ready
```

Conceptually:

```text
Docker/Kubernetes
       |
       v
Health Endpoint
       |
       +--> Healthy
       |
       +--> Unhealthy
```

The exact health-check implementation is defined further in the Observability and Kubernetes documents.

---

## 29. Docker Compose Health Dependencies

Infrastructure startup order does not guarantee application readiness.

For example:

```text
depends_on:
  postgres
```

does not necessarily mean PostgreSQL is ready to accept queries.

Health checks should therefore be used where appropriate.

The system should distinguish:

```text
Container started
```

from:

```text
Dependency ready
```

---

## 30. Startup Resilience

Applications should tolerate infrastructure becoming ready slightly after the application starts.

For example:

```text
Order Service starts
       |
       X
Kafka not ready
       |
       v
Retry/reconnect
       |
       v
Kafka becomes ready
       |
       v
Order Service operates normally
```

Applications should not require a perfectly synchronized startup sequence.

---

## 31. Docker Logging

Applications should write logs to standard output/error.

Preferred:

```text
stdout
stderr
```

rather than writing application logs into container-local files.

This allows Docker, Kubernetes, and centralized observability tooling to collect logs consistently.

---

## 32. Structured Logging

Services should produce structured logs.

Example conceptual event:

```json
{
  "level": "Information",
  "service": "Order",
  "event": "OrderCreated",
  "orderId": "...",
  "correlationId": "...",
  "timestamp": "..."
}
```

Do not log:

- Passwords.
- Access tokens.
- Refresh tokens.
- Database passwords.
- Payment credentials.

---

## 33. Resource Limits

Containers should have reasonable resource limits, especially in Minikube.

Relevant resources:

- CPU.
- Memory.
- Ephemeral storage.

Without limits, one service can consume excessive local resources.

Initial values should be measured and adjusted rather than treating arbitrary values as final production capacity.

---

## 34. Docker Compose Resource Management

Because CommerceX contains many containers, running everything simultaneously can consume significant memory.

Recommended development approach:

### Infrastructure Mode

```text
PostgreSQL
Redis
Kafka
Schema Registry
```

### Service Development Mode

```text
Infrastructure
+
One or a few services
```

### Full Integration Mode

```text
Infrastructure
+
Gateway
+
All services
```

This makes the project manageable on a developer workstation.

---

## 35. Build Performance

Docker builds should be optimized using layer caching.

A common strategy is:

```dockerfile
COPY *.sln .
COPY Directory.Packages.props .
COPY project files .
RUN dotnet restore

COPY source .
RUN dotnet publish
```

This allows dependency restoration to remain cached when only application source code changes.

The exact Dockerfile structure should match the repository layout.

---

## 36. Build Reproducibility

Docker builds should be deterministic as far as practical.

Recommended practices:

- Central package version management.
- Lock or control dependency versions.
- Use explicit base images.
- Build from clean source.
- Avoid downloading arbitrary files during image build.
- Do not depend on developer-local files.

---

## 37. Image Naming

A consistent naming convention should be used.

Example:

```text
commercex/gateway
commercex/auth
commercex/users
commercex/products
commercex/inventory
commercex/cart
commercex/orders
commercex/payments
commercex/shipping
commercex/reviews
commercex/notifications
commercex/promotions
commercex/search
```

For a registry:

```text
ghcr.io/<owner>/commercex-auth
```

The exact registry namespace can be finalized during CI/CD implementation.

---

## 38. Image Tags

Images should use immutable or traceable tags.

Useful tags include:

```text
v1.0.0
<git-sha>
```

Avoid relying exclusively on:

```text
latest
```

because `latest` does not uniquely identify the deployed artifact.

For learning environments, a Git commit SHA tag is particularly useful.

---

## 39. Image Versioning

Application versioning should be traceable to Git.

Conceptually:

```text
Git Commit
     |
     v
Docker Build
     |
     v
commercex-auth:<commit-sha>
```

This makes it possible to identify exactly which source version produced an image.

---

## 40. Local Development Workflow

A typical workflow is:

```text
1. Clone repository
2. Start infrastructure
3. Apply database migrations
4. Start service
5. Run tests
6. Build Docker image
7. Run container
8. Validate through Gateway
```

Example:

```bash
docker compose up -d postgres redis kafka schema-registry
```

Then the required services can be started.

The exact Compose service names are implementation details.

---

## 41. Application Image Workflow

For an individual service:

```text
Source Code
    |
    v
dotnet test
    |
    v
docker build
    |
    v
Docker Image
    |
    v
docker run
```

This provides a clear separation between application correctness and container packaging.

---

## 42. Docker and Database Migrations

Database migrations should not be blindly executed every time a container starts.

Possible approaches:

1. Run migrations explicitly during development.
2. Use a controlled migration job.
3. Use a deployment pipeline migration step.
4. Run migrations during application startup only if the project intentionally accepts the operational trade-off.

For CommerceX, explicit/controlled migrations are preferred for clarity.

Each service owns its migrations.

---

## 43. Docker and gRPC

gRPC services must bind to an address reachable from other containers.

Conceptually:

```text
Order Container
      |
      | gRPC
      v
Inventory Container
```

Applications should not bind only to `localhost` when another container needs to connect.

Service DNS names should be used for container-to-container communication.

---

## 44. Docker and Kafka

Kafka clients should use the Docker service hostname from inside the Compose network.

Example conceptual configuration:

```text
kafka:9092
```

Host applications may require a different advertised listener.

The final Docker Compose configuration should explicitly support both development scenarios if both are required.

---

## 45. Docker and Redis

Redis clients inside Docker should connect using:

```text
redis:6379
```

rather than:

```text
localhost:6379
```

because `localhost` inside a container refers to that same container.

This distinction is critical for containerized services.

---

## 46. Docker and PostgreSQL

Similarly, services inside Docker should use:

```text
postgres:5432
```

rather than:

```text
localhost:5432
```

when PostgreSQL is another Compose container.

---

## 47. Container Security

The Docker baseline should include:

- Non-root execution where practical.
- Minimal runtime images.
- No secrets in images.
- Trusted base images.
- Dependency updates.
- Image vulnerability scanning.
- Minimal Linux capabilities.
- Read-only filesystem where practical.
- Explicit exposed ports.
- `.dockerignore`.

---

## 48. Container Vulnerability Scanning

CI should eventually scan built images.

Possible tools include:

- Trivy.
- Docker Scout.
- GitHub security tooling.

The exact tool can be selected during CI/CD design.

The objective is to identify:

- OS vulnerabilities.
- Vulnerable application packages.
- Known CVEs.

---

## 49. Docker Image Lifecycle

Images should follow:

```text
Build
  |
  v
Test
  |
  v
Scan
  |
  v
Tag
  |
  v
Publish
  |
  v
Deploy
```

An image should not be deployed merely because `docker build` succeeded.

---

## 50. Docker Compose File Organization

The repository may organize Docker infrastructure as:

```text
infra/
└── docker/
    ├── docker-compose.yml
    ├── .env.example
    └── README.md
```

If separate Compose files are useful, they can be introduced later:

```text
docker-compose.infra.yml
docker-compose.services.yml
docker-compose.full.yml
```

However, multiple Compose files should only be introduced if they improve usability.

---

## 51. Environment Separation

At minimum, CommerceX should recognize:

```text
Development
Test
Minikube
CI
```

The same application image should ideally be usable across these environments with different runtime configuration.

The image itself should not change merely because the environment changes.

---

## 52. Docker and GitHub Actions

GitHub Actions will eventually:

```text
Checkout
   |
   v
Restore
   |
   v
Build/Test
   |
   v
Docker Build
   |
   v
Security Scan
   |
   v
Tag
   |
   v
Push Registry
```

The detailed CI/CD pipeline is defined in a later document.

---

## 53. Docker and Kubernetes

Docker is responsible for packaging the application.

Kubernetes is responsible for:

- Scheduling.
- Restarting.
- Scaling.
- Service discovery.
- Configuration injection.
- Deployment management.

The separation is:

```text
Docker
  =
Build/package/run container

Kubernetes
  =
Manage containers
```

The same CommerceX application images should be usable in Minikube.

---

## 54. Local Image Development With Minikube

When using Minikube, developers can either:

- Push images to a registry.
- Load locally built images into Minikube.
- Build using Minikube's container runtime where appropriate.

For a learning environment, loading local images is useful because it avoids pushing every development build to a remote registry.

---

## 55. Image Pull Policy

For local development, Kubernetes image pull behavior must match the image workflow.

If images are loaded directly into Minikube, an appropriate pull policy should prevent unnecessary registry pulls.

For CI/CD deployments, immutable registry-tagged images should normally be used.

---

## 56. Graceful Shutdown

Containers should allow applications to shut down gracefully.

When receiving a termination signal:

```text
SIGTERM
   |
   v
ASP.NET Core
   |
   +--> Stop accepting work
   +--> Finish/cancel active operations
   +--> Stop Kafka consumers
   +--> Dispose resources
   |
   v
Process exits
```

This is particularly important for Kafka consumers and services processing business operations.

---

## 57. Kafka Consumer Shutdown

Notification, Shipping, Search, and other Kafka-consuming services should stop consumers gracefully.

The service should:

- Stop fetching new messages.
- Complete/cancel the current processing according to the consumer design.
- Commit offsets only when processing semantics permit.
- Close the consumer cleanly.

This reduces duplicate processing during normal deployments.

Idempotent consumers remain required because Kafka delivery is at least once.

---

## 58. Docker Health and Dependency Checks

A container's health should not simply mean that the process exists.

For example:

```text
Order Service
    |
    +--> Process healthy
    +--> PostgreSQL reachable
    +--> Required dependencies available
```

Readiness should be more meaningful than a basic process check.

The final dependency policy should follow the service's role:

- Required dependencies may affect readiness.
- Optional caches should not necessarily make a service unavailable.

---

## 59. Docker Networking Security

The Docker network should not expose infrastructure unnecessarily.

Recommended:

```text
Host
 |
 +--> Gateway
 |
 +--> Developer tools
```

while:

```text
PostgreSQL
Redis
Kafka
```

remain internal where external access is not required.

Local development may expose selected infrastructure ports for debugging, but this should be treated as a development convenience rather than a production architecture.

---

## 60. Container-to-Container Security

Internal networking should still follow the Security Design.

Do not assume:

```text
Same Docker network = trusted
```

Sensitive service calls should still use appropriate authentication.

---

## 61. Persistent Volumes

Expected local volumes:

```text
postgres-data
redis-data
kafka-data
```

Schema Registry storage requirements depend on the selected implementation/configuration.

Application services should generally remain stateless and should not depend on local container filesystem persistence.

---

## 62. Stateless Application Containers

Application services should be designed to scale horizontally.

Avoid storing durable state in:

```text
/app/data
/tmp
container filesystem
```

except for temporary files.

Durable data belongs in:

```text
PostgreSQL
Redis
Kafka
```

according to the data architecture.

---

## 63. Container Resource Planning

CommerceX contains many application containers, so resource planning matters.

A local full-stack deployment can be resource-intensive.

The development environment should allow:

```text
Infrastructure only
```

or:

```text
Infrastructure + selected services
```

rather than requiring the entire architecture to run continuously.

Minikube resource allocation should be documented separately.

---

## 64. Docker Troubleshooting

Useful commands during development include:

```bash
docker ps
docker compose ps
docker compose logs <service>
docker logs <container>
docker inspect <container>
docker network ls
docker volume ls
docker exec -it <container> sh
```

For service connectivity:

```text
Container
  |
  +--> DNS/service-name resolution
  +--> Port connectivity
  +--> Application health
```

Do not troubleshoot only from the host perspective because container networking differs from host networking.

---

## 65. Common Docker Errors

### `localhost` Connection Failure

Inside a container:

```text
localhost
```

means the current container.

Use:

```text
postgres
redis
kafka
```

for other Compose services.

### Kafka Connection Failure

Check:

- Listeners.
- Advertised listeners.
- Host vs container address.
- Port mappings.

### Database Connection Failure

Check:

- Database exists.
- Credentials.
- Container health.
- Network.
- Connection string.

### Image Not Found in Minikube

Check:

- Image loaded into Minikube.
- Image pull policy.
- Registry name/tag.

---

## 66. Docker Anti-Patterns

CommerceX should avoid:

### 66.1 One Container for the Entire System

This destroys service independence.

### 66.2 Secrets Inside Images

Images may be stored in registries and inspected by many systems.

### 66.3 `latest` as the Only Version

It makes deployments difficult to reproduce.

### 66.4 Huge Runtime Images

Do not ship the SDK and unnecessary tooling in production runtime images.

### 66.5 Hostname/IP Hard-Coding

Use service discovery and environment-specific configuration.

### 66.6 Storing Business Data in Application Containers

Containers should be replaceable.

### 66.7 Assuming Startup Order Equals Readiness

Use health checks and retry behavior.

### 66.8 Running Everything as Root

Use least privilege.

### 66.9 Baking Environment Configuration Into Images

Inject configuration at runtime.

### 66.10 Excessive Compose Complexity

Compose should remain understandable and useful for local development.

---

## 67. Initial Docker Implementation Scope

### Implement Initially

- Dockerfile for Gateway.
- Dockerfiles for all 12 services.
- Multi-stage .NET builds.
- `.dockerignore`.
- Docker Compose infrastructure.
- Redis.
- PostgreSQL.
- Kafka.
- Schema Registry.
- Dedicated Docker network.
- Environment-based configuration.
- Health checks.
- Persistent infrastructure volumes.
- Local service discovery.
- Basic resource controls.
- Secure runtime images.
- Local image build/test workflow.

### Defer

- Multi-architecture publishing unless required.
- Advanced image optimization.
- Private enterprise registry infrastructure.
- Kubernetes-native build systems.
- Service mesh sidecars.
- Complex container orchestration outside Kubernetes.
- Production-grade multi-node Kafka/PostgreSQL/Redis topology.

---

## 68. Implementation Checklist

### Application Images

- [ ] Create Gateway Dockerfile.
- [ ] Create Auth Dockerfile.
- [ ] Create User Dockerfile.
- [ ] Create Product Dockerfile.
- [ ] Create Inventory Dockerfile.
- [ ] Create Cart Dockerfile.
- [ ] Create Order Dockerfile.
- [ ] Create Payment Dockerfile.
- [ ] Create Shipping Dockerfile.
- [ ] Create Review Dockerfile.
- [ ] Create Notification Dockerfile.
- [ ] Create Promotion Dockerfile.
- [ ] Create Search Dockerfile.

### Infrastructure

- [ ] Configure PostgreSQL container.
- [ ] Configure Redis container.
- [ ] Configure Kafka container.
- [ ] Configure Schema Registry.
- [ ] Configure persistent volumes.
- [ ] Configure Docker network.
- [ ] Configure health checks.

### Configuration

- [ ] Remove hard-coded environment values.
- [ ] Add environment variables.
- [ ] Add `.env.example`.
- [ ] Keep secrets out of Git.
- [ ] Verify container-to-container connection strings.

### Security

- [ ] Use minimal runtime images.
- [ ] Run application containers as non-root where practical.
- [ ] Scan images.
- [ ] Keep base images updated.
- [ ] Do not embed secrets.
- [ ] Limit exposed ports.

### Development

- [ ] Start infrastructure independently.
- [ ] Run individual services.
- [ ] Run full Compose environment.
- [ ] Verify service discovery.
- [ ] Verify health endpoints.
- [ ] Test graceful shutdown.

---

## 69. Docker Architecture Summary

The final initial Docker architecture is:

```text
                         Host
                          |
                  +-------+-------+
                  | Docker Network|
                  +-------+-------+
                          |
        +-----------------+------------------+
        |                                    |
        v                                    v
+---------------+                    +---------------+
| API Gateway   |                    | Infrastructure|
+-------+-------+                    +-------+-------+
        |                                    |
        v                          +---------+---------+
+-----------------------+          |         |         |
| 12 Application        |          v         v         v
| Service Containers    |      PostgreSQL  Redis     Kafka
+-----------------------+                              |
                                                       v
                                                Schema Registry
```

Each business service has its own image and remains independently deployable.

Infrastructure containers provide shared platform capabilities but do not change service ownership boundaries.

---

## 70. Architectural Decisions

The following decisions are established for the CommerceX Docker baseline:

1. Every deployable application has its own Docker image.
2. The API Gateway is independently containerized.
3. The 12 business services are independently containerized.
4. .NET services use multi-stage Docker builds.
5. Official .NET images are preferred.
6. Runtime images should be minimal.
7. Application containers should run as non-root where practical.
8. Secrets are injected at runtime and never baked into images.
9. Docker Compose is the primary local orchestration tool before Kubernetes.
10. Infrastructure services run in Docker for local development.
11. Containers communicate through a dedicated internal network.
12. Container service names are used instead of IP addresses.
13. PostgreSQL uses persistent local storage.
14. Redis persistence is configured according to cart requirements.
15. Kafka uses persistent local storage where useful for development.
16. Application containers remain stateless.
17. Health checks distinguish process startup from service readiness.
18. Logs are written to stdout/stderr.
19. Images use traceable tags rather than relying only on `latest`.
20. Git commit identifiers should be usable for image traceability.
21. Docker packages applications; Kubernetes manages their deployment and runtime lifecycle.
22. The same application images should be reusable in Minikube.
23. Full local deployment is supported, but developers are not required to run every service for every change.
24. Production-grade multi-node infrastructure is outside the initial Docker scope.

---

## 71. Relationship With Other Documents

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

It provides containerization requirements for:

- Kubernetes Design
- Observability Design
- Testing Strategy
- CI/CD Design
- Development Roadmap
- Final Project Report

---

## 72. Baseline Status

This document establishes the **CommerceX Docker Design baseline**.

Future Docker implementation should follow these decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 15 — Kubernetes Design
