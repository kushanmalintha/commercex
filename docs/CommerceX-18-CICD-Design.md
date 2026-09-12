# CommerceX — CI/CD Design

**Document:** 18 — CI/CD Design  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 17 — Testing Strategy  
**Next Document:** 19 — Development Roadmap

---

## 1. Purpose

This document defines the Continuous Integration and Continuous Delivery strategy for CommerceX.

CommerceX consists of an API Gateway, 12 independently deployable services, shared technical BuildingBlocks, PostgreSQL, Redis, Kafka, Schema Registry, Docker containers, and Kubernetes deployments.

The CI/CD strategy must therefore support:

- Independent service development.
- Automated validation.
- Automated testing.
- Docker image creation.
- Security checks.
- Artifact traceability.
- Kubernetes deployment.
- Environment promotion.
- Fast feedback for pull requests.
- Safe releases.
- Easy rollback.

The strategy is intentionally practical for a learning project while following patterns used in real distributed systems.

---

## 2. CI/CD Objectives

The CommerceX pipeline should:

1. Validate every code change automatically.
2. Build affected applications reliably.
3. Execute automated tests.
4. Detect security and dependency issues.
5. Build reproducible Docker images.
6. Tag images with traceable versions.
7. Publish images to a container registry.
8. Deploy validated images to Kubernetes.
9. Run deployment smoke tests.
10. Provide clear failure reports.
11. Support rollback.
12. Avoid deploying untested code.
13. Keep service deployments independently manageable.

---

## 3. CI/CD Principles

CommerceX follows these principles:

### 3.1 Automate Repetition

Build, test, scanning, packaging, and deployment steps should be automated.

### 3.2 Fail Fast

Cheap checks should execute before expensive checks.

### 3.3 Build Once, Promote the Same Artifact

An image that passes validation should be promoted rather than rebuilt differently for each environment.

### 3.4 Traceability

Every deployable image should be traceable to:

```text
Git Commit
    ↓
Build
    ↓
Docker Image
    ↓
Deployment
```

### 3.5 Secure by Default

Secrets must never be committed to the repository or embedded in images.

### 3.6 Independent Service Delivery

A change to Product Service should not require rebuilding unrelated services unless shared code or infrastructure requires it.

---

## 4. CI/CD Platform

The primary CI/CD platform is:

**GitHub Actions**

The source repository is hosted on GitHub.

GitHub Actions will execute workflows for:

- Pull requests.
- Pushes.
- Main branch changes.
- Releases.
- Manual deployment requests.

---

## 5. Repository Strategy

CommerceX uses a monorepo.

A simplified structure:

```text
commercex/
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
├── .github/
│   └── workflows/
└── docs/
```

---

## 6. Branching Strategy

Recommended branches:

```text
main
  |
  +-- feature/*
  +-- fix/*
  +-- refactor/*
```

Example:

```text
feature/auth-foundation
feature/product-service
fix/order-payment-timeout
```

The `main` branch should represent code that has passed the required validation gates.

---

## 7. Pull Request Workflow

The standard development flow is:

```text
Developer
   |
   v
Feature Branch
   |
   v
Pull Request
   |
   v
GitHub Actions
   |
   +--> Build
   +--> Unit Tests
   +--> Integration Tests
   +--> Contract Tests
   +--> Security Checks
   |
   v
Review
   |
   v
Merge to main
```

---

## 8. Continuous Integration

CI is responsible for validating code before it becomes a release candidate.

The initial CI stages are:

```text
Checkout
   ↓
Restore Dependencies
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
   ↓
Contract/API Tests
   ↓
Security/Dependency Checks
   ↓
Docker Build
   ↓
Artifact Validation
```

---

## 9. CI Trigger Strategy

Recommended triggers:

### Pull Requests

Run:

- Build.
- Unit tests.
- Relevant integration tests.
- Contract tests.
- Security/dependency checks.

### Push to Main

Run:

- Full required test suite.
- Docker builds.
- Image security scanning.
- Artifact publishing.

### Release Tag

Run:

- Release validation.
- Production-like image validation.
- Deployment workflow where configured.

### Manual

Allow maintainers to manually trigger selected deployment workflows.

---

## 10. Path-Based Optimization

Because CommerceX is a monorepo, pipelines should avoid rebuilding every service for every unrelated change when practical.

Example:

```text
src/Services/Product/*
      |
      v
Product CI
```

A Product-only change should primarily validate Product and shared dependencies.

Changes to:

```text
src/BuildingBlocks/*
```

