# CommerceX — Security Design

**Document:** 13 — Security Design  
**Project:** CommerceX  
**Status:** Architecture Baseline  
**Previous Document:** 12 — Redis Caching Strategy  
**Next Document:** 14 — Docker Design

---

## 1. Purpose

This document defines the security architecture and security requirements for CommerceX.

CommerceX is a distributed e-commerce backend consisting of an API Gateway and 12 independently deployable services. Security therefore has to be considered at several boundaries:

- Client to API Gateway.
- Gateway to backend services.
- Service to service.
- Application to database.
- Application to Redis.
- Producers and consumers communicating through Kafka.
- Developers and CI/CD pipelines interacting with infrastructure.

The design focuses on realistic security practices suitable for a learning-oriented distributed system without introducing unnecessary enterprise complexity.

---

## 2. Security Objectives

The primary security objectives are:

1. Authenticate users securely.
2. Authorize operations based on identity and role.
3. Protect passwords and authentication credentials.
4. Protect access and refresh tokens.
5. Prevent unauthorized access to customer resources.
6. Protect service-to-service communication.
7. Validate and sanitize untrusted input.
8. Protect databases, Redis, and Kafka from unauthorized access.
9. Avoid exposing sensitive information through logs or error responses.
10. Protect secrets and configuration.
11. Provide security-relevant observability.
12. Maintain security boundaries between microservices.
13. Detect and handle common API and application attacks.
14. Keep the security architecture understandable and implementable.

---

## 3. Security Architecture

The high-level security model is:

```text
                    Internet / Client
                           |
                           | HTTPS
                           v
                  +-------------------+
                  |    API Gateway    |
                  | Authentication    |
                  | Authorization     |
                  | Rate Limiting     |
                  +---------+---------+
                            |
                 +----------+----------+
                 |                     |
              REST/gRPC             REST/gRPC
                 |                     |
        +--------+--------+    +-------+-------+
        | Backend Services|    | Backend Services|
        +--------+--------+    +-------+-------+
                 |                     |
          +------+-------+       +-----+------+
          | PostgreSQL   |       |    Redis   |
          +--------------+       +------------+
                                               \ Kafka
                                             +----------+
                    |  Kafka   |
                    +----------+
```

Security is applied at every boundary rather than relying exclusively on the Gateway.

---

## 4. Security Principles

CommerceX follows these principles:

### 4.1 Least Privilege

Users and services receive only the permissions required for their responsibilities.

### 4.2 Defense in Depth

Security should not depend on one layer.

For example:

```text
Authentication
    +
Authorization
    +
Input Validation
    +
Database Permissions
    +
Network Restrictions
```

### 4.3 Secure by Default

Endpoints and infrastructure should deny access unless access is explicitly permitted.

### 4.4 Service Ownership

Each service owns its security-sensitive data and business authorization rules.

### 4.5 No Trust Based on Network Location Alone

Being inside the Kubernetes cluster does not automatically make a request trusted.

### 4.6 Fail Securely

Failures should not accidentally grant access.

### 4.7 Minimize Sensitive Data

Only collect and store information that is required.

---

## 5. Threat Model

CommerceX should consider threats from:

- Unauthenticated internet users.
- Authenticated users attempting to access other customers' resources.
- Malicious clients using Postman or custom scripts.
- Compromised service credentials.
- Malicious or malformed API requests.
- Stolen access tokens.
- Brute-force login attempts.
- Credential stuffing.
- Injection attacks.
- Malicious file uploads if upload functionality is introduced.
- Compromised infrastructure components.
- Malicious or incorrectly implemented Kafka consumers.
- Secret leakage through source code or logs.
- Vulnerable third-party dependencies.

The system should be designed assuming that client requests cannot be trusted.

---

## 6. Authentication Architecture

Authentication is owned by the Auth Service.

The Auth Service is responsible for:

- User registration.
- Credential storage.
- Password verification.
- Access token issuance.
- Refresh token management.
- Logout/revocation.
- Password reset.
- Authentication-related events.

The User Service owns customer profile data but does not own authentication credentials.

---

## 7. Registration Flow

A simplified registration flow:

```text
Client
  |
  v
API Gateway
  |
  v
Auth Service
  |
  +--> Validate request
  |
  +--> Hash password
  |
  +--> Store credential
  |
  +--> Publish UserRegistered
  |
  v
Response
```

The password must never be stored in plaintext.

---

## 8. Password Security

Passwords must be stored using a strong password hashing algorithm designed for password storage.

