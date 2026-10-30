
# Implementation Roadmap

## 1. Project Goal

Build a working university finance portal using Next.js, ASP.NET Core, Business Central and AL.

Implementation will proceed incrementally. Each phase must produce a testable result before the next phase begins.

## Phase 1: Requirements and Environment Discovery

Tasks:

- Finalize the initial feature scope.
- Document student and finance officer workflows.
- Inspect the local Business Central version and deployment.
- Verify available APIs and authentication options.
- Identify relevant customer, invoice and ledger records.
- Confirm how the development environment can be reached from the .NET application.

Deliverables:

- Approved requirements.
- Architecture and workflow documentation.
- Verified Business Central integration plan.

Exit criteria:

- The team understands the initial workflow and knows which API access method is feasible.

## Phase 2: C# and ASP.NET Core Foundation

Tasks:

- Review C# classes, interfaces, collections, LINQ and async/await.
- Create the ASP.NET Core Web API project.
- Organize controllers, DTOs, services and configuration.
- Enable Swagger/OpenAPI.
- Implement sample student and fee endpoints.
- Add input validation and error handling.
- Write initial unit tests.

Deliverables:

- Running API.
- Documented sample endpoints.
- Passing unit tests.

Exit criteria:

- Endpoints can be tested independently of Business Central.

## Phase 3: Frontend Foundation

Tasks:

- Create the Next.js application.
- Build the student dashboard.
- Build the fee statement and invoice list screens.
- Build the finance officer dashboard.
- Connect the frontend to the sample .NET API.
- Add loading, empty and error states.

Deliverables:

- Responsive portal using sample data.

Exit criteria:

- Users can navigate the interface and retrieve sample data from the .NET API.

## Phase 4: Authentication and Authorization

Tasks:

- Choose and configure the portal identity approach.
- Protect frontend and backend routes.
- Implement role-aware authorization.
- Define the student-to-customer mapping.
- Add authorization tests.

Deliverables:

- Protected endpoints and role-aware user flows.

Exit criteria:

- Students cannot access other students' records or finance-only operations.

## Phase 5: Read-Only Business Central Integration

Tasks:

- Configure secure authentication to the target BC environment.
- Implement a typed Business Central API client.
- Retrieve permitted customer information.
- Retrieve the relevant invoice and financial information.
- Map BC responses into portal DTOs.
- Handle pagination, errors and timeouts.
- Compare portal results with BC.

Deliverables:

- Portal displaying actual Business Central data.

Exit criteria:

- Read operations work reliably and the displayed information is verified against BC.

## Phase 6: Custom AL API (Conditional)

Tasks:

- Identify a documented gap in the standard APIs.
- Create the minimum required AL extension.
- Implement the appropriate API page or supported operation.
- Configure permissions.
- Publish and test the extension.
- Consume it from ASP.NET Core.

Deliverables:

- A tested custom API only if the project requires one.

Exit criteria:

- The custom requirement works without bypassing BC business rules.

If standard APIs are sufficient, document the decision and skip this phase.

## Phase 7: Invoice and Test Payment Workflows

Tasks:

- Define the invoice creation contract.
- Validate customer and document inputs.
- Create invoices through supported BC operations.
- Test posting separately from document creation.
- Implement the approved test-payment workflow.
- Verify ledger entries and outstanding balances.
- Protect write operations against duplicate requests.
- Add integration tests for success and failure scenarios.

Deliverables:

- Demonstrable invoice-to-payment workflow.

Exit criteria:

- Test transactions are reflected correctly in BC and can be reconciled with the portal.

## Phase 8: Hardening and Documentation

Tasks:

- Review authentication and authorization.
- Test timeouts and uncertain transaction outcomes.
- Improve logging and correlation identifiers.
- Review configuration and secret management.
- Document local setup and test procedures.
- Package the project for demonstration.

Deliverables:

- Reproducible project with documentation and tests.

Exit criteria:

- Another developer can configure the project and reproduce the principal test scenarios.

## 2. Testing Strategy

### Unit Tests

Test validation, mapping, calculations and application services independently.

### Integration Tests

Test communication with Business Central in an appropriate development environment.

### Authorization Tests

Verify access boundaries between students, finance officers and administrators.

### Financial Reconciliation Tests

Compare portal figures with the corresponding Business Central records.

### Failure Tests

Test invalid input, unavailable services, timeouts, expired credentials and duplicate submissions.

## 3. Development Practices

- Use Git and commit changes in small, understandable increments.
- Keep configuration environment-specific.
- Avoid committing secrets.
- Document important architectural decisions.
- Prefer small working increments over large untested implementations.
- Do not modify Business Central financial data directly through its database.
- Update the documentation when implementation decisions change.

## 4. Future Enhancements

After the first release:

- M-Pesa payment integration and callback reconciliation.
- Bank reconciliation support.
- University student registration integration.
- HR and employee self-service.
- Payroll integration.
- Advanced finance reporting.
- Deployment automation and monitoring.

These features require separate requirements, security reviews and workflow designs.

## 5. Definition of Done

A feature is complete when:

- Requirements are documented.
- Implementation is reviewed and tested.
- Authorization is verified.
- Errors are handled.
- Relevant documentation is updated.
- Business Central outcomes are confirmed where financial operations are involved.