may require broader validation.

Changes to:

```text
infra/*
.github/workflows/*
```

may trigger infrastructure or full validation.

---

## 11. Build Stage

The build stage should:

1. Install the required .NET SDK.
2. Restore NuGet dependencies.
3. Build the solution/projects.
4. Treat warnings according to the repository policy.
5. Produce deterministic build output where practical.

Example conceptual command:

```bash
dotnet restore
dotnet build --no-restore
```

---

## 12. .NET Version

The project targets:

```text
.NET 9
```

CI should use a pinned .NET SDK version compatible with the repository's project configuration.

A `global.json` can be used when exact SDK reproducibility is required.

---

## 13. Dependency Management

CI should validate:

- NuGet restore.
- Package compatibility.
- Vulnerable dependencies.
- Unexpected dependency changes.

Dependency versions should remain centrally manageable where the repository uses shared package management.

---

## 14. Unit Test Stage

The unit test stage executes fast tests first.

Example:

```bash
dotnet test --no-build
```

The exact project selection should match the affected service or the complete suite depending on the workflow.

Unit test failures must fail CI.

---

## 15. Integration Test Stage

Integration tests should run against disposable infrastructure.

Recommended technologies:

```text
xUnit
Testcontainers
PostgreSQL
Redis
Kafka
```

Conceptually:

```text
GitHub Runner
      |
      +--> PostgreSQL Container
      +--> Redis Container
      +--> Kafka Container
      |
      v
CommerceX Tests
```

This reduces dependency on preconfigured CI infrastructure.

---

## 16. Contract Test Stage

Contract tests validate:

- REST contracts.
- gRPC contracts.
- Kafka event contracts.

The pipeline should detect accidental breaking changes before deployment.

---

## 17. Security Testing

CI should include automated security checks.

Initial checks:

- NuGet vulnerability scanning.
- Dependency scanning.
- Secret detection.
- Docker image scanning.
- Static analysis where practical.

The pipeline should fail for vulnerabilities that exceed the project's accepted severity threshold.

---

## 18. Secret Scanning

Secrets must never be committed.

CI should detect potential:

- API keys.
- Passwords.
- JWT signing keys.
- Database credentials.
- Kafka credentials.
- Redis credentials.
- Cloud credentials.

Examples of sensitive files that should not be committed:

```text
.env
appsettings.Production.json
private keys
credential files
```

Developer-only secrets should use local secret storage such as .NET User Secrets where appropriate.

---

## 19. Code Quality

The CI pipeline should validate basic code quality through:

- Compiler analysis.
- Formatting checks where adopted.
- Static analysis.
- Nullable reference type checks.
- Dependency warnings.

The project should prioritize useful quality gates rather than adding excessive tooling.

---

## 20. Docker Build Stage

Each deployable application has its own Docker image.

Images include:

```text
commercex-gateway
commercex-auth
commercex-user
commercex-product
commercex-inventory
commercex-cart
commercex-order
commercex-payment
commercex-shipping
commercex-review
commercex-notification
commercex-promotion
commercex-search
```

Infrastructure images such as PostgreSQL, Redis, Kafka, and Schema Registry should normally use approved upstream images rather than custom CommerceX images.

---

## 21. Multi-Stage Docker Builds

The Docker pipeline should use the multi-stage architecture defined in Document 14.

Conceptually:

```text
.NET SDK Image
     |
     v
Restore + Build + Publish
     |
     v
ASP.NET Runtime Image
     |
     v
Final Application Image
```

This keeps runtime images smaller.

---

## 22. Docker Image Validation

After building an image, CI should verify:

- Image exists.
- Container starts.
- Required configuration can be injected.
- Health endpoint becomes available.
- Application exits cleanly.
- No unexpected runtime dependency exists.

---

## 23. Image Tagging

Images should be traceable to source code.

Recommended tags include:

```text
commercex-order:<git-sha>
commercex-order:<version>
```

For example:

```text
commercex-order:5f5e622
```

A semantic version can additionally be used for releases.

The `latest` tag should not be the only identifier.

---

## 24. Container Registry

A container registry should store validated images.

The project can use a registry such as:

- GitHub Container Registry.
- Docker Hub.
- Another compatible OCI registry.

For a GitHub-based project, GitHub Container Registry is a natural initial choice.

---

## 25. Image Immutability

Once an image is associated with a Git commit or release, the same image should be promoted between environments.

Conceptually:

```text
Build Image
     |
     v
Scan Image
     |
     v
Push Image
     |
     v
Deploy Same Image
```

