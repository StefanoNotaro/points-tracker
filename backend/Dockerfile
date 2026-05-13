# syntax=docker/dockerfile:1.7

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Restore first for better layer caching.
COPY ["src/PointsTracker.Domain/PointsTracker.Domain.csproj",                 "src/PointsTracker.Domain/"]
COPY ["src/PointsTracker.Application/PointsTracker.Application.csproj",       "src/PointsTracker.Application/"]
COPY ["src/PointsTracker.Infrastructure/PointsTracker.Infrastructure.csproj", "src/PointsTracker.Infrastructure/"]
COPY ["src/PointsTracker.Api/PointsTracker.Api.csproj",                       "src/PointsTracker.Api/"]
RUN dotnet restore "src/PointsTracker.Api/PointsTracker.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/PointsTracker.Api/PointsTracker.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# The official aspnet:9.0-alpine image ships a non-root user named "app"
# (UID 1654). Use it directly — recreating it breaks the build.
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "PointsTracker.Api.dll"]
