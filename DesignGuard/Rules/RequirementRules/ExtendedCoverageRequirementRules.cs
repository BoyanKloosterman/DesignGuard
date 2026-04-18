using DesignGuard.Models;

namespace DesignGuard.Rules.RequirementRules;

public sealed class SecretsManagementRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.HasExternalService && !ctx.HasDatabase && !ctx.EffectiveSensitiveStorage) yield break;

        yield return new RequirementModel
        {
            Title = "Centraal geheimenbeheer en rotatie",
            Category = "Geheimen en sleutels",
            Priority = ctx.Project.InternetExposed ? RequirementPriority.High : RequirementPriority.Medium,
            SourceTags = new List<string> { "OWASP", "NIS2", "CRA" },
            PlainExplanation =
                "API-sleutels, DB-credentials en certificaat-private keys worden centraal beheerd, niet in broncode of tickets.",
            WhyApplies = "Externe koppelingen, databases of gevoelige opslag impliceren langdurige geheimen.",
            ImplementationDirection =
                "Vault/KMS, IAM-rolls per omgeving, automatische rotatie waar mogelijk, geen echo van secrets in logs.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Geheimen hebben een gecontroleerde levenscyclus.",
                WhyItMatters = "Gelekte credentials zijn een snelle route naar datalekken.",
                WhyIncluded = "Je ontwerp bevat externe diensten, database of gevoelige opslag."
            }
        };
    }
}

public sealed class BackupRestoreRequirementRule : IRequirementRule
{
    public IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx)
    {
        if (!ctx.Project.CriticalBusinessFunction && !ctx.EffectiveSensitiveStorage && !ctx.EffectivePersonalData)
            yield break;

        yield return new RequirementModel
        {
            Title = "Back-up, herstel en integriteitstests (RTO/RPO)",
            Category = "Continuïteit en herstel",
            Priority = ctx.Project.CriticalBusinessFunction ? RequirementPriority.High : RequirementPriority.Medium,
            SourceTags = new List<string> { "NIS2", "CRA" },
            PlainExplanation =
                "Back-ups zijn versleuteld, afgeschermd en periodiek getest; herstelpad is gedocumenteerd.",
            WhyApplies = "Kritieke processen of waardevolle data vragen aantoonbaar herstel.",
            ImplementationDirection =
                "Immutability/offsite waar passend, restore drills, monitoring op back-up-falen, versiebeheer van configs.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Je kunt na ransomware of fouten terug naar een betrouwbare staat.",
                WhyItMatters = "Zonder getest herstel zijn back-ups slechts theoretisch.",
                WhyIncluded = "Bedrijfskritisch of gevoelige/persoonsdata in scope."
            }
        };
    }
}
