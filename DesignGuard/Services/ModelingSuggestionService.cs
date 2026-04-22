using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Regelgebaseerde suggesties op basis van vlaggen en componenttags.</summary>
public sealed class ModelingSuggestionService
{
    public IReadOnlyList<ModelingSuggestion> Evaluate(ProjectModel project, IReadOnlySet<string> dismissedKeys)
    {
        var list = new List<ModelingSuggestion>();

        void Add(string key, string title, string detail, string because)
        {
            if (dismissedKeys.Contains(key)) return;
            list.Add(new ModelingSuggestion
            {
                Key = key,
                Title = title,
                Detail = detail,
                Because = because
            });
        }

        if (project.HasAdmin)
            Add("s-admin-review", "Review privileged toegang",
                "Leg vast wie admin mag doen, hoe accounts worden uitgegeven en hoe je misbruik signaleert.",
                "Adminfunctionaliteit staat aan in systeemcontext.");

        if (project.PersonalDataProcessed)
            Add("s-privacy-controls", "Privacy-controls en dataminimalisatie",
                "Inventariseer welke persoonsgegevens waar stromen; beperk bewaartermijn en toegang.",
                "Persoonsgegevens gemarkeerd in systeemcontext.");

        if (project.InternetExposed)
            Add("s-attack-surface", "Aanvalsoppervlak inventariseren",
                "Check publieke endpoints, rate limiting, TLS en scheiding van admin.",
                "Internetblootstelling staat aan.");

        if (project.FileUpload)
            Add("s-upload-hardening", "Upload-pad hardenen",
                "Validatie (type/grootte), veilige opslag en scanning overwegen.",
                "Bestandsupload staat aan.");

        if (project.ExternalApis)
            Add("s-dependency-boundary", "Externe koppelingen en grenzen",
                "Documenteer afhankelijkheden, timeouts, secrets en foutscenario's.",
                "Externe API's staan aan.");

        var adminComponent = project.Components.FirstOrDefault(c =>
            c.Tag.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("admin", StringComparison.OrdinalIgnoreCase));
        if (adminComponent != null)
            Add("s-admin-component", "Admin-interface als entry point",
                $"Component '{adminComponent.Name}' lijkt admin-gerelateerd: expliciete trust boundary en logging.",
                "Componentnaam/tag suggereert admin.");

        var piiAsset = project.Assets.Any(a =>
            DesignOntwerpWaarden.IsAssetSensitivityElevatedForSuggestions(a.Sensitivity) ||
            DesignOntwerpWaarden.IsAssetClassificationRestricted(a.Classification));
        var piiSensitive = project.SensitiveDataItems.Any(s =>
            s.Category.Contains("PII", StringComparison.OrdinalIgnoreCase) ||
            s.Category.Contains("persoon", StringComparison.OrdinalIgnoreCase));
        if (piiAsset || piiSensitive)
            Add("s-asset-pii", "PII in assets / gevoelige data",
                "Koppel opslaglocatie, toegang en encryptie-eisen aan deze datasets.",
                "Assets of gevoelige data categorie wijzen op PII.");

        return list;
    }
}
