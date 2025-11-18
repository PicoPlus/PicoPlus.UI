# Extension System Architecture

## System Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         Application Startup                       │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Program.cs - Service Setup                    │
│  builder.Services.AddExtensions(builder.Configuration)           │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ExtensionManager                            │
│  1. DiscoverExtensions()                                         │
│     - Scan assembly for IExtension types                         │
│     - Create instances                                           │
│     - Check enabled/disabled status                              │
│     - Sort by dependencies                                       │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. ConfigureServices(IServiceCollection, IConfiguration)        │
│     For each extension:                                          │
│       extension.ConfigureServices(services, configuration)       │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Program.cs - App Build                        │
│  var app = builder.Build();                                      │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Program.cs - Pipeline Setup                     │
│  app.UseExtensions();                                            │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. ConfigureApplication(WebApplication)                         │
│     For each extension:                                          │
│       extension.ConfigureApplication(app)                        │
└─────────────────────────────────────────────────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Application Running                         │
└─────────────────────────────────────────────────────────────────┘
```

## Component Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                         IExtension                               │
│  ┌───────────────────────────────────────────────────────┐      │
│  │ + Metadata: ExtensionMetadata                         │      │
│  │ + ConfigureServices(services, config): void           │      │
│  │ + ConfigureApplication(app): void                     │      │
│  └───────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
                                 △
                                 │ implements
                                 │
         ┌───────────────────────┴───────────────────────┐
         │                                               │
┌────────┴────────────┐                     ┌────────────┴──────────┐
│   BaseExtension     │                     │  Custom Extensions     │
│  (abstract class)   │                     │  (concrete classes)    │
├─────────────────────┤                     ├────────────────────────┤
│ Default empty       │                     │ HealthCheckExtension   │
│ implementations     │                     │ CustomLoggingExtension │
│                     │                     │ ExtensionInfoApi...    │
└─────────────────────┘                     │ YourCustomExtension    │
                                            └────────────────────────┘
```

## Extension Lifecycle

```
┌─────────────┐
│  CREATED    │  Extension instance created via reflection
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  ENABLED?   │  Check ExtensionOptions + EnabledByDefault
└──────┬──────┘
       │ Yes
       ▼
┌─────────────┐
│   LOADED    │  Added to ExtensionManager._extensions list
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   SORTED    │  Ordered by dependencies
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  SERVICES   │  ConfigureServices() called
│ REGISTERED  │  Services added to DI container
└──────┬──────┘
       │
       ▼
┌─────────────┐
│     APP     │  ConfigureApplication() called
│ CONFIGURED  │  Middleware/endpoints added
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   ACTIVE    │  Extension is running
└─────────────┘
```

## Dependency Resolution Example

```
Extension A (no dependencies)
Extension B (depends on A)
Extension C (depends on A, B)
Extension D (no dependencies)

Load Order:
1. Extension A (dependency of B, C)
2. Extension D (no dependencies)
3. Extension B (dependency of C)
4. Extension C (depends on A, B - both loaded)
```

## Data Flow

```
┌─────────────────┐
│  Configuration  │ ─────────┐
│   (appsettings) │          │
└─────────────────┘          │
                             ▼
┌─────────────────┐    ┌──────────────────┐
│ ExtensionOptions│───▶│ ExtensionManager │
│ Enable/Disable  │    └────────┬─────────┘
└─────────────────┘             │
                                │ manages
                                ▼
                     ┌──────────────────────┐
                     │   IExtension List    │
                     │  [Ext1, Ext2, Ext3]  │
                     └──────────┬───────────┘
                                │
         ┌──────────────────────┼──────────────────────┐
         ▼                      ▼                      ▼
┌────────────────┐    ┌────────────────┐    ┌────────────────┐
│   Extension 1  │    │   Extension 2  │    │   Extension 3  │
│                │    │                │    │                │
│ Services: []   │    │ Services: []   │    │ Services: []   │
│ Endpoints: []  │    │ Endpoints: []  │    │ Middleware: [] │
└────────┬───────┘    └────────┬───────┘    └────────┬───────┘
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               ▼
                    ┌──────────────────────┐
                    │  WebApplication      │
                    │  (with all services  │
                    │   and middleware)    │
                    └──────────────────────┘
```

