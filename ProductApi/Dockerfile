# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy NuGet configuration
COPY NuGet.Config .

# Copy csproj files first to leverage Docker layer caching for restore
COPY src/ProductApi.Domain/ProductApi.Domain.csproj src/ProductApi.Domain/
COPY src/ProductApi.Application/ProductApi.Application.csproj src/ProductApi.Application/
COPY src/ProductApi.Infrastructure/ProductApi.Infrastructure.csproj src/ProductApi.Infrastructure/
COPY src/ProductApi.API/ProductApi.API.csproj src/ProductApi.API/

# Restore using Linux compatible NuGet config
RUN dotnet restore src/ProductApi.API/ProductApi.API.csproj --configfile NuGet.Config

# Copy everything else and build
COPY src/ src/

RUN dotnet publish src/ProductApi.API/ProductApi.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Run as a non-root user
RUN useradd -m appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ProductApi.API.dll"]