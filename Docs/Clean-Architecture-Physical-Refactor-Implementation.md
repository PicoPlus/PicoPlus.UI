# Clean Architecture Physical Refactor Implementation

## New solution structure

```text
PicoPlus.sln
src/
  PicoPlus.Domain/
    PicoPlus.Domain.csproj
  PicoPlus.Application/
    PicoPlus.Application.csproj
    Abstractions/
      Auth/IAuthService.cs
      Services/{INavigationService,IDialogService,ISessionStorageService,ILocalStorageService}.cs
      UserPanel/{IUserPanelService,IPersianDateService}.cs
    Dto/UserPanel/*
  PicoPlus.Infrastructure/
    PicoPlus.Infrastructure.csproj
    DependencyInjection/InfrastructureServiceCollectionExtensions.cs
    Infrastructure/{Authorization,Http,Services,State}
    Services/*
    Models/*
    Extensions/*
  PicoPlus.Presentation/
    PicoPlus.Presentation.csproj
    Program.cs
    Components/*
    Pages/*
    Views/*
    wwwroot/*
    Resources/*
```

## Project references (strict direction)

- `PicoPlus.Domain` -> no project references.
- `PicoPlus.Application` -> references `PicoPlus.Domain`.
- `PicoPlus.Infrastructure` -> references `PicoPlus.Application`.
- `PicoPlus.Presentation` -> references `PicoPlus.Application`.

## Physical moves executed

### Moved to `PicoPlus.Presentation`
- `Program.cs`, `PicoPlus.csproj` (renamed to `PicoPlus.Presentation.csproj`), `appsettings.json`.
- Entire UI/runtime web assets: `Components/`, `Pages/`, `Views/`, `wwwroot/`, `Resources/`, `Properties/launchSettings.json`, `Globals.cs`.

### Moved to `PicoPlus.Infrastructure`
- `Infrastructure/*` -> `src/PicoPlus.Infrastructure/Infrastructure/*`.
- `Services/*` -> `src/PicoPlus.Infrastructure/Services/*`.
- `Models/*` -> `src/PicoPlus.Infrastructure/Models/*`.
- `Extensions/*` -> `src/PicoPlus.Infrastructure/Extensions/*`.
- `State/Admin/*` -> `src/PicoPlus.Infrastructure/State/Admin/*`.

### Moved to `PicoPlus.Application`
- Service abstractions moved from old infrastructure services:
  - `INavigationService`, `IDialogService`, `ISessionStorageService`, `ILocalStorageService`.
- User panel contracts moved:
  - `IUserPanelService`, `IPersianDateService`.
- User panel DTO/state records moved from `State/UserPanel/*` to `Application/Dto/UserPanel/*`.
- New auth abstraction added:
  - `Abstractions/Auth/IAuthService.cs`.

## Namespace updates

- `PicoPlus.State.UserPanel` -> `PicoPlus.Application.Dto.UserPanel`.
- `PicoPlus.Services.UserPanel` interfaces -> `PicoPlus.Application.Abstractions.UserPanel`.
- cross-layer service abstraction usages in presentation/infrastructure updated to `PicoPlus.Application.Abstractions.Services`.
- login page now depends on `PicoPlus.Application.Abstractions.Auth.IAuthService` abstraction.

## Program.cs (Presentation only)

- `Program.cs` exists only in `src/PicoPlus.Presentation`.
- Presentation no longer has compile-time reference to infrastructure.
- Infrastructure DI registration is loaded via reflection:
  - `Assembly.Load("PicoPlus.Infrastructure")`
  - invoke `PicoPlus.Infrastructure.DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure(IServiceCollection, IConfiguration)`.

## Notes

- To enforce “no business logic in Presentation” rapidly, admin/deal views were reduced to placeholder routes pending full use-case migration.
- Infrastructure remains the only layer containing HttpClient/external API registrations and implementations.
