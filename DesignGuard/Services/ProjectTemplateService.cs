using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Bindbaar voor WPF (geen ValueTuple — die hebben alleen Item1/2/3 voor binding).</summary>
public sealed class ProjectTemplateItem
{
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
}

/// <summary>Ingebouwde startsjablonen — richtinggevend, geen volledige threat library.</summary>
public sealed class ProjectTemplateService
{
    public IReadOnlyList<ProjectTemplateItem> ListTemplates() =>
        new ProjectTemplateItem[]
        {
            new()
            {
                Key = "web_admin",
                Title = "Webapp met adminpaneel",
                Description = "SPA + API + DB + admin + externe betaling"
            },
            new()
            {
                Key = "rest_external",
                Title = "REST API met externe integraties",
                Description = "Headless API met externe providers"
            },
            new()
            {
                Key = "desktop_sensitive",
                Title = "Desktopapp met lokale gevoelige data",
                Description = "Client-only met lokale opslag"
            },
            new()
            {
                Key = "iot_cloud",
                Title = "IoT / product met cloud-backend",
                Description = "Devices, gateway en cloud"
            }
        };

    public ProjectModel Create(string key) => key switch
    {
        "web_admin" => WebAdmin(),
        "rest_external" => RestExternal(),
        "desktop_sensitive" => DesktopSensitive(),
        "iot_cloud" => IotCloud(),
        _ => WebAdmin()
    };

