# PicoPlus.UI Clean Architecture Refactor Package

## Project Context
- **Language & Runtime:** C# / .NET 9
- **UI Framework:** Blazor Server
- **State/UI Pattern:** Mixed MVVM + Razor code-behind
- **External Systems:** HubSpot CRM API, Zibal identity APIs, SMS.ir/Faraz SMS providers, browser session/local storage
- **Goal:** Move from mixed architecture to modular Clean Architecture while keeping existing behavior and routes stable.

---

## 1) Comprehensive Static Analysis

### 1.1 Current architecture style (observed)
The current codebase combines:
- Blazor component/page code-behind orchestration
- MVVM ViewModels with `CommunityToolkit.Mvvm`
- Service-oriented integration classes directly calling external APIs
- App-level state services for authentication/admin context

This is **partially layered** but not strictly Clean Architecture, because business flows depend heavily on infrastructure DTOs and UI adapters.

### 1.2 Modules identified
- **Composition root:** `Program.cs`
- **Interface/UI:** `Pages/*`, `Views/*`, `Components/*`, `ViewModels/*`
- **State:** `Infrastructure/State/*`, `State/Admin/*`, `State/UserPanel/*`
- **Application-like services:** `Services/Auth/*`, `Services/UserPanel/*`, `Services/Admin/*`
- **Infrastructure/integrations:** `Services/CRM/*`, `Services/SMS/*`, `Services/Utils/*`, `Infrastructure/Http/*`, `Infrastructure/Services/*`
- **Transport models:** `Models/CRM/*`, `Models/Services/*`, `Models/Admin/*`

### 1.3 Architectural violations and risks
1. **Business logic in ViewModels/UI adapters**
   - Login/registration orchestration, OTP policies, contact enrichment, and session updates are performed directly in UI-facing classes.
2. **Infrastructure DTO leakage**
   - HubSpot/Zibal DTOs are used outside integration boundaries.
3. **Hard-coded admin credentials**
   - Security-critical credential data appears in source code.
4. **Key/role contract duplication**
   - Auth/session keys and role decisions are repeated in multiple services.
5. **Service overlap and duplication**
   - SMS utility classes and provider wrappers overlap in responsibilities.
6. **Low-cohesion service classes**
   - API wrappers are broad and difficult to test in isolation.
7. **Heavy composition root**
   - Dependency registrations are dense and concrete-type oriented.

### 1.4 High-priority refactor targets
- Authentication flow (user + admin)
- Registration/identity verification flow
- User panel aggregation flow
- Admin owner/dashboard selection flow
- Unified auth/session storage contract

---

## 2) Clean Architecture Redesign

## 2.1 Target layering

### Domain
- Entities: `User`, `Deal`, `Owner`
- Value Objects: `NationalCode`, `PhoneNumber`, `Money`
- Domain Policies: identity validation, role constraints, stage rules

### Application
- Use Cases:
  - `LoginByNationalCode`
  - `RegisterUser`
  - `SendOtp`
  - `VerifyOtp`
  - `LoadUserPanel`
  - `AdminLogin`
- Ports (interfaces):
  - `IUserRepository`
  - `IIdentityVerificationPort`
  - `ISmsPort`
  - `IAuthSessionPort`
  - `IAdminCredentialVerifier`
- Application DTOs:
  - Request/Response contracts per use case

### Interface Adapters
- ViewModel adapters calling use cases
- Presenters/mappers transforming use case results into UI state
- No external API DTOs crossing into pages/components

### Frameworks & Drivers
- Blazor, HubSpot, Zibal, SMS providers
- Storage wrappers and HTTP clients
- Infrastructure adapters implementing application ports

## 2.2 Dependency rules
- `InterfaceAdapters -> Application -> Domain`
- `Infrastructure` implements `Application.Ports`
- `Domain` has no dependency on UI/infrastructure frameworks

## 2.3 Backward compatibility strategy
- Keep existing routes/pages/components unchanged initially.
- Introduce adapters/use cases incrementally.
- Migrate one vertical slice at a time (Auth -> User Panel -> Admin).
- Keep legacy services behind adapter interfaces until fully replaced.

---

## 3) Refactoring Guidance + Templates

## 3.1 Implemented starter skeleton (added in this change)
- `CleanArchitecture/Domain/Entities/User.cs`
- `CleanArchitecture/Domain/ValueObjects/NationalCode.cs`
- `CleanArchitecture/Application/DTOs/AuthDtos.cs`
- `CleanArchitecture/Application/Ports/*`
- `CleanArchitecture/Application/UseCases/Auth/LoginByNationalCodeUseCase.cs`
- `CleanArchitecture/InterfaceAdapters/Auth/LoginViewModelAdapter.cs`
- `CleanArchitecture/Infrastructure/DependencyInjection/CleanArchitectureModuleExtensions.cs`

These classes provide a compile-safe starter module for migration.

