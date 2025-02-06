#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/WebApps/SnWebApplication.Api.Sql.TokenAuth.Preview"
RUN dotnet restore "SnWebApplication.Api.Sql.TokenAuth.Preview.csproj"
RUN dotnet build "SnWebApplication.Api.Sql.TokenAuth.Preview.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnWebApplication.Api.Sql.TokenAuth.Preview.csproj" -c Release -o /app/publish

FROM base AS final
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnWebApplication.Api.Sql.TokenAuth.Preview.dll"]