    private static ProjectModel WebAdmin()
    {
        var p = new ProjectModel
        {
            Name = "Sjabloon: webapp + admin",
            Description = "Startpunt voor een typische webapplicatie met beheerderspaneel.",
            SystemName = "Webapplicatie",
            SystemType = SystemType.WebApp,
            DeploymentContext = DeploymentContext.Cloud,
            InternetExposed = true,
            PersonalDataProcessed = true,
            HasAuthentication = true,
            HasAdmin = true,
            ExternalApis = true,
            FileUpload = true,
            SensitiveDataStored = true,
            LoggingMonitoringPresent = true,
            CriticalBusinessFunction = false
        };
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Internet", Description = "Publiek bereikbare zone" });
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Bedrijfsnetwerk", Description = "API en data" });
        var dmz = p.TrustBoundaries[0];
        var corp = p.TrustBoundaries[1];
        p.Components.Add(new ComponentModel
        {
            Name = "Browser UI",
            Tag = "frontend",
            TrustBoundaryName = dmz.Name,
            IsEntryPoint = true,
            StoresOrProcesses = nameof(DataSensitivity.Personal)
        });
        p.Components.Add(new ComponentModel
        {
            Name = "API",
            Tag = "api",
            TrustBoundaryName = corp.Name,
            IsEntryPoint = true
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Database",
            Tag = "database",
            TrustBoundaryName = corp.Name,
            StoresOrProcesses = nameof(DataSensitivity.Sensitive)
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Admin UI",
            Tag = "admin",
            TrustBoundaryName = dmz.Name,
            IsEntryPoint = true
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Externe PSP",
            Tag = "external",
            TrustBoundaryName = dmz.Name
        });
        p.DataFlows.Add(new DataFlowModel
            { Label = "HTTPS", SourceComponentName = "Browser UI", TargetComponentName = "API" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Queries", SourceComponentName = "API", TargetComponentName = "Database" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Beheer", SourceComponentName = "Admin UI", TargetComponentName = "API" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Betaling", SourceComponentName = "API", TargetComponentName = "Externe PSP" });
        p.UserRoles.Add(new UserRoleModel { Name = "Gebruiker", Description = "Standaard gebruiker" });
        p.UserRoles.Add(new UserRoleModel { Name = "Beheerder", Description = "Intern beheer" });
        p.DesignNotes.Add(new DesignNoteModel
        {
            Kind = DesignNoteKind.Assumption,
            Title = "TLS overal",
            Description = "Alle client-server verkeer loopt via TLS1.2+."
        });
        return p;
    }

    private static ProjectModel RestExternal()
    {
        var p = new ProjectModel
        {
            Name = "Sjabloon: REST + extern",
            Description = "API-first met meerdere externe afhankelijkheden.",
            SystemName = "Platform API",
            SystemType = SystemType.Api,
            DeploymentContext = DeploymentContext.Cloud,
            InternetExposed = true,
            HasAuthentication = true,
            ExternalApis = true,
            SensitiveDataStored = true,
            LoggingMonitoringPresent = true
        };
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Publiek" });
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Intern platform" });
        p.Components.Add(new ComponentModel
            { Name = "API Gateway", Tag = "api", TrustBoundaryName = "Publiek", IsEntryPoint = true });
        p.Components.Add(new ComponentModel { Name = "Worker", Tag = "backend", TrustBoundaryName = "Intern platform" });
        p.Components.Add(new ComponentModel { Name = "Database", Tag = "database", TrustBoundaryName = "Intern platform" });
        p.Components.Add(new ComponentModel { Name = "Externe KYC", Tag = "external", TrustBoundaryName = "Publiek" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Requests", SourceComponentName = "API Gateway", TargetComponentName = "Worker" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Persist", SourceComponentName = "Worker", TargetComponentName = "Database" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Verificatie", SourceComponentName = "Worker", TargetComponentName = "Externe KYC" });
        p.DesignNotes.Add(new DesignNoteModel
        {
            Kind = DesignNoteKind.Decision,
            Title = "Idempotency keys",
            Description = "Schrijf-operaties naar extern zijn idempotent gemaakt."
        });
        return p;
    }

    private static ProjectModel DesktopSensitive()
    {
        var p = new ProjectModel
        {
            Name = "Sjabloon: desktop + lokale data",
            Description = "Offline-first client met gevoelige lokale opslag.",
            SystemName = "Desktopclient",
            SystemType = SystemType.DesktopApp,
            DeploymentContext = DeploymentContext.DesktopOnly,
            InternetExposed = false,
            PersonalDataProcessed = true,
            SensitiveDataStored = true,
            HasAuthentication = true,
            LoggingMonitoringPresent = false
        };
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Endpoint" });
        p.Components.Add(new ComponentModel
        {
            Name = "Desktop UI",
            Tag = "frontend",
            TrustBoundaryName = "Endpoint",
            IsEntryPoint = true,
            StoresOrProcesses = nameof(DataSensitivity.Sensitive)
        });
        p.Components.Add(new ComponentModel
            { Name = "Lokale DB", Tag = "database", TrustBoundaryName = "Endpoint" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Lokale calls", SourceComponentName = "Desktop UI", TargetComponentName = "Lokale DB" });
        p.DesignNotes.Add(new DesignNoteModel
        {
            Kind = DesignNoteKind.OpenQuestion,
            Title = "Schijfencryptie",
            Description = "Is OS-schijfencryptie verplicht gesteld voor alle werkstations?"
        });
        return p;
    }

    private static ProjectModel IotCloud()
    {
        var p = new ProjectModel
        {
            Name = "Sjabloon: IoT + cloud",
            Description = "Devices, gateway en cloud backend.",
            SystemName = "Connected product",
            SystemType = SystemType.IotOrProduct,
            DeploymentContext = DeploymentContext.Hybrid,
            InternetExposed = true,
            ExternalApis = true,
            CriticalBusinessFunction = true,
            LoggingMonitoringPresent = true
        };
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Veld / device" });
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Cloud" });
        p.Components.Add(new ComponentModel
            { Name = "Device firmware", Tag = "frontend", TrustBoundaryName = "Veld / device" });
        p.Components.Add(new ComponentModel
            { Name = "Gateway", Tag = "api", TrustBoundaryName = "Veld / device", IsEntryPoint = true });
        p.Components.Add(new ComponentModel
            { Name = "Cloud ingest", Tag = "api", TrustBoundaryName = "Cloud", IsEntryPoint = true });
        p.Components.Add(new ComponentModel { Name = "Tijdreeks DB", Tag = "database", TrustBoundaryName = "Cloud" });
        p.Components.Add(new ComponentModel { Name = "Externe maps", Tag = "external", TrustBoundaryName = "Cloud" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Telemetrie", SourceComponentName = "Device firmware", TargetComponentName = "Gateway" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Upload", SourceComponentName = "Gateway", TargetComponentName = "Cloud ingest" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Opslag", SourceComponentName = "Cloud ingest", TargetComponentName = "Tijdreeks DB" });
        p.DataFlows.Add(new DataFlowModel
            { Label = "Kaarten", SourceComponentName = "Cloud ingest", TargetComponentName = "Externe maps" });
        return p;
    }
}
