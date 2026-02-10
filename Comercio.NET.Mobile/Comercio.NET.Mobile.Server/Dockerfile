FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar el .csproj desde la ruta correcta (contexto desde raíz del repo)
COPY ["Comercio.NET.Mobile/Comercio.NET.Mobile.Server/Comercio.NET.Mobile.Server.csproj", "Comercio.NET.Mobile/Comercio.NET.Mobile.Server/"]
RUN dotnet restore "Comercio.NET.Mobile/Comercio.NET.Mobile.Server/Comercio.NET.Mobile.Server.csproj"

# Copiar el resto de archivos del proyecto
COPY ["Comercio.NET.Mobile/Comercio.NET.Mobile.Server/", "Comercio.NET.Mobile/Comercio.NET.Mobile.Server/"]
WORKDIR "/src/Comercio.NET.Mobile/Comercio.NET.Mobile.Server"
RUN dotnet build "Comercio.NET.Mobile.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Comercio.NET.Mobile.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Comercio.NET.Mobile.Server.dll"]