Do not rebuild an image separately for each environment.

---

## 26. Image Security

CI should scan container images for:

- OS vulnerabilities.
- Runtime vulnerabilities.
- Known CVEs.
- Unnecessary packages.

Runtime images should remain minimal.

---

## 27. Artifact Traceability

Every deployment should be traceable to:

```text
Repository
Commit SHA
Workflow Run
Docker Image Digest
Environment
Deployment Time
```

This allows operators to answer:

> Which exact source version is running?

---

## 28. Continuous Delivery

Continuous Delivery means a validated artifact is always deployable.

The recommended flow is:

```text
main
 |
 v
CI
 |
 v
Docker Images
 |
 v
Registry
 |
 v
Deployment Environment
 |
 v
Smoke Tests
```

Automatic production deployment is not required for the initial learning project.

---

## 29. Environment Strategy

CommerceX uses three conceptual environments:

### Development

Developer workstation / Docker Compose.

### Integration

Automated CI environment for cross-service testing.

### Kubernetes

Minikube environment representing the deployment architecture.

A future hosted environment can be introduced later.

---

## 30. Environment Promotion

The recommended promotion model is:

```text
Build
  |
  v
Test
  |
  v
Publish Image
  |
  v
Integration
  |
  v
Kubernetes
```

The same image should move through environments.

---

## 31. Configuration Management

Configuration must be external to Docker images.

Examples:

```text
Database connection strings
Redis endpoints
Kafka brokers
JWT configuration
Service URLs
Feature flags
```

Docker and Kubernetes inject environment-specific configuration at runtime.

---

## 32. Secrets Management

Sensitive configuration should be provided through:

### Local

- .NET User Secrets.
- Environment variables.
- Local secret files excluded from Git.

### GitHub Actions

- GitHub Actions Secrets/Variables.

### Kubernetes

- Kubernetes Secrets.

Secrets must not appear in:

- Source code.
- Dockerfiles.
- Image layers.
- Git history.
- CI logs.

---

## 33. Database Migration Strategy

Database migrations require special handling.

The pipeline should not blindly execute:

```text
Database.Migrate()
```

during every application startup.

Instead, migrations should be:

- Explicit.
- Versioned.
- Reviewed.
- Executed in a controlled deployment step.

Possible Kubernetes implementation:

```text
Deployment
   |
   v
Migration Job
   |
   v
Application Deployment
```

The exact mechanism can be refined during implementation.

---

## 34. Migration Safety

Before migration:

- Validate migration generation.
- Review SQL/schema changes.
- Ensure backward compatibility where needed.
- Back up important environments where applicable.

For the learning project, migration complexity should remain modest.

---

## 35. Kubernetes Deployment

Kubernetes deployment follows Document 15.

The CI/CD pipeline can apply manifests using:

```text
kubectl
```

or a later declarative deployment mechanism.

Initial Minikube deployment can use:

```text
kubectl apply
```

with repository-managed manifests.

---

## 36. Kubernetes Image Updates

A deployment should reference a specific image tag or digest.

Example:

```yaml
image: ghcr.io/<organization>/commercex-order:<git-sha>
```

CI updates the deployment reference for the validated artifact.

---

## 37. Deployment Sequence

A simplified deployment is:

```text
Build
  |
  v
Test
  |
  v
Security Scan
  |
  v
Build Docker Images
  |
  v
Push Images
  |
  v
Apply Kubernetes Configuration
  |
  v
Wait for Rollout
  |
  v
Smoke Tests
```

---

## 38. Rollout Verification

After deployment:

```bash
kubectl rollout status
```

should confirm that affected deployments become ready.

CI should fail if required workloads do not become healthy within an appropriate timeout.

---

## 39. Smoke Testing

Post-deployment smoke tests should verify:

- Gateway is reachable.
- Health endpoints work.
- Authentication works.
- Product read works.
- Cart operation works.
- Core service discovery works.
- Kafka is available.
- Redis is available.
- PostgreSQL is available.

A full checkout smoke test can be included once all required services are deployed.

---

## 40. Deployment Failure

If deployment fails:

```text
Deployment
    |
    X
Failure
    |
    +--> Collect logs
    +--> Check rollout
    +--> Diagnose
    +--> Roll back if appropriate
```

CI should clearly identify which service failed.

---

## 41. Rollback Strategy

Kubernetes Deployment history should support rollback.

Conceptually:

```text
Version A
   |
   v
Version B
   |
   X failure
   |
   v
Rollback to A
```