Recommended options include:

- Argon2id.
- PBKDF2.
- bcrypt.

For the CommerceX .NET implementation, a well-established framework/library implementation should be preferred rather than writing a custom password hashing algorithm.

The system must never:

- Store plaintext passwords.
- Store reversible encrypted passwords as the normal credential representation.
- Log passwords.
- Return passwords through APIs.

Password hashing parameters should be configurable and reviewed as dependencies evolve.

---

## 9. Login Flow

```text
Client
  |
  | username/password
  v
Gateway
  |
  v
Auth Service
  |
  +--> Find credential
  |
  +--> Verify password hash
  |
  +--> Issue access token
  +--> Issue refresh token
  |
  v
Client
```

Authentication failure responses should avoid revealing whether a specific username/email exists.

Prefer a generic response such as:

```text
Invalid credentials.
```

rather than:

```text
Email does not exist.
```

This reduces account enumeration risk.

---

## 10. Access Tokens

CommerceX uses bearer access tokens for authenticated API access.

The initial design uses JWT access tokens.

A token may contain claims such as:

```text
sub
role
iat
exp
iss
aud
jti
```

Where appropriate:

- `sub` identifies the authenticated user.
- `role` identifies authorization role(s).
- `iat` identifies issuance time.
- `exp` identifies expiration time.
- `iss` identifies the issuer.
- `aud` identifies intended audience.
- `jti` identifies the token.

Only required claims should be included.

---

## 11. Access Token Lifetime

Access tokens should be short-lived.

An initial configuration may use:

```text
Access Token Lifetime: 15 minutes
```

The exact value should be configurable.

Shorter access-token lifetimes reduce the impact of token theft.

Refresh tokens are used to obtain new access tokens.

---

## 12. Refresh Tokens

Refresh tokens should be:

- High entropy.
- Unpredictable.
- Stored securely.
- Associated with the appropriate user/session.
- Revocable.
- Expirable.
- Rotated where appropriate.

The Auth Service stores refresh-token state in PostgreSQL.

A refresh-token database record can include:

```text
RefreshTokenId
UserId
TokenHash
ExpiresAt
CreatedAt
RevokedAt
```

The raw refresh token should not need to be stored permanently when a secure hash can be used for lookup/verification.

---

## 13. Refresh Token Rotation

A recommended flow is:

```text
Refresh Token A
      |
      v
Auth Service
      |
      +--> Validate A
      |
      +--> Revoke A
      |
      +--> Create Refresh Token B
      |
      +--> Issue new Access Token
```

Rotation limits the useful lifetime of a stolen refresh token.

If reuse detection is implemented, suspicious reuse can cause the associated refresh-token session/family to be revoked.

The initial implementation can keep this mechanism simple while maintaining revocation and expiration.

---

## 14. Logout

Logout should invalidate the relevant refresh-token session.

Example:

```text
Client
  |
  v
Auth Service
  |
  v
Revoke refresh token
```

JWT access tokens that are already issued may remain valid until expiration unless a token-revocation mechanism is introduced.

The short access-token lifetime limits this window.

---

## 15. Password Reset

Password reset must use a secure, unpredictable, short-lived reset token.

Flow:

```text
Request Reset
     |
     v
Auth Service
     |
     v
Generate secure token
     |
     v
Store hashed token
     |
     v
Simulated notification/event
```

Reset tokens should:

- Have a short expiration.
- Be single-use.
- Be high entropy.
- Be stored securely.
- Not be logged.
- Not be returned through normal administrative APIs.

After successful reset:

```text
Reset Token -> Consumed
Credentials -> Updated
Existing Sessions -> Optionally Revoked
```

The exact session-revocation behavior should be defined during implementation.

---

## 16. Authorization

Authentication answers:

> Who is the caller?

Authorization answers:

> What is the caller allowed to do?

CommerceX uses role-based authorization initially.

Primary roles:

```text
Customer
Admin
```

More roles can be introduced later if business requirements justify them.

---

## 17. Role-Based Access Control

Example:

| Operation | Customer | Admin |
|---|---:|---:|
| View products | Yes | Yes |
| Manage own profile | Yes | Yes |
| Manage own cart | Yes | Yes |
| Create order | Yes | Yes |
| Manage products | No | Yes |
| Adjust inventory | No | Yes |
| Create promotion | No | Yes |
| Manage categories | No | Yes |
| Manage all users | No | Yes |

Authorization should be enforced at the service/application boundary, not only by the frontend.

---

