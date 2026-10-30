
# Security Design

## 1. Purpose

This document defines the security requirements for the University Finance Portal and its Business Central integration.

The initial implementation is a development project using fictitious records. Production deployment would require additional review and controls.

## 2. Security Principles

- Apply least-privilege access.
- Authenticate users before accessing protected resources.
- Authorize every sensitive operation on the server.
- Keep privileged credentials out of browser code and source control.
- Treat client-supplied identifiers and data as untrusted.
- Preserve Business Central's financial validation and posting controls.
- Log important security and financial events without exposing secrets.
- Fail safely when identity or authorization cannot be verified.

## 3. Authentication

### Portal Users

Select an appropriate identity provider and authentication mechanism before implementation.

The design must establish:

- How students and finance officers sign in.
- How sessions or access tokens are validated.
- How users are disabled or access revoked.
- How roles are assigned and maintained.
- How authentication errors are handled.

### Business Central Integration

Use a supported Microsoft Entra ID authentication flow for the target BC environment.

The application must:

- Request only the permissions it needs.
- Store credentials in server-side configuration or a managed secret store.
- Protect access tokens and never log them.
- Handle token expiration and authentication failures.
- Verify which authentication methods are supported by the local BC installation.

The portal user's identity and the application's Business Central identity are distinct security concerns. A valid application token does not automatically authorize a student to access every customer account.

## 4. Authorization

### Student

May view only financial records mapped to their authenticated identity.

### Finance Officer

May view and perform only the finance operations granted to their role.

### Administrator

May manage permitted portal settings and user mappings. Administrative access does not automatically grant unrestricted accounting permissions.

Authorization must be enforced in ASP.NET Core and, where applicable, in Business Central.

## 5. Financial Operation Controls

- Validate all financial inputs on the server.
- Verify customer, document and payment relationships.
- Use supported Business Central posting operations.
- Prevent students from creating or posting invoices.
- Require explicit permissions for financial write operations.
- Protect against duplicate requests.
- Verify the resulting Business Central document or ledger state.
- Record the requesting user and relevant document identifiers.
- Never claim that a transaction succeeded solely because an HTTP request returned successfully.

## 6. API Security

- Use HTTPS in deployed environments.
- Validate input and enforce appropriate request limits.
- Restrict cross-origin requests to trusted frontend origins.
- Use appropriate session, cookie and CSRF protections for the chosen authentication model.
- Avoid exposing unnecessary Business Central fields.
- Return safe errors rather than stack traces.
- Apply timeouts and controlled retry policies.
- Validate pagination and filtering parameters.
- Use rate limiting where appropriate.

## 7. Secret Management

Secrets include:

- Client secrets or certificates.
- Database credentials.
- Signing keys.
- API keys for external services.

Rules:

- Never commit secrets to Git.
- Never embed privileged credentials in Next.js client-side code.
- Use local development secrets or environment-specific configuration.
- Use a managed secret store for production where available.
- Rotate compromised credentials promptly.

## 8. Logging and Audit

Log relevant events such as:

- Authentication and authorization failures.
- Invoice creation attempts.
- Posting attempts and outcomes.
- Payment workflow outcomes.
- Business Central integration errors.

Where appropriate, include:

- Timestamp.
- Correlation identifier.
- Authenticated actor identifier.
- Operation type.
- Relevant document identifier.
- Outcome.

Do not log access tokens, passwords or unnecessary personal and financial information.

## 9. Data Protection

- Use fictitious student records in development.
- Avoid copying real student financial records into local test environments.
- Limit data returned by each endpoint.
- Avoid exposing one student's information to another.
- Define data retention for application logs and portal-specific data.
- Protect backups and development configuration.

## 10. Failure Handling

If authentication, authorization or a required security check fails, reject the operation.

If a financial write times out and its outcome is uncertain, verify its state before retrying. Do not automatically repeat an operation that could create duplicate financial transactions.

## 11. Security Acceptance Criteria

- A student cannot access another student's financial statement.
- A student cannot call finance-only endpoints successfully.
- Privileged BC credentials are absent from browser code and source control.
- Invalid requests are rejected server-side.
- Failed financial operations report their actual status.
- Logs contain useful diagnostics without secrets.
- Business Central permissions are limited to the application's actual requirements.
