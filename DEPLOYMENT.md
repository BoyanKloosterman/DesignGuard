# DesignGuard v5 — deployment

## Overzicht

DesignGuard is een **WPF-desktopapp** die projectdata in **MongoDB** opslaat. De app zelf draait op Windows; MongoDB kan lokaal, op een server, in Docker of bij Atlas draaien.

## Vereisten

- Windows-client met .NET 10 runtime (of SDK om te bouwen).
- Bereikbare MongoDB 4.4+ (getest met standaard driver 3.x).

## Omgeving instellen

Zie [CONFIGURATION.md](CONFIGURATION.md). Minimaal:

```text
DESIGNGUARD_MONGODB_CONNECTION_STRING=mongodb://host:27017
DESIGNGUARD_MONGODB_DATABASE=designguard
DESIGNGUARD_MONGODB_APPNAME=DesignGuard
DESIGNGUARD_ENVIRONMENT=Production
```

Gebruik voor Atlas een `mongodb+srv://`-string en zet gebruikersnaam/wachtwoord via **secrets** (niet in git).

## Docker — MongoDB alleen (voorbeeld)

```yaml
services:
  mongo:
    image: mongo:7
    ports:
      - "27017:27017"
    volumes:
      - mongo_data:/data/db

volumes:
  mongo_data: {}
```

Connection string voor de app op dezelfde machine:

```text
DESIGNGUARD_MONGODB_CONNECTION_STRING=mongodb://localhost:27017
```

## Server wisselen

1. Wijzig alleen de omgevingsvariabelen (of het `.env`-bestand in development).
2. Herstart de app.
3. Gebruik **Instellingen → Verbinding testen (ping)**.

Geen code-aanpassingen nodig.

## Migratie van oude SQLite-data

1. Zorg dat MongoDB-configuratie werkt (ping OK).
2. In de app: **Instellingen → Importeer SQLite → MongoDB** en kies `designguard-v3.db` (of ander back-upbestand).
3. Controleer de projectlijst en open een project.

**Wel gemigreerd:** alle projecten en bijbehorende entiteiten zoals opgeslagen in de SQLite-database.

**Niet gemigreerd via deze import:** `user-settings.json`, knowledge pack JSON op schijf (blijven lokaal).

## Back-up

- **MongoDB:** gebruik `mongodump` / Atlas backups / je eigen DB-backupbeleid.
- **Exports:** Markdown/HTML/PDF/JSON blijven werken zolang het project geladen kan worden; bij DB-storing kun je eerst connectivity herstellen.

## Secrets

- Sla connection strings op in een secret store, CI-variabelen of OS-omgeving.
- Commit geen `.env` met echte waarden; gebruik `.env.example` als sjabloon.