Rollback should be possible without rebuilding the previous image.

---

## 42. Backward Compatibility

Distributed services may not be upgraded simultaneously.

Therefore:

- REST changes should be compatible where practical.
- gRPC contracts should evolve compatibly.
- Kafka event schemas should be versioned.
- Database changes should avoid breaking running application versions.

This is especially important during rolling deployments.

---

## 43. Zero-Downtime Considerations

For stateless services, rolling updates should be used.

Requirements include:

- Readiness probes.
- Graceful shutdown.
- Correct termination handling.
- Sufficient replica overlap when scaling beyond one replica.

Minikube initially uses one replica for simplicity, so true zero-downtime behavior is limited until replicas are increased.

---

## 44. Graceful Shutdown Testing

CI/deployment testing should verify that:

- HTTP requests are handled appropriately during shutdown.
- Kafka consumers stop cleanly.
- In-flight processing is not unnecessarily duplicated.
- Connections are disposed correctly.

---

## 45. Kafka Consumer Deployment

Kafka consumers such as:

- Notification.
- Search.
- Shipping.
- Order event consumers.

should be deployed as independently scalable services.

Scaling is controlled by:

```text
Consumer Group
      +
Kafka Partitions
```

The CI/CD pipeline should not assume that one Pod equals one Kafka consumer globally.

---

## 46. Service-Level Deployment

Each service should be independently deployable.

Example:

```text
Product changed
    |
    v
Product image rebuilt
    |
    v
Product deployment updated
```

There should be no requirement to redeploy all 12 services for a Product-only change.

---

## 47. Shared BuildingBlocks Changes

Shared BuildingBlocks require additional care.

If:

```text
src/BuildingBlocks/*
```

changes, dependent services may need to:

- Rebuild.
- Retest.
- Repackage.
- Redeploy.

The pipeline should identify affected services where practical.

---

## 48. GitHub Actions Workflow Structure

A possible workflow layout:

```text
.github/workflows/
├── ci.yml
├── integration-tests.yml
├── docker.yml
├── security.yml
├── deploy-minikube.yml
└── release.yml
```

These can initially be consolidated if the project is easier to maintain with fewer workflow files.

---

## 49. Recommended Initial Workflow Design

For the learning project, start with a small number of workflows:

```text
ci.yml
    |
    +--> Build
    +--> Unit Tests
    +--> Integration/Contract Tests
    +--> Security checks

docker.yml
    |
    +--> Build images
    +--> Scan images
    +--> Push images

deploy.yml
    |
    +--> Deploy Kubernetes
    +--> Rollout verification
    +--> Smoke tests
```

Split further only when complexity justifies it.

---

## 50. CI Matrix Strategy

A service matrix can reduce duplication.

Conceptually:

```yaml
matrix:
  service:
    - auth
    - user
    - product
    - inventory
    - cart
    - order
    - payment
    - shipping
    - review
    - notification
    - promotion
    - search
```

The exact implementation should be introduced only after service project paths are stable.

---

## 51. Parallel Execution

Independent CI jobs should run in parallel.

For example:

```text
                 +--> Auth Tests
                 |
                 +--> Product Tests
                 |
Build ---------->+--> Cart Tests
                 |
                 +--> Order Tests
```

This reduces overall pipeline duration.

---

## 52. Job Dependencies

Expensive jobs should depend on cheaper validation.

Example:

```text
Build
  |
  +--> Unit Tests
  |
  +--> Static Analysis
       |
       v
Integration Tests
       |
       v
Docker Build
       |
       v
Deployment
```

A failing prerequisite should stop downstream deployment.

---

## 53. Caching

GitHub Actions caching may be used for:

- NuGet packages.
- Docker build layers where appropriate.

Caching should improve speed without affecting correctness.

Cache invalidation must be handled safely.

---

## 54. CI Resource Management

The pipeline should avoid:

- Starting unnecessary infrastructure.
- Running all E2E tests for trivial changes.
- Building unrelated images.
- Repeating expensive scans unnecessarily.

However, optimization should not compromise important validation.

---

## 55. Testcontainers in CI

Testcontainers is especially useful because the CI runner does not need a permanent PostgreSQL/Redis/Kafka environment.

Example:

```text
GitHub Runner
      |
      v
Docker
      |
      +--> PostgreSQL
      +--> Redis
      +--> Kafka
      |
      v
Integration Tests
```

This aligns CI with local integration testing.

---

## 56. Docker Compose in CI

