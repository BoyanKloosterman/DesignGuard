using DesignGuard.Models;

namespace DesignGuard.Rules.RequirementRules;

/// <summary>
/// Vereisten geïnspireerd op OWASP, AVG, NIS2, CRA — geen juridische claim.
/// </summary>
public sealed class AuthenticationRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.Project.HasAuthentication) yield break;

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
        if (!ctx.Project.HasAuthentication) yield break;

        yield return new RequirementModel
        {
            Title = "Autorisatie en rolscheiding",
            Category = "Toegangsbeheer",
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
            Category = "Toegangsbeheer",
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
        if (!ctx.Project.PersonalDataProcessed) yield break;

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
        if (!ctx.Project.HasAuthentication && !ctx.HasAdminSurface) yield break;

        yield return new RequirementModel
        {
            Title = "Logging, monitoring en incidentrespons-basis",
            Category = "Detectie & respons",
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
            Category = "Leveringsketen & onderhoud",
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
                Category = "Integraties",
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
            Category = "Applicatiebeveiliging",
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

        if (!ctx.Project.FileUpload) yield break;

        yield return new RequirementModel
        {
            Title = "Veilige verwerking van uploads",
            Category = "Applicatiebeveiliging",
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
            Category = "Beschikbaarheid",
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
