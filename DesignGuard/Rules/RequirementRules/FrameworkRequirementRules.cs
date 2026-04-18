using DesignGuard.Models;

namespace DesignGuard.Rules.RequirementRules;

/// <summary>
/// Vereisten geïnspireerd op OWASP, AVG, NIS2, CRA — geen juridische claim.
/// </summary>
public sealed class AuthenticationRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveHasAuthentication) yield break;

        yield return Req(
            "Sterke authenticatie en bescherming van accounts",
            "Authenticatie",
            new[] { "OWASP", "NIS2", "CRA" },
            "Zorg dat alleen echte gebruikers kunnen inloggen en dat accounts moeilijk over te nemen zijn.",
            "Je ontwerp bevat inlogfunctionaliteit; zwakke authenticatie is een veelvoorkomende aanvalsvector.",
            "Gebruik moderne hashing voor wachtwoorden, ondersteun MFA, beperk mislukte pogingen en monitor verdacht gedrag.",
            "Dit betekent: inloggen moet betrouwbaar zijn en lastig te misbruiken voor anderen.",
            "Zwakke accounts leiden tot datalekken en ongeautoriseerde acties.",
            "Omdat je aangaf dat gebruikers zich authenticeren."
        );
    }

    private static RequirementModel Req(string title, string category, string[] tags, string plain, string why,
        string impl, string what, string matters, string included) => new()
    {
        Title = title,
        Category = category,
        SourceTags = tags.ToList(),
        PlainExplanation = plain,
        WhyApplies = why,
        ImplementationDirection = impl,
        Explanation = new ExplanationModel
        {
            WhatItMeans = what,
            WhyItMatters = matters,
            WhyIncluded = included
        }
    };
}

public sealed class AuthorizationRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveHasAuthentication) yield break;

        yield return new RequirementModel
        {
            Title = "Autorisatie en rolscheiding",
            Category = "Autorisatie",
            SourceTags = new List<string> { "OWASP", "NIS2", "CRA" },
            PlainExplanation =
                "Elke actie en elk gegeven moet alleen toegankelijk zijn voor rollen die daar recht op hebben.",
            WhyApplies = "Er is authenticatie; zonder duidelijke autorisatie ontstaan IDOR- en privilege-fouten.",
            ImplementationDirection =
                "Policy-based checks dicht bij de domeinlogica, centrale rol/rechten-tabel, tests per endpoint.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Niet alle ingelogde gebruikers mogen hetzelfde.",
                WhyItMatters = "Zo voorkom je dat iemand per ongeluk data van een ander ziet of wijzigt.",
                WhyIncluded = "Authenticatie is aanwezig in jouw beschrijving."
            }
        };

        if (!ctx.HasAdminSurface) yield break;

        yield return new RequirementModel
        {
            Title = "Strikte scheiding van admin- en gebruikersfuncties",
            Category = "Administratieve toegang",
            SourceTags = new List<string> { "OWASP", "NIS2" },
            PlainExplanation =
                "Beheerderspaden krijgen extra bescherming en mogen niet 'per ongeluk' voor normale gebruikers openstaan.",
            WhyApplies = "Adminfunctionaliteit vergroot de impact bij misbruik.",
            ImplementationDirection =
                "Aparte route/URL, MFA voor admins, logging van admin-acties, minimale admin-accounts.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Beheer is een klein, zwaar beveiligd deel van het systeem.",
                WhyItMatters = "Een fout hier heeft grote gevolgen voor alle gebruikers en data.",
                WhyIncluded = "Je gaf aan dat er beheerfunctionaliteit bestaat."
            }
        };
    }
}

