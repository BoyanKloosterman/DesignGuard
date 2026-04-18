using System.IO;
using System.Windows;
using DesignGuard.Configuration;
using DesignGuard.Data;
using DesignGuard.Data.Mongo;
using DesignGuard.Export;
using DesignGuard.Knowledge;
using DesignGuard.Rules;
using DesignGuard.Rules.RequirementRules;
using DesignGuard.Rules.ThreatRules;
using DesignGuard.Services;
using DesignGuard.Settings;
using DesignGuard.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace DesignGuard;

public partial class App : Application
{
    private Microsoft.Extensions.DependencyInjection.ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = LicenseType.Community;

        DevelopmentEnvFileLoader.TryApplyOptionalDotEnv();

        var services = new ServiceCollection();
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesignGuard");
        Directory.CreateDirectory(dir);

        services.AddSingleton<EnvironmentConfigurationProvider>();
        services.AddSingleton<IAppConfigurationService, AppConfigurationService>();
        services.AddSingleton<MongoConnectionFactory>();
        services.AddSingleton<IMongoDiagnosticsService, MongoDiagnosticsService>();
        services.AddSingleton<IProjectRepository, MongoProjectRepository>();
        services.AddSingleton<SqliteToMongoImportService>();

        services.AddSingleton(_ => new UserSettingsService(dir));
        services.AddSingleton(sp =>
        {
            var ks = new KnowledgePackService(sp.GetRequiredService<UserSettingsService>());
            ks.Reload();
            return ks;
        });
        services.AddSingleton<KnowledgePackRemoteSyncService>();
        services.AddSingleton<ControlLibraryService>();
        services.AddSingleton<ModelingSuggestionService>();
        services.AddSingleton<DiagramLayoutService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<DiagramRasterizer>();
        services.AddSingleton<C4ModelRasterizer>();
        services.AddSingleton<PdfReportService>();
        services.AddSingleton<AppSecurityReviewService>();
        services.AddSingleton<AnalysisMergeService>();
        services.AddSingleton<TraceabilityService>();
        services.AddSingleton<ProjectTemplateService>();
        services.AddSingleton<DesignValidationService>();
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
            new BusinessCriticalThreatRule(),
            new OperationalSecretsThreatRule(),
            new SupplyChainPipelineThreatRule()
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
            new ResilienceRequirementRule(),
            new SecretsManagementRequirementRule(),
            new BackupRestoreRequirementRule()
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
