# Neo4j Backend Migration Blueprint

This blueprint defines a **strict Clean Architecture + Microkernel/Plugin** backend migration strategy to Neo4j while preserving the existing frontend and API contract.

> Recommended concrete stack for this repository context: **ASP.NET Core (.NET 9) + Neo4j.Driver**.

---

## 1) Target Architecture (Clean + Plugin)

### 1.1 Layering Rules (Non-Negotiable)

- **Domain (Entities + Value Objects + Domain Interfaces)** has zero dependency on framework, HTTP, database, or logging packages.
- **Application (Use Cases)** depends only on Domain abstractions.
- **Adapters (HTTP, Persistence, Messaging)** implement Application/Domain ports.
- **Infrastructure (Neo4j driver, hosting, plugin loader)** is outermost and depends inward.
- Dependency direction always points inward.

### 1.2 Microkernel Rules

- Kernel owns process startup, config, observability, DI root, HTTP host, event bus, and plugin lifecycle.
- Each feature is an isolated plugin package with:
  - its own domain/application/adapter folders,
  - a plugin manifest,
  - explicit route registration.
- Plugins communicate through kernel abstractions (`IEventBus`, contracts), not by direct references.

---

## 2) Deliverable A — File Structure

```text
/src
  /Kernel
    /Abstractions
      IPlugin.cs
      IPluginContext.cs
      IEventBus.cs
      IUnitOfWork.cs
      INeo4jSessionFactory.cs
    /Bootstrap
      ServiceCollectionExtensions.cs
      PluginLoader.cs
      Neo4jRegistration.cs
    /Hosting
      KernelHost.cs
      RouteRegistry.cs

  /Plugins
    /UserModule
      /Domain
        User.cs
        IUserRepository.cs
      /Application
        CreateUserUseCase.cs
        GetUserByIdUseCase.cs
      /Adapters
        /Neo4j
          Neo4jUserRepository.cs
          UserCypher.cs
        /Http
          UserEndpoints.cs
          UserDtos.cs
      UserPlugin.cs
      user-module.plugin.json

/tests
  /Kernel.Tests
    PluginLoaderTests.cs
  /UserModule.Tests
    CreateUserUseCaseTests.cs
```

---

## 3) Deliverable B — Core Interface Definitions (C#)

### 3.1 Plugin contract

```csharp
namespace Kernel.Abstractions;

public interface IPlugin
{
    string Name { get; }
    Task InitializeAsync(IPluginContext context, CancellationToken ct);
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    void MapEndpoints(IEndpointRouteBuilder app);
}
```

### 3.2 Plugin context + shared kernel services

```csharp
namespace Kernel.Abstractions;

public interface IPluginContext
{
    IServiceProvider Services { get; }
    IConfiguration Configuration { get; }
    ILoggerFactory LoggerFactory { get; }
    IEventBus EventBus { get; }
    INeo4jSessionFactory Neo4j { get; }
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct);
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
}

public interface INeo4jSessionFactory
{
    IAsyncSession OpenReadSession();
    IAsyncSession OpenWriteSession();
}
```

### 3.3 Plugin manifest model

```csharp
public sealed class PluginManifest
{
    public required string Name { get; init; }
    public required string AssemblyPath { get; init; }
    public required string EntryType { get; init; } // fully qualified type implementing IPlugin
    public bool Enabled { get; init; } = true;
}
```

---

## 4) Deliverable C — Neo4j Repository Pattern (Adapter isolation)

### 4.1 Domain entity + repository port

```csharp
namespace Plugins.UserModule.Domain;

public sealed class User
{
    public Guid Id { get; }
    public string Mobile { get; }
    public string DisplayName { get; }

    public User(Guid id, string mobile, string displayName)
    {
        Id = id;
        Mobile = mobile;
        DisplayName = displayName;
    }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task CreateAsync(User user, CancellationToken ct);
}
```

### 4.2 Use case stays database-agnostic

```csharp
namespace Plugins.UserModule.Application;

public sealed class CreateUserUseCase
{
    private readonly IUserRepository _repo;

    public CreateUserUseCase(IUserRepository repo) => _repo = repo;

    public async Task ExecuteAsync(Guid id, string mobile, string displayName, CancellationToken ct)
    {
        var user = new User(id, mobile, displayName);
        await _repo.CreateAsync(user, ct);
    }
}
```

