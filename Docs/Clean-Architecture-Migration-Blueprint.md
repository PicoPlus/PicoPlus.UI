# Clean Architecture + Bootstrap Migration Blueprint

## Roadmap alignment
- #27 Define solution structure for Clean Architecture ✅
- #28 Integrate latest Bootstrap: setup and theming ✅ (foundation)
- #29 Migrate UI components to Bootstrap + clean patterns ✅ (initial shell + dashboard)
- #30 Refactor services, models, and logic into layers ✅ (first auth slice)
- #31 End-to-end testing after migration 🟡 (blocked by missing .NET SDK in runtime)

## Scope of this phase
- Introduce an initial Clean Architecture skeleton (`Presentation`, `Application`, `Domain`, `Infrastructure`).
- Keep existing features working while gradually moving logic from legacy folders.
- Standardize on Bootstrap 5.3.3 CDN references and central theming variables.

## Target architecture

### Presentation layer
- Blazor pages/components and UI state.
- Calls application use-cases only.
- No direct access to external APIs.

### Application layer
- Use-case classes and orchestration.
- Depends only on domain abstractions.
- Defines interfaces for repositories/services.

### Domain layer
- Core entities and value objects.
- Framework-independent rules.

### Infrastructure layer
- API/database adapters.
- Implements application interfaces.

## Incremental migration strategy
1. Keep current `Services/*` implementation as legacy adapters.
2. Introduce matching abstractions in `Application/Abstractions`.
3. Move business rules into use-cases under `Application/*`.
4. Keep Blazor pages thin and bind them to use-cases.
5. Replace in-memory adapters with production-ready infrastructure implementations.

## Completed in this commit
- Switched Bootstrap include to `@latest` CDN reference and removed legacy local Bootstrap v5.1 static assets.
- Removed redundant global stylesheet include (`/css/app.css`) from app shell to reduce overlap with Bootstrap theming layers.
- Added architecture structure guidance (`Docs/Clean-Architecture-Solution-Structure.md`).
- Refactored home-landing decision into `ResolveLandingRouteUseCase` + infrastructure auth session adapter.
- Modernized shell setup with Bootstrap theming file (`wwwroot/css/bootstrap-theme.css`).
- Updated layout/header and architecture page styles to align with Bootstrap-first patterns.