public sealed class DataProtectionRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectivePersonalData) yield break;

        yield return new RequirementModel
        {
            Title = "Data minimalisatie en doelbinding (privacy)",
            Category = "Gegevensbescherming",
            SourceTags = new List<string> { "AVG", "OWASP" },
            PlainExplanation =
                "Verzamel en bewaar alleen gegevens die nodig zijn voor een duidelijk doel — niet 'voor het geval dat'.",
            WhyApplies = "Persoonsgegevens zijn aanwezig; dit vraagt bewuste keuzes over hoeveelheid en bewaartermijn.",
            ImplementationDirection =
                "Datamatrix per veld (doel, basis, bewaartermijn), periodieke opschoning, anonimisering in rapportages.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je houdt gegevens beperkt en gebruikt ze alleen waarvoor je ze nodig hebt.",
                WhyItMatters = "Minder data betekent minder risico bij een lek en meer vertrouwen.",
                WhyIncluded = "Je verwerkings van persoonsgegevens staat aan in het ontwerp."
            }
        };

        yield return new RequirementModel
        {
            Title = "Beveiliging tijdens transport en opslag",
            Category = "Gegevensbescherming",
            SourceTags = new List<string> { "AVG", "NIS2", "CRA" },
            PlainExplanation =
                "Gegevens moeten onderweg en op schijf beschermd zijn tegen meelezen en ongeoorloofde wijziging.",
            WhyApplies = "Persoons- of gevoelige data vraagt basis-hygiëne rond encryptie en toegang.",
            ImplementationDirection = "TLS overal, sterke sleutelbeheer, versleuteling at rest waar passend, backups afschermen.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Data is versleuteld of anderszins niet vrij leesbaar voor onbevoegden.",
                WhyItMatters = "Dit beperkt schade bij diefstal van schijven of afluisteren op netwerken.",
                WhyIncluded = "Persoonsgegevens of gevoelige opslag is onderdeel van jouw model."
            }
        };
    }
}

public sealed class LoggingRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveHasAuthentication && !ctx.HasAdminSurface) yield break;

        yield return new RequirementModel
        {
            Title = "Logging, monitoring en incidentrespons-basis",
            Category = "Logging en monitoring",
            SourceTags = new List<string> { "NIS2", "OWASP", "CRA" },
            PlainExplanation =
                "Leg security-relevante gebeurtenissen vast op een manier die onderzoek en herstel mogelijk maakt.",
            WhyApplies = "Bij accounts en beheer is zicht op misbruik essentieel.",
            ImplementationDirection =
                "Gestandaardiseerde logvelden, centrale aggregatie, retentiebeleid, alerts op kritieke patronen.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je kunt achteraf zien wat er gebeurde en snel ingrijpen.",
                WhyItMatters = "Zonder logs is een aanval een black box.",
                WhyIncluded = "Authenticatie of admin maakt logging een logische eis."
            }
        };
    }
}

public sealed class SecureDevelopmentRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        yield return new RequirementModel
        {
            Title = "Veilige configuratie en afhankelijkheden bijwerken",
            Category = "Secure development en onderhoud",
            SourceTags = new List<string> { "CRA", "OWASP" },
            PlainExplanation =
                "Houd frameworks en libraries bij en sluit standaard onveilige instellingen uit.",
            WhyApplies = "Elk softwaresysteem heeft technische schuld en bekende kwetsbaarheden in dependencies.",
            ImplementationDirection = "SBOM-light (lijst met libs), geautomatiseerde updates/scans, harde baseline configs.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je bouwt niet alleen features, maar houdt het platform ook veilig bij de tijd.",
                WhyItMatters = "Veel aanvallen misbruiken bekende oude versies.",
                WhyIncluded = "Algemene baseline voor elk project in DesignGuard."
            }
        };

        if (ctx.HasExternalService)
        {
            yield return new RequirementModel
            {
                Title = "Integraties en leveranciers afschermen",
                Category = "Externe afhankelijkheden en integraties",
                SourceTags = new List<string> { "NIS2", "CRA", "OWASP" },
                PlainExplanation =
                    "Externe diensten krijgen alleen de sleutels en data die nodig zijn; contracten en logging zijn helder.",
                WhyApplies = "Externe API's vormen een vertrouwensgrens.",
                ImplementationDirection =
                    "Geheimen in vault, scopes per omgeving, foutafhandeling zonder datalek, SLA/exit waar zinvol.",
                Explanation = new ExplanationModel
                {
                    WhatItMeans = "Koppelingen met buiten zijn expliciet ingericht, niet 'even snel geplakt'.",
                    WhyItMatters = "Fouten bij integraties leiden vaak tot datalekken of fraude.",
                    WhyIncluded = "Je ontwerp noemt externe API's of externe componenten."
                }
            };
        }
    }
}

