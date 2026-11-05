# ============================================================
# ETAPA 1: BUILD
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Copiar sólo los .csproj (cache de restore)
COPY ["Sitrac.Server/Sitrac.Server.csproj",          "Sitrac.Server/"]
COPY ["Sitrac.Service/Sitrac.Service.csproj",        "Sitrac.Service/"]
COPY ["Sitrac.Repository/Sitrac.Repository.csproj",  "Sitrac.Repository/"]
COPY ["Sitrac.DataAccess/Sitrac.DataAccess.csproj",  "Sitrac.DataAccess/"]
COPY ["Sitrac.DbMigration/Sitrac.DbMigration.csproj","Sitrac.DbMigration/"]
RUN dotnet restore "Sitrac.Server/Sitrac.Server.csproj"

# 2) Copiar el resto del código y publicar
COPY . .
WORKDIR "/src/Sitrac.Server"
RUN dotnet publish "Sitrac.Server.csproj" -c Release -o /app/publish --no-restore

# ============================================================
# ETAPA 2: RUNTIME
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# 3) Binarios publicados (incluye appsettings*.json, MailKit, etc.)
COPY --from=build /app/publish .

# 4) Scripts de Evolve
#    Program.cs usa: Locations = { "../Sitrac.DbMigration/Scripts" }
#    Con CWD = /app  →  resuelve a  /Sitrac.DbMigration/Scripts
#    OJO: "dotnet publish" NO los copia porque Sitrac.Server NO referencia
#    a Sitrac.DbMigration como proyecto; hay que copiarlos manualmente.
COPY --from=build /src/Sitrac.DbMigration/Scripts /Sitrac.DbMigration/Scripts
RUN chmod -R a+rX /Sitrac.DbMigration/Scripts

# 5) Usuario no-root
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "Sitrac.Server.dll"]