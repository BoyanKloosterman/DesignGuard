# DesignGuard — configuratie (v5)

## Principes

- **Geen secrets in broncode of git.** Connection strings en wachtwoorden alleen via omgeving of lokaal `.env` (zie hieronder).
- **Omgevingsvariabelen zijn leidend.** Een optioneel `.env`-bestand is alleen voor ontwikkeling en wordt niet in productie verwacht.
- **Geen volledige connection strings loggen of tonen.** De app toont een **gemaskeerde** weergave op het tabblad Instellingen.

## Verplichte variabelen (MongoDB)

| Variabele | Beschrijving |
|-----------|--------------|
| `DESIGNGUARD_MONGODB_CONNECTION_STRING` | MongoDB connection string (bijv. `mongodb://host:27017` of Atlas `mongodb+srv://...`). |
| `DESIGNGUARD_MONGODB_DATABASE` | Databasenaam (bijv. `designguard`). |

## Aanbevolen

| Variabele | Beschrijving |
|-----------|--------------|
| `DESIGNGUARD_MONGODB_APPNAME` | `ApplicationName` voor de driver (monitoring / logs op de server). |
| `DESIGNGUARD_ENVIRONMENT` | Bijv. `Development` of `Production`. |

## Optioneel

| Variabele | Beschrijving |
|-----------|--------------|
| `DESIGNGUARD_MONGODB_TIMEOUT_SECONDS` | Server selection / connect / socket timeout (seconden). |
| `DESIGNGUARD_MONGODB_TLS` | `true` / `1` om TLS aan te zetten op clientniveau (aanvullend op de string). |
| `DESIGNGUARD_MONGODB_READ_PREFERENCE` | `primary`, `primaryPreferred`, `secondary`, `secondaryPreferred`, `nearest`. |

## `.env` (alleen development)

1. Kopieer `.env.example` naar `.env`. De app zoekt (in volgorde) omhoog vanaf de map van de executable tot ca. 10 niveaus, en daarna in de huidige werkmap — een `.env` in de **repository-root** (`DesignGuard/.env`) wordt daardoor ook gevonden tijdens F5-debug.
2. Het bestand wordt **alleen** gelezen als:
   - `DESIGNGUARD_ENVIRONMENT=Development` in het `.env`-bestand staat, **of**
   - `DESIGNGUARD_LOAD_DOTENV=1` in de **process**-omgeving staat vóór start.
3. Regels in `.env` overschrijven **niet** variabelen die al op de machine gezet zijn.

**Productie:** gebruik systeem- of container-omgeving; commit geen `.env` met echte secrets (staat in `.gitignore`).

## Lokale voorkeuren (niet in MongoDB)

- `user-settings.json` onder `%LOCALAPPDATA%\DesignGuard\` (o.a. uitgeschakelde knowledge packs, exportmap, thema).
- Knowledge pack **bestanden** blijven onder `KnowledgePacks\` naast de app.

## Diagnose in de app

Tab **Instellingen**: omgeving, welke env-keys gezien zijn, databasenaam, gemaskeerde connection string, ping-knop.

## Zie ook

- [DEPLOYMENT.md](DEPLOYMENT.md) — Docker-voorbeeld en serverwissel.
- [README.md](README.md) — snelstart.
