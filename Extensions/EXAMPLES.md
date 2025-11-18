# Extension System Usage Examples

This document provides practical examples of how to use the PicoPlus Extension System.

## Example 1: Simple Feature Toggle Extension

Create an extension that adds a feature toggle system:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions;

public class FeatureToggleExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "feature-toggle",
        Name = "Feature Toggle System",
        Description = "Adds feature toggle capabilities",
        Version = "1.0.0",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
    }

    public override void ConfigureApplication(WebApplication app)
    {
        // Optionally add an endpoint to check feature status
        app.MapGet("/api/features/{name}", (string name, IFeatureToggleService service) =>
        {
            var isEnabled = service.IsEnabled(name);
            return Results.Ok(new { Feature = name, Enabled = isEnabled });
        });
    }
}
```

## Example 2: Database Backup Extension

Add scheduled database backup functionality:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions;

public class DatabaseBackupExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "database-backup",
        Name = "Database Backup",
        Description = "Automated database backup service",
        Version = "1.0.0",
        EnabledByDefault = false  // Disabled by default, enable in production
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the backup service as a hosted service
        services.AddHostedService<DatabaseBackupHostedService>();
        
        // Register backup configuration
        services.Configure<BackupOptions>(configuration.GetSection("Backup"));
    }
}
```

## Example 3: Email Notification Extension

Add email notification capabilities:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions;

public class EmailNotificationExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "email-notification",
        Name = "Email Notifications",
        Description = "Sends email notifications for important events",
        Version = "1.0.0",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configure email settings from configuration
        var smtpSettings = configuration.GetSection("Email:Smtp");
        
        services.Configure<SmtpSettings>(smtpSettings);
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
    }
}
```

## Example 4: API Rate Limiting Extension

Add rate limiting to API endpoints:

```csharp
using PicoPlus.Infrastructure.Extensions;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace PicoPlus.Extensions;

public class RateLimitingExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "rate-limiting",
        Name = "API Rate Limiting",
        Description = "Adds rate limiting to API endpoints",
        Version = "1.0.0",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
    }

    public override void ConfigureApplication(WebApplication app)
    {
        app.UseRateLimiter();
    }
}
```

## Example 5: Swagger Documentation Extension

Add Swagger/OpenAPI documentation:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions;

public class SwaggerExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "swagger-docs",
        Name = "Swagger Documentation",
        Description = "Adds Swagger/OpenAPI documentation",
        Version = "1.0.0",
        EnabledByDefault = false  // Enable in development
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "PicoPlus API", Version = "v1" });
        });
    }

    public override void ConfigureApplication(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
```

## Example 6: Extension with Dependencies

Create an extension that depends on another:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions;

public class AdvancedReportingExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "advanced-reporting",
        Name = "Advanced Reporting",
        Description = "Advanced reporting features",
        Version = "1.0.0",
        Dependencies = new[] { "email-notification", "database-backup" },
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // This extension requires email-notification and database-backup
        services.AddScoped<IReportingService, ReportingService>();
    }
}
```

## Example 7: Conditional Extension

Extension that only activates under certain conditions:

```csharp
using PicoPlus.Infrastructure.Extensions;

namespace PicoPlus.Extensions;

public class DevelopmentToolsExtension : BaseExtension
{
    public override ExtensionMetadata Metadata => new()
    {
        Id = "dev-tools",
        Name = "Development Tools",
        Description = "Development and debugging tools",
        Version = "1.0.0",
        EnabledByDefault = true
    };

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Only register in development
        var isDevelopment = configuration.GetValue<bool>("IsDevelopment");
        
        if (isDevelopment)
        {
            services.AddScoped<IDebugService, DebugService>();
        }
    }

    public override void ConfigureApplication(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Add development-only endpoints
            app.MapGet("/debug/config", (IConfiguration config) =>
            {
                return Results.Ok(config.AsEnumerable());
            });
        }
    }
}
```

## Configuration in Program.cs

### Enable Specific Extensions

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    options.EnabledExtensions.Add("database-backup");
    options.EnabledExtensions.Add("swagger-docs");
});
```

### Disable Specific Extensions

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    options.DisabledExtensions.Add("email-notification");
});
```

### Conditional Loading Based on Environment

```csharp
builder.Services.AddExtensions(builder.Configuration, options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.EnabledExtensions.Add("swagger-docs");
        options.EnabledExtensions.Add("dev-tools");
    }
    else
    {
        options.EnabledExtensions.Add("database-backup");
        options.DisabledExtensions.Add("dev-tools");
    }
});
```

## Testing Your Extensions

### 1. Check Logs

When the application starts, check the logs for extension loading:

```
info: PicoPlus.Infrastructure.Extensions.ExtensionManager[0]
      Starting extension discovery...
info: PicoPlus.Infrastructure.Extensions.ExtensionManager[0]
      Found 5 extension type(s)
info: PicoPlus.Infrastructure.Extensions.ExtensionManager[0]
      Loaded extension: Health Check Extension (v1.0.0)
info: PicoPlus.Infrastructure.Extensions.ExtensionManager[0]
      Loaded extension: Extension Info API (v1.0.0)
```

### 2. Test Extension Endpoints

If your extension adds endpoints, test them:

```bash
# Test health check extension
curl http://localhost:5000/health

# Test extension info API
curl http://localhost:5000/api/extensions
```

### 3. Verify Services

Inject and use services registered by extensions:

```csharp
public class MyService
{
    private readonly IEmailService _emailService;  // From EmailNotificationExtension
    
    public MyService(IEmailService emailService)
    {
        _emailService = emailService;
    }
}
```

## Best Practices

1. **Keep extensions focused** - One extension = one feature
2. **Use configuration** - Make extensions configurable
3. **Handle errors gracefully** - Don't break the app if extension fails
4. **Log important events** - Use ILogger for debugging
5. **Document dependencies** - List required extensions
6. **Version properly** - Use semantic versioning
7. **Test thoroughly** - Ensure extension works in isolation

## Common Patterns

### Configuration Pattern

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    var settings = configuration.GetSection($"{Metadata.Id}:Settings").Get<MySettings>();
    
    if (settings?.Enabled == true)
    {
        services.AddSingleton(settings);
        services.AddScoped<IMyService, MyService>();
    }
}
```

### Service Factory Pattern

```csharp
public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddSingleton<IMyServiceFactory>(sp =>
    {
        return new MyServiceFactory(
            sp.GetRequiredService<ILogger<MyServiceFactory>>(),
            sp.GetRequiredService<IConfiguration>()
        );
    });
}
```

### Middleware Pattern

```csharp
public override void ConfigureApplication(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        // Pre-processing
        await next();
        // Post-processing
    });
}
```
