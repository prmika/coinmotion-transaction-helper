FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY backend/ backend/
RUN dotnet restore backend/CryptoTaxHelper.sln
RUN dotnet publish backend/src/CryptoTaxHelper.Api/CryptoTaxHelper.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=15s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail --silent --output /dev/null http://127.0.0.1:8080/healthz || exit 1
ENTRYPOINT ["dotnet", "CryptoTaxHelper.Api.dll"]