## 18. Resource Ownership

Role checks alone are insufficient.

For customer-owned resources, the service must verify ownership.

Example:

```text
GET /api/v1/users/{id}
```

must not allow Customer A to retrieve Customer B's private profile simply because Customer A is authenticated.

Prefer APIs such as:

```text
GET /api/v1/users/me
```

for self-service operations.

For resources identified by arbitrary IDs:

```text
Authenticated User
       |
       v
Does resource.ownerId == userId?
       |
   +---+---+
  Yes     No
   |       |
Allow     Deny
```

---

## 19. Gateway Security Responsibilities

The API Gateway should provide security controls such as:

- TLS termination where appropriate.
- JWT validation.
- Routing.
- Authentication propagation.
- Rate limiting.
- Request-size limits.
- Correlation IDs.
- Basic security headers where applicable.

The Gateway should not contain core business authorization logic.

Services must still enforce authorization for sensitive operations.

---

## 20. Defense in Depth for Authorization

A request should not be considered authorized solely because it passed through the Gateway.

Example:

```text
Client
  |
  v
Gateway
  |
  +--> Valid JWT
  |
  v
Order Service
  |
  +--> Verify role
  +--> Verify resource ownership
  +--> Execute operation
```

This protects the system if a service is later exposed internally or the Gateway configuration is accidentally weakened.

---

## 21. Service-to-Service Authentication

Internal service communication should be authenticated.

For the initial implementation, services can use authenticated service credentials/tokens for internal calls where required.

Possible approaches include:

- Service JWTs.
- Internal API keys for limited development scenarios.
- mTLS in a more advanced deployment.

The initial learning implementation should avoid introducing a complex service mesh solely for service identity.

---

## 22. gRPC Security

gRPC is primarily internal.

Security requirements include:

- Authenticate service callers.
- Authorize sensitive operations.
- Use TLS in environments where traffic crosses untrusted networks.
- Configure deadlines.
- Avoid transmitting secrets unnecessarily.
- Validate request values.

Example:

```text
Order Service
    |
    | authenticated gRPC
    v
Inventory Service
```

The Inventory Service remains responsible for determining whether the requested operation is permitted.

---

## 23. Kafka Security

Kafka is an internal infrastructure component.

Security considerations:

- Do not expose Kafka directly to the public internet.
- Restrict broker access to authorized services.
- Authenticate producers/consumers where appropriate.
- Authorize topic access.
- Protect Kafka credentials.
- Use TLS for encrypted transport where required.
- Avoid sensitive information in event payloads.

Example principle:

```text
Order Service
   |
   | write access
   v
commercex.order.events
```

A service should not automatically receive permission to produce to every topic.

---

## 24. Event Security

Kafka events should contain only the data required by consumers.

Avoid placing:

- Passwords.
- Password hashes.
- Access tokens.
- Refresh tokens.
- Payment credentials.
- Unnecessary personal information.

Example acceptable event:

```json
{
  "eventType": "OrderConfirmed",
  "data": {
    "orderId": "order-id",
    "customerId": "customer-id",
    "totalAmountMinor": 12500,
    "currency": "USD"
  }
}
```

The event should not contain the customer's password or authentication token.

---

## 25. Database Security

Each service database should have controlled credentials.

Recommended principle:

```text
Auth Service
    |
    +--> Auth DB credentials

Order Service
    |
    +--> Order DB credentials
```

The Order Service should not receive credentials for the Auth database.

This enforces database ownership at the infrastructure level.

---

## 26. PostgreSQL Permissions

Where practical, each service should use a database user with only the required privileges.

For example:

```text
commercex_order_app
       |
       +--> orders
       +--> order_items
       +--> order_status_history
```

It should not have permissions on:

```text
commercex_auth
commercex_inventory
commercex_payment
```

Local development may simplify PostgreSQL administration, but service ownership should still be preserved architecturally.

---

## 27. Redis Security

Redis should be private infrastructure.

Requirements:

- Do not expose Redis to clients.
- Restrict network access.
- Protect authentication credentials.
- Use TLS where required.
- Avoid sensitive data storage.
- Do not log authentication tokens or confidential cart contents.
- Restrict service access according to need.

Only services that require Redis should have network access to it.

---

## 28. Input Validation

All external input must be considered untrusted.

Validation should occur at appropriate layers.

### API Layer

Validate:

- Required fields.
- Formats.
- Lengths.
- Numeric ranges.
- Pagination limits.

### Application Layer

Validate:

