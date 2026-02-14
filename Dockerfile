# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /app
COPY ["DockerLogStoreAgent.csproj", "."]
RUN dotnet restore "./LocalCache/LocalCache.csproj"
COPY . .
RUN dotnet publish "./LocalCache/LocalCache.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
WORKDIR "/app/publish"
ENTRYPOINT ["dotnet", "DockerLogStoreAgent.dll"]
