# Infrastructure Layer

Contains implementations for external systems (HTTP, storage, providers).

Rules:
- Implement abstractions from `Application`.
- Keep provider-specific concerns out of Presentation.
- Register adapters via `Infrastructure/DependencyInjection`.
