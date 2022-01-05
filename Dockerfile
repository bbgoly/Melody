FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Melody.csproj", "./"]
COPY ["NuGet.Config", "./"]
COPY ["config.json", "./"]
COPY ["application.yml", "./"]
RUN dotnet restore
COPY . .
WORKDIR "/src/"
RUN dotnet build "Melody.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Melody.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY config.json .
COPY application.yml .
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Melody.dll"]
