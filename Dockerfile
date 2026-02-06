FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1) Copie apenas os .csproj para maximizar o cache do restore
COPY users-api/users-api.csproj users-api/
COPY contracts/Contracts.csproj contracts/

# 2) Restore baseado nos .csproj
RUN dotnet restore users-api/users-api.csproj

# 3) Copie os fontes completos
COPY users-api/ users-api/
COPY contracts/ contracts/

# 4) Build
WORKDIR /src/users-api
RUN dotnet build users-api.csproj -c $BUILD_CONFIGURATION -o /app/build

# ===== Publish =====
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/users-api
RUN dotnet publish users-api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===== Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "users-api.dll"]