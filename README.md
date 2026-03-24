# PMTool_API

ASP.NET Core Backend fuer das PM-Tool.

## Stack

- ASP.NET Core 8
- Entity Framework Core
- SQL Server / SQLite / PostgreSQL Support
- JWT Auth
- Swagger

## Lokal starten

1. Restore:

```bash
dotnet restore
```

2. Starten:

```bash
dotnet run
```

Die lokale Standardkonfiguration nutzt SQLite ueber [appsettings.json](c:/Users/AtesogluBerk-Can/Desktop/realcore_pm/backend/PmTool.Api/appsettings.json).

## Production

Die Production-Konfiguration liegt in [appsettings.Production.json](c:/Users/AtesogluBerk-Can/Desktop/realcore_pm/backend/PmTool.Api/appsettings.Production.json).

Vor Deployment muessen dort echte Werte gesetzt werden:

```json
{
  "DatabaseProvider": "sqlserver",
  "ConnectionStrings": {
    "Default": "Server=YOUR_SQL_HOST,1433;Database=YOUR_DB_NAME;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=False;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "YOUR_LONG_PRODUCTION_JWT_SECRET"
  }
}
```

## MonsterASP Deployment

1. Release bauen:

```bash
dotnet publish -c Release -o ./publish
```

2. Dateien aus dem Publish-Ordner nach MonsterASP `wwwroot` hochladen
3. sicherstellen, dass `ASPNETCORE_ENVIRONMENT=Production` aktiv ist
4. `https://projektmanagement.runasp.net/health` pruefen
5. `https://projektmanagement.runasp.net/swagger` pruefen

## API Basis-URL

```text
https://projektmanagement.runasp.net/api/v1
```

## Wichtige Endpunkte

- `POST /api/v1/auth/login`
- `GET /api/v1/projects/portfolio`
- `GET /api/v1/projects/{id}`
- `GET /api/v1/projects/{id}/team`
- `GET /api/v1/projects/{id}/notes`
- `GET /api/v1/projects/{id}/lead-tasks`

## Demo-Login

- E-Mail: `berkcan@realcore.de`
- Passwort: `demo1234`

