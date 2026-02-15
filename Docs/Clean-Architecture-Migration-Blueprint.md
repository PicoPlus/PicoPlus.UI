# Clean Architecture + Bootstrap Migration Blueprint

## Scope of this phase
- Introduce an initial Clean Architecture skeleton (`Presentation`, `Application`, `Domain`, `Infrastructure`).
- Keep existing features working while gradually moving logic from legacy folders.
- Standardize on Bootstrap 5.3.3 CDN references.

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

## Bootstrap modernization strategy
- Use Bootstrap v5.3.3 RTL + bundle from jsDelivr.
- Favor utility classes and cards/grid over custom CSS when possible.
- Keep custom CSS only for brand-specific styling.

## Completed in this commit
- Added initial layer folders and dependency-injection extension points.
- Added a sample use-case (`GetUserProfileUseCase`) and in-memory repository adapter.
- Added `/architecture` page to visualize migration direction.
