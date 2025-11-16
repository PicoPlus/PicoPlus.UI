# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["PicoPlus.csproj", "./"]
RUN dotnet restore "PicoPlus.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "PicoPlus.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "PicoPlus.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install required dependencies for Iranian networks
RUN apt-get update && apt-get install -y \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create data directory for persistent storage
RUN mkdir -p /app/data

# Expose port
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Run the application
ENTRYPOINT ["dotnet", "PicoPlus.dll"]
