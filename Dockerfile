# syntax=docker/dockerfile:1

# ---- Build the Angular SPA ----
FROM node:22-bookworm-slim AS client-build
WORKDIR /src/client
COPY client/package.json client/package-lock.json ./
RUN npm ci
COPY client/ ./
RUN npm run build -- --configuration production

# ---- Build and publish the ASP.NET Core app ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY RadafraApp/RadafraApp.csproj RadafraApp/
RUN dotnet restore RadafraApp/RadafraApp.csproj
COPY RadafraApp/ RadafraApp/
# Serve the compiled SPA from wwwroot (the MSBuild npm step is skipped in Docker)
COPY --from=client-build /src/client/dist/client/browser/ RadafraApp/wwwroot/
RUN dotnet publish RadafraApp/RadafraApp.csproj -c Release -o /app/publish \
    /p:UseAppHost=false /p:SkipSpaBuild=true

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api-build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "RadafraApp.dll"]
