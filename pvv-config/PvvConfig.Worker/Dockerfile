# pvv-config CacheSyncWorker image
# Build context must be the solution root (pvv-config/):
#   docker build -f PvvConfig.Worker/Dockerfile -t pvv-config-worker .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (copy csproj files first to leverage layer caching).
COPY PvvConfig.sln .
COPY PvvConfig.API/PvvConfig.API.csproj PvvConfig.API/
COPY PvvConfig.Application/PvvConfig.Application.csproj PvvConfig.Application/
COPY PvvConfig.Domain/PvvConfig.Domain.csproj PvvConfig.Domain/
COPY PvvConfig.Infrastructure/PvvConfig.Infrastructure.csproj PvvConfig.Infrastructure/
COPY PvvConfig.Worker/PvvConfig.Worker.csproj PvvConfig.Worker/
RUN dotnet restore PvvConfig.Worker/PvvConfig.Worker.csproj

# Build and publish.
COPY . .
RUN dotnet publish PvvConfig.Worker/PvvConfig.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PvvConfig.Worker.dll"]
