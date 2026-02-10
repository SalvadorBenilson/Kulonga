FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5230

# Environment variables for connection and configuration
ENV ASPNETCORE_URLS=http://+:5230
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["api-gateway/api-gateway.csproj", "api-gateway/"]
RUN dotnet restore "api-gateway/api-gateway.csproj"
COPY . .
WORKDIR "/src/api-gateway"
RUN dotnet build "api-gateway.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "api-gateway.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5230/schools || exit 1

ENTRYPOINT ["dotnet", "api-gateway.dll"]
