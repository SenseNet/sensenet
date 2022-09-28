#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/WebApps/SnWebApplication.Api.InMem.TokenAuth"
RUN dotnet restore "SnWebApplication.Api.InMem.TokenAuth.csproj"
RUN dotnet build "SnWebApplication.Api.InMem.TokenAuth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnWebApplication.Api.InMem.TokenAuth.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnWebApplication.Api.InMem.TokenAuth.dll"]