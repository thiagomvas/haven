# Stage 1: Build the React frontend
FROM node:22-alpine AS web-build
# Mirror the real directory structure so Vite's outDir (../Haven.Presentation.Api/wwwroot) resolves correctly
WORKDIR /src/Presentation/Haven.Web
COPY src/Presentation/Haven.Web/package*.json ./
RUN npm ci
COPY src/Presentation/Haven.Web/ ./
RUN npx vite build
# Output lands at /src/Presentation/Haven.Presentation.Api/wwwroot

# Stage 2: Build the .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

# Restore dependencies first (layer cache friendly)
COPY Haven.slnx Directory.Build.props Directory.Packages.props ./
COPY src/Haven.Domain/Haven.Domain.csproj src/Haven.Domain/
COPY src/Haven.Application/Haven.Application.csproj src/Haven.Application/
COPY src/Haven.Infrastructure/Haven.Infrastructure.csproj src/Haven.Infrastructure/
COPY src/Presentation/Haven.Presentation.Api/Haven.Presentation.Api.csproj src/Presentation/Haven.Presentation.Api/
RUN dotnet restore src/Presentation/Haven.Presentation.Api/Haven.Presentation.Api.csproj

# Copy source then layer in the built frontend
COPY src/ src/
COPY --from=web-build /src/Presentation/Haven.Presentation.Api/wwwroot src/Presentation/Haven.Presentation.Api/wwwroot/

RUN dotnet publish src/Presentation/Haven.Presentation.Api/Haven.Presentation.Api.csproj \
    -c Release \
    -o /publish \
    --no-restore

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=api-build /publish ./

# Mount a volume here to persist SQLite databases across restarts
VOLUME /data

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/data/haven.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "Haven.Presentation.Api.dll"]
