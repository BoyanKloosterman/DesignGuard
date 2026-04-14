using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>
/// Voorbeeldproject: webshop met admin en externe betalingen.
/// </summary>
public static class DemoProjectFactory
{
    public static ProjectModel CreateDemoProject()
    {
        var p = new ProjectModel
        {
            Name = "Demo — Webshop (voorbeeld)",
            Description =
                "Voorbeeld: SPA-webshop met API, database, adminpaneel en externe betalingsprovider. Persoonsgegevens en uploads aanwezig.",
            SystemName = "Demo Webshop",
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

        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Publiek", Description = "Browser / CDN" });
        p.TrustBoundaries.Add(new TrustBoundaryModel { Name = "Backend", Description = "API en data" });

        p.Components.Add(new ComponentModel
        {
            Name = "Web Frontend",
            Description = "React/SPA in de browser",
            Tag = "frontend",
            TrustBoundaryName = "Publiek",
            IsEntryPoint = true
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Backend API",
            Description = "REST API en bedrijfslogica",
            Tag = "api",
            TrustBoundaryName = "Backend",
            IsEntryPoint = true
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Database",
            Description = "Relationele database voor orders en profielen",
            Tag = "database",
            TrustBoundaryName = "Backend",
            StoresOrProcesses = DataSensitivity.Personal
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Admin Panel",
            Description = "Beheer voor medewerkers",
            Tag = "admin",
            TrustBoundaryName = "Publiek",
            IsEntryPoint = true
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Payment Provider",
            Description = "Externe PSP voor betalingen",
            Tag = "external",
            TrustBoundaryName = "Publiek"
        });

        p.DataFlows.Add(new DataFlowModel
        {
            Label = "API-aanroepen (HTTPS)",
            SourceComponentName = "Web Frontend",
            TargetComponentName = "Backend API"
        });
        p.DataFlows.Add(new DataFlowModel
        {
            Label = "Queries/transacties",
            SourceComponentName = "Backend API",
            TargetComponentName = "Database"
        });
        p.DataFlows.Add(new DataFlowModel
        {
            Label = "Admin-acties",
            SourceComponentName = "Admin Panel",
            TargetComponentName = "Backend API"
        });
        p.DataFlows.Add(new DataFlowModel
        {
            Label = "Betaling starten / webhook",
            SourceComponentName = "Backend API",
            TargetComponentName = "Payment Provider"
        });

        p.UserRoles.Add(new UserRoleModel
        {
            Name = "Klant",
            Description = "Bestelt en beheert eigen gegevens"
        });
        p.UserRoles.Add(new UserRoleModel
        {
            Name = "Beheerder",
            Description = "Beheert catalogus, orders en gebruikers"
        });

        return p;
    }
}