- Business command requirements.
- Authorization context.
- Cross-entity rules.

### Domain Layer

Enforce:

- Invariants.
- Valid state transitions.
- Business rules.

### Database

Enforce:

- Unique constraints.
- Required values.
- Referential integrity within the service.

---

## 29. SQL Injection

Entity Framework Core parameterized queries should be preferred.

Avoid constructing SQL using direct string concatenation with user input.

Unsafe concept:

```csharp
$"SELECT * FROM Products WHERE Name = '{name}'"
```

Prefer parameterized LINQ/EF Core operations.

If raw SQL is required, parameters must be used.

---

## 30. NoSQL/Redis Injection Considerations

Redis commands should not be constructed from arbitrary unvalidated user input.

Key generation should use controlled templates.

For example:

```text
commercex:cart:{validatedCustomerId}
```

Do not allow users to submit arbitrary Redis keys.

---

## 31. Cross-Site Scripting and JSON APIs

CommerceX primarily exposes JSON APIs rather than server-rendered HTML.

Nevertheless:

- Validate input.
- Avoid returning unsafe HTML.
- Treat review text and other user-generated content as untrusted.
- If a future frontend renders user-generated text, it must perform appropriate output encoding.

Review content should never be trusted merely because it came from the Review Service.

---

## 32. Request Size Limits

The Gateway and services should enforce reasonable request-size limits.

This reduces the risk of resource exhaustion.

Examples:

- JSON body size limits.
- Pagination maximums.
- Maximum review length.
- Maximum cart item count where appropriate.
- Maximum search query length.

Limits should be configuration-driven.

---

## 33. Rate Limiting

The API Gateway should apply rate limiting to abuse-sensitive operations.

High-priority targets include:

- Login.
- Registration.
- Password reset.
- Token refresh.
- Search.
- Public product endpoints.

Example:

```text
Client
  |
  v
Rate Limiter
  |
  +--> Within limit -> API
  |
  +--> Exceeded -> 429
```

Rate limits should be selected based on expected traffic and adjusted through testing.

---

## 34. Brute-Force Protection

Authentication endpoints should have protections against repeated credential attempts.

Possible controls:

- Rate limiting.
- Temporary throttling.
- Monitoring repeated failures.
- Progressive delays where appropriate.
- Account/session protection.

The system should avoid permanent account lockout designs that could allow attackers to intentionally lock legitimate users out.

---

## 35. Error Handling

Security-sensitive errors should not reveal internal implementation details.

Avoid responses such as:

```text
PostgreSQL connection failed at 10.0.0.12:5432
```

or:

```text
Credential row exists for user@example.com
```

Use standardized error responses.

Example:

```json
{
  "type": "https://commercex/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "detail": "You are not authorized to perform this operation.",
  "traceId": "..."
}
```

Internal technical details belong in secure logs, not public responses.

---

## 36. Security Headers

For HTTP responses, appropriate security headers should be considered.

Depending on Gateway/frontend deployment:

- `Strict-Transport-Security`
- `X-Content-Type-Options`
- `Content-Security-Policy` where applicable
- `Referrer-Policy`

The exact header policy should reflect whether CommerceX is serving only APIs or also web content.

---

## 37. HTTPS and TLS

External API traffic should use HTTPS.

Development may use locally trusted development certificates or HTTP in an isolated local environment where necessary, but production-like environments should use TLS.

Internal traffic should also be encrypted when it crosses security boundaries or untrusted networks.

---

## 38. Secrets Management

Secrets must never be committed to Git.

Sensitive values include:

- JWT signing keys.
- Database passwords.
- Redis passwords.
- Kafka credentials.
- Service authentication secrets.
- TLS private keys.

Development configuration should use mechanisms such as:

```text
Environment variables
User Secrets
Kubernetes Secrets
CI/CD secret storage
```

Never commit:

```text
appsettings.Production.json
```

containing real secrets.

---

## 39. JWT Signing Key Protection

The JWT signing key is security-critical.

It should:

- Have sufficient entropy.
- Be stored outside source control.
- Be provided through secure configuration.
- Be rotated according to an appropriate operational policy.
- Not be logged.

The Auth Service is responsible for token issuance.

Services validating tokens need the appropriate validation configuration/key material without obtaining unnecessary Auth database access.

---

## 40. Secret Rotation

The architecture should allow secrets to be changed without source-code modification.

Examples:

```text
JWT signing key
Database password
Redis credential
Kafka credential
```

Secret rotation procedures can be expanded in the deployment documentation.

