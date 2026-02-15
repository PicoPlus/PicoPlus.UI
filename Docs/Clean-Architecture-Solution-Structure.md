# Clean Architecture Solution Structure

## Workstream mapping
- #27 Define solution structure for Clean Architecture
- #28 Integrate latest Bootstrap setup and theming
- #29 Migrate UI components to Bootstrap + clean patterns
- #30 Refactor services, models, and logic into layers
- #31 End-to-end testing after migration

## Current layout (incremental migration)
- `Presentation/`: Blazor pages, layout-facing components, UI composition.
- `Application/`: use-cases and layer abstractions.
- `Domain/`: business entities and shared domain primitives.
- `Infrastructure/`: implementations for storage, http adapters, and framework-specific services.

## Conventions
- Presentation references only Application contracts/use-cases.
- Application references Domain only.
- Infrastructure references Application + Domain.
- Keep legacy `Services/*` alive while introducing adapters under `Infrastructure/*`.

## Next migration slices
1. Move authentication flows from view-models into `Application/Auth` use-cases.
2. Move user panel orchestration into `Application/UserPanel`.
3. Introduce infrastructure repository interfaces for CRM and SMS.
4. Add bUnit page tests and Playwright smoke tests once CI has .NET SDK.


## UI component migration rules
- Prefer Bootstrap utility/layout classes over custom one-off classes.
- Keep shared components stateless (parent controls visibility/state through parameters).
- Keep service calls and business logic in Application/Infrastructure layers and pass prepared data to UI components.
