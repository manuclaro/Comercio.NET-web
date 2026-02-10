FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el .csproj primero (mejor caché)
COPY ["Comercio.NET.Mobile.Server.csproj", "./"]
RUN dotnet restore "Comercio.NET.Mobile.Server.csproj"

# Copiar el resto de archivos
COPY . .

# Build especificando el .csproj
RUN dotnet build "Comercio.NET.Mobile.Server.csproj" -c Release -o /app/build

FROM build AS publish
# Publish especificando el .csproj
RUN dotnet publish "Comercio.NET.Mobile.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .

# wwwroot ya está incluido en /app/publish, no necesita copia adicional
ENTRYPOINT ["dotnet", "Comercio.NET.Mobile.Server.dll"]