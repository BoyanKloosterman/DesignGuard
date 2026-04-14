namespace DesignGuard.Rules;

/// <summary>Stabiele sleutels voor traceability (geen framework-claims).</summary>
public static class RuleTriggerKeys
{
    public const string InternetExposed = nameof(InternetExposed);
    public const string HasAuthentication = nameof(HasAuthentication);
    public const string AdminSurface = nameof(AdminSurface);
    public const string PersonalData = nameof(PersonalData);
    public const string SensitiveStorage = nameof(SensitiveStorage);
    public const string ExternalIntegration = nameof(ExternalIntegration);
    public const string FileUpload = nameof(FileUpload);
    public const string DatabasePresent = nameof(DatabasePresent);
    public const string ApiLayer = nameof(ApiLayer);
    public const string Frontend = nameof(Frontend);
    public const string TrustBoundaryCrossing = nameof(TrustBoundaryCrossing);
    public const string LoggingMonitoringMissing = nameof(LoggingMonitoringMissing);
    public const string CriticalBusiness = nameof(CriticalBusiness);
    public const string InternetFacingAdmin = nameof(InternetFacingAdmin);
}
