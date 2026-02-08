# Project Reanalysis After MVVM Removal

Date: 2026-02-08
Scope: Full repository static analysis of current `work` branch after commit `8a24f1b`.
Method: file inventory + targeted ripgrep dependency tracing + focused file reads.

## Architecture Findings

1. **No formal clean architecture layers exist yet (single web project still contains all concerns).**
   - Evidence: `PicoPlus.csproj` is still a single `Microsoft.NET.Sdk.Web` app and no separate Domain/Application/Infrastructure/Presentation projects are present.
   - Files: `PicoPlus.csproj`, `Program.cs`.

2. **Layer mixing remains: presentation consumes service implementations directly instead of application interfaces/use-cases.**
   - `Views/auth/Login.razor` injects `AuthService` directly.
   - `Pages/User/Panel.razor.cs` injects `IUserPanelService` from `Services/` namespace (not Application layer contract assembly).
   - Files: `Views/auth/Login.razor`, `Pages/User/Panel.razor.cs`, `Services/Auth/AuthService.cs`, `Services/UserPanel/IUserPanelService.cs`.

3. **State-pattern remnants still active (explicitly disallowed by requirements).**
   - `State/Admin/AdminStateService.cs` and `State/UserPanel/*` are still core to page behavior.
   - Admin pages inject `AdminStateService` directly.
   - Files: `State/Admin/AdminStateService.cs`, `State/UserPanel/UserPanelState.cs`, `Views/Admin/Dashboard.razor`, `Views/Admin/Kanban.razor`, `Views/Admin/Settings.razor`, `Views/Admin/ContactManagement.razor`, `Components/Layout/AdminLayout.razor`.

## UI Findings

1. **Functional regression: registration flow replaced by placeholder screen.**
   - Current `/auth/register` page explicitly states registration was removed and deferred.
   - File: `Views/auth/Register.razor`.

2. **Security regression in login page: hardcoded admin credentials in UI logic.**
   - Email/password check (`admin@picoplus.app` / `Admin@123`) is embedded in component code.
   - File: `Views/auth/Login.razor`.

3. **Business/auth logic remains in Razor component code-behind (UI-bound logic).**
   - Login page sets session keys and decides role navigation directly.
   - File: `Views/auth/Login.razor`.

4. **Mixed route model still present (`Views`, `Pages`, `Components/Pages`).**
   - Coexistence of routable features in multiple folder paradigms increases cognitive load.
   - Files: `Views/auth/Login.razor`, `Pages/User/Panel.razor`, `Components/Pages/Home.razor`, `Components/Pages/Counter.razor`, `Components/Pages/Weather.razor`.

5. **Template/demo pages remain routable in production surface.**
   - `/counter` and `/weather` still exist.
   - Files: `Components/Pages/Counter.razor`, `Components/Pages/Weather.razor`.

6. **Potential dead UI component:**
   - `Components/Dialogs/CompleteBirthDateDialog.razor` has no incoming references.
   - File: `Components/Dialogs/CompleteBirthDateDialog.razor`.

## Services Findings

1. **Session key inconsistency bug between auth and user-panel services.**
   - `AuthService` writes role key as `user_role`.
   - `UserPanelService` clears `UserRole` (different key), which can leave stale role data.
   - Files: `Services/Auth/AuthService.cs`, `Services/UserPanel/UserPanelService.cs`.

2. **HubSpot token/header logic duplication persists across CRM object services.**
   - HubSpot auth header is configured in `Program.cs` named client and also manually added in services.
   - Files: `Program.cs`, `Services/CRM/Objects/Contact.cs`, `Services/CRM/Objects/Deal.cs`, `Services/CRM/Objects/Company.cs`, `Services/CRM/Objects/Ticket.cs`.

3. **Unused DI registration detected.**
   - `builder.Services.AddScoped<PicoPlus.Views.Deal.Create>();` has no consumer references.
   - File: `Program.cs`.

## Infrastructure Findings

1. **Orphan configuration classes likely unused in runtime graph.**
   - `HubSpotHttpClientConfig` and `SmsIrHttpClientConfig` are present but not referenced by registrations.
   - Files: `Infrastructure/Http/HubSpotHttpClientConfig.cs`, `Infrastructure/Http/SmsIrHttpClientConfig.cs`, `Program.cs`.

2. **Infrastructure abstractions still leak into presentation.**
   - Login page uses `ISessionStorageService` and concrete `AuthService` from UI component.
   - File: `Views/auth/Login.razor`.

## External API Findings

1. **External API orchestration is not isolated behind application ports.**
   - CRM, SMS, Liara services are registered and consumed in web project directly.
   - Files: `Program.cs`, `Services/CRM/**/*`, `Services/SMS/**/*`, `Services/Utils/LiaraApiService.cs`.

2. **User authentication no longer verifies against CRM/OTP flow.**
   - User login only validates national-code length and sets local session state.
   - File: `Views/auth/Login.razor`.

## Dead/Unused/Redundant Artifacts (current state)

- Candidate dead page flow: `Views/User/Home.razor` now only redirects to `/user/panel`; original user-home implementation removed.
- Candidate dead dialog: `Components/Dialogs/CompleteBirthDateDialog.razor` (no references found).
- Candidate stale static config helpers: `Infrastructure/Http/HubSpotHttpClientConfig.cs`, `Infrastructure/Http/SmsIrHttpClientConfig.cs`.
- Candidate unnecessary DI entry: `AddScoped<PicoPlus.Views.Deal.Create>()` in `Program.cs`.

## Conclusion

MVVM files were removed, but the current system is not yet aligned with the target clean architecture constraints. The biggest immediate risks are:
1) broken registration use case,
2) hardcoded admin credentials and UI-bound authentication logic,
3) persistent state/layer leakage and key inconsistency across auth/session services.
