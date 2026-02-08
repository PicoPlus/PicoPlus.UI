# Clean Architecture Full Assessment and Refactor Blueprint

## Phase 1 — Full Project Analysis (Factual)

### Architecture
- **Hybrid UI architecture (`Views/` + `Pages/` + `Components/Pages`)**: routing and rendering are split across multiple patterns (`Views/*.razor`, `Pages/User/Panel.razor`, `Components/Pages/*.razor`), which creates unclear ownership of presentation responsibilities and duplicate route surface. A clean architecture presentation layer should have a single composition style and bounded feature folders.
  - Files: `Views/auth/Login.razor`, `Views/auth/Register.razor`, `Views/User/Home.razor`, `Pages/User/Panel.razor`, `Components/Pages/Counter.razor`, `Components/Pages/Weather.razor`.
- **MVVM in Blazor (forbidden by constraints)**: ViewModels are directly injected into Razor components and registered in DI.
  - Files: `Components/_Imports.razor`, `Views/auth/Login.razor`, `Views/auth/Register.razor`, `Views/User/Home.razor`, `Components/Dialogs/CreateDealDialog.razor`, `Program.cs`, `ViewModels/**/*`.
- **State pattern usage in UI flow (forbidden by constraints)**: `State/*` directory and state-centric services are active and injected into presentation.
  - Files: `State/Admin/AdminStateService.cs`, `State/UserPanel/*.cs`, `Components/Layout/AdminLayout.razor`, `Pages/User/Panel.razor`.
- **Layer boundary leak from Application/Domain perspective**: presentation pages import infrastructure/service implementation types directly instead of application interfaces.
  - Files: `Views/auth/Login.razor` (`LiaraApiService`, ViewModels), `Views/User/Home.razor` (ViewModel with orchestration), `Pages/User/Panel.razor.cs` (service under `Services/`).

### UI
- **Overloaded pages**: several pages exceed 250–600 lines and combine orchestration, state transitions, conditional workflows, and large markup trees.
  - Files: `Views/User/Home.razor` (619 lines), `Views/auth/Register.razor` (370 lines), `Views/auth/Login.razor` (250 lines), `Components/Dialogs/CreateDealDialog.razor` (322 lines).
- **Duplicate login artifacts**: one active login page (`Views/auth/Login.razor`) and one stale malformed duplicate (`Views/auth/Login`) coexist.
  - Files: `Views/auth/Login.razor`, `Views/auth/Login`.
- **Sample/template pages left in production app**: default `Counter`/`Weather` pages remain routable and increase attack and maintenance surface.
  - Files: `Components/Pages/Counter.razor`, `Components/Pages/Weather.razor`.

### Services
- **Repeated HubSpot request/authorization setup**: token retrieval and `Authorization` header assignment are repeated in service classes while also configured in `Program.cs` named client.
  - Files: `Program.cs`, `Services/CRM/Objects/Contact.cs`, `Services/CRM/Objects/Deal.cs`, `Services/CRM/Objects/Company.cs`, `Services/CRM/Objects/Ticket.cs`.
- **Mixed service responsibilities**: some services combine orchestration, caching, session persistence, and transformation logic in a single class.
  - File: `Services/UserPanel/UserPanelService.cs`.
- **Service composition bypass candidates**: static HTTP client factory helpers exist in infrastructure but app primarily uses DI client registration in `Program.cs`, causing dual patterns.
  - Files: `Infrastructure/Http/HubSpotHttpClientConfig.cs`, `Infrastructure/Http/SmsIrHttpClientConfig.cs`.

### Infrastructure
- **Infrastructure types injected into UI and higher layers**: wrappers/state implementations are consumed directly by view models and pages.
  - Files: `Infrastructure/State/AuthenticationStateService.cs`, `Infrastructure/Services/*.cs`, `Views/auth/*.razor`, `Program.cs`.
- **Configuration composition concentrated in `Program.cs`**: many external API registrations and business-adjacent wiring are centralized in one file, limiting modularity and testability.
  - File: `Program.cs`.

### External APIs
- **HubSpot logic duplication across object services**: contact/deal/company/ticket client behavior appears repeated rather than unified behind one gateway abstraction.
  - Files: `Services/CRM/Objects/Contact.cs`, `Services/CRM/Objects/Deal.cs`, `Services/CRM/Objects/Company.cs`, `Services/CRM/Objects/Ticket.cs`.
- **Multiple external integrations referenced from presentation flow**: login page currently pulls build metadata directly from Liara service.
  - File: `Views/auth/Login.razor`.
- **Misplaced API quick-reference docs in runtime service folders** (documentation mixed into executable areas):
  - Files: `Services/SMS/README.md`, `Services/Utils/README_SmsIr.md`, `Services/Utils/README_Zibal.md`.

---

## Phase 2 — Target Clean Architecture Design

## Final solution/folder structure

```text
src/
  PicoPlus.Domain/
    Entities/
    ValueObjects/
    Enums/
    Services/ (domain services only)
    Abstractions/ (domain interfaces)

  PicoPlus.Application/
    Abstractions/
      External/
      Persistence/
      Security/
    Features/
      Auth/
        Dtos/
        UseCases/
      UserPanel/
        Dtos/
        UseCases/
      Admin/
        Dtos/
        UseCases/
      Deals/
        Dtos/
        UseCases/
    Common/

  PicoPlus.Infrastructure/
    HubSpot/
      Clients/
      Repositories/
      Mappers/
    Sms/
    Payment/
    Liara/
    Persistence/
    Security/
    DependencyInjection/

  PicoPlus.Presentation.Blazor/
    Components/
      Pages/
      Features/
      Shared/
    Layout/
    DependencyInjection/
    Program.cs
```

