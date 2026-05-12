FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["src/PointsTracker.Domain/PointsTracker.Domain.csproj", "src/PointsTracker.Domain/"]
COPY ["src/PointsTracker.Application/PointsTracker.Application.csproj", "src/PointsTracker.Application/"]
COPY ["src/PointsTracker.Infrastructure/PointsTracker.Infrastructure.csproj", "src/PointsTracker.Infrastructure/"]
COPY ["src/PointsTracker.Api/PointsTracker.Api.csproj", "src/PointsTracker.Api/"]
RUN dotnet restore "src/PointsTracker.Api/PointsTracker.Api.csproj"

COPY . .
RUN dotnet publish "src/PointsTracker.Api/PointsTracker.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "PointsTracker.Api.dll"]
