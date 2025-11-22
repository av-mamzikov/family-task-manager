# Multi-stage build for .NET 9.0 Family Task Manager
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy global configuration files first for better Docker layer caching
COPY ["global.json", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["nuget.config", "."]

# Copy all project files maintaining folder structure
COPY ["src/FamilyTaskManager.Core/FamilyTaskManager.Core.csproj", "src/FamilyTaskManager.Core/"]
COPY ["src/FamilyTaskManager.UseCases/FamilyTaskManager.UseCases.csproj", "src/FamilyTaskManager.UseCases/"]
COPY ["src/FamilyTaskManager.Infrastructure/FamilyTaskManager.Infrastructure.csproj", "src/FamilyTaskManager.Infrastructure/"]
COPY ["src/FamilyTaskManager.ServiceDefaults/FamilyTaskManager.ServiceDefaults.csproj", "src/FamilyTaskManager.ServiceDefaults/"]
COPY ["src/FamilyTaskManager.Host/FamilyTaskManager.Host.csproj", "src/FamilyTaskManager.Host/"]

# Restore dependencies
RUN dotnet restore "src/FamilyTaskManager.Host/FamilyTaskManager.Host.csproj"

# Copy all source code
COPY . .

# Build and publish the application
WORKDIR "/src/src/FamilyTaskManager.Host"
RUN dotnet publish "FamilyTaskManager.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV TZ=UTC

# Health check (if you have health endpoints, uncomment and modify)
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#   CMD curl -f http://localhost:8080/health || exit 1

# Expose port (if your worker service has HTTP endpoints)
# EXPOSE 8080

ENTRYPOINT ["dotnet", "FamilyTaskManager.Host.dll"]