### Dependency flow (inward only)

```text
Presentation (Blazor)
    -> Application (Use Cases + DTOs + Interfaces)
        -> Domain (Entities + Value Objects + Domain contracts)
Infrastructure (implementations) ---implements---> Application interfaces
```

### Responsibility per layer
- **Domain**: pure business rules, invariants, value objects, and interfaces with no framework dependencies.
- **Application**: use-case orchestration, DTOs, and ports/interfaces to external systems.
- **Infrastructure**: implementations for HubSpot/SMS/Payment/Liara/persistence and technical adapters.
- **Presentation**: Razor components only, display + user interaction, no direct external API usage, no business rules.

---

## Phase 3 — Codebase Cleanup Plan (before broad refactor)

### Files to delete
- `Views/auth/Login` (stale malformed duplicate).
- `temp_fix.txt` (temporary artifact).
- `et --hard 78dc6c6` (accidental artifact file).

### Files to move (target paths)
- `State/UserPanel/*.cs` → `Application/Features/UserPanel/Dtos/*`.
- `State/Admin/AdminStateService.cs` → `Application/Features/Admin/UseCases/AdminSessionService.cs` (rename to use-case/service naming).
- `Services/Auth/AuthService.cs` → `Application/Features/Auth/UseCases/AuthSessionService.cs` (interface-first).
- `Services/UserPanel/IUserPanelService.cs` and `Services/UserPanel/UserPanelService.cs` → `Application/Features/UserPanel/UseCases/*`.
- `Services/CRM/**/*` → split: contracts in `Application/Abstractions/External/HubSpot/*`, implementations in `Infrastructure/HubSpot/*`.

### Files to merge (with rationale)
- Merge duplicated login interaction logic into one `Auth` feature service and one login page.
  - Merge source: `Views/auth/Login.razor` + obsolete `Views/auth/Login`.
- Merge repeated HubSpot auth/header setup into a centralized infrastructure gateway.
  - Merge source: `Services/CRM/Objects/Contact.cs`, `Deal.cs`, `Company.cs`, `Ticket.cs`.
- Merge static HTTP config helpers into DI extension modules.
  - Merge source: `Infrastructure/Http/HubSpotHttpClientConfig.cs`, `Infrastructure/Http/SmsIrHttpClientConfig.cs`, `Program.cs` registrations.

---

## Phase 4 — Refactor to Clean Architecture (implementation blueprint)

### Application service example
```csharp
public interface ILoginUseCase
{
    Task<LoginResultDto> ExecuteAsync(LoginRequestDto request, CancellationToken ct = default);
}

public sealed class LoginUseCase : ILoginUseCase
{
    private readonly IAuthGateway _authGateway;
    public LoginUseCase(IAuthGateway authGateway) => _authGateway = authGateway;

    public Task<LoginResultDto> ExecuteAsync(LoginRequestDto request, CancellationToken ct = default)
        => _authGateway.AuthenticateAsync(request, ct);
}
```

### Infrastructure implementation example
```csharp
public sealed class HubSpotAuthGateway : IAuthGateway
{
    private readonly HttpClient _http;
    public HubSpotAuthGateway(HttpClient http) => _http = http;

    public async Task<LoginResultDto> AuthenticateAsync(LoginRequestDto request, CancellationToken ct)
    {
        // external call + mapping
    }
}
```

### Presentation usage example
```razor
@inject ILoginUseCase LoginUseCase

<EditForm Model="_form" OnValidSubmit="LoginAsync">
...
</EditForm>

@code {
  private async Task LoginAsync()
    => _result = await LoginUseCase.ExecuteAsync(_form);
}
```

### DI registration target
```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();
```

---

## Phase 5 — UI Decomposition & Deduplication Plan

### New component tree (example for User panel)
```text
Pages/User/Panel
  ├─ UserHeaderContainer
  │   └─ UserIdentityCard (presentational)
  ├─ UserStatsContainer
  │   ├─ StatisticsCards
  │   └─ StatisticsCard
  ├─ UserTabsContainer
  │   ├─ ProfileTab
  │   │   └─ ProfileField (reusable)
  │   └─ DealsTab
  │       ├─ DealsTable
  │       └─ DealDetailsModal
  └─ Shared/LoadingOverlay + Shared/ErrorToast
```

### Duplicates to remove
- Duplicate login view artifact (`Views/auth/Login`).
- Repeated badge/status and formatting logic should be consolidated into small stateless display components with DTO-ready inputs.

### Rendering optimization controls
- Keep containers stateful and pass immutable DTOs into presentational components.
- Use granular parameters and callbacks to avoid full-page rerenders.
- Isolate async loading boundaries around specific sections instead of full page.

---

## Phase 6 — Validation Checklist

### Mandatory checks
- No MVVM remnants in presentation folders.
- No `ViewModels` namespace usage in Razor components.
- No infrastructure namespace references in Razor pages/components.
- DI graph composes only through layer extension methods.
- External APIs isolated behind Application interfaces + Infrastructure implementations.
- `dotnet build` and smoke routes pass.

### Current execution status in this environment
- Static code audit completed.
- Build execution is blocked in current environment because `.NET SDK` is unavailable (`dotnet: command not found`).

---

## Immediate actions performed in this change set
1. Deleted stale duplicate login artifact file: `Views/auth/Login`.
2. Deleted temporary artifact file: `temp_fix.txt`.
3. Deleted accidental artifact file: `et --hard 78dc6c6`.
