using DesignGuard.Knowledge;
using DesignGuard.Rules;
using DesignGuard.Rules.RequirementRules;
using DesignGuard.Rules.ThreatRules;
using DesignGuard.Services;
using DesignGuard.Settings;

namespace DesignGuard.Tests.Support;

/// <summary>Zelfde regelsets als App.xaml.cs; bij nieuwe regels in DI ook hier uitbreiden (regressie).</summary>
internal static class RegressionServiceFactory
{
    public static KnowledgePackService CreateKnowledgePack(UserSettingsService userSettings)
    {
        var kp = new KnowledgePackService(userSettings);
        kp.Reload();
        return kp;
    }

    public static ThreatGenerationService CreateThreatGeneration(KnowledgePackService kp) =>
        new(new IThreatRule[]
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
        }, kp);

    public static RequirementGenerationService CreateRequirementGeneration(KnowledgePackService kp) =>
        new(new IRequirementRule[]
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
        }, kp);
}
