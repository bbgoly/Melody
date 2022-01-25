FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Melody.csproj", "./"]
COPY ["NuGet.Config", "./"]
RUN dotnet restore
COPY . .
WORKDIR "/src/"
RUN dotnet build "Melody.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Melody.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY /bin/Debug/net5.0/config.json .
COPY ./Lavalink/application.yml .
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Melody.dll"]
