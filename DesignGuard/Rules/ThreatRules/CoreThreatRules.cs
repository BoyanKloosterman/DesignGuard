using DesignGuard.Models;

namespace DesignGuard.Rules.ThreatRules;

public sealed class AuthenticationThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveHasAuthentication) yield break;

        yield return new ThreatModel
        {
            Title = "Identiteit vervalsen (login / sessie)",
            StrideCategory = StrideCategory.Spoofing,
            Description =
                "Aanvallers kunnen proberen zich voor te doen als een echte gebruiker via gestolen wachtwoorden, phishing of sessiecookies.",
            AffectedComponents = ctx.Project.Components
                .Where(c => c.Tag.Contains("api", StringComparison.OrdinalIgnoreCase) ||
                            c.Tag.Contains("frontend", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Name).DefaultIfEmpty("Authenticatie").Take(4).ToList(),
            GenerationReason = "Authenticatie is ingeschakeld: spoofing en misbruik van sessies zijn relevant.",
            SuggestedMitigations = new List<string>
            {
                "MFA waar passend",
                "Beveiligde sessiebeheer (httpOnly, secure, korte levensduur)",
                "Rate limiting en monitoring op inlogpogingen"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Iemand doet alsof hij jouw gebruiker is om binnen te komen.",
                WhyItMatters = "Dan kunnen gegevens worden ingezien of acties worden uitgevoerd in jouw naam.",
                WhyIncluded = "Omdat je aangaf dat er ingelogd wordt."
            }
        };

        yield return new ThreatModel
        {
            Title = "Brute force en credential stuffing",
            StrideCategory = StrideCategory.DenialOfService,
            Description =
                "Automatische gissing van wachtwoorden of het proberen van gelekte combinaties op jouw inlogpagina.",
            AffectedComponents = new List<string> { "Authenticatie-endpoint" },
            GenerationReason = "Authenticatie aanwezig: aanvallers kunnen automatisch proberen in te loggen.",
            SuggestedMitigations = new List<string>
            {
                "Progressieve vertraging / lockout-beleid",
                "CAPTCHA of risicogebaseerde stappen na mislukte pogingen",
                "Detectie van ongebruikelijke locaties en apparaten"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Een script probeert heel vaak achter elkaar in te loggen.",
                WhyItMatters = "Zwakke wachtwoorden of hergebruik van gelekte accounts worden zo uitgebuit.",
                WhyIncluded = "Je systeem heeft een login; dat is een typisch doelwit."
            }
        };
    }
}

public sealed class DatabaseThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasDatabase) yield break;

        var names = ctx.NamesOfDatabaseComponents().ToList();
        if (names.Count == 0) names.Add("Database");

        yield return new ThreatModel
        {
            Title = "SQL-/NoSQL-injectie en ongeautoriseerde queries",
            StrideCategory = StrideCategory.Tampering,
            Description =
                "Onveilige query-opbouw kan aanvallers laten knoeien met data of extra data uitlezen.",
            AffectedComponents = names,
            GenerationReason = "Er is een databasecomponent: injectie en integriteitsrisico's horen bij STRIDE Tampering.",
            SuggestedMitigations = new List<string>
            {
                "Parameterized queries / ORM correct gebruiken",
                "Minimale DB-rechten per rol",
                "Inputvalidatie en allowlists"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Invoer wordt per ongeluk als commando voor de database geïnterpreteerd.",
                WhyItMatters = "Data kan worden gewijzigd, verwijderd of ongewenst worden getoond.",
                WhyIncluded = "Omdat je ontwerp een database bevat."
            }
        };

        yield return new ThreatModel
        {
            Title = "Ongeautoriseerde toegang tot data (oversharing / IDOR)",
            StrideCategory = StrideCategory.InformationDisclosure,
            Description =
                "API's kunnen per ongeluk records van andere gebruikers tonen als autorisatie ontbreekt of zwak is.",
            AffectedComponents = names,
            GenerationReason = "Database + (web/API) verhoogt risico op datalek via applicatielaag.",
            SuggestedMitigations = new List<string>
            {
                "Strikte autorisatie per object (bv. order hoort bij gebruiker)",
                "Audits en tests op horizontale privilege-escalatie",
                "Encryptie at rest voor gevoelige kolommen waar nodig"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Iemand ziet of download gegevens die niet voor hem bedoeld zijn.",
                WhyItMatters = "Dit is een veelvoorkomende datalek-route in web- en API-systemen.",
                WhyIncluded = "Database aanwezig en data wordt typisch via de app ontsloten."
            }
        };
    }
}

