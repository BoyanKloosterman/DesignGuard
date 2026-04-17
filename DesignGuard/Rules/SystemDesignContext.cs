using DesignGuard.Models;

namespace DesignGuard.Rules;

/// <summary>Snapshot van systeemkenmerken voor regels; leest ook vrije tekst op componenten.</summary>
public sealed class SystemDesignContext
{
    private static readonly string[] DatabaseTerms =
    {
        "database", "postgresql", "postgres", "mysql", "mariadb", "mongodb", "mongo",
        "redis", "elasticsearch", "dynamodb", "cassandra", "sql server", "mssql",
        "sqlite", "oracle", "neo4j", "influxdb", "timescaledb", "cockroach", "firestore",
        "supabase", "cosmos db", "snowflake", "orm ", "entity framework", "hibernate"
    };

    private static readonly string[] ExternalTerms =
    {
        "externe api", "external api", "third-party", "third party", "webhook", "integratie",
        "saas", "stripe", "mollie", "paypal", "adyen", "sendgrid", "twilio", "slack",
        "payment provider", "betaling", "maps api", "google api", "azure service bus",
        "message queue", "kafka", "rabbitmq", "sns", "sqs"
    };

    private static readonly string[] ApiTerms =
    {
        "rest api", "graphql", "grpc", "openapi", "swagger", "microservice", "endpoint",
        "webservice", "http api", "json api", "web api", "api gateway"
    };

    private static readonly string[] FrontendTerms =
    {
        "react", "vue", "angular", "svelte", "next.js", "blazor", "spa ", "single page",
        "webapp", "browser", "frontend", "portaal", "gebruikersinterface", "ui "
    };

    private static readonly string[] AdminTerms =
    {
        "backoffice", "beheer", "beheerder", "admin-console", "admin console",
        "administratiepaneel", "admin panel", " admin", "administrator"
    };

    private static readonly string[] PersonalDataTerms =
    {
        "persoonsgegeven", "persoonsgegevens", "avg", "gdpr", "privacy", "bsn",
        "burgerservicenummer", "e-mailadres", "emailadres", "persoonlijke data",
        "persoonsdata", "gezondheidsdata", "patient", "patiënt", "personally identifiable",
        "pii"
    };

    private static readonly string[] FileUploadTerms =
    {
        "file upload", "bestandsupload", "bestand upload", "document upload", "bijlage",
        "attachment", "multipart/form-data", "uploaden van", "upload van bestand"
    };

    private static readonly string[] AuthTerms =
    {
        "inloggen", "login", "oauth", "oidc", "openid", "saml", "sso", "jwt",
        "bearer token", "authenticatie", "keycloak", "entra id", "azure ad", "auth0",
        "session ", "mfa", "2fa", "twee-factor"
    };

    private static readonly string[] SensitiveStorageTerms =
    {
        "api key", "api-key", "geheim", "secret manager", "vault", "hashicorp",
        "credential", "wachtwoord opslag", "kms", "private key", "encryptie at rest"
    };

    private readonly string _blob;

    public SystemDesignContext(ProjectModel project)
    {
        Project = project;
        _blob = string.Join('\u001f', project.Components.Select(StaticBlob)).ToLowerInvariant();
    }

    public ProjectModel Project { get; }

    /// <summary>True als projectvlag aan staat of tekst op componenten het suggereert.</summary>
    public bool EffectivePersonalData =>
        Project.PersonalDataProcessed || Mentions(PersonalDataTerms);

    public bool EffectiveFileUpload =>
        Project.FileUpload || Mentions(FileUploadTerms);

    public bool EffectiveHasAuthentication =>
        Project.HasAuthentication || Mentions(AuthTerms);

    public bool EffectiveSensitiveStorage =>
        Project.SensitiveDataStored || Mentions(SensitiveStorageTerms);

    public bool HasDatabase =>
        Project.Components.Any(c => TagEquals(c.Tag, "database", "db", "datastore")) ||
        Project.Components.Any(ComponentSuggestsDatabase) ||
        Mentions(DatabaseTerms);

    public bool HasExternalService =>
        Project.ExternalApis ||
        Project.Components.Any(c => TagEquals(c.Tag, "external", "third-party", "saas")) ||
        Project.Components.Any(ComponentSuggestsExternal) ||
        Mentions(ExternalTerms);

    public bool HasApiLayer =>
        Project.SystemType is SystemType.Api or SystemType.WebApp or SystemType.MobileBackend ||
        Project.Components.Any(c => TagEquals(c.Tag, "api", "backend", "service")) ||
        Project.Components.Any(ComponentSuggestsApi) ||
        Mentions(ApiTerms);

