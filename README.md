# DesignGuard

DesignGuard is een **Windows-desktopapp** (WPF) voor **security-by-design**: je legt je systeemontwerp vast, de app helpt daarbij **dreigingen (STRIDE)** en **security-eisen** te genereren, te beheren en te **exporteren**. Het is een **lokale werkbank**; er is geen cloud of account nodig.

> De app maakt **geen claim op juridische conformiteit** (AVG, NIS2, CRA, enz.). Export en bron-tags bij eisen zijn **ondersteunend**, geen certificering.

## Vereisten

- **Windows** (WPF)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (komt overeen met `TargetFramework` in het project)

Controleer in een terminal:

```powershell
dotnet --version
```

## Bouwen en starten

Vanuit de root van deze repository (de map waar `DesignGuard.sln` staat):

```powershell
dotnet build
dotnet run --project DesignGuard\DesignGuard.csproj
```

Of open `DesignGuard.sln` in Visual Studio en start met F5.

## Waar worden gegevens opgeslagen?

- De database is een **SQLite**-bestand: `%LOCALAPPDATA%\DesignGuard\designguard-v2.db`
- **Back-up:** kopieer die map of het `.db`-bestand voordat je experimenteert of de app verwijdert.

## Eerste gebruik

1. Start de app; bij de eerste start wordt de database aangemaakt.
2. Er is een **Demo**-project beschikbaar (knop **Demo** of selectie in de projectlijst) om de werking te verkennen.
3. De **statusbalk** onderaan toont meldingen (geladen project, fouten, enz.).

## Schermindeling

- **Links — Projecten:** Nieuw, Wizard, Opslaan, Verwijderen, Demo; daaronder de **projectlijst**.
- **Midden — Navigatie:** tabs voor Dashboard, Ontwerp, Dreigingen, Eisen, Beslissingen, Traceability, Export.
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
   - Knop **Analyse vernieuwen** (ook wel **Regenerate from design** in de code): op basis van het ontwerp worden dreigingen en eisen opnieuw opgebouwd (met merge-logica: bestaande items kunnen behouden blijven als je dat zo instelt).
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
   - **Markdown**, **Tekst**, **HTML**, **JSON**: preview in het scherm; gebruik dit voor wiki’s, rapporten of verdere verwerking.

8. **Opslaan**
   - Gebruik **Opslaan** om wijzigingen naar de database te schrijven. Werk periodiek en vóór het sluiten van de app.

## Dashboard en sjablonen

- **Dashboard** toont tellingen (open/gemitigeerde dreigingen, eisen).
- **Sjablonen** op het dashboard vullen het ontwerp met een voorbeeldstructuur; pas daarna je eigen situatie aan en vernieuw de analyse.

## Tips

- Hoe **rijker** het ontwerp (boundaries, stromen, vinkjes), hoe **zinvoller** de voorgestelde dreigingen en eisen.
- Na grote wijzigingen in het ontwerp: **Analyse vernieuwen** zodat de lijsten aansluiten — controleer daarna of handmatige aanpassingen nog kloppen.
- Maak een **back-up** van `%LOCALAPPDATA%\DesignGuard\` als je meerdere machines gebruikt of vóór upgrades.

## Technische stack (kort)

- **.NET / WPF**, **Entity Framework Core** + **SQLite**, **CommunityToolkit.Mvvm**.

## Licentie en ondersteuning

Geen licentie-informatie in deze repository vermeld; voeg die toe indien je het project publiceert.
