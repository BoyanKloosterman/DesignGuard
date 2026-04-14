using DesignGuard.Models;

namespace DesignGuard.Rules.ThreatRules;

internal static class RuleTriggerBootstrap
{
    public static void Apply(string ruleName, ThreatModel t, SystemDesignContext ctx)
    {
        void add(string k)
        {
            if (!t.TriggerKeys.Contains(k))
                t.TriggerKeys.Add(k);
        }

        switch (ruleName)
        {
            case nameof(AuthenticationThreatRule):
                add(RuleTriggerKeys.HasAuthentication);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(DatabaseThreatRule):
                add(RuleTriggerKeys.DatabasePresent);
                if (ctx.HasApiLayer) add(RuleTriggerKeys.ApiLayer);
                break;
            case nameof(ExternalApiThreatRule):
                add(RuleTriggerKeys.ExternalIntegration);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(AdminThreatRule):
                add(RuleTriggerKeys.AdminSurface);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetFacingAdmin);
                break;
            case nameof(FileUploadThreatRule):
                add(RuleTriggerKeys.FileUpload);
                if (ctx.HasApiLayer) add(RuleTriggerKeys.ApiLayer);
                break;
            case nameof(PersonalDataThreatRule):
                add(RuleTriggerKeys.PersonalData);
                if (ctx.Project.SensitiveDataStored) add(RuleTriggerKeys.SensitiveStorage);
                break;
            case nameof(TransportAndApiThreatRule):
                add(RuleTriggerKeys.ApiLayer);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(DenialOfServiceThreatRule):
                add(RuleTriggerKeys.InternetExposed);
                if (ctx.HasFrontend) add(RuleTriggerKeys.Frontend);
                if (ctx.HasApiLayer) add(RuleTriggerKeys.ApiLayer);
                break;
            case nameof(RepudiationAuditThreatRule):
                add(RuleTriggerKeys.HasAuthentication);
                if (ctx.HasAdminSurface) add(RuleTriggerKeys.AdminSurface);
                if (!ctx.Project.LoggingMonitoringPresent) add(RuleTriggerKeys.LoggingMonitoringMissing);
                break;
            case nameof(TrustBoundaryCrossingThreatRule):
                add(RuleTriggerKeys.TrustBoundaryCrossing);
                if (ctx.Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(InternetExposureThreatRule):
                add(RuleTriggerKeys.InternetExposed);
                break;
            case nameof(MissingLoggingThreatRule):
                add(RuleTriggerKeys.LoggingMonitoringMissing);
                if (ctx.HasAdminSurface) add(RuleTriggerKeys.AdminSurface);
                break;
            case nameof(BusinessCriticalThreatRule):
                add(RuleTriggerKeys.CriticalBusiness);
                break;
        }
    }
}
