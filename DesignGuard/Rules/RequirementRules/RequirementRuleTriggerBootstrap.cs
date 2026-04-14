using DesignGuard.Models;

namespace DesignGuard.Rules.RequirementRules;

internal static class RequirementRuleTriggerBootstrap
{
    public static void Apply(string ruleName, RequirementModel r, SystemDesignContext ctx)
    {
        void add(string k)
        {
            if (!r.TriggerKeys.Contains(k))
                r.TriggerKeys.Add(k);
        }

        switch (ruleName)
        {
            case nameof(AuthenticationRequirementRule):
                add(RuleTriggerKeys.HasAuthentication);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(AuthorizationRequirementRule):
                add(RuleTriggerKeys.HasAuthentication);
                if (ctx.HasAdminSurface) add(RuleTriggerKeys.AdminSurface);
                break;
            case nameof(SessionManagementRequirementRule):
                add(RuleTriggerKeys.HasAuthentication);
                if (ctx.HasFrontend) add(RuleTriggerKeys.Frontend);
                break;
            case nameof(DataProtectionRequirementRule):
                if (ctx.Project.PersonalDataProcessed) add(RuleTriggerKeys.PersonalData);
                if (ctx.Project.SensitiveDataStored) add(RuleTriggerKeys.SensitiveStorage);
                break;
            case nameof(LoggingRequirementRule):
                add(RuleTriggerKeys.HasAuthentication);
                if (ctx.HasAdminSurface) add(RuleTriggerKeys.AdminSurface);
                if (!ctx.Project.LoggingMonitoringPresent) add(RuleTriggerKeys.LoggingMonitoringMissing);
                break;
            case nameof(SecureDevelopmentRequirementRule):
                if (ctx.HasExternalService) add(RuleTriggerKeys.ExternalIntegration);
                break;
            case nameof(InputValidationRequirementRule):
                if (ctx.HasApiLayer) add(RuleTriggerKeys.ApiLayer);
                if (ctx.HasFrontend) add(RuleTriggerKeys.Frontend);
                if (ctx.Project.FileUpload) add(RuleTriggerKeys.FileUpload);
                break;
            case nameof(ResilienceRequirementRule):
                if (ctx.HasApiLayer) add(RuleTriggerKeys.ApiLayer);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(SecureConfigurationRequirementRule):
                add(RuleTriggerKeys.ApiLayer);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(PrivacyMinimizationRequirementRule):
                if (ctx.Project.PersonalDataProcessed) add(RuleTriggerKeys.PersonalData);
                break;
            case nameof(AdministrativeAccessRequirementRule):
                add(RuleTriggerKeys.AdminSurface);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetFacingAdmin);
                break;
            case nameof(TrustBoundaryRequirementRule):
                if (ctx.HasTrustBoundaryCrossing) add(RuleTriggerKeys.TrustBoundaryCrossing);
                break;
        }
    }
}
