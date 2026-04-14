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
            PersonalDataProcessed = true,
            HasAuthentication = true,
            HasAdmin = true,
            ExternalApis = true,
            FileUpload = true,
            SensitiveDataStored = true
        };

        p.Components.Add(new ComponentModel
        {
            Name = "Web Frontend",
            Description = "React/SPA in de browser",
            Tag = "frontend"
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Backend API",
            Description = "REST API en bedrijfslogica",
            Tag = "api"
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Database",
            Description = "Relationele database voor orders en profielen",
            Tag = "database"
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Admin Panel",
            Description = "Beheer voor medewerkers",
            Tag = "admin"
        });
        p.Components.Add(new ComponentModel
        {
            Name = "Payment Provider",
            Description = "Externe PSP voor betalingen",
            Tag = "external"
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