public sealed class ExternalApiThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasExternalService) yield break;

        var ext = ctx.NamesOfExternalishComponents().ToList();
        if (ext.Count == 0 && (ctx.Project.ExternalApis || ctx.HasExternalService))
            ext.Add("Externe dienst");

        yield return new ThreatModel
        {
            Title = "Vertrouwensgrens naar externe API",
            StrideCategory = StrideCategory.Spoofing,
            Description =
                "Een aanvaller kan proberen jouw backend te misleiden met valse callbacks of een nagemaakte externe dienst.",
            AffectedComponents = ext,
            GenerationReason = "Externe API of vlag 'externe diensten': trust boundary tussen jouw systeem en derden.",
            SuggestedMitigations = new List<string>
            {
                "TLS pinning/overwegingen, host-validatie",
                "Webhook-handtekeningen en idempotency keys",
                "Secrets roteren en niet in code loggen"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Verkeer over de grens met een andere partij kan misbruikt of nagemaakt worden.",
                WhyItMatters = "Foute aannames over 'wie de afzender is' leiden tot frauduleuze acties.",
                WhyIncluded = "Je maakte melding van externe API's of een extern component."
            }
        };

        yield return new ThreatModel
        {
            Title = "Datalek via integratie (logging, replay, abuse)",
            StrideCategory = StrideCategory.InformationDisclosure,
            Description =
                "Te gedetailleerde logs, replay van requests of gebrek aan quota kan gevoelige data of dienstmisbruik geven.",
            AffectedComponents = ext,
            GenerationReason = "Externe koppelingen verwerken vaak gevoelige payloads.",
            SuggestedMitigations = new List<string>
            {
                "Minimale logging van PII",
                "Replay-bescherming en nonce/timestamp checks",
                "API-sleutels scopen en rate limits"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Bij koppelingen kan meer informatie weglekken dan je denkt.",
                WhyItMatters = "Integraties zijn vaak het zwakste punt in observability en geheimen.",
                WhyIncluded = "Externe integratie is onderdeel van jouw ontwerp."
            }
        };
    }

    private static bool TagLike(string tag, string needle) =>
        !string.IsNullOrWhiteSpace(tag) && tag.Contains(needle, StringComparison.OrdinalIgnoreCase);
}

public sealed class AdminThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasAdminSurface) yield break;

        yield return new ThreatModel
        {
            Title = "Privilege-escalatie naar beheerdersrechten",
            StrideCategory = StrideCategory.ElevationOfPrivilege,
            Description =
                "Zwakke scheiding tussen normale en admin-functies kan leiden tot hogere rechten dan bedoeld.",
            AffectedComponents = ctx.Project.Components
                .Where(c => c.Name.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrWhiteSpace(c.Tag) &&
                             c.Tag.Contains("admin", StringComparison.OrdinalIgnoreCase)) ||
                            ctx.ComponentSuggestsAdmin(c))
                .Select(c => c.Name).DefaultIfEmpty("Admin").Take(3).ToList(),
            GenerationReason = "Admin-functionaliteit aanwezig: EoP en gebroken toegangscontrole zijn relevant.",
            SuggestedMitigations = new List<string>
            {
                "Strikte rol- en rechtenmatrix",
                "Apart admin-pad met extra bescherming (MFA, IP-beperking)",
                "Unit/integration tests op autorisatie"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Een gebruiker krijgt meer macht dan hij zou moeten hebben.",
                WhyItMatters = "Admins kunnen brede schade aanrichten of alle data zien.",
                WhyIncluded = "Je gaf aan dat er beheerfunctionaliteit is."
            }
        };
    }
}

public sealed class FileUploadThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveFileUpload) yield break;

        yield return new ThreatModel
        {
            Title = "Kwaadaardige uploads en type-verwarring",
            StrideCategory = StrideCategory.Tampering,
            Description =
                "Gebruikers kunnen bestanden uploaden die schadelijk zijn of als code worden uitgevoerd als validatie ontbreekt.",
            AffectedComponents = ctx.HasApiLayer
                ? ctx.Project.Components.Where(c => TagLike(c.Tag, "api") || TagLike(c.Tag, "frontend"))
                    .Select(c => c.Name).DefaultIfEmpty("Upload").Take(3).ToList()
                : new List<string> { "Upload" },
            GenerationReason = "Bestandsupload is ingeschakeld in het ontwerp.",
            SuggestedMitigations = new List<string>
            {
                "Content-type + magic-byte checks, sandbox/quarantaine",
                "Opslag buiten webroot, unieke bestandsnamen",
                "Limieten op grootte en antivirus/scan waar passend"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Een geüpload bestand kan meer zijn dan een simpele afbeelding of PDF.",
                WhyItMatters = "Verkeerde verwerking kan leiden tot malware of code-uitvoering.",
                WhyIncluded = "Je vermeldde expliciet dat er uploads zijn."
            }
        };
    }

    private static bool TagLike(string tag, string needle) =>
        !string.IsNullOrWhiteSpace(tag) && tag.Contains(needle, StringComparison.OrdinalIgnoreCase);
}

