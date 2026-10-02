#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# ── Build the install-services-core.zip (normally done by CompressInstall.ps1 on Windows) ──
RUN apt-get update && apt-get install -y --no-install-recommends zip \
    && cd /src/nuget/snadmin \
    && mkdir -p install-services-core/import \
    && cp -r install-services/import/* install-services-core/import/ \
    && rm -rf install-services-core/import/'(apps)' \
              install-services-core/import/WebRoot \
              install-services-core/import/ErrorMessages \
              install-services-core/import/WebRoot.Content \
              install-services-core/import/ErrorMessages.Content \
              install-services-core/import/'(apps).Content' \
    && if [ -d install-services/importNetCore ]; then cp -r install-services/importNetCore/* install-services-core/import/ 2>/dev/null || true; fi \
    && cd install-services-core \
    && zip -r /src/Services.Core.Install/install-services-core.zip . \
    && echo "=== install-services-core.zip ===" && unzip -l /src/Services.Core.Install/install-services-core.zip | tail -1

WORKDIR "/src/WebApps/SnWebApplication.Api.Sql.TokenAuth"
RUN dotnet restore "SnWebApplication.Api.Sql.TokenAuth.csproj"
RUN dotnet build "SnWebApplication.Api.Sql.TokenAuth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnWebApplication.Api.Sql.TokenAuth.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnWebApplication.Api.Sql.TokenAuth.dll"]