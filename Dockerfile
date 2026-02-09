FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Comercio.NET.Mobile.Server/Comercio.NET.Mobile.Server.csproj", "Comercio.NET.Mobile.Server/"]
RUN dotnet restore "Comercio.NET.Mobile.Server/Comercio.NET.Mobile.Server.csproj"
COPY . .
WORKDIR "/src/Comercio.NET.Mobile.Server"
RUN dotnet build "Comercio.NET.Mobile.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Comercio.NET.Mobile.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Asegurar que wwwroot se copió correctamente
COPY --from=publish /app/publish/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "Comercio.NET.Mobile.Server.dll"]