Docker Compose can be used for broader integration or E2E environments.

Example:

```text
docker compose up
       |
       v
Gateway + Services + Infrastructure
       |
       v
E2E Tests
       |
       v
docker compose down
```

This is useful before Kubernetes deployment.

---

## 57. Kubernetes CI Strategy

Full Kubernetes testing can be more expensive.

For the learning project:

### Pull Request

Use:

- Unit.
- Integration.
- Contract.
- Docker validation.

### Main

Optionally run:

- Full E2E.
- Compose integration.
- Image publishing.

### Deployment

Run:

- Kubernetes deployment.
- Rollout.
- Smoke tests.

---

## 58. Minikube Strategy

Minikube is primarily a local deployment environment.

The CI pipeline does not initially need to run a complete Minikube cluster for every pull request.

A future pipeline can use:

- Kind.
- Minikube.
- Another lightweight Kubernetes environment.

The production-like Kubernetes design remains represented by the manifests.

---

## 59. GitHub Actions Permissions

Workflows should use least privilege.

Examples:

- Read-only repository access where possible.
- Package write permission only for image publishing.
- Deployment credentials only in deployment workflows.
- Separate environment permissions.

---

## 60. Environment Protection

If a hosted deployment environment is introduced, GitHub environment protection can require:

- Approval.
- Restricted branches.
- Environment-specific secrets.

For the initial Minikube setup, manual approval is optional.

---

## 61. Release Strategy

A release can be created using Git tags.

Example:

```text
v0.1.0
v0.2.0
v1.0.0
```

The release pipeline can:

1. Validate the tagged commit.
2. Build images.
3. Scan images.
4. Push immutable tags.
5. Generate release metadata.
6. Deploy if configured.

---

## 62. Versioning Strategy

There are multiple useful identifiers:

```text
Application Version
Git Commit SHA
Docker Image Tag
Docker Image Digest
```

The Git SHA should always remain available for traceability.

---

## 63. Deployment Metadata

Services should expose version information through:

- Health/info endpoints where appropriate.
- Logs.
- Metrics resource attributes.
- Traces.

Example:

```text
service.name = commercex.order
service.version = 0.1.0
```

This connects CI/CD with Observability Design.

---

## 64. Deployment Observability

After deployment, monitor:

- Pod readiness.
- Restart count.
- HTTP error rate.
- HTTP latency.
- Kafka consumer lag.
- Redis connectivity.
- PostgreSQL connectivity.
- Checkout success/failure.

Deployment is not complete simply because Kubernetes reports Pods as running.

---

## 65. Failed Deployment Diagnostics

When a deployment fails, CI should collect useful diagnostics.

Examples:

```bash
kubectl get pods
kubectl describe pod
kubectl get events
kubectl logs
kubectl rollout status
```

The exact diagnostic commands can be automated.

---

## 66. Database Migration Failure

If a migration fails:

```text
Deployment
    |
    v
Migration
    |
    X
Failure
    |
    v
Stop Deployment
```

The application should not blindly start against an incompatible schema.

Migration failures should be clearly surfaced in CI/CD logs.

---

## 67. Rollback and Database Changes

Application rollback and database rollback are different problems.

For destructive schema changes, prefer compatible migration sequences.

Example:

```text
Release A
  |
  v
Add new column
  |
  v
Release B uses new column
  |
  v
Later remove old column
```

Avoid migrations that make immediate rollback impossible.

---

## 68. Supply Chain Security

The CI/CD process should eventually consider:

- Dependency scanning.
- Container scanning.
- Secret scanning.
- Trusted base images.
- Pinned action versions.
- Artifact provenance where practical.

The initial project should implement the first four without overcomplicating the pipeline.

---

## 69. GitHub Actions Security

Third-party GitHub Actions should be selected carefully.

Prefer:

- Official actions.
- Well-maintained actions.
- Pinned versions.
- Minimal permissions.

Avoid blindly copying arbitrary workflow code from the internet.

---

## 70. Branch Protection

The `main` branch should eventually require:

- Pull request.
- Successful CI.
- Review where appropriate.

Direct pushes can be restricted.

For early solo development, the rules can remain lightweight and become stricter as the project matures.

---

## 71. Quality Gates

Minimum CI quality gates:

```text
Build passes
AND
Required tests pass
AND
Contract checks pass
AND
Security checks pass
AND
Docker build passes
```

Deployment additionally requires:

```text
Kubernetes rollout passes
AND
Smoke tests pass
```

---

## 72. Pipeline Failure Policy

A pipeline should fail when:

- Compilation fails.
- Required tests fail.
- Critical contract incompatibility is detected.
- Unacceptable security vulnerability is detected.
- Docker build fails.
- Deployment rollout fails.
- Critical smoke tests fail.

Non-critical informational warnings should not unnecessarily block development.

---

## 73. Notifications

The initial project can rely on GitHub workflow status and pull-request feedback.

Later, notifications can be integrated with:

- Slack.
- Email.
- Other team communication systems.

A dedicated notification integration is not required for the initial implementation.

---

## 74. CI/CD Metrics

Useful pipeline metrics include:

- Build duration.
- Test duration.
- Failure rate.
- Deployment duration.
- Deployment success rate.
- Mean time to recovery.
- Failed deployment count.
- Rollback count.

These metrics can later become part of engineering observability.

---

## 75. Developer Feedback

A good pipeline should make failures understandable.

Each job should have clear names such as:

```text
Build
Unit Tests
Integration Tests
Contract Tests
Security Scan
Docker Build
Deploy
Smoke Tests
```

Avoid a single massive job containing every operation.

---

## 76. Local-to-CI Consistency

Developers should be able to reproduce important CI failures locally.

For example:

```bash
dotnet build
dotnet test
docker build
docker compose up
```

This minimizes the:

> Works on my machine

problem.

---

## 77. Local Pre-Push Validation

A lightweight local sequence can be:

```text
Format/Analyze
    ↓
Build
    ↓
Unit Tests
    ↓
Relevant Integration Tests
```

Full E2E tests can be run when necessary.

---

## 78. Development Workflow

Recommended developer workflow:

```text
Create Branch
     |
     v
Implement Change
     |
     v
Run Local Tests
     |
     v
Commit
     |
     v
Push
     |
     v
Open PR
     |
     v
CI Validation
     |
     v
Review
     |
     v
Merge
```

---

## 79. Main Branch Workflow

After merge:

```text
main
 |
 v
Full Validation
 |
 v
Docker Build
 |
 v
Security Scan
 |
 v
Push Images
 |
 v
Deploy Integration/Kubernetes
 |
 v
Smoke Tests
```

The exact automatic deployment policy can evolve.

---

## 80. Service Independence in CI/CD

The pipeline must preserve service independence.

For example:

```text
Product Service
   |
   +--> Product Unit Tests
   +--> Product Integration Tests
   +--> Product Docker Image
   +--> Product Deployment
```

This should not require rebuilding Payment Service unless a shared dependency changed.

---

## 81. Gateway CI/CD

Gateway changes should validate:

- Build.
- Routing.
- Authentication handling.
- Rate limiting.
- API tests.
- Docker image.
- Deployment.
- Smoke tests.

Because the Gateway is the main external entry point, it receives additional E2E attention.

---

## 82. Kafka CI/CD Considerations

Kafka-based services require validation that:

- Topics/configuration exist.
- Producers can publish.
- Consumers can connect.
- Event contracts remain compatible.
- Consumer groups process events.
- Duplicate events remain safe.

Kafka configuration should remain externalized.

---

## 83. Redis CI/CD Considerations

Redis-dependent services should validate:

- Connection.
- Authentication/configuration where enabled.
- Key behavior.
- TTLs.
- Cache fallback behavior.
- Cart persistence behavior.

---

## 84. PostgreSQL CI/CD Considerations

Database-dependent services should validate:

- Migration generation.
- Migration application.
- Schema compatibility.
- Connection configuration.
- Required extensions where used.

The Auth Service's PostgreSQL migration process is an example of this controlled approach.

---

## 85. gRPC CI/CD Considerations

gRPC services should validate:

- Proto compilation.
- Generated client compatibility.
- Server startup.
- Method behavior.
- Status-code mapping.
- Internal service connectivity.

Breaking protobuf changes should fail contract validation.

---

## 86. Event Schema CI/CD Considerations

Kafka schemas should be treated as versioned integration contracts.

CI should check:

```text
New Event Schema
      |
      v
Compatibility Validation
      |
      v
Consumer Tests
      |
      v
Approve
```

Schema Registry can support compatibility validation as the event architecture matures.

---

## 87. Artifact Retention

CI should retain enough information to diagnose failures and reproduce releases.

Useful artifacts:

- Test results.
- Coverage reports.
- Security scan reports.
- Build logs.
- Deployment logs.

Large temporary artifacts should not be retained indefinitely.

---

## 88. Dependency on External Services

CI should avoid unnecessary external dependencies.

