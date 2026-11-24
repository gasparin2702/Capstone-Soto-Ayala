# ETAPA 1: BUILD
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Copiar sólo los .csproj (cache de restore)
COPY ["Sitrac.Servidor/Sitrac.Servidor.csproj",           "Sitrac.Servidor/"]
COPY ["Sitrac.Servicio/Sitrac.Servicio.csproj",         "Sitrac.Servicio/"]
COPY ["Sitrac.Repositorio/Sitrac.Repositorio.csproj",   "Sitrac.Repositorio/"]
COPY ["Sitrac.AccesoDatos/Sitrac.AccesoDatos.csproj",   "Sitrac.AccesoDatos/"]
COPY ["Sitrac.MigracionBd/Sitrac.MigracionBd.csproj", "Sitrac.MigracionBd/"]
RUN dotnet restore "Sitrac.Servidor/Sitrac.Servidor.csproj"

# 2) Copiar el resto del código y publicar
COPY . .
WORKDIR "/src/Sitrac.Servidor"
RUN dotnet publish "Sitrac.Servidor.csproj" -c Release -o /app/publish --no-restore

# ETAPA 2: RUNTIME

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Puerto por defecto (el compose también lo pasa por env var)
ENV ASPNETCORE_URLS=http://+:8080

# 3) Binarios publicados (incluye appsettings*.json, Microsoft Graph, Evolve, etc.)
COPY --from=build /app/publish .

# 4) Scripts de Evolve
#    Program.cs usa: Locations = { "../Sitrac.MigracionBd/Scripts" }
#    Con WORKDIR /app  →  resuelve a  /Sitrac.MigracionBd/Scripts
COPY --from=build /src/Sitrac.MigracionBd/Scripts /Sitrac.MigracionBd/Scripts
RUN chmod -R a+rX /Sitrac.MigracionBd/Scripts

# 5) Usuario no-root (incluido desde .NET 8)
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "Sitrac.Servidor.dll"]