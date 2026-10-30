
# University Finance Portal

## 1. Project Overview

The University Finance Portal is a practice project designed to demonstrate how a web application can integrate with Microsoft Dynamics 365 Business Central to support university fee management.

The portal will provide a student-facing interface for viewing fee statements and a finance-facing interface for managing fee invoices and reviewing payment status.

Business Central will remain the system of record for financial transactions and posted accounting balances.

## 2. Project Objectives

- Build a university-style portal using Next.js and TypeScript.
- Develop a REST API using C# and ASP.NET Core.
- Integrate the API with Business Central using supported APIs.
- Learn to extend Business Central using AL when standard APIs are insufficient.
- Understand invoice posting, customer ledger entries, payments and outstanding balances.
- Implement secure authentication and role-based authorization.
- Produce a documented, testable application suitable for a development portfolio.

## 3. User Roles

### 3.1 Student

- View their own fee statement.
- View permitted invoice details.
- View payment and credit information exposed by the system.
- View the current outstanding balance.

### 3.2 Finance Officer

- View authorized student/customer financial records.
- Create fee invoices through an approved workflow.
- Review invoice status and outstanding balances.
- Review payment information.
- Initiate supported financial operations where authorized.

### 3.3 Administrator

- Manage portal-specific users and role assignments.
- Configure permitted application settings.
- Review operational logs and integration errors.

The administrator role does not automatically grant unrestricted access to Business Central.

## 4. Functional Requirements

### FR-01: Student Financial Dashboard

The portal shall display the authenticated student's permitted financial summary.

### FR-02: Fee Statement

The portal shall display relevant invoices, amounts, dates, payment applications and outstanding balances where supported by the integration.

### FR-03: Invoice Details

Authorized users shall be able to view invoice details and document status.

### FR-04: Finance Dashboard

Finance officers shall be able to review authorized financial records and outstanding balances.

### FR-05: Invoice Creation

Authorized finance officers shall be able to initiate fee invoice creation through a validated backend workflow.

### FR-06: Invoice Posting

Posting shall use a supported Business Central process. The portal shall not directly modify posted ledger entries.

### FR-07: Payment Information

The portal shall display payment information based on the relevant Business Central records.

### FR-08: Payment Recording

During the practice release, payments shall be simulated or entered as test transactions through an approved Business Central workflow. No real money shall be collected.

### FR-09: Error Handling

The application shall handle validation failures, unavailable services, authorization failures and integration errors without exposing secrets or sensitive technical details.

### FR-10: Auditability

Financial actions initiated through the portal shall be traceable to the requesting user and corresponding Business Central document or transaction where applicable.

## 5. Non-Functional Requirements

- Security: enforce authentication, authorization and least-privilege access.
- Integrity: treat posted Business Central records as authoritative for accounting balances.
- Reliability: handle API failures and retries without unintentionally duplicating financial operations.
- Maintainability: separate controllers, services, DTOs and integration code.
- Usability: provide responsive pages, loading states and understandable error messages.
- Testability: support automated unit tests and integration tests.
- Observability: record useful application and integration events without logging credentials or unnecessary personal data.

## 6. Initial Scope

Included:

- Student finance dashboard.
- Invoice listing and details.
- Fee statement and outstanding balance display.
- Basic finance officer dashboard.
- ASP.NET Core API.
- Business Central API integration.
- Custom AL API only where justified.
- Test invoice and payment workflows.
- Documentation and automated tests.

Excluded from the initial release:

- Admissions and course registration.
- Full university management system.
- HR and payroll.
- Real M-Pesa or bank payment collection.
- Production deployment for a real university.
- Replacement of Business Central's accounting functionality.

## 7. Assumptions and Constraints

- The existing local Business Central installation will be investigated before integration work begins.
- API availability, authentication and endpoint support must be verified.
- CRONUS is a demonstration company, not a production university finance database.
- Student records and payment scenarios will use fictitious test data.
- Standard Business Central APIs will be preferred over custom APIs when suitable.
- A separate portal database will be introduced only if portal-specific data requires persistence.

## 8. Acceptance Criteria

The initial release is considered successful when:

1. The portal can authenticate users through the chosen supported authentication design.
2. A student can only view their own authorized financial records.
3. A finance officer can access permitted finance features.
4. The portal retrieves financial information from Business Central.
5. A test invoice can be processed through a supported Business Central workflow.
6. The displayed outstanding balance can be reconciled with the corresponding Business Central records.
7. Integration failures are handled safely and documented.
8. The application can be set up using the project's documentation.

## 9. Success Measures

- Accurate financial information sourced from Business Central.
- No unauthorized cross-student access.
- No duplicate financial transactions caused by unsafe retries.
- Reproducible development and testing setup.
- Clear documentation of architecture, workflows and API contracts.

## 10. Open Questions

The following must be confirmed before implementation:

- Which Business Central APIs are available in the local environment?
- Which authentication method is supported?
- Will the portal require a separate identity provider?
- Which Business Central records will represent student fee accounts?
- Which invoice and payment operations are exposed through supported APIs?