Tests should prefer:

```text
Testcontainers
Docker
Local test infrastructure
```

instead of relying on:

```text
Third-party APIs
Real payment gateways
Real email providers
```

This keeps builds deterministic.

---

## 89. Simulated Payment in CI

Payment Service remains simulated.

CI should test deterministic scenarios such as:

```text
Valid test payment → success
Configured failure case → failure
Duplicate request → same result
```

No real financial provider is involved.

---

## 90. Security of CI Credentials

Credentials used by CI should:

- Be stored in GitHub secrets/environments.
- Have minimum permissions.
- Be rotated.
- Never be printed.
- Never be included in Docker images.

Deployment credentials should be separate from image-publishing credentials where practical.

---

## 91. Initial CI/CD Tooling

The initial toolset is intentionally small:

| Concern | Tool |
|---|---|
| Source Control | Git/GitHub |
| CI/CD | GitHub Actions |
| Build | .NET CLI |
| Testing | xUnit |
| Integration Infrastructure | Testcontainers |
| Containerization | Docker |
| Registry | GitHub Container Registry or equivalent |
| Deployment | Kubernetes |
| Kubernetes CLI | kubectl |
| Local Kubernetes | Minikube |
| Security | Dependency/container/secret scanning |
| Observability | OpenTelemetry + Grafana |

---

## 92. Tools Deferred

The following are not required initially:

- Jenkins.
- Argo CD.
- Flux.
- Helm-based release platform.
- Terraform.
- Pulumi.
- Service mesh.
- Complex deployment orchestrators.
- Multi-cloud deployment.
- Full GitOps platform.

They can be introduced later if the learning objectives require them.

---

## 93. GitOps Consideration

GitOps is a possible future extension.

The initial project can use GitHub Actions directly with Kubernetes because it keeps the architecture understandable.

A future design could introduce:

```text
Git
 |
 v
GitOps Controller
 |
 v
Kubernetes
```

This is intentionally deferred.

---

## 94. Canary/Blue-Green Deployment

Advanced deployment strategies are not required initially.

Start with:

```text
Rolling Update
```

Later, CommerceX could experiment with:

- Blue/green.
- Canary.
- Progressive delivery.

---

## 95. Deployment Strategy by Project Phase

### Early Development

```text
Local Docker Compose
```

### Service Integration

```text
GitHub Actions
  +
Docker Images
```

### Kubernetes Phase

```text
GitHub Actions
  +
Container Registry
  +
Minikube
```

### Future Hosted Environment

```text
GitHub Actions
  +
Container Registry
  +
Managed Kubernetes
```

---

## 96. CI/CD Pipeline Overview

The complete conceptual pipeline is:

```text
                    Developer
                        |
                        v
                 Feature Branch
                        |
                        v
                    Pull Request
                        |
                        v
              +-------------------+
              | GitHub Actions CI |
              +-------------------+
                        |
        +---------------+----------------+
        |               |                |
        v               v                v
      Build          Unit Tests       Security
        |               |                |
        +---------------+----------------+
                        |
                        v
               Integration Tests
                        |
                        v
                Contract/API Tests
                        |
                        v
                  Docker Build
                        |
                        v
                  Image Scan
                        |
                        v
                 Container Registry
                        |
                        v
                     Deploy
                        |
                        v
                Kubernetes Rollout
                        |
                        v
                  Smoke Tests
                        |
                        v
                    Running
```

---

## 97. End-to-End Release Flow

A release should follow:

```text
Code
 ↓
Pull Request
 ↓
CI
 ↓
Review
 ↓
main
 ↓
Full Validation
 ↓
Docker Images
 ↓
Security Scan
 ↓
Registry
 ↓
Kubernetes Deployment
 ↓
Rollout Verification
 ↓
Smoke/E2E Tests
 ↓
Release
```

---

## 98. Definition of Done for CI/CD

The CI/CD implementation is considered complete when:

- GitHub Actions builds the project.
- Automated tests execute.
- Integration infrastructure can start.
- Security checks execute.
- Docker images build.
- Images can be pushed to a registry.
- Kubernetes manifests can be deployed.
- Rollouts are verified.
- Smoke tests execute.
- Deployment failures are visible.
- Rollback can be performed.
- Secrets are externalized.
- Artifacts are traceable to Git commits.

---

## 99. Implementation Checklist

### Repository

- [ ] Branch strategy documented.
- [ ] Main branch protection configured.
- [ ] GitHub Actions directory created.
- [ ] Workflow naming established.