### 4.3 Neo4j adapter implementation

```csharp
namespace Plugins.UserModule.Adapters.Neo4j;

public sealed class Neo4jUserRepository : IUserRepository
{
    private readonly INeo4jSessionFactory _sessions;

    public Neo4jUserRepository(INeo4jSessionFactory sessions) => _sessions = sessions;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        const string cypher = """
            MATCH (u:User {id: $id})
            RETURN u.id AS id, u.mobile AS mobile, u.displayName AS displayName
            LIMIT 1
            """;

        await using var session = _sessions.OpenReadSession();
        var cursor = await session.RunAsync(cypher, new { id = id.ToString() });
        var record = await cursor.SingleOrDefaultAsync();
        if (record is null) return null;

        return new User(
            Guid.Parse(record["id"].As<string>()),
            record["mobile"].As<string>(),
            record["displayName"].As<string>()
        );
    }

    public async Task CreateAsync(User user, CancellationToken ct)
    {
        const string cypher = """
            CREATE (u:User {id: $id, mobile: $mobile, displayName: $displayName})
            """;

        await using var session = _sessions.OpenWriteSession();
        await session.RunAsync(cypher, new
        {
            id = user.Id.ToString(),
            mobile = user.Mobile,
            displayName = user.DisplayName
        });
    }
}
```

### 4.4 Relationship-first graph modeling example

```cypher
// user purchases product with transaction metadata
MATCH (u:User {id:$userId}), (p:Product {id:$productId})
MERGE (u)-[r:PURCHASED]->(p)
SET r.lastPurchasedAt = datetime(),
    r.count = coalesce(r.count, 0) + 1,
    r.totalAmount = coalesce(r.totalAmount, 0) + $amount;
```

---

## 5) Deliverable D — Runtime DI + Dynamic Plugin Loading

### 5.1 Kernel startup sequence

1. Build root `ServiceCollection`.
2. Register singleton `IDriver` and `INeo4jSessionFactory`.
3. Read plugin manifests from `/plugins/**.plugin.json`.
4. For each enabled plugin:
   - Load assembly (`AssemblyLoadContext`),
   - instantiate `EntryType` implementing `IPlugin`,
   - call `InitializeAsync(context)`.
5. After all initialized, call `StartAsync`.
6. Map endpoints from each plugin on the shared ASP.NET pipeline.

### 5.2 DI registration (kernel)

```csharp
public static class Neo4jRegistration
{
    public static IServiceCollection AddNeo4j(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
            cfg["Neo4j:Uri"],
            AuthTokens.Basic(cfg["Neo4j:User"], cfg["Neo4j:Password"])
        ));

        services.AddSingleton<INeo4jSessionFactory, Neo4jSessionFactory>();
        return services;
    }
}
```

### 5.3 Plugin module + runtime instance (recommended split)

```csharp
public interface IPluginModule
{
    string Name { get; }
    void Register(IServiceCollection services, IConfiguration config);
    IPlugin Build(IServiceProvider rootProvider);
}

public sealed class UserPluginModule : IPluginModule
{
    public string Name => "UserModule";

    public void Register(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IUserRepository, Neo4jUserRepository>();
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<GetUserByIdUseCase>();
    }

    public IPlugin Build(IServiceProvider rootProvider)
        => ActivatorUtilities.CreateInstance<UserPlugin>(rootProvider);
}

public sealed class UserPlugin : IPlugin
{
    public string Name => "UserModule";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct) => Task.CompletedTask;
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    public void MapEndpoints(IEndpointRouteBuilder app)
        => UserEndpoints.Map(app);
}
```

---

## 6) Migration Strategy (Execution Plan)

### Phase 1 — Architectural Foundation (Microkernel)

- Freeze plugin contracts and publish versioned package (e.g., `Kernel.Abstractions v1`).
- Add contract tests for plugin lifecycle and endpoint mapping invariants.
- Add observability baseline: request IDs, query timings, plugin health endpoints.

#### 1) Define plugin contracts
- Required lifecycle methods:
  - `InitializeAsync(IPluginContext, CancellationToken)`
  - `StartAsync(CancellationToken)`
  - `StopAsync(CancellationToken)`
