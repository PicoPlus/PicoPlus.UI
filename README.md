# PicoPlus.UI

**PicoPlus.UI** is a robust, Blazor-based web application serving as the user interface layer for the PicoPlus ecosystem. Built in C#, HTML, and CSS, it leverages modern design principles, best-in-class UX, and optimized performance for deployment on platforms like Liara.

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Deployment](#deployment)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Credits](#credits)
- [License](#license)

---

## Features

- **Blazor Server Architecture**: Interactive, real-time web UI with ASP.NET Core.
- **User & Admin Panels**: Clean separation of responsibilities, state management for both admin and user.
- **Authentication**: Role-based authentication and OTP support.
- **Notifications**: Integrated toast and dialog notification services.
- **Optimized for Iranian Networks**: Includes custom DNS handlers and local caching for reliability.
- **Modern UI**: RTL-compatible layouts, Bootstrap 5.3, and custom theming.
- **Persistent Data Storage**: Uses `/app/data` directory for storing user data and configuration.
- **Environment Driven**: Supports `.env` and environment variable-based configuration.
- **Extensible Services**: Easily extendable with new ViewModels and Services for business logic.

---

## Installation

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Docker](https://www.docker.com/get-started) (for deployment/testing)
- (Optional) [Liara CLI](https://cli.liara.ir/)
- Node.js for modern frontend asset workflows (if applicable)

### Clone the Repository

```bash
git clone https://github.com/PicoPlus/PicoPlus.UI.git
cd PicoPlus.UI
```

---

## Usage

### Local Development

1. Create a `.env` file in the project root:
   ```
   HUBSPOT_TOKEN=your_hubspot_token
   ZIBAL_TOKEN=your_zibal_token
   ```
2. Build and Run:
   ```bash
   dotnet build -c Release
   dotnet run
   ```
3. Open your browser at [http://localhost:5000](http://localhost:5000)

### Docker Compose

```bash
docker-compose up --build
```

### Docker Manual Build

```bash
docker build -t picoplus:test .
docker run -p 5000:5000 \
  -e HUBSPOT_TOKEN=your_token \
  -e ZIBAL_TOKEN=your_token \
  picoplus:test
```

---

## Deployment (Liara Example)

1. Install [Liara CLI](https://cli.liara.ir/)
2. Login:
   ```bash
   liara login
   ```
3. Configure environment variables in Liara dashboard:
   ```
   HUBSPOT_TOKEN
   ZIBAL_TOKEN
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://+:5000
   ```
4. Deploy:
   ```bash
   liara deploy
   ```
5. Monitor logs:
   ```bash
   liara logs --app ipicoplus
   ```

Full deployment guide is in [LIARA_DEPLOYMENT_GUIDE.md](LIARA_DEPLOYMENT_GUIDE.md).

---

## Configuration

- All configuration can be set via `.env`, `appsettings.json`, or environment variables.
- Sensitive credentials (API tokens, etc.) should never be committed; set them via the platform dashboard or `.env`.

---

## Troubleshooting

- **Port Binding Errors**: Liara automatically maps external ports 80/443 to internal 5000.
- **Environment Variables**: Ensure they are set in the Liara dashboard, not just your local `.env`.
- **Build Issues**: Run:
   ```bash
   dotnet clean
   dotnet build -c Release
   ```
- **Network Issues**: Custom DNS handler is provided for Iranian networks.
- **SignalR Issues**: Blazor Server is configured for optimal connections.

---

## Contributing

Contributions welcome! Please fork this repo and submit a pull request.
- All code should be idiomatic C# and follow project conventions.
- Discuss large changes with maintainers in advance.
- Add relevant tests and documentation.

---

## Credits

Developed and maintained by [PicoPlus](https://github.com/PicoPlus).

---

## License

**No specific license detected. Please add a LICENSE.md file.**
This project is currently proprietary unless otherwise noted.

---
