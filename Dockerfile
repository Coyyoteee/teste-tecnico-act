FROM node:24-bookworm-slim AS node-tools

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
COPY --from=node-tools /usr/local/ /usr/local/

WORKDIR /src
COPY NuGet.Config ./
COPY src/Backend/Challenge.Api.csproj src/Backend/
RUN dotnet restore src/Backend/Challenge.Api.csproj --configfile NuGet.Config

COPY . .
RUN dotnet publish src/Backend/Challenge.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__FilePath=/app/data/movements.json

COPY --from=publish /app/publish .

RUN mkdir -p /app/data && chown "$APP_UID:$APP_UID" /app/data

USER $APP_UID
EXPOSE 8080
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "Challenge.Api.dll"]
