# Clean Architecture Structure Plan

This document defines the **target folder structure**, **dependency boundaries**, and **migration mapping** for the current mixed layout.

## Target folders

```text
Application/
  Abstractions/
  Auth/
  Users/
  UseCases/
Domain/
  Common/
  Users/
  Models/
Infrastructure/
  DependencyInjection/
  Persistence/
  Services/
  State/
Presentation/
  Components/
  Layout/
  Pages/
  Views/
  DependencyInjection/
```

## Dependency boundaries

- `Presentation` → may depend on `Application` only.
- `Application` → may depend on `Domain` only.
- `Infrastructure` → may depend on `Application` + `Domain`.
- `Domain` → depends on no project layer.

## Naming conventions

- **Use cases**: `<Verb><Entity>UseCase` (example: `ResolveLandingRouteUseCase`).
- **Repository contracts**: `I<Entity>Repository` in `Application/Abstractions`.
- **Infrastructure adapters**: `<Provider><Purpose>Service` (example: `AuthSessionService`).
- **Presentation components**: pure/presentational components should not perform data access.

## Migration mapping for current folders

| Current folder | Target layer | Target path | Migration rule |
|---|---|---|---|
| `Components/*` | Presentation | `Presentation/Components/*` | Keep components UI-only; pass data via parameters/callbacks. |
| `Views/*` | Presentation | `Presentation/Views/*` | Route pages call Application use-cases via DI. |
| `Services/*` | Infrastructure + Application | `Infrastructure/Services/*` + `Application/Abstractions/*` | Split contracts (Application) from implementations (Infrastructure). |
| `Models/*` | Domain | `Domain/Models/*` | Keep framework-free model/value-object types in Domain. |
| `State/*` | Presentation or Infrastructure | `Presentation/State/*` or `Infrastructure/State/*` | UI state stays in Presentation; storage/session state in Infrastructure. |

## Phase plan

1. Keep existing files operational in-place.
2. Introduce new files in target folders first.
3. Move one feature slice at a time (Auth, User panel, Admin).
4. Delete legacy folder paths only after references are migrated.
