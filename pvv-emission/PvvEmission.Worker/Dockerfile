# pvv-emission Worker image
# Build context must be the solution root (pvv-emission/):
#   docker build -f PvvEmission.Worker/Dockerfile -t pvv-emission .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (copy csproj files first to leverage layer caching).
COPY PvvEmission.sln .
COPY PvvEmission.Worker/PvvEmission.Worker.csproj PvvEmission.Worker/
COPY PvvEmission.Application/PvvEmission.Application.csproj PvvEmission.Application/
COPY PvvEmission.Domain/PvvEmission.Domain.csproj PvvEmission.Domain/
COPY PvvEmission.Infrastructure/PvvEmission.Infrastructure.csproj PvvEmission.Infrastructure/
RUN dotnet restore PvvEmission.Worker/PvvEmission.Worker.csproj

# Build and publish.
COPY . .
RUN dotnet publish PvvEmission.Worker/PvvEmission.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "PvvEmission.Worker.dll"]