public sealed class InputValidationRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasApiLayer && !ctx.HasFrontend) yield break;

        yield return new RequirementModel
        {
            Title = "Invoervalidatie en veilige foutafhandeling",
            Category = "Invoervalidatie",
            SourceTags = new List<string> { "OWASP" },
            PlainExplanation =
                "Alle invoer wordt gecontroleerd; foutmeldingen geven geen interne details prijs.",
            WhyApplies = "Web/API's accepteren gebruikersinvoer — dat is een klassieke aanvalsvector.",
            ImplementationDirection =
                "Allowlists, lengtegrenzen, consistente error-envelope, geen stack traces naar clients.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Het systeem vertrouwt niets wat binnenkomt zonder check.",
                WhyItMatters = "Zo voorkom je injecties en informatielekken via foutpagina's.",
                WhyIncluded = "Er is een publieke interface (web of API) in je model."
            }
        };

        if (!ctx.EffectiveFileUpload) yield break;

        yield return new RequirementModel
        {
            Title = "Veilige verwerking van uploads",
            Category = "Bestandsverwerking",
            SourceTags = new List<string> { "OWASP", "CRA" },
            PlainExplanation =
                "Uploads worden getypeerd, beperkt in grootte en los van uitvoerbare paden opgeslagen.",
            WhyApplies = "Bestandsuploads vergroten het risico op malware en servermisconfiguratie.",
            ImplementationDirection =
                "Content-inspectie, virusscan waar nodig, random bestandsnamen, geen directe uitvoering vanaf uploadpad.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Geüploade bestanden worden behandeld als potentieel gevaarlijk.",
                WhyItMatters = "Eén kwaadaardig bestand kan een heel systeem compromitteren.",
                WhyIncluded = "Je gaf aan dat uploads bestaan."
            }
        };
    }
}

public sealed class ResilienceRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasApiLayer && ctx.Project.SystemType != SystemType.Api) yield break;

        yield return new RequirementModel
        {
            Title = "Beschikbaarheid: limieten en degradeer netjes",
            Category = "Beschikbaarheid en veerkracht",
            Priority = RequirementPriority.Medium,
            SourceTags = new List<string> { "NIS2", "OWASP" },
            PlainExplanation =
                "Beperk misbruik van endpoints en zorg dat afhankelijkheden het systeem niet onnodig laten crashen.",
            WhyApplies = "API's zijn gevoelig voor overbelasting en kettingfouten.",
            ImplementationDirection = "Rate limits, timeouts, retries met jitter, bulkheads/circuit breakers.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Het systeem blijft bruikbaar ook als het druk wordt of een onderdeel faalt.",
                WhyItMatters = "Beschikbaarheid is onderdeel van vertrouwen en continuïteit.",
                WhyIncluded = "Je model bevat een API-laag."
            }
        };
    }
}

public sealed class SessionManagementRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveHasAuthentication) yield break;

        yield return new RequirementModel
        {
            Title = "Sessiebeheer: timeouts, rotatie en diefstal beperken",
            Category = "Sessiebeheer",
            Priority = ctx.Project.InternetExposed ? RequirementPriority.High : RequirementPriority.Medium,
            SourceTags = new List<string> { "OWASP", "CRA" },
            PlainExplanation =
                "Sessies moeten voorspelbaar verlopen en moeilijk te stelen of te hergebruiken zijn.",
            WhyApplies = "Bij web en API's zijn sessies een veelgebruikte aanvalsvector.",
            ImplementationDirection =
                "Secure/HttpOnly/SameSite cookies of tokens met korte TTL, refresh-flow, server-side invalidatie bij uitloggen.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Een sessie is je 'ingelogd blijven' — dat moet strak geregeld zijn.",
                WhyItMatters = "Gestolen sessies voelen voor het systeem hetzelfde als de echte gebruiker.",
                WhyIncluded = "Authenticatie is onderdeel van je ontwerp."
            }
        };
    }
}

