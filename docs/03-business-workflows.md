
# Business Workflows

## 1. Purpose

This document defines how users interact with the University Finance Portal and how the portal coordinates financial operations with Business Central.

## 2. Workflow A: Student Views Fee Statement

### Actors

- Student
- Next.js portal
- ASP.NET Core API
- Business Central

### Preconditions

- The student is authenticated.
- The student's portal identity is mapped to an authorized financial account.
- The backend has permission to retrieve the relevant records.

### Steps

1. The student opens the finance dashboard.
2. The frontend requests the student's fee statement.
3. The backend validates the session and account mapping.
4. The backend retrieves the relevant invoice and ledger information from Business Central.
5. The backend calculates or derives the displayed summary only from appropriate authoritative records.
6. The portal displays invoice amounts, payments and outstanding balances.

### Exceptions

- Unauthenticated request: return an authentication error.
- Account mapping missing: reject the request and log a safe diagnostic event.
- Business Central unavailable: return a controlled service error.
- No invoices found: display an empty state.

### Expected Result

The student sees only their own authorized financial information.

## 3. Workflow B: Finance Officer Creates an Invoice

### Actors

- Finance officer
- Portal
- ASP.NET Core API
- Business Central

### Preconditions

- The finance officer is authenticated and authorized.
- The customer or student account exists in the appropriate Business Central data model.
- Required invoice fields and posting configuration are available.

### Steps

1. The finance officer opens invoice creation.
2. The portal submits the invoice details.
3. The backend validates the request and checks permissions.
4. The backend identifies the appropriate Business Central customer and required document setup.
5. The backend creates the supported sales document or university-specific billing record.
6. The document is validated in Business Central.
7. Posting is performed through an explicitly supported and authorized operation.
8. The backend records the returned document identifiers and status.
9. The portal displays the result.

### Exceptions

- Missing customer: reject the request.
- Invalid posting setup: return a safe, actionable error.
- Validation failure: do not report success.
- Timeout during a write: check the transaction's status before retrying.
- Posting failure: preserve and report the actual document status.

### Expected Result

An invoice is created and, where requested and supported, posted correctly in Business Central.

## 4. Workflow C: Record a Test Payment

### Actors

- Authorized finance officer
- Portal backend
- Business Central

### Preconditions

- The invoice exists.
- The finance officer has the necessary permissions.
- The selected payment and application process is supported by the configured BC environment.

### Steps

1. The finance officer selects the relevant invoice.
2. The finance officer enters test payment details.
3. The backend validates the amount, account and request.
4. The backend invokes the supported Business Central payment workflow.
5. The payment is recorded and applied using the appropriate BC process.
6. The backend checks the resulting document or ledger state.
7. The portal refreshes the fee statement.

### Exceptions

- Invalid amount: reject the request.
- Payment application failure: show the actual status.
- Duplicate request: prevent an unintended duplicate transaction.
- Business Central unavailable: check transaction status before attempting a retry.

### Expected Result

The outstanding balance reflects the resulting Business Central records.

For the practice project, use test transactions only. No real money is collected.

## 5. Workflow D: Finance Officer Reviews Outstanding Balances

1. The finance officer opens the finance dashboard.
2. The backend verifies finance permissions.
3. The backend retrieves authorized customer and outstanding-balance information.
4. The portal displays a list with pagination and appropriate filters.
5. The finance officer opens an account to inspect its financial details.

The dashboard must use authoritative Business Central information and must not expose unauthorized accounts.

## 6. Workflow E: Integration Failure

1. A user submits a request.
2. The backend validates authorization and input.
3. A Business Central request fails or times out.
4. The backend records a safe diagnostic event.
5. The backend returns an appropriate error response.
6. The frontend displays an understandable message.
7. For a write operation with an uncertain outcome, the backend checks the transaction status before retrying.

## 7. Business Rules

- Posted Business Central records are authoritative for accounting.
- The portal must not directly edit posted ledger entries.
- Financial operations must use supported Business Central processes.
- Users may only perform operations allowed by their roles.
- Students may only access their own mapped accounts.
- Invoice creation and posting are distinct steps unless a deliberately designed workflow supports both.
- A successful HTTP request alone does not prove that a financial document was posted successfully.
- Payment amounts, applications and outstanding balances must be reconciled against BC records.
- All test transactions must be identifiable as development activity.

## 8. Future Workflows

The following are outside the initial release:

- Real M-Pesa payment initiation and callback reconciliation.
- Bank statement reconciliation.
- HR employee self-service.
- Payroll processing.
- Student admissions and course registration.
