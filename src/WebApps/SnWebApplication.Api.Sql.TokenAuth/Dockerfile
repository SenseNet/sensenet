#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/WebApps/SnWebApplication.Api.Sql.TokenAuth"
RUN dotnet restore "SnWebApplication.Api.Sql.TokenAuth.csproj"
RUN dotnet build "SnWebApplication.Api.Sql.TokenAuth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnWebApplication.Api.Sql.TokenAuth.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnWebApplication.Api.Sql.TokenAuth.dll"]