# PicoPlus.UI

PicoPlus.UI is a **Blazor Server** web application for the PicoPlus platform.
It provides user-facing and admin-facing workflows, integrates with external services (HubSpot, SMS providers, Zibal, Liara), and includes localized RTL-first UI support for Persian users.

---

## Tech Stack

- **.NET 9 / ASP.NET Core Blazor Server**
- **C# 12**
- **Bootstrap 5 + custom CSS**
- **Blazored SessionStorage + LocalStorage**
- **ImageSharp** for image processing

---

## Architecture Direction

The project is being aligned toward a cleaner layered architecture.
Current code organization already separates major responsibilities into folders such as:

- `Components/` → reusable UI components, layouts, shared widgets, dialogs
- `Views/` and `Pages/` → route-level screens
- `Services/` → application/business/integration services (Auth, Admin, CRM, SMS, UserPanel, Utils)
- `Infrastructure/` → cross-cutting concerns (HTTP handlers, wrappers, state/auth infra)
- `State/` → app state models/services
- `ViewModels/` → UI view-models
- `Models/` → DTO/domain transfer objects

> Note: the repository contains transitional structures due to active migration/refactoring.

---

## Prerequisites

- **.NET SDK 9.0**
- Optional: **Docker**
- Optional: **Liara CLI** for deployment

---

## Getting Started

### 1) Clone

```bash
git clone https://github.com/PicoPlus/PicoPlus.UI.git
cd PicoPlus.UI
```

### 2) Configure environment

Create a `.env` file in the repository root (or provide equivalent environment variables):

```env
HUBSPOT_TOKEN=your_hubspot_token
ZIBAL_TOKEN=your_zibal_token
```

You can also use `appsettings.json` and user-secrets where applicable.

### 3) Run locally

```bash
dotnet restore
dotnet build
dotnet run
```

Default app URL in local/dev is typically `http://localhost:5000` (depending on launch profile).

---

## Configuration Notes

Configuration sources are loaded in this order pattern:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. User secrets
5. Optional `.env` file (when present)

Key operational variables:

- `HUBSPOT_TOKEN`
- `ZIBAL_TOKEN`
- `ASPNETCORE_ENVIRONMENT`

In production, Kestrel is configured to listen on port `5000`.

---

## Docker

### Build

```bash
docker build -t picoplus-ui:local .
```

### Run

```bash
docker run --rm -p 5000:5000 \
  -e HUBSPOT_TOKEN=your_hubspot_token \
  -e ZIBAL_TOKEN=your_zibal_token \
  -e ASPNETCORE_ENVIRONMENT=Production \
  picoplus-ui:local
```

Or use:

```bash
docker-compose up --build
```

---

## Deployment (Liara)

A full deployment walkthrough is available at:

- [`LIARA_DEPLOYMENT_GUIDE.md`](./LIARA_DEPLOYMENT_GUIDE.md)

Typical production env setup includes:

- `HUBSPOT_TOKEN`
- `ZIBAL_TOKEN`
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:5000`

---

## Troubleshooting

- If app startup fails, verify required environment variables are present.
- If service/API calls fail in production, verify token values and outbound network access.
- If static assets appear stale, clear browser cache and redeploy.
- If build fails locally, run:

```bash
dotnet clean
dotnet restore
dotnet build
```

---

## Contributing

- Keep changes scoped and consistent with current migration direction.
- Prefer small, reviewable PRs.
- Update docs when behavior or structure changes.

---

## License

No explicit open-source license file is currently included in this repository.
Assume all rights reserved unless maintainers publish a license.
