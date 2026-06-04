# pvv-bff API image
# Build context must be the solution root (pvv-bff/):
#   docker build -f PvvBff.API/Dockerfile -t pvv-bff .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore (copy csproj files first to leverage layer caching).
COPY PvvBff.sln .
COPY PvvBff.API/PvvBff.API.csproj PvvBff.API/
COPY PvvBff.Application/PvvBff.Application.csproj PvvBff.Application/
COPY PvvBff.Domain/PvvBff.Domain.csproj PvvBff.Domain/
COPY PvvBff.Infrastructure/PvvBff.Infrastructure.csproj PvvBff.Infrastructure/
RUN dotnet restore PvvBff.API/PvvBff.API.csproj

# Build and publish.
COPY . .
RUN dotnet publish PvvBff.API/PvvBff.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "PvvBff.API.dll"]