### CI

- [ ] .NET SDK setup.
- [ ] Restore.
- [ ] Build.
- [ ] Unit tests.
- [ ] Integration tests.
- [ ] API/contract tests.
- [ ] Test reports.
- [ ] Code quality checks.

### Security

- [ ] Dependency scanning.
- [ ] Secret scanning.
- [ ] Docker image scanning.
- [ ] Minimal workflow permissions.
- [ ] GitHub secrets configured.

### Docker

- [ ] Service Dockerfiles.
- [ ] Multi-stage builds.
- [ ] Image tagging.
- [ ] Registry authentication.
- [ ] Image publishing.
- [ ] Container smoke tests.

### Kubernetes

- [ ] Deployment workflow.
- [ ] Namespace.
- [ ] ConfigMaps.
- [ ] Secrets.
- [ ] Services.
- [ ] Rollout verification.
- [ ] Smoke tests.
- [ ] Rollback validation.

### Database

- [ ] Migration process defined.
- [ ] Migration validation.
- [ ] Controlled migration execution.
- [ ] Compatibility reviewed.

### Release

- [ ] Git tag strategy.
- [ ] Image versioning.
- [ ] Artifact traceability.
- [ ] Release notes.
- [ ] Rollback process.

---

## 100. CI/CD Anti-Patterns

CommerceX should avoid:

### 100.1 One Giant Pipeline

Everything in one unmaintainable job.

### 100.2 Rebuilding Per Environment

This breaks artifact consistency.

### 100.3 `latest` as the Only Image Tag

It destroys deployment traceability.

### 100.4 Secrets in Git

A major security failure.

### 100.5 Blind Database Migration on Startup

Can create deployment races and unsafe schema changes.

### 100.6 Deploying Without Tests

Defeats the purpose of CI/CD.

### 100.7 Deploying Every Service for Every Change

Removes microservice independence.

### 100.8 Excessive Pipeline Complexity

Do not introduce enterprise tooling before the learning objective requires it.

### 100.9 Ignoring Rollback

Every deployment should have a recovery path.

### 100.10 Ignoring Observability After Deployment

A successful rollout does not necessarily mean a healthy system.

---

## 101. CI/CD Quality Gates Summary

### Pull Request

```text
Build
+
Unit Tests
+
Relevant Integration Tests
+
Contract Tests
+
Security Checks
```

### Main Branch

```text
Full Required Tests
+
Docker Build
+
Image Scan
+
Image Publish
```

### Deployment

```text
Kubernetes Rollout
+
Health Checks
+
Smoke Tests
```

### Release

```text
Validated Artifact
+
Traceable Version
+
Deployment Verification
+
Rollback Capability
```

---

## 102. Architecture Decisions

The following decisions establish the CommerceX CI/CD baseline:

1. GitHub Actions is the primary CI/CD platform.
2. GitHub is the primary source-control platform.
3. CommerceX uses a monorepo.
4. Pull requests trigger automated CI.
5. `main` represents validated code.
6. Unit tests execute before expensive integration tests.
7. Testcontainers is recommended for disposable integration infrastructure.
8. REST, gRPC, and Kafka contracts are tested automatically where applicable.
9. Security and dependency checks are part of CI.
10. Each deployable application has its own Docker image.
11. Docker images use multi-stage .NET builds.
12. Images are tagged with Git-commit-based identifiers.
13. Images are stored in an OCI-compatible registry.
14. The same validated image should be promoted between environments.
15. Configuration is externalized from images.
16. Secrets are stored outside source code and images.
17. Database migrations are executed in a controlled process.
18. Kubernetes is the deployment target.
19. Minikube is the initial Kubernetes environment.
20. Kubernetes rolling updates are the initial deployment strategy.
21. Post-deployment rollout and smoke tests are required.
22. Kubernetes rollback is supported.
23. Service deployments remain independently deployable.
24. Shared BuildingBlocks changes may trigger broader builds.
25. GitOps, advanced progressive delivery, and hosted multi-environment deployment are deferred.
26. CI/CD should remain simple enough to understand and maintain as a learning project.

---

## 103. Relationship With Other Documents

This document builds directly on:

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
- Kubernetes Design
- Observability Design
- Testing Strategy

It provides the delivery process required to implement those architectural decisions.

The next document is:

**Document 19 — Development Roadmap**

---

## 104. Baseline Status

This document establishes the **CommerceX CI/CD Design baseline**.

Future CI/CD implementation should follow these decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 19 — Development Roadmap