- Required route registration:
  - `MapEndpoints(IEndpointRouteBuilder)`
- Required module registration boundary:
  - `IPluginModule.Register(IServiceCollection, IConfiguration)`

#### 2) Implement core kernel
- Configuration loading:
  - Use layered providers (`appsettings.json`, env vars, secret store).
  - Validate required config at startup (fail fast).
- Neo4j driver initialization (singleton pool):

```csharp
services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
    cfg["Neo4j:Uri"],
    AuthTokens.Basic(cfg["Neo4j:User"], cfg["Neo4j:Password"]),
    o => o.WithMaxConnectionPoolSize(100)
          .WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(15))
));
```

- Dynamic plugin discovery/loading:
  - Read manifests from a trusted plugin folder.
  - Load only allow-listed/signature-verified assemblies in production.
  - Resolve `IPluginModule`, call `Register(...)`, then build plugin instance.
- Global logging/error handling:
  - Structured logs with plugin name, route, correlation ID.
  - Kernel-level exception middleware that normalizes response envelope.

#### 3) Establish event bus
- Internal Pub/Sub contract only (`IEventBus`) for cross-plugin communication.
- No direct plugin references; only event payload contracts.
- Delivery expectations should be explicit:
  - at-most-once (in-memory bus) for local orchestration,
  - outbox-backed reliable delivery when business-critical.

#### Phase 1 acceptance criteria
- Kernel boots with config validation and Neo4j connectivity check.
- At least one plugin discovered from manifest and mapped to HTTP routes.
- Plugin lifecycle hooks invoked in order (`Initialize` -> `Start` -> `Stop`).
- One cross-plugin event published and consumed through `IEventBus`.
- Unhandled exceptions captured by global middleware and logged with correlation IDs.

### Phase 2 — Graph Modeling and Query Contracts
- Define canonical graph aggregates (`User`, `Order`, `Product`, `InventoryItem`, etc.).
- Encode relationship semantics and cardinality rules.
- Add constraints/indexes early:

```cypher
CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.id IS UNIQUE;

CREATE INDEX product_sku_idx IF NOT EXISTS
FOR (p:Product) ON (p.sku);
```

### Phase 3 — Vertical Slice Pilot (UserModule)
- Port one bounded context end-to-end.
- Keep endpoint path, HTTP verb, request/response DTO shape identical.
- Run backward-compatibility contract tests against old/new backends.

### Phase 4 — Strangler Rollout
- Route migrated modules to new kernel.
- Keep non-migrated modules on legacy backend.
- Optionally enable dual writes/CDC for short sync windows.

### Phase 5 — Decommission
- Freeze writes on legacy.
- Execute final delta sync.
- Switch 100% traffic and archive legacy data.

---

## 7) API Compatibility Guardrails

- Preserve URL paths, status codes, validation messages, and paging formats.
- Add API snapshot tests for JSON payload parity.
- Use consumer-driven contract tests where frontend expects strict shapes.

---

## 8) Optimized Prompt (for Codex/GPT)

```text
Act as a Senior Software Architect specializing in Graph Databases and Modular Systems.

Objective: Design a backend migration strategy to Neo4j with a strictly enforcing "Clean Architecture" and a "Microkernel/Plugin" pattern.

Context:
- Current Backend: ASP.NET Core (.NET 9 Web API)
- Target Database: Neo4j (Cypher)
- Frontend: Existing UI must remain untouched. REST API contract must remain identical.

Architectural Constraints:
1) Enforce Clean Architecture dependency direction inward only.
2) Implement Microkernel where each business domain is a standalone plugin implementing a strict IPlugin interface.
3) Model graph by relationships-first semantics, not SQL table mirroring.
4) Keep Domain and Use Cases database-agnostic.

Deliverables:
1) File tree (Kernel + one UserModule plugin).
2) C# interfaces for IPlugin, IPluginContext, IEventBus, IUserRepository.
3) Repository adapter example with Neo4j.Driver and Cypher, isolated from Domain.
4) Runtime plugin loading + DI flow using manifests and assembly loading.
5) HTTP endpoint example preserving an existing API contract.

Output style:
- Provide compilable C# skeletons.
- Mark boundaries explicitly (Domain/Application/Adapters/Infrastructure).
- Include migration phases and risk controls.
```
