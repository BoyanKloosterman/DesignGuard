using System.IO;
using System.Windows;
using DesignGuard.Data;
using DesignGuard.Export;
using DesignGuard.Knowledge;
using DesignGuard.Rules;
using DesignGuard.Rules.RequirementRules;
using DesignGuard.Rules.ThreatRules;
using DesignGuard.Services;
using DesignGuard.Settings;
using DesignGuard.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace DesignGuard;

public partial class App : Application
{
    // Volledige typenaam: geen verwarring met andere ServiceProvider-typen.
    private Microsoft.Extensions.DependencyInjection.ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = LicenseType.Community;
        var services = new ServiceCollection();
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesignGuard");
        Directory.CreateDirectory(dir);
        var cs = $"Data Source={Path.Combine(dir, "designguard-v3.db")}";
        services.AddDbContextFactory<DesignGuardDbContext>(o => o.UseSqlite(cs));
        services.AddSingleton<IProjectRepository, ProjectRepository>();
        services.AddSingleton(_ => new UserSettingsService(dir));
        services.AddSingleton(sp =>
        {
            var ks = new KnowledgePackService(sp.GetRequiredService<UserSettingsService>());
            ks.Reload();
            return ks;
        });
        services.AddSingleton<ControlLibraryService>();
        services.AddSingleton<ModelingSuggestionService>();
        services.AddSingleton<DiagramLayoutService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<DiagramRasterizer>();
        services.AddSingleton<PdfReportService>();
        services.AddSingleton<AppSecurityReviewService>();
        services.AddSingleton<AnalysisMergeService>();
        services.AddSingleton<TraceabilityService>();
        services.AddSingleton<ProjectTemplateService>();
        services.AddSingleton(sp => new ThreatGenerationService(new IThreatRule[]
        {
            new InternetExposureThreatRule(),
            new TrustBoundaryCrossingThreatRule(),
            new AuthenticationThreatRule(),
            new DatabaseThreatRule(),
            new ExternalApiThreatRule(),
            new AdminThreatRule(),
            new FileUploadThreatRule(),
            new PersonalDataThreatRule(),
            new TransportAndApiThreatRule(),
            new DenialOfServiceThreatRule(),
            new MissingLoggingThreatRule(),
            new RepudiationAuditThreatRule(),
            new BusinessCriticalThreatRule()
        }, sp.GetRequiredService<KnowledgePackService>()));
        services.AddSingleton(sp => new RequirementGenerationService(new IRequirementRule[]
        {
            new AuthenticationRequirementRule(),
            new SessionManagementRequirementRule(),
            new AuthorizationRequirementRule(),
            new AdministrativeAccessRequirementRule(),
            new DataProtectionRequirementRule(),
            new PrivacyMinimizationRequirementRule(),
            new LoggingRequirementRule(),
            new SecureDevelopmentRequirementRule(),
            new SecureConfigurationRequirementRule(),
            new InputValidationRequirementRule(),
            new TrustBoundaryRequirementRule(),
            new ResilienceRequirementRule()
        }, sp.GetRequiredService<KnowledgePackService>()));
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        _provider = services.BuildServiceProvider();
        var main = _provider.GetRequiredService<MainWindow>();
        main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }
}
