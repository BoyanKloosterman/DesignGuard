# DesignGuard

DesignGuard is een **Windows-desktopapp** (WPF) voor **security-by-design**: je legt je systeemontwerp vast, de app helpt daarbij **dreigingen (STRIDE)** en **security-eisen** te genereren, te beheren en te **exporteren**. **v5** gebruikt **MongoDB** als primaire datastore; er is geen cloud-account in de app zelf nodig, wel een bereikbare MongoDB waar jij toegang toe geeft.

> De app maakt **geen claim op juridische conformiteit** (AVG, NIS2, CRA, enz.). Export en bron-tags bij eisen zijn **ondersteunend**, geen certificering.

## Vereisten

- **Windows** (WPF)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (komt overeen met `TargetFramework` in het project)
- **MongoDB** (lokaal, Docker, eigen server of Atlas) — zie [DEPLOYMENT.md](DEPLOYMENT.md)

Controleer in een terminal:

```powershell
dotnet --version
```

## Configuratie (v5)

Stel minimaal deze **omgevingsvariabelen** in (of gebruik een `.env` in development — zie [CONFIGURATION.md](CONFIGURATION.md)):

```text
DESIGNGUARD_MONGODB_CONNECTION_STRING=mongodb://localhost:27017
DESIGNGUARD_MONGODB_DATABASE=designguard
DESIGNGUARD_MONGODB_APPNAME=DesignGuard
DESIGNGUARD_ENVIRONMENT=Development
```

Sjabloon zonder secrets: [.env.example](.env.example).

## Bouwen en starten

Vanuit de root van deze repository (de map waar `DesignGuard.sln` staat):

```powershell
dotnet build
dotnet run --project DesignGuard\DesignGuard.csproj
```

Of open `DesignGuard.sln` in Visual Studio en start met F5.

Zorg dat MongoDB draait en bereikbaar is voordat je projecten opslaat.

## Waar worden gegevens opgeslagen?

- **Projecten en ontwerpdata:** MongoDB-database (configureerbaar via env).
- **Lokale voorkeuren:** `%LOCALAPPDATA%\DesignGuard\user-settings.json` (o.a. knowledge pack toggles, exportmap).
- **Knowledge packs:** JSON onder `KnowledgePacks\` naast de executable.
- **Back-up:** gebruik MongoDB-backups (`mongodump`, Atlas, enz.) en kopieer indien nodig de LocalAppData-map voor instellingen.

### Migratie vanaf SQLite (v4)

In de app: **Instellingen → Importeer SQLite → MongoDB** en selecteer het oude `.db`-bestand. Zie [DEPLOYMENT.md](DEPLOYMENT.md).

## Eerste gebruik

1. Configureer MongoDB (env of `.env` in Development).
2. Start de app; bij ontbrekende config zie je een waarschuwing op **Instellingen** en in de statusbalk.
3. Gebruik **Verbinding testen (ping)** op Instellingen.
4. Er is een **Demo**-project beschikbaar als de database bereikbaar is.

## Schermindeling

- **Links — Projecten:** Nieuw, Wizard, Opslaan, Verwijderen, Demo; daaronder de **projectlijst**.
- **Midden — Navigatie:** tabs voor Dashboard, Ontwerp, Dreigingen, Eisen, Beslissingen, Traceability, Export, **Instellingen** (Mongo-diagnose), App security review.
- **Rechts — Inspector:** details en bewerking van de geselecteerde **dreiging**, **eis** of **component** (afhankelijk van je selectie).

## Aanbevolen werkwijze

1. **Project aanmaken**
   - **Nieuw** voor een leeg project, of **Wizard** voor een snelle start met naam, beschrijving, systeemcontext en vinkjes (internet, persoonsgegevens, auth, …).
   - Geef het project een duidelijke **naam** en vul **Ontwerp** zo volledig mogelijk in.

2. **Ontwerp invullen** (tab **Ontwerp**)
   - **Systeemcontext:** type, deployment, vinkjes die je risicoprofiel beschrijven.
   - **Trust boundaries:** zones (bv. “Internet”, “Intern netwerk”).
   - **Componenten:** bouwstenen; koppel ze aan een boundary, markeer **entry** waar nodig, vul **data**-gevoeligheid en notities in.
   - **Datastromen:** wie praat met wie (van/tot component, label).
   - **Rollen** en **Assets** waar relevant.
   - Gebruik **Diagram verversen** om het architectuurdiagram bij te werken.

3. **Analyse vernieuwen**
   - Op basis van het ontwerp worden dreigingen en eisen opgebouwd (met merge-logica: bestaande items kunnen behouden blijven als je dat zo instelt).
   - Items die je **handmatig sterk wilt behouden** bij een volgende regeneratie: vink in de Inspector **Behoud bij hergeneratie (UserModified)** aan bij die dreiging of eis.

4. **Dreigingen en eisen afwerken** (tabs **Dreigingen** / **Eisen**)
   - Zoek en sorteer in de lijsten.
   - Selecteer een item om in de **Inspector** STRIDE-categorie, ernst, status, teksten, enz. aan te passen.
   - Voeg **handmatige dreigingen/eisen** toe waar de regels iets missen.

5. **Beslissingen** (tab **Beslissingen**)
   - Vul **open issues**, **design-notities** (soort vrije tekst, bv. Assumption, Decision) en **aanbevolen controls** in voor overleg en naslag.

6. **Traceability** (tab **Traceability**)
   - Geeft een **read-only overzicht** van relaties (handig voor review of documentatie).

7. **Exporteren** (tab **Export**)
   - **Markdown**, **Tekst**, **HTML**, **JSON**, **PDF**: preview in het scherm; gebruik dit voor wiki’s, rapporten of verdere verwerking.

8. **Opslaan**
   - Gebruik **Opslaan** om wijzigingen naar MongoDB te schrijven. Werk periodiek en vóór het sluiten van de app.

## Dashboard en sjablonen

- **Dashboard** toont tellingen (open/gemitigeerde dreigingen, eisen).
- **Sjablonen** op het dashboard vullen het ontwerp met een voorbeeldstructuur; pas daarna je eigen situatie aan en vernieuw de analyse.

## Tips

- Hoe **rijker** het ontwerp (boundaries, stromen, vinkjes), hoe **zinvoller** de voorgestelde dreigingen en eisen.
- Na grote wijzigingen in het ontwerp: **Analyse vernieuwen** zodat de lijsten aansluiten — controleer daarna of handmatige aanpassingen nog kloppen.
- Zorg voor **back-ups** van MongoDB en van `%LOCALAPPDATA%\DesignGuard\` als je meerdere machines gebruikt of vóór upgrades.

## Technische stack (kort)

- **.NET / WPF**, **MongoDB.Driver**, **CommunityToolkit.Mvvm**, **QuestPDF**.
- **Entity Framework Core + SQLite** alleen nog voor **import** van oude databases.

## Documentatie

- [CONFIGURATION.md](CONFIGURATION.md) — omgevingsvariabelen en `.env`.
- [DEPLOYMENT.md](DEPLOYMENT.md) — Docker, serverwissel, migratie.
- [SECURITY_REVIEW.md](SECURITY_REVIEW.md) — engineering security note.

## Licentie en ondersteuning

Geen licentie-informatie in deze repository vermeld; voeg die toe indien je het project publiceert.