For the initial project, the important requirement is that secrets are configuration-managed rather than hard-coded.

---

## 41. Logging Security

Logs are an important attack and troubleshooting surface.

Do not log:

- Passwords.
- Password hashes.
- Access tokens.
- Refresh tokens.
- Reset tokens.
- Payment credentials.
- Database passwords.
- Redis credentials.

Sensitive identifiers should be minimized or masked when necessary.

Useful security-related logs include:

```text
Authentication failure
Authorization failure
Rate limit exceeded
Invalid token
Password reset requested
Refresh token revoked
Suspicious repeated login failures
```

---

## 42. Audit Logging

Security-sensitive actions should be auditable.

Examples:

- Login success/failure.
- Logout.
- Password reset.
- Role/permission changes.
- Product administration.
- Inventory adjustment.
- Promotion creation/update.
- Order cancellation.
- Payment processing result.

An audit abstraction can be introduced without forcing all services to share a common business-domain audit model.

Audit records should not contain secrets.

---

## 43. Correlation and Trace IDs

Every request should have a correlation/trace identifier.

Example:

```text
Client
  |
  | X-Correlation-ID / trace context
  v
Gateway
  |
  v
Order Service
  |
  +--> Inventory
  |
  +--> Payment
  |
  +--> Kafka
```

This allows security and operational events to be connected across services.

Sensitive information should not be placed inside correlation IDs.

---

## 44. File Upload Security

The initial CommerceX scope does not require file uploads.

If future features introduce:

- Product images.
- Profile images.
- Review attachments.

the system must not trust client-provided file types.

Controls should include:

1. Allowlist permitted file types.
2. Validate actual file signatures.
3. Limit file size.
4. Generate safe server-side filenames.
5. Store uploads outside executable paths.
6. Scan files for malware where appropriate.
7. Do not trust the extension alone.
8. Prevent path traversal.
9. Restrict content types.
10. Use object storage rather than service-local filesystem storage where appropriate.

This functionality is explicitly deferred from the initial implementation.

---

## 45. Dependency Security

CommerceX uses many third-party packages.

Security practices include:

- Keep .NET and NuGet packages updated.
- Review dependency vulnerabilities.
- Remove unused dependencies.
- Pin or centrally manage versions.
- Run automated dependency/security checks in CI.
- Review high-severity vulnerabilities before release.

GitHub Actions can later include dependency/security scanning.

---

## 46. Container Security

Docker images should follow secure practices.

Requirements:

- Use official trusted base images.
- Keep images updated.
- Avoid unnecessary packages.
- Do not embed secrets.
- Prefer non-root execution where practical.
- Use `.dockerignore`.
- Keep runtime images smaller than build images.
- Scan images for vulnerabilities.

The detailed container design is defined in Document 14.

---

## 47. Kubernetes Security

Kubernetes security should include:

- Namespace isolation.
- Service accounts with least privilege.
- Secrets for sensitive configuration.
- Network policies where practical.
- Resource limits.
- Non-root containers where possible.
- Read-only filesystems where practical.
- Restricted container capabilities.
- No unnecessary host access.

The initial Minikube environment can implement the most important controls first and evolve later.

---

## 48. Kubernetes Secrets

Sensitive configuration should be represented using Kubernetes Secrets.

Examples:

```text
JWT signing key
PostgreSQL password
Kafka credentials
Redis credentials
```

Non-sensitive configuration can use ConfigMaps.

Important:

Kubernetes Secret objects provide configuration management, but production environments may require additional secret-management systems.

---

## 49. Network Security

A logical network model is:

```text
External
   |
   v
Gateway
   |
   v
Services
   |
   +--> PostgreSQL
   +--> Redis
   +--> Kafka
```

Databases, Redis, and Kafka should not be directly exposed to external clients.

Only required ports should be exposed.

---

## 50. Kubernetes Network Policies

Network policies can restrict traffic.

Example conceptual rules:

```text
Gateway → Backend Services
Backend Services → Own Database
Required Services → Kafka
Cart/Product → Redis
```

A service should not have unrestricted access to every infrastructure component.

Network policies can initially be introduced after the application communication model is stable.

---

## 51. Database Isolation

The strongest service boundary is:

```text
Service
  |
  v
Own Database
```

For example:

```text
Order Service
     |
     v
commercex_orders
```

The Order Service must not directly query:

```text
commercex_auth
commercex_users
commercex_products
```

If it needs information, it should use REST, gRPC, or Kafka-based integration.

---

## 52. Authorization for Administrative Operations

Administrative endpoints require explicit authorization.

Examples:

```text
POST /api/v1/products
PUT /api/v1/products/{id}
POST /api/v1/promotions
POST /api/v1/inventory/{productId}/adjust
```

These operations should require:

```text
role = Admin
```

The service should enforce the requirement even if the Gateway already checks it.

---

## 53. Customer Data Protection

Customer data should be minimized and protected.

Examples of personal data include:

- Name.
- Email.
- Phone number.
- Address.

The system should:

- Restrict access.
- Avoid unnecessary replication.
- Avoid placing sensitive data in Kafka events.
- Avoid logging complete profiles.
- Encrypt transport.
- Retain data only as required by project requirements.

---

## 54. Order Data Protection

Order records can contain customer and address information.

Orders should therefore be accessible only to:

- The owning customer.
- Authorized administrative users.
- Authorized internal services through defined contracts.

Historical address snapshots are required for business correctness but should not be exposed unnecessarily.

---

## 55. Payment Security

CommerceX uses simulated payment processing.

The project must not store real:

- Credit card numbers.
- CVVs.
- Bank credentials.
- Payment-provider secrets.

The Payment Service can simulate a result based on test inputs or deterministic rules.

This deliberately keeps the project outside the complexity of integrating a real payment processor.

---

## 56. Inventory Security

Inventory adjustment operations are administrative.

Customers may:

```text
View appropriate availability
```

but must not directly:

```text
Adjust stock
```

Stock reservation is performed through the defined Order → Inventory service interaction.

Inventory remains the authoritative owner of stock quantities.

---

## 57. Promotion Security

Promotion creation and modification are administrative operations.

Promotion validation during checkout is a controlled internal operation.

Customers should not be able to modify:

- Discount percentage.
- Fixed discount.
- Expiration.
- Usage limits.
- Promotion status.

The Promotion Service remains the authority for promotion validity.

---

## 58. Review Security

Customers may create and modify their own reviews subject to business rules.

Security rules include:

- Customer identity comes from authenticated context.
- Customer cannot impersonate another customer in the request body.
- Customer can modify/delete only their own review.
- Rating must be 1–5.
- Review content must meet configured length limits.

For example, the API should not trust:

```json
{
  "customerId": "another-customer"
}
```

when the authenticated identity is already available from the token.

---

## 59. Search Security

Search requests are public or customer-accessible depending on deployment requirements.

Controls include:

- Query-length limits.
- Pagination limits.
- Filter validation.
- Rate limiting.
- Protection against expensive unrestricted queries.

Search should not expose internal product fields that are not intended for public consumption.

---

## 60. Security for Notification Data

Notification events should contain only the information required to generate a notification.

The Notification Service should not receive:

- Passwords.
- Tokens.
- Payment credentials.

Notification records should be protected because they may contain customer contact information and message content.

---

## 61. Security Testing

Security testing should be integrated into normal development.

### Unit Tests

Test:

- Authorization policies.
- Ownership checks.
- Token validation.
- Password rules.
- Business security invariants.

### Integration Tests

Test:

- Authentication.
- Authorization.
- Gateway access.
- Database isolation.
- Redis access.
- Kafka permissions where configured.

### API Security Tests

Test:

- Missing token.
- Invalid token.
- Expired token.
- Wrong role.
- Wrong resource owner.
- Malformed input.
- Excessive request size.
- Rate limit behavior.

---

## 62. Negative Testing

Security tests should focus heavily on denied behavior.

Examples:

```text
Customer → Admin endpoint → 403
Customer A → Customer B's order → 403/404
Expired JWT → protected endpoint → 401
Invalid JWT → protected endpoint → 401
Anonymous → protected endpoint → 401
Malformed request → 400
Rate limit exceeded → 429
```

A secure system must demonstrate that unauthorized operations fail.

---

## 63. Security Headers and API Documentation

OpenAPI/Swagger documentation should not expose secrets or internal credentials.

Development Swagger UI may be enabled for local environments.

Production deployment should:

- Restrict Swagger access where necessary.
- Avoid exposing internal service details publicly.
- Require authentication for administrative API documentation if appropriate.

---

## 64. Development Security

Developers should:

- Never commit secrets.
- Use local environment configuration.
- Use test credentials only.
- Avoid production data in development.
- Keep dependencies updated.
- Run tests before commits.
- Review security-sensitive changes.
- Use GitHub branch protection/PR review where appropriate.

---

## 65. CI/CD Security

GitHub Actions should follow least privilege.

CI/CD workflows should:

- Use GitHub Secrets for sensitive credentials.
- Avoid printing secrets.
- Pin important action versions where practical.
- Scan dependencies.
- Build reproducible images.
- Scan container images.
- Run tests.
- Avoid giving deployment jobs unnecessary repository permissions.

Deployment credentials should be separated from normal CI credentials.

Detailed CI/CD controls are defined in the CI/CD Design document.

---

## 66. Security Observability

Security-relevant metrics and events should be visible through the observability stack.

Examples:

- Authentication failures.
- Authorization failures.
- HTTP 401 count.
- HTTP 403 count.
- HTTP 429 count.
- Password-reset requests.
- Token refresh failures.
- Service authentication failures.
- Kafka authentication/authorization errors.
- Database connection failures.

Grafana dashboards can later provide a security-oriented operational view.

---

## 67. Incident Response for the Learning Project

The initial project does not require a full enterprise SOC process.

A basic response model is sufficient:

```text
Detect
  |
  v
Investigate
  |
  v
Contain
  |
  v
Correct
  |
  v
Verify
  |
  v
Document
```

Examples:

### Stolen JWT

- Revoke affected refresh sessions if possible.
- Wait for short access-token expiry or introduce token revocation if required.
- Rotate signing keys if compromise is suspected.

### Database Credential Exposure

- Rotate the credential.
- Update deployment configuration.
- Review access logs.
- Remove the secret from source control/history where necessary.

### Compromised Service

- Restrict service access.
- Review credentials.
- Rotate service credentials.
- Inspect logs and events.
- Redeploy from a trusted image.

---

## 68. Security Boundaries by Service

| Service | Primary Security Responsibility |
|---|---|
| Auth | Identity, credentials, tokens |
| User | Profile/address ownership |
| Product | Product administration |
| Inventory | Stock administration/reservation authorization |
| Cart | Customer cart ownership |
| Order | Order ownership/lifecycle authorization |
| Payment | Internal payment authorization/idempotency |
| Shipping | Shipment access/lifecycle |
| Review | Review ownership |
| Notification | Notification data protection |
| Promotion | Promotion administration/validation |
| Search | Query/resource exposure |

---

## 69. Security Communication Matrix

| From | To | Mechanism | Security |
|---|---|---|---|
| Client | Gateway | HTTPS/REST | JWT where required |
| Gateway | Services | REST | Authenticated internal request |
| Order | Inventory | gRPC | Service authentication |
| Order | Promotion | gRPC | Service authentication |
| Order | Payment | gRPC | Service authentication |
| Services | Kafka | Kafka | Broker authentication/authorization |
| Services | PostgreSQL | TCP | DB credentials/network restriction |
| Cart/Product | Redis | TCP | Redis authentication/network restriction |

The exact internal authentication mechanism can be refined during implementation.

---

## 70. Security and Eventual Consistency

Security decisions must not depend on stale derived data when authorization is critical.

For example:

- A user's role must be validated from an authoritative/authenticated context.
- Ownership must be determined from trusted service data.
- Product cache should not determine authorization.
- Kafka-derived search data should not be treated as the authoritative source for sensitive operations.

Eventual consistency is appropriate for derived data but not as a justification for bypassing authorization.

---

## 71. Security and Idempotency

Security-sensitive operations should also consider replay.

Examples:

- Payment processing.
- Inventory reservation.
- Refresh token operations.
- Password reset.
- Order creation.

Idempotency keys or unique operation identifiers should be used where repeated requests could otherwise create duplicate business effects.

---

## 72. Security and Distributed Transactions

CommerceX will not use distributed database transactions merely to provide security.

Instead:

- Each service secures its own database.
- Authentication/authorization occurs at service boundaries.
- Business operations use gRPC/Kafka contracts.
- Idempotency handles retries.
- Local transactions protect local state.

Security and consistency are related but should remain separate architectural concerns.

---

## 73. Initial Security Scope

### Implement Initially

- JWT authentication.
- Password hashing.
- Refresh tokens.
- Password reset tokens.
- Role-based authorization.
- Customer resource ownership.
- Gateway authentication.
- Service-level authorization.
- Input validation.
- Rate limiting.
- Secure error responses.
- Secret configuration.
- HTTPS for production-like environments.
- Database credential isolation.
- Redis access restrictions.
- Kafka access restrictions.
- Security-focused logging.
- Basic security tests.
- Dependency and container security checks.

### Defer

