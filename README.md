# HR Management Service

A comprehensive HR Management System built with **.NET 10** as a console application, designed to explore **system design**, **low-level design (LLD)** patterns, and **cloud-native architecture** using Azure services.

> This is a learning project to understand real-world system design concepts by building them from scratch.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                      Program.cs                         │
│              (Login Loop + Session Loop)                 │
├─────────────────────────────────────────────────────────┤
│                     MenuRouter.cs                       │
│           (Menu Display + Action Routing)                │
├──────────┬──────────┬───────────┬───────────────────────┤
│ Employee │ Payroll  │ Holiday   │ Performance  │  Team  │
│ Service  │ Service  │ Service   │ Service      │Service │
├──────────┴──────────┴───────────┴───────────────────────┤
│                    Repository Layer                     │
│        (Cosmos DB - 14 Containers)                      │
├─────────────────────────────────────────────────────────┤
│  AuthN (JWT + Session)  │  AuthZ (RBAC + Rule Engine)   │
├─────────────────────────────────────────────────────────┤
│  Key Vault  │  Service Bus  │  Azure OpenAI             │
└─────────────────────────────────────────────────────────┘
```

## Features

### Core HR Operations
- **Employee Onboarding** — Multi-step pipeline with Service Bus orchestration
- **Employee Termination** — Offboarding pipeline with cascading cleanup
- **Promotion Management** — Manager proposes → HR reviews → AI advisor assists
- **Salary Management** — Level-based salary ranges with payroll tracking
- **Holiday Management** — Holiday config, request/approve flow, holiday bank
- **Team Management** — Create/update teams, assign managers
- **Performance Reviews** — Employee self-review → Manager evaluation cycle

### Authentication (Hybrid: Stateless + Stateful)
- **JWT Tokens** — HMAC-SHA256, 30-minute expiry, ClockSkew=Zero
- **Server-Side Sessions** — Cosmos DB session tracking (catches force-logout)
- **Double Validation** — JWT check (fast, no DB) → Session DB check (catches force-logout)

### Authorization (RBAC + Rule Engine)
- **Role-Based Access Control** — Permissions stored in Cosmos DB, cached in memory
- **Scope-Based Access** — ALL (HR) > TEAM_AND_SELF (Manager) > SELF (Employee)
- **Rule Engine** — Chain of Responsibility pattern:
  1. **PermissionRule** — Does role have this permission? (cache, no DB)
  2. **ScopeRule** — Can user access this target? (Team DB lookup)
  3. **StateRule** — Is target employee active? (Employee DB lookup)
- **Callback Injection** — Services receive `Func<Permission, string, Task<bool>>` for scope checks (Dependency Inversion)

### AI Integration (Azure OpenAI)
- **Performance Review Assistant** — AI drafts review comments and suggests ratings
- **Promotion Advisor** — AI analyzes performance history for promotion readiness
- **HR Policy Chatbot** — Interactive Q&A about company policies

### Event-Driven Pipeline
- **Service Bus Queues** — Async processing for onboarding, offboarding, promotions, payroll
- **Pipeline Pattern** — Sequential steps with rollback support
- **Audit Logging** — Every action tracked with performer, timestamp, details

## Design Patterns Used

| Pattern | Where Used |
|---|---|
| **Chain of Responsibility** | Rule Engine — rules evaluated in sequence |
| **Repository Pattern** | All data access via repository classes |
| **Pipeline Pattern** | Employee onboarding/offboarding steps |
| **Strategy Pattern** | Different scope checks per role |
| **Callback Injection** | Scope checker passed as `Func<>` to services |
| **Single Responsibility** | Bootstrap split into Config, KeyVault, Cosmos, ServiceBus |

## Project Structure

```
HRManagementService/
├── Program.cs                  # Entry point — login/session loop
├── MenuRouter.cs               # Menu display + action routing
├── appsettings.json            # Configuration (placeholders)
│
├── Bootstrap/                  # Application startup
│   ├── AppBuilder.cs           # Orchestrates all bootstrappers
│   ├── AppServices.cs          # Service container
│   ├── ConfigLoader.cs         # Load appsettings.json
│   ├── CosmosBootstrapper.cs   # Cosmos DB containers + repositories
│   ├── KeyVaultBootstrapper.cs # Azure Key Vault secrets
│   └── ServiceBusBootstrapper.cs
│
├── AuthService/                # Authentication & Authorization
│   ├── AuthManager.cs          # Login flow
│   ├── JwtService.cs           # JWT generation + validation
│   ├── AuthorizationService.cs # RBAC permissions + cache
│   └── Rules/                  # Rule Engine
│       ├── RuleContracts.cs    # IAuthorizationRule, AuthorizationRequest, RuleResult
│       ├── RuleEngine.cs       # Chain orchestrator
│       ├── PermissionRule.cs   # Permission cache check
│       ├── ScopeRule.cs        # ALL/TEAM_AND_SELF/SELF scope check
│       └── StateRule.cs        # Target employee active check
│
├── EmployeeService/            # Employee operations
├── PayrollService/             # Salary levels + payroll
├── HolidayService/             # Holiday config + requests
├── PerformanceService/         # Performance reviews
├── TeamService/                # Team management
├── AIService/                  # Azure OpenAI integration
├── Pipeline/                   # Service Bus + onboarding/offboarding pipelines
│
├── Models/                     # Data models (Cosmos documents)
├── Repository/                 # Data access layer
└── Enums/                      # Permission, UserRole, ScopeType, RuleStatus
```

## Azure Services Used

| Service | Purpose |
|---|---|
| **Azure Cosmos DB** | Primary database — 14 containers |
| **Azure Key Vault** | Secrets management (connection strings, API keys) |
| **Azure Service Bus** | Async event processing (7 queues) |
| **Azure OpenAI** | AI-powered features (GPT-4o) |
| **Azure AD (DefaultAzureCredential)** | Authentication to Azure services |

## Cosmos DB Containers

| Container | Partition Key | Purpose |
|---|---|---|
| Users | `/email` | Authentication users |
| Employees | `/id` | Employee records |
| Teams | `/teamId` | Team structure |
| LevelSalaryRange | `/level` | Salary bands per level |
| EmployeePayroll | `/employeeId` | Individual payroll |
| HolidayConfig | `/id` | Holiday definitions |
| EmployeeHolidayBank | `/employeeId` | Leave balances |
| EmployeePerformance | `/alias` | Performance reviews |
| PromotionRequests | `/alias` | Promotion proposals |
| AuditLogs | `/performedBy` | Audit trail |
| OnboardingStatus | `/id` | Pipeline tracking |
| Sessions | `/alias` | Active sessions |
| RolePermissions | `/role` | RBAC definitions |

## Role-Based Menus

**HR:** Employee Actions • Setup Salary Levels • Holidays • Team Management • Payroll • Update Info • HR Bot • Active Sessions

**Manager:** Performance Reviews • Propose Promotion • Payroll • Holidays • Update Info • HR Bot

**Employee:** Holidays • Check Own Salary • Update Info • Submit Performance Review • HR Bot

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Azure Subscription with:
  - Azure Cosmos DB account
  - Azure Key Vault
  - Azure Service Bus namespace
  - Azure OpenAI resource (GPT-4o deployment)
- Azure CLI (`az login`) for DefaultAzureCredential

## Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/<your-username>/HRManagementService.git
   cd HRManagementService
   ```

2. **Update `appsettings.json`** — Replace all `<placeholder>` values with your Azure resource details

3. **Store secrets in Key Vault**
   ```bash
   az keyvault secret set --vault-name <your-vault> --name CosmosDbConnectionString --value "<your-connection-string>"
   az keyvault secret set --vault-name <your-vault> --name ServiceBusConnectionString --value "<your-connection-string>"
   az keyvault secret set --vault-name <your-vault> --name OpenAIApiKey --value "<your-api-key>"
   az keyvault secret set --vault-name <your-vault> --name OpenAIEndpoint --value "<your-endpoint>"
   ```

4. **Login to Azure**
   ```bash
   az login
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

## Tech Stack

- **Runtime:** .NET 10
- **Database:** Azure Cosmos DB (NoSQL)
- **Messaging:** Azure Service Bus
- **AI:** Azure OpenAI (GPT-4o)
- **Auth:** JWT (System.IdentityModel.Tokens.Jwt)
- **Secrets:** Azure Key Vault
- **Serialization:** Newtonsoft.Json
