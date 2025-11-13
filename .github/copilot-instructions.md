# Copilot Custom Instructions for PicoPlus.UI

## Project Overview

PicoPlus.UI is a Blazor Server application built with .NET 8.0 that provides a CRM (Customer Relationship Management) interface. The application uses MudBlazor as the primary UI component library and integrates with HubSpot CRM APIs for managing contacts, deals, pipelines, and commerce operations.

**Key Technologies:**
- .NET 8.0 with Blazor Server (Interactive Server Components)
- MudBlazor 8.9.0 for UI components
- Blazored.SessionStorage for client-side storage
- RestSharp for HTTP API calls
- HubSpot CRM integration
- Telegram.Bot integration
- Zibal payment gateway integration

## Project Structure

- **`/Components/`** - Blazor components including App.razor, Layout, Pages, and Routes
- **`/Views/`** - Feature-specific Razor components (Deal, User, auth, Invoice)
- **`/Services/`** - Business logic and API integration layers
  - `CRM/` - HubSpot CRM integrations (Contacts, Deals, Pipelines, Commerce)
  - `SMS/` - SMS service integration
  - `Identity/` - Payment gateway (Zibal) integration
  - `Utils/` - Utility services
- **`/Models/`** - Data Transfer Objects (DTOs) for CRM and Services
- **`/wwwroot/`** - Static files (CSS, JS, images)
- **`/Properties/`** - Project properties
- **`.github/workflows/`** - CI/CD pipeline for deployment to Liara

## Build and Test Guidelines

### Building the Project

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Publish for deployment
dotnet publish -c Release -o out
```

### Running Locally

```bash
dotnet run
```

The application will start on the configured ports (HTTPS/HTTP).

### Deployment

The project uses GitHub Actions for automated deployment to Liara platform:
- Deployment triggers on push to `master` branch
- The workflow builds, publishes, and deploys to Liara using the Liara CLI
- Deployment configuration is defined in `.github/workflows/dotnet.yml` and `liara.json`

## Coding Standards and Principles

### General Guidelines

1. **Nullable Reference Types**: The project has nullable reference types enabled. Always handle potential null values appropriately.

2. **Dependency Injection**: All services should be registered in `Program.cs` and injected via constructor injection in Razor components.

3. **Component Structure**: 
   - Use Blazor's component lifecycle methods appropriately (`OnInitializedAsync`, `OnParametersSet`, etc.)
   - Keep component logic focused and single-purpose
   - Extract reusable UI patterns into separate components

4. **API Integration**:
   - All external API calls should be made through service classes in `/Services/`
   - Use RestSharp for HTTP communications
   - Implement proper error handling for API calls
   - Use DTOs defined in `/Models/` for API request/response handling

5. **Session Management**:
   - Use `Blazored.SessionStorage` for client-side state management
   - Store user authentication state and session data appropriately

### MudBlazor Component Usage

- Follow MudBlazor's naming conventions and component patterns
- Use MudBlazor's built-in validation and form handling
- Leverage MudBlazor's theming capabilities (MudBlazor.ThemeManager is included)
- Be aware of Right-to-Left (RTL) support requirements in the layout

### Code Style

- Follow standard C# naming conventions (PascalCase for classes/methods, camelCase for local variables)
- Use implicit typing (`var`) where type is obvious
- Keep methods focused and reasonably short
- Add XML documentation comments for public APIs and complex logic
- Organize using statements and remove unused ones

## Special Considerations

1. **Existing Build Issues**: There is a pre-existing build error in `Views/auth/Login.razor` (line 138) related to type conversion. Do not attempt to fix this unless explicitly asked, as it may be work-in-progress.

2. **Warnings**: The project has numerous nullable reference warnings and MudBlazor attribute warnings. These are known issues and should not be addressed unless they are directly related to new code changes.

3. **Configuration Secrets**: 
   - Never commit actual API tokens or secrets to the repository
   - HubSpot token in `appsettings.json` appears to be a development token
   - Use environment variables or secure configuration for production secrets

4. **Localization**: The application appears to support Persian/Farsi language (comments in Persian in workflow file). Be mindful of RTL layout requirements.

5. **Third-Party Integrations**:
   - HubSpot CRM is the primary data source
   - Zibal is used for payment processing
   - SMS services are integrated for notifications
   - Telegram bot integration is available

## File Patterns to Avoid Modifying

- Do not modify files in `.github/agents/` - these contain instructions for other agents
- Avoid changing deployment configuration (`liara.json`, `.github/workflows/dotnet.yml`) unless specifically requested
- Don't alter the main application entry point (`Program.cs`) without good reason

## Testing

Currently, the project does not have a dedicated test project. When adding new features:
- Manually test all UI interactions
- Verify API integrations work correctly
- Test error handling paths
- Ensure responsive design works properly with MudBlazor components

## Dependencies Management

- Use `dotnet add package` to add new NuGet packages
- Keep package versions compatible with .NET 8.0
- Prefer stable package versions over pre-release versions
- Document why new dependencies are added

## Common Tasks

### Adding a New Service
1. Create service class in appropriate `/Services/` subdirectory
2. Register service in `Program.cs` using `builder.Services.AddScoped<>()` or appropriate lifetime
3. Create corresponding DTO models in `/Models/`
4. Inject and use the service in Razor components

### Adding a New Page/View
1. Create `.razor` file in `/Views/` or `/Components/Pages/`
2. Use MudBlazor components for UI
3. Add route using `@page` directive
4. Implement proper error handling and loading states
5. Follow existing component patterns in the project

### Working with HubSpot CRM
- Use existing service classes in `/Services/CRM/`
- Follow the patterns established in `Objects/Contact.cs`, `Objects/Deal.cs`, etc.
- Use proper DTOs from `/Models/CRM/`
- Handle API errors gracefully

## Questions or Clarifications

If you're unsure about:
- Business logic requirements - ask for clarification
- Integration details - refer to existing service implementations
- UI/UX decisions - follow MudBlazor best practices and existing patterns
- Persian/RTL layout requirements - consult with the team
