FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5230

ENV ASPNETCORE_URLS=http://+:5230

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
ENTRYPOINT ["dotnet", "api-gateway.dll"]