public sealed class SecureConfigurationRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasApiLayer && !ctx.HasFrontend) yield break;

        yield return new RequirementModel
        {
            Title = "Veilige standaardconfiguratie (headers, secrets, omgevingen)",
            Category = "Veilige configuratie",
            Priority = RequirementPriority.Medium,
            SourceTags = new List<string> { "OWASP", "NIS2" },
            PlainExplanation =
                "Productie verschilt van dev: geen debug, geen default-wachtwoorden, geen secrets in repo.",
            WhyApplies = "Publieke lagen en API's hebben harde configuratie-eisen om misbruik te beperken.",
            ImplementationDirection =
                "Secret manager, strikte CORS, security headers waar passend, least privilege voor service-accounts.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Het platform staat strak afgesteld, niet op 'makkelijk voor demo'.",
                WhyItMatters = "Veel incidenten beginnen met een vergeten toggle of een gelekte sleutel.",
                WhyIncluded = "Er is een publieke of API-laag in je model."
            }
        };
    }
}

public sealed class PrivacyMinimizationRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectivePersonalData) yield break;

        yield return new RequirementModel
        {
            Title = "Privacy by design: minimale verwerking en duidelijke rollen",
            Category = "Privacy en dataminimalisatie",
            Priority = RequirementPriority.Medium,
            SourceTags = new List<string> { "AVG", "OWASP" },
            PlainExplanation =
                "Leg vast wie welke persoonsgegevens mag zien en hoe lang data bewaard blijft — bij voorkeur in het ontwerp.",
            WhyApplies = "Bij persoonsgegevens helpt vroeg nadenken over minimalisatie en transparantie.",
            ImplementationDirection =
                "Dataclassificatie per veld, rolbeperkingen, export/verwijder-flows voor gebruikers waar passend.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je verwerkt alleen wat nodig is en maakt het voor gebruikers begrijpelijk.",
                WhyItMatters = "Dit verlaagt risico en vergroot vertrouwen — richtinggevend, geen juridisch advies.",
                WhyIncluded = "Je gaf aan dat er persoonsgegevens zijn."
            }
        };
    }
}

public sealed class AdministrativeAccessRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasAdminSurface) yield break;

        yield return new RequirementModel
        {
            Title = "Beheerdersaccounts beschermen (MFA, least privilege, logging)",
            Category = "Administratieve toegang",
            Priority = ctx.Project.InternetExposed ? RequirementPriority.High : RequirementPriority.Medium,
            SourceTags = new List<string> { "OWASP", "NIS2", "CRA" },
            PlainExplanation =
                "Admin-rechten zijn hoog impact: extra controles en zicht op gebruik zijn nodig.",
            WhyApplies = "Adminfunctionaliteit vergroot de schade bij misbruik.",
            ImplementationDirection =
                "Aparte admin-URL, MFA, break-glass procedure, volledige audit trail van admin-acties.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Beheerderspaden zijn zwaarder beveiligd dan normale gebruikersroutes.",
                WhyItMatters = "Eén gecompromitteerd admin-account kan het hele systeem raken.",
                WhyIncluded = "Je ontwerp bevat beheer of admin."
            }
        };
    }
}

public sealed class TrustBoundaryRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasTrustBoundaryCrossing) yield break;

        yield return new RequirementModel
        {
            Title = "Expliciete beveiliging op trust boundaries",
            Category = "Architectuur en trust boundaries",
            Priority = RequirementPriority.High,
            SourceTags = new List<string> { "OWASP" },
            PlainExplanation =
                "Iedere grens tussen vertrouwde zones krijgt authenticatie, autorisatie en schema-afspraken.",
            WhyApplies = "Datastromen kruisen trust boundaries — daar horen expliciete afspraken bij.",
            ImplementationDirection =
                "mTLS/overleg per integratie, allowlists, geen impliciet vertrouwen op 'intern netwerk'.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je behandelt overgangen tussen zones als risicovolle punten.",
                WhyItMatters = "Veel datalekken ontstaan doordat intern verkeer te veel vertrouwen krijgt.",
                WhyIncluded = "Je model bevat minstens één grensoverschrijdende datastroom."
            }
        };
    }
}
