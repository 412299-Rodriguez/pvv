# pvv-soat API image
# Build context must be the solution root (pvv-soat/):
#   docker build -f PvvSoat.API/Dockerfile -t pvv-soat .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (copy csproj files first to leverage layer caching).
COPY PvvSoat.sln .
COPY PvvSoat.API/PvvSoat.API.csproj PvvSoat.API/
COPY PvvSoat.Application/PvvSoat.Application.csproj PvvSoat.Application/
COPY PvvSoat.Domain/PvvSoat.Domain.csproj PvvSoat.Domain/
COPY PvvSoat.Infrastructure/PvvSoat.Infrastructure.csproj PvvSoat.Infrastructure/
RUN dotnet restore PvvSoat.API/PvvSoat.API.csproj

# Build and publish.
COPY . .
RUN dotnet publish PvvSoat.API/PvvSoat.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "PvvSoat.API.dll"]