    public bool HasFrontend =>
        Project.SystemType == SystemType.WebApp ||
        Project.Components.Any(c => TagEquals(c.Tag, "frontend", "ui", "spa", "web")) ||
        Project.Components.Any(ComponentSuggestsFrontend) ||
        Mentions(FrontendTerms);

    public bool HasAdminSurface =>
        Project.HasAdmin ||
        Project.Components.Any(c =>
            c.Name.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
            TagEquals(c.Tag, "admin")) ||
        Project.Components.Any(ComponentSuggestsAdmin) ||
        Mentions(AdminTerms);

    public bool HasTrustBoundaryCrossing =>
        Project.DataFlows.Any(f =>
        {
            var from = Project.Components.FirstOrDefault(c => c.Id == f.FromComponentId);
            var to = Project.Components.FirstOrDefault(c => c.Id == f.ToComponentId);
            if (from == null || to == null) return false;
            return from.TrustBoundaryId != to.TrustBoundaryId &&
                   from.TrustBoundaryId is not null &&
                   to.TrustBoundaryId is not null;
        });

    public bool InternetFacingHighRisk =>
        Project.InternetExposed && (EffectivePersonalData || HasAdminSurface || EffectiveSensitiveStorage);

    public bool ComponentSuggestsDatabase(ComponentModel c) => BlobContains(StaticBlob(c), DatabaseTerms);

    public bool ComponentSuggestsExternal(ComponentModel c) => BlobContains(StaticBlob(c), ExternalTerms);

    public bool ComponentSuggestsApi(ComponentModel c) => BlobContains(StaticBlob(c), ApiTerms);

    public bool ComponentSuggestsAdmin(ComponentModel c) => BlobContains(StaticBlob(c), AdminTerms);

    public IReadOnlyList<string> NamesOfExternalishComponents()
    {
        var q = Project.Components
            .Where(c => TagLike(c.Tag, "external") || ComponentSuggestsExternal(c))
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();
        return q;
    }

    public IReadOnlyList<string> NamesOfDatabaseComponents()
    {
        var q = Project.Components
            .Where(c =>
                c.Tag.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                c.Name.Contains("database", StringComparison.OrdinalIgnoreCase) ||
                ComponentSuggestsDatabase(c))
            .Select(c => c.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();
        return q;
    }

    public IReadOnlyList<string> AllTriggerKeys()
    {
        var keys = new List<string>();
        void add(string k)
        {
            if (!keys.Contains(k)) keys.Add(k);
        }

        if (Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
        if (EffectiveHasAuthentication) add(RuleTriggerKeys.HasAuthentication);
        if (HasAdminSurface) add(RuleTriggerKeys.AdminSurface);
        if (EffectivePersonalData) add(RuleTriggerKeys.PersonalData);
        if (EffectiveSensitiveStorage) add(RuleTriggerKeys.SensitiveStorage);
        if (HasExternalService) add(RuleTriggerKeys.ExternalIntegration);
        if (EffectiveFileUpload) add(RuleTriggerKeys.FileUpload);
        if (HasDatabase) add(RuleTriggerKeys.DatabasePresent);
        if (HasApiLayer) add(RuleTriggerKeys.ApiLayer);
        if (HasFrontend) add(RuleTriggerKeys.Frontend);
        if (HasTrustBoundaryCrossing) add(RuleTriggerKeys.TrustBoundaryCrossing);
        if (!Project.LoggingMonitoringPresent) add(RuleTriggerKeys.LoggingMonitoringMissing);
        if (Project.CriticalBusinessFunction) add(RuleTriggerKeys.CriticalBusiness);
        if (Project.InternetExposed && HasAdminSurface) add(RuleTriggerKeys.InternetFacingAdmin);

        return keys;
    }

    private bool Mentions(IReadOnlyList<string> terms) => BlobContains(_blob, terms);

    private static string StaticBlob(ComponentModel c) =>
        $"{c.Name} {c.Tag} {c.Description} {c.Notes}";

    private static bool BlobContains(string blobLower, IReadOnlyList<string> terms)
    {
        foreach (var t in terms)
        {
            if (blobLower.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool ComponentSuggestsFrontend(ComponentModel c) =>
        BlobContains(StaticBlob(c), FrontendTerms);

    private static bool TagEquals(string tag, params string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return allowed.Any(a => tag.Equals(a, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TagLike(string tag, string needle) =>
        !string.IsNullOrWhiteSpace(tag) && tag.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