public sealed class PersonalDataThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectivePersonalData) yield break;

        yield return new ThreatModel
        {
            Title = "Onbedoelde blootstelling van persoonsgegevens",
            StrideCategory = StrideCategory.InformationDisclosure,
            Description =
                "Persoonsgegevens kunnen zichtbaar worden via logs, exports, caches of verkeerde API-responses.",
            AffectedComponents = ctx.Project.Components.Select(c => c.Name).DefaultIfEmpty("Systeem").Take(5).ToList(),
            GenerationReason = "Persoonsgegevens worden verwerkt: focus op informatie-lekken.",
            SuggestedMitigations = new List<string>
            {
                "Data minimaliseren en pseudonimiseren waar kan",
                "Maskeren in niet-productie en strikte toegang tot exports",
                "Encryptie in transit en waar nodig at rest"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Gegevens over mensen raken op een plek waar ze niet horen.",
                WhyItMatters = "Dit raakt privacy, vertrouwen en kan tot meldplichten leiden.",
                WhyIncluded = "Je gaf aan dat er persoonsgegevens zijn."
            }
        };
    }
}

public sealed class TransportAndApiThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasApiLayer && ctx.Project.SystemType != SystemType.Api) yield break;

        yield return new ThreatModel
        {
            Title = "Onveilige communicatie of ontbrekende integriteitscontrole",
            StrideCategory = StrideCategory.Tampering,
            Description =
                "API-verkeer kan worden gemanipuleerd als TLS of message-integriteit ontbreekt of verkeerd is geconfigureerd.",
            AffectedComponents = ctx.Project.Components.Where(c => TagLike(c.Tag, "api") || TagLike(c.Tag, "backend") ||
                                                                   ctx.ComponentSuggestsApi(c))
                .Select(c => c.Name).DefaultIfEmpty("API").Take(3).ToList(),
            GenerationReason = "API-laag gedetecteerd: manipulatie van berichten is een STRIDE Tampering-scenario.",
            SuggestedMitigations = new List<string>
            {
                "TLS overal afdwingen, HSTS waar relevant",
                "Request-signing waar nodig (webhooks, mobiel)",
                "Versiebeheer en deprecatie van onveilige endpoints"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Iemand wijzigt onderweg gegevens die tussen client en server gaan.",
                WhyItMatters = "Dan kunnen acties of inhoud stiekem veranderen zonder dat het opvalt.",
                WhyIncluded = "Je ontwerp bevat een API of backend-laag."
            }
        };
    }

    private static bool TagLike(string tag, string needle) =>
        !string.IsNullOrWhiteSpace(tag) && tag.Contains(needle, StringComparison.OrdinalIgnoreCase);
}

public sealed class DenialOfServiceThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasFrontend && !ctx.HasApiLayer) yield break;

        yield return new ThreatModel
        {
            Title = "Overbelasting van dienst (DoS)",
            StrideCategory = StrideCategory.DenialOfService,
            Description =
                "Publieke endpoints kunnen worden overspoeld met verkeer waardoor echte gebruikers worden buitengesloten.",
            AffectedComponents = ctx.Project.Components.Select(c => c.Name).DefaultIfEmpty("Publieke dienst").Take(4)
                .ToList(),
            GenerationReason = "Er is een publiek bereikbare laag (web/API): beschikbaarheid is bedreigd.",
            SuggestedMitigations = new List<string>
            {
                "Rate limiting, WAF/CDN waar passend",
                "Schalen en health checks",
                "Timeouts en circuit breakers naar afhankelijkheden"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Het systeem krijgt zo veel verzoeken dat het traag wordt of uitvalt.",
                WhyItMatters = "Beschikbaarheid is net zo belangrijk als vertrouwelijkheid voor veel diensten.",
                WhyIncluded = "Publieke interface (web/API) maakt DoS relevant."
            }
        };
    }
}

public sealed class RepudiationAuditThreatRule : IThreatRule
{
    public IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.EffectiveHasAuthentication && !ctx.HasAdminSurface) yield break;

        yield return new ThreatModel
        {
            Title = "Ontkennen van acties (gebrek aan audit trail)",
            StrideCategory = StrideCategory.Repudiation,
            Description =
                "Zonder betrouwbare logging is het lastig te bewijzen wie welke gevoelige actie heeft uitgevoerd.",
            AffectedComponents = new List<string> { "Applicatie / API" },
            GenerationReason = "Authenticatie of admin: gevoelige acties moeten traceerbaar zijn.",
            SuggestedMitigations = new List<string>
            {
                "Centrale, tijdgesynchroniseerde audit logs",
                "Immutability of log forwarding naar SIEM",
                "Correlatie met gebruikers- en sessie-id"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Iemand zegt later: 'dat heb ik niet gedaan' en je kunt het niet checken.",
                WhyItMatters = "Bij incidenten en fraude is bewijs cruciaal.",
                WhyIncluded = "Er zijn ingelogde of beheerdersacties in het ontwerp."
            }
        };
    }
}
