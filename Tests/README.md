# NovinCRM Test Suite

Three test projects live under `Tests/`:

| Project | Framework | Purpose |
|---------|-----------|---------|
| `NovinCRM.Tests.Unit` | xUnit + NSubstitute + FluentAssertions | Pure unit tests — no I/O, no HTTP |
| `NovinCRM.Tests.Integration` | xUnit + `WebApplicationFactory` | In-process ASP.NET Core tests |
| `NovinCRM.Tests.Component` | bUnit + xUnit | Blazor component tests |

## Running all tests

```bash
# From repo root
dotnet test Tests/NovinCRM.Tests.Unit/NovinCRM.Tests.Unit.csproj
dotnet test Tests/NovinCRM.Tests.Integration/NovinCRM.Tests.Integration.csproj
dotnet test Tests/NovinCRM.Tests.Component/NovinCRM.Tests.Component.csproj
```

Or, once a solution file is configured:

```bash
dotnet test
```

## Covered paths (initial)

- `HubSpotSignatureVerifier` — valid signature, missing headers, expired timestamp, signature mismatch, replay detection
- `InMemorySyncStateRepository` — get/set version, idempotency, deleted flag
- `CacheKeys` — all key factory methods

## Adding new tests

1. Place unit tests in `Tests/NovinCRM.Tests.Unit/<Layer>/`
2. Place integration tests in `Tests/NovinCRM.Tests.Integration/<Feature>/`
3. Place component tests in `Tests/NovinCRM.Tests.Component/<ComponentName>/`

Follow the naming convention: `{ClassUnderTest}Tests.cs`
