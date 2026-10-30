
# System Flowcharts

## 1. System Context

```mermaid
flowchart LR
    Student[Student]
    Finance[Finance Officer]
    Portal[Next.js Portal]
    Backend[ASP.NET Core API]
    BC[Business Central]
    AL[Optional AL Extension]

    Student --> Portal
    Finance --> Portal
    Portal --> Backend
    Backend <--> BC
    AL <--> BC
```

## 2. Student Fee Statement

```mermaid
flowchart TD
    A[Student opens dashboard] --> B[Request fee statement]
    B --> C[Validate authentication]
    C --> D{Authenticated?}
    D -- No --> E[Return 401]
    D -- Yes --> F[Authorize account access]
    F --> G{Access allowed?}
    G -- No --> H[Return 403]
    G -- Yes --> I[Query Business Central]
    I --> J{Request successful?}
    J -- No --> K[Return controlled error]
    J -- Yes --> L[Map financial records]
    L --> M[Return statement]
    M --> N[Display statement]
```

## 3. Invoice Creation and Posting

```mermaid
flowchart TD
    A[Finance officer submits invoice] --> B[Validate request]
    B --> C{Valid and authorized?}
    C -- No --> D[Return validation or authorization error]
    C -- Yes --> E[Create supported BC document]
    E --> F{Document created?}
    F -- No --> G[Return creation error]
    F -- Yes --> H[Review document]
    H --> I{Ready to post?}
    I -- No --> J[Correct document or leave unposted]
    I -- Yes --> K[Invoke supported posting operation]
    K --> L{Posting successful?}
    L -- No --> M[Return actual status and error]
    L -- Yes --> N[Retrieve document identifiers]
    N --> O[Display posted invoice]
```

## 4. Test Payment Workflow

```mermaid
flowchart TD
    A[Finance officer selects invoice] --> B[Enter test payment details]
    B --> C[Validate user and payment request]
    C --> D{Valid?}
    D -- No --> E[Return error]
    D -- Yes --> F[Check invoice and payment status]
    F --> G[Execute supported BC payment process]
    G --> H{Payment recorded and applied?}
    H -- No --> I[Report actual status]
    H -- Yes --> J[Refresh fee statement]
    J --> K[Compare balance with BC]
    K --> L[Display updated balance]
```

## 5. Integration Request Lifecycle

```mermaid
sequenceDiagram
    actor User
    participant UI as Next.js
    participant API as ASP.NET Core
    participant ID as Identity Provider
    participant BC as Business Central

    User->>UI: Request fee statement
    UI->>API: Authenticated API request
    API->>ID: Validate identity or session as configured
    ID-->>API: Identity result
    API->>API: Authorize access to account
    API->>BC: Request permitted financial records
    BC-->>API: Financial data or error
    API->>API: Map response and handle errors
    API-->>UI: Portal response
    UI-->>User: Display statement
```

The precise authentication sequence will depend on the selected portal authentication design. The diagram represents the logical flow, not a commitment to a particular token-validation implementation.

## 6. Deployment Context

```mermaid
flowchart TD
    Browser[User Browser]
    Frontend[Hosted Next.js Frontend]
    Backend[Hosted ASP.NET Core API]
    Entra[Microsoft Entra ID]
    BC[Business Central Environment]
    Logs[Application Logs]

    Browser --> Frontend
    Frontend --> Backend
    Backend <--> Entra
    Backend <--> BC
    Backend --> Logs
```

## 7. Diagram Maintenance

Update these diagrams whenever a major workflow, API boundary, authentication method or system responsibility changes.

Do not treat a diagram as proof that a feature exists. Implementation and tests must verify each workflow.