## Extension Types by Purpose

```
┌───────────────────────────────────────────────────────────────┐
│                        Extension Types                        │
└───────────────────────────────────────────────────────────────┘

Service Registration Extensions
├─ Register new services with DI
├─ Example: CustomLoggingExtension
└─ Override: ConfigureServices()

Endpoint Extensions
├─ Add new API endpoints
├─ Example: HealthCheckExtension, ExtensionInfoApiExtension
└─ Override: ConfigureApplication()

Middleware Extensions
├─ Add middleware to pipeline
├─ Example: RateLimitingExtension
└─ Override: ConfigureApplication()

Background Service Extensions
├─ Register hosted services
├─ Example: DatabaseBackupExtension
└─ Override: ConfigureServices()

Configuration Extensions
├─ Configure app settings/options
├─ Example: CorsExtension
└─ Override: ConfigureServices()

Composite Extensions
├─ Combine multiple concerns
├─ Example: AdvancedReportingExtension
└─ Override: Both methods
```

## Integration Points

```
┌─────────────────────────────────────────────────────────────┐
│                         Your Code                           │
└─────────────────────────────────────────────────────────────┘
                              ││
                              ││ Can inject services
                              ││ registered by extensions
                              ▼▼
┌─────────────────────────────────────────────────────────────┐
│                  Dependency Injection                       │
│  Services from:                                             │
│  - Core Application                                         │
│  - Extension 1                                              │
│  - Extension 2                                              │
│  - Extension N                                              │
└─────────────────────────────────────────────────────────────┘
                              ││
                              ││ Extensions can use
                              ││ core services
                              ▼▼
┌─────────────────────────────────────────────────────────────┐
│                    Extension System                         │
│  - ILogger<T>                                               │
│  - IConfiguration                                           │
│  - IServiceProvider                                         │
│  - ExtensionManager                                         │
└─────────────────────────────────────────────────────────────┘
```

## Configuration Flow

```
appsettings.json
     │
     ▼
┌─────────────────┐
│ IConfiguration  │
└────────┬────────┘
         │
         ├─────────────────────────────────┐
         │                                 │
         ▼                                 ▼
┌────────────────┐              ┌──────────────────┐
│ Core Services  │              │ Extension System │
│  - HubSpot     │              │  Options Config  │
│  - SMS         │              └────────┬─────────┘
│  - CRM         │                       │
└────────────────┘                       │
                                         ▼
                              ┌──────────────────┐
                              │   Extensions     │
                              │  Can read from   │
                              │  IConfiguration  │
                              └──────────────────┘
```

## Error Handling

```
Extension Load Attempt
         │
         ▼
    ┌────────┐
    │ Try    │
    └───┬────┘
        │
        ├─ Success ───────▶ Extension Loaded
        │
        ├─ Disabled ──────▶ Log Info & Skip
        │
        └─ Error ─────────▶ Log Error & Continue
                            (Other extensions still load)
```

## Benefits Visualization

```
Traditional Approach:
┌──────────────────────────────────────┐
│         Monolithic Core              │
│  ┌────────────────────────────────┐  │
│  │ Feature A │ Feature B │ ...    │  │
│  └────────────────────────────────┘  │
│  All features tightly coupled        │
└──────────────────────────────────────┘
         ❌ Hard to maintain
         ❌ Difficult to test
         ❌ Can't disable features

Extension Approach:
┌──────────────────────────────────────┐
│          Core Application            │
└──────────────────────────────────────┘
         │       │       │
         ▼       ▼       ▼
    ┌────┐  ┌────┐  ┌────┐
    │Ext │  │Ext │  │Ext │
    │ A  │  │ B  │  │ C  │
    └────┘  └────┘  └────┘
         ✅ Easy to maintain
         ✅ Easy to test
         ✅ Can enable/disable
```

## Summary

The extension system provides:
- **Loose Coupling**: Extensions are independent
- **High Cohesion**: Each extension has a single purpose
- **Easy Testing**: Test extensions in isolation
- **Flexibility**: Enable/disable at runtime
- **Scalability**: Add features without changing core code
- **Maintainability**: Clear separation of concerns
