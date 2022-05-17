#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["WebApps/SnWebApplication.Api.InMem.TokenAuth/SnWebApplication.Api.InMem.TokenAuth.csproj", "WebApps/SnWebApplication.Api.InMem.TokenAuth/"]
COPY ["OData/SenseNet.OData.csproj", "OData/"]
COPY ["ContentRepository/SenseNet.ContentRepository.csproj", "ContentRepository/"]
COPY ["Storage/SenseNet.Storage.csproj", "Storage/"]
COPY ["Common/SenseNet.Common.csproj", "Common/"]
COPY ["Abstractions/SenseNet.Abstractions.csproj", "Abstractions/"]
COPY ["Search/SenseNet.Search.csproj", "Search/"]
COPY ["BlobStorage/SenseNet.BlobStorage.csproj", "BlobStorage/"]
COPY ["Services.Core/SenseNet.Services.Core.csproj", "Services.Core/"]
COPY ["WebHooks/SenseNet.WebHooks.csproj", "WebHooks/"]
COPY ["WebHooks.Common/SenseNet.WebHooks.Common.csproj", "WebHooks.Common/"]
COPY ["ContentRepository.InMemory/SenseNet.ContentRepository.InMemory.csproj", "ContentRepository.InMemory/"]
COPY ["Services.Wopi/SenseNet.Services.Wopi.csproj", "Services.Wopi/"]
RUN dotnet restore "WebApps/SnWebApplication.Api.InMem.TokenAuth/SnWebApplication.Api.InMem.TokenAuth.csproj"
COPY . .
WORKDIR "/src/WebApps/SnWebApplication.Api.InMem.TokenAuth"
RUN dotnet build "SnWebApplication.Api.InMem.TokenAuth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SnWebApplication.Api.InMem.TokenAuth.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SnWebApplication.Api.InMem.TokenAuth.dll"]