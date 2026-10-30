
# Data and API Design

## 1. Purpose

This document defines the proposed portal data concepts, API boundaries and Business Central integration contracts.

The exact Business Central entities and endpoint fields must be verified against the installed version and environment before implementation.

## 2. Data Ownership

### Business Central owns

- Customer records used for financial accounts.
- Sales documents and posted invoices.
- Customer ledger entries.
- Payment records and applications.
- General ledger entries.
- Accounting balances derived from posted transactions.

### Portal owns or may own

- Portal-specific identity mappings.
- User interface preferences.
- Workflow metadata that does not belong in the ERP.
- Integration correlation identifiers and operational status where needed.

A separate database is optional and will only be introduced for a defined requirement.

## 3. Core Domain Concepts

### Portal User

Represents a person authenticated to the portal.

Proposed fields:

- User ID.
- Identity provider subject.
- Display name.
- Portal role.
- Active status.

Credentials and authentication secrets must be managed by the selected identity provider rather than stored as plain text.

### Student Account Mapping

Links an authenticated student to the appropriate financial account.

Proposed fields:

- Portal user ID.
- Business Central company identifier.
- Business Central customer identifier or number.
- Mapping status.

The mapping must be controlled by an authorized process. A student must not choose an arbitrary customer number to access another account.

### Fee Statement

A read model assembled from relevant Business Central financial records.

Possible information:

- Customer identifier.
- Invoice number.
- Document date.
- Due date.
- Original amount.
- Remaining amount.
- Currency.
- Payment or application status.

The displayed balance must reflect the relevant Business Central records and agreed currency and calculation rules.

### Invoice Request

Represents a validated request to create a fee invoice.

Possible fields:

- Request identifier.
- Requesting user.
- Customer identifier.
- Fee description.
- Amount and currency.
- Intended document date.
- Processing status.
- Business Central document identifier.
- Correlation identifier.

The application must not treat an unposted invoice request as a posted financial transaction.

## 4. Proposed Portal API

These are proposed portal endpoints, not existing Business Central endpoints.

| Method | Endpoint                                 | Purpose                                    |
| ------ | ---------------------------------------- | ------------------------------------------ |
| GET    | `/api/v1/students/me/fees`             | Retrieve the current student's fee summary |
| GET    | `/api/v1/students/me/invoices`         | List permitted invoices                    |
| GET    | `/api/v1/students/me/invoices/{id}`    | Retrieve an authorized invoice             |
| GET    | `/api/v1/finance/outstanding-balances` | List balances for authorized finance users |
| POST   | `/api/v1/finance/invoices`             | Initiate invoice creation                  |
| GET    | `/api/v1/finance/invoices/{id}`        | Retrieve invoice processing status         |
| POST   | `/api/v1/finance/payments`             | Initiate a test payment workflow           |
| GET    | `/api/v1/health`                       | Check application health                   |

The student endpoints derive the student's identity from the authenticated session. They must not rely solely on a client-supplied student ID.

Finance endpoints require appropriate finance permissions.

## 5. Business Central Integration

### Standard APIs

Prefer existing standard APIs for supported entities and operations.

### Custom AL APIs

Introduce a custom API only when a documented requirement cannot be satisfied appropriately through standard APIs.

Possible uses:

- Exposing a custom university-fee entity.
- Providing a controlled university-specific operation.
- Returning a purpose-built data structure when the standard API is insufficient.

### API Discovery

Before implementation:

1. Confirm the BC version and environment.
2. Inspect available API pages and documentation.
3. Identify the company and entity identifiers.
4. Verify the required permissions.
5. Test reading data.
6. Verify write and posting capabilities separately.

Do not assume that every Business Central document can be created and posted through a single generic HTTP request.

## 6. Proposed API Response Shapes

### Fee Summary

```json
{
  "customerNumber": "STU-1001",
  "currencyCode": "KES",
  "totalInvoiced": 50000.00,
  "totalOutstanding": 20000.00
}
```

These are illustrative portal response fields. The amounts are sample values, and the final calculation and field mapping must be verified against Business Central.

### Invoice

```json
{
  "invoiceNumber": "INV-TEST-001",
  "documentDate": "2026-10-27",
  "dueDate": "2026-11-10",
  "currencyCode": "KES",
  "amount": 50000.00,
  "remainingAmount": 20000.00,
  "status": "posted"
}
```

This is a proposed portal DTO, not a guarantee of the fields or statuses returned by a standard BC API.

## 7. Error Contract

The portal API should return consistent, non-sensitive error responses.

Example:

```json
{
  "code": "BUSINESS_CENTRAL_UNAVAILABLE",
  "message": "Financial information is temporarily unavailable.",
  "correlationId": "example-correlation-id"
}
```

Do not return access tokens, credentials, raw stack traces or sensitive internal details to the browser.

## 8. Pagination and Consistency

- Support pagination for invoice lists and finance reports.
- Use appropriate filters to avoid retrieving unnecessary records.
- Respect Business Central API pagination and throttling.
- Treat posted BC data as authoritative.
- If caching is introduced, define freshness and invalidation rules.
- Handle timeouts and uncertain write outcomes safely.
- Use idempotency or duplicate-prevention mechanisms for financial operations where applicable.

## 9. Currency and Amounts

- Preserve currency codes.
- Do not assume every transaction is in KES.
- Use appropriate decimal types for monetary values.
- Avoid floating-point calculations for financial amounts.
- Document whether summaries include credit memos, unapplied payments, discounts or other adjustments.

## 10. API Versioning

The portal API begins at `/api/v1`.

Changes that break existing contracts should be versioned deliberately. The Business Central API version is configured separately and must match the target environment.