- Full OAuth2/OIDC provider integration.
- External identity providers.
- Service mesh.
- Full mTLS everywhere.
- Hardware security modules.
- Advanced fraud detection.
- Web Application Firewall.
- Enterprise SIEM integration.
- Multi-region key management.
- Complex policy engines.

---

## 74. Implementation Checklist

### Authentication

- [ ] Implement password hashing.
- [ ] Implement registration.
- [ ] Implement login.
- [ ] Implement JWT access tokens.
- [ ] Implement refresh tokens.
- [ ] Implement refresh-token rotation/revocation.
- [ ] Implement logout.
- [ ] Implement password reset.
- [ ] Protect authentication secrets.

### Authorization

- [ ] Define Customer/Admin roles.
- [ ] Add endpoint authorization policies.
- [ ] Implement resource ownership checks.
- [ ] Protect administrative operations.
- [ ] Enforce authorization inside services.

### API Security

- [ ] Validate all request DTOs.
- [ ] Configure request-size limits.
- [ ] Add rate limiting.
- [ ] Standardize 401/403 responses.
- [ ] Avoid information disclosure.
- [ ] Configure HTTPS.

### Infrastructure

- [ ] Protect PostgreSQL credentials.
- [ ] Protect Redis.
- [ ] Protect Kafka.
- [ ] Configure Kubernetes Secrets.
- [ ] Restrict network access.
- [ ] Avoid running containers as root where practical.

### Observability

- [ ] Log authentication failures.
- [ ] Log authorization failures.
- [ ] Track 401/403/429 metrics.
- [ ] Trace distributed requests.
- [ ] Exclude secrets from logs.

### Testing

- [ ] Test invalid credentials.
- [ ] Test expired JWT.
- [ ] Test invalid JWT.
- [ ] Test role restrictions.
- [ ] Test resource ownership.
- [ ] Test rate limiting.
- [ ] Test malformed input.
- [ ] Test secret/configuration handling.
- [ ] Run dependency vulnerability checks.

---

## 75. Security Architecture Summary

The initial CommerceX security architecture is:

```text
                       CLIENT
                          |
                        HTTPS
                          |
                          v
                 +----------------+
                 | API Gateway    |
                 | JWT Validation |
                 | Rate Limiting  |
                 +-------+--------+
                         |
                  Authenticated Request
                         |
          +--------------+---------------+
          |                              |
          v                              v
   +-------------+                +-------------+
   | Auth        |                | Other       |
   | Service     |                | Services    |
   +------+------+                +------+------+
          |                              |
          v                              v
   Auth PostgreSQL              Service Databases
                                         |
                              +----------+----------+
                              |                     |
                              v                     v
                           Redis                 Kafka
```

The main security boundary is:

```text
Untrusted Client
       |
       v
Authenticated + Authorized Service Operation
```

Every service remains responsible for protecting its own resources.

---

## 76. Architectural Decisions

The following decisions are established for the CommerceX security baseline:

1. Auth Service owns authentication and credential data.
2. JWT is used for initial access-token authentication.
3. Access tokens are short-lived and configurable.
4. Refresh tokens are persisted and revocable.
5. Passwords are never stored in plaintext.
6. Password reset tokens are secure, short-lived, and single-use.
7. Customer and Admin are the initial roles.
8. Authorization is enforced at service boundaries.
9. Customer-owned resources require explicit ownership checks.
10. The Gateway performs common authentication/security controls but does not own core business authorization.
11. Internal service communication must be authenticated where required.
12. gRPC is treated as an internal authenticated communication mechanism.
13. Kafka is private infrastructure with controlled producer/consumer access.
14. Services do not access other services' databases directly.
15. Secrets are externalized from source code.
16. PostgreSQL, Redis, and Kafka are not publicly exposed.
17. Input is always considered untrusted.
18. Rate limiting is applied to abuse-sensitive APIs.
19. Security-sensitive information is excluded from logs and error responses.
20. Payment processing remains simulated and does not store real payment credentials.
21. Advanced identity infrastructure and service mesh technologies are deferred.

---

## 77. Relationship With Other Documents

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

It provides security requirements for:

- Docker Design
- Kubernetes Design
- Observability Design
- Testing Strategy
- CI/CD Design
- Development Roadmap
- Final Project Report

---

## 78. Baseline Status

This document establishes the **CommerceX Security Design baseline**.

Future implementation work should follow these security decisions unless a later architecture decision explicitly changes them.

**Next document:** Document 14 — Docker Design