## 3.2 Entity template
```csharp
public sealed class Deal
{
    public string Id { get; }
    public string Name { get; }
    public decimal Amount { get; }
    public string Stage { get; }

    public Deal(string id, string name, decimal amount, string stage)
    {
        Id = id;
        Name = name;
        Amount = amount;
        Stage = stage;
    }
}
```

## 3.3 Use case template
```csharp
public interface ILoadUserPanelUseCase
{
    Task<LoadUserPanelResult> ExecuteAsync(LoadUserPanelRequest request, CancellationToken ct);
}
```

## 3.4 Port + adapter template
```csharp
public interface ISmsPort
{
    Task SendOtpAsync(string phone, string code, CancellationToken ct);
}

public sealed class SmsIrAdapter : ISmsPort
{
    private readonly PicoPlus.Services.SMS.SmsIrService _service;
    public SmsIrAdapter(PicoPlus.Services.SMS.SmsIrService service) => _service = service;

    public Task SendOtpAsync(string phone, string code, CancellationToken ct)
        => _service.SendOtpAsync(phone, code);
}
```

## 3.5 ViewModel adapter template
```csharp
public sealed class RegisterViewModelAdapter
{
    private readonly IRegisterUserUseCase _useCase;
    public RegisterViewModelAdapter(IRegisterUserUseCase useCase) => _useCase = useCase;

    public Task<RegisterUserResult> RegisterAsync(RegisterUserRequest request, CancellationToken ct)
        => _useCase.ExecuteAsync(request, ct);
}
```

## 3.6 Modular DI template
```csharp
public static class ModularServiceRegistration
{
    public static IServiceCollection AddDomainModule(this IServiceCollection services) => services;

    public static IServiceCollection AddApplicationModule(this IServiceCollection services)
    {
        services.AddScoped<ILoginByNationalCodeUseCase, LoginByNationalCodeUseCase>();
        return services;
    }

    public static IServiceCollection AddInterfaceAdapters(this IServiceCollection services)
    {
        services.AddScoped<LoginViewModelAdapter>();
        return services;
    }
}
```

## 3.7 Safe migration playbook
1. Keep existing page/viewmodel contract.
2. Extract business logic into use case class.
3. Inject use case into viewmodel adapter.
4. Replace direct service calls in viewmodel with use case call.
5. Add tests for use case before deleting old branch logic.
6. Migrate next feature slice.

---

## 4) Documentation Package

## 4.1 Architecture overview

```text
[Blazor Pages/Components]
        |
        v
[ViewModel Adapters / Presenters]  (Interface Adapters)
        |
        v
[Use Cases / Interactors]          (Application)
        |
        v
[Domain Entities + Policies]       (Domain)
        ^
        |
[Infrastructure Adapters: HubSpot, Zibal, SMS, Storage]
```

## 4.2 Layer responsibilities with examples
- **Domain:** `NationalCode` checksum validation.
- **Application:** login decision tree, role-based redirect decisions.
- **Interface Adapters:** map form inputs and bind error/loading UI state.
- **Infrastructure:** call HubSpot, send SMS, persist session/local storage.

## 4.3 Old-to-new mapping matrix
- `ViewModels/Auth/LoginViewModel` -> `LoginViewModelAdapter` + `LoginByNationalCodeUseCase`
- `ViewModels/Auth/RegisterViewModel` -> `RegisterViewModelAdapter` + `RegisterUserUseCase`
- `Services/CRM/Objects/Contact` -> `IUserRepository` adapter
- `Services/Auth/OtpService` + `Services/SMS/*` -> `VerifyOtpUseCase` + `ISmsPort`
- `Services/UserPanel/UserPanelService` -> `LoadUserPanelUseCase`

## 4.4 Feature addition guide (entity -> port -> use case -> adapter -> UI)
1. Define/extend domain entity or value object.
2. Add application DTO request/response.
3. Define/update port interface.
4. Implement use case.
5. Implement infrastructure adapter for the port.
6. Add/modify ViewModel adapter.
7. Bind to page/component.
8. Add tests (unit + integration + UI adapter).

## 4.5 Testing guidance
- **Domain tests:** pure validation logic (fast and deterministic).
- **Use case tests:** mock ports; verify branching and outputs.
- **Infrastructure tests:** provider contracts and serialization.
- **UI adapter tests:** command handling, loading/error state transitions.

---

## 5) Execution Plan (practical rollout)

### Phase 1
- Establish clean folders/modules and DI extension points.
- Introduce use-case skeletons and adapters for auth.

### Phase 2
- Migrate login + registration flows to use cases.
- Remove duplicated key handling behind `IAuthSessionPort`.

### Phase 3
- Migrate user panel and admin analytics use cases.
- Replace direct DTO leakage with internal domain/application contracts.

### Phase 4
- Decommission legacy duplicated utility services.
- Harden security (admin credential verifier + hashed secrets).

### Success metrics
- ViewModels no longer reference CRM/SMS/Zibal classes directly.
- External DTOs no longer appear outside infrastructure.
- New module onboarding requires only entity/port/use case/adapter wiring.
- Unit test coverage focuses on application and domain behavior.
