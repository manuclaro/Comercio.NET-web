FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar solo el .csproj específico del proyecto
COPY ["Comercio.NET.Mobile.Server.csproj", "./"]
RUN dotnet restore "Comercio.NET.Mobile.Server.csproj"

# Copiar el resto de archivos
COPY . .
RUN dotnet build "Comercio.NET.Mobile.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Comercio.NET.Mobile.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Comercio.NET.Mobile.Server.dll"]