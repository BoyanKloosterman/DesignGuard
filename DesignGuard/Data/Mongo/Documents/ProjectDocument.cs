using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DesignGuard.Data.Mongo.Documents;

/// <summary>Eén Mongo-document per project (embedded werkruimte).</summary>
[BsonIgnoreExtraElements]
public sealed class ProjectDocument
{
    [BsonId]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public string SystemName { get; set; } = "";
    public string SystemType { get; set; } = "WebApp";
    public string DeploymentContext { get; set; } = "Cloud";

    public bool InternetExposed { get; set; } = true;
    public bool PersonalDataProcessed { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasAdmin { get; set; }
    public bool ExternalApis { get; set; }
    public bool FileUpload { get; set; }
    public bool SensitiveDataStored { get; set; }
    public bool LoggingMonitoringPresent { get; set; } = true;
    public bool CriticalBusinessFunction { get; set; }

    public string OpenIssuesSummary { get; set; } = "";
    public string DismissedSuggestionKeysJson { get; set; } = "[]";

    public string GovernanceSecurityOwner { get; set; } = "";
    public string GovernanceTechnicalOwner { get; set; } = "";
    public string GovernanceComplianceStakeholder { get; set; } = "";
    public string GovernanceReviewCadence { get; set; } = "";

    public string AssessmentGoal { get; set; } = "";
    public string AssessmentTestType { get; set; } = "Unspecified";
    public string ScopeIn { get; set; } = "";
    public string ScopeOut { get; set; } = "";
    public string RulesOfEngagementNotes { get; set; } = "";
    public string CompletedPlaybookItemIdsJson { get; set; } = "[]";

    public string C4ElementsJson { get; set; } = "[]";
    public string C4RelationsJson { get; set; } = "[]";

    public List<TrustBoundaryDoc> TrustBoundaries { get; set; } = new();
    public List<ComponentDoc> Components { get; set; } = new();
    public List<DataFlowDoc> DataFlows { get; set; } = new();
    public List<UserRoleDoc> UserRoles { get; set; } = new();
    public List<AssetDoc> Assets { get; set; } = new();
    public List<DesignNoteDoc> DesignNotes { get; set; } = new();
    public List<ControlDoc> Controls { get; set; } = new();
    public List<EntryPointDoc> EntryPoints { get; set; } = new();
    public List<SensitiveDataDoc> SensitiveDataItems { get; set; } = new();
    public List<ReviewItemDoc> ReviewItems { get; set; } = new();
    public List<SnapshotDoc> Snapshots { get; set; } = new();
    public List<ThreatDoc> Threats { get; set; } = new();
    public List<RequirementDoc> Requirements { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class TrustBoundaryDoc
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
    public string ColorHint { get; set; } = "#4472C4";
}

[BsonIgnoreExtraElements]
public sealed class ComponentDoc
{
    public int Id { get; set; }
    public int? TrustBoundaryId { get; set; }
    public bool IsEntryPoint { get; set; }
    public string AssetClassification { get; set; } = "Unspecified";
    public string DataSensitivity { get; set; } = "None";
    public string Notes { get; set; } = "";
    public double? VisualX { get; set; }
    public double? VisualY { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tag { get; set; } = "";
}

[BsonIgnoreExtraElements]
public sealed class DataFlowDoc
{
    public int Id { get; set; }
    public int FromComponentId { get; set; }
    public int ToComponentId { get; set; }
    public string Label { get; set; } = "";
    public string? Notes { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class UserRoleDoc
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

[BsonIgnoreExtraElements]
public sealed class AssetDoc
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Classification { get; set; } = "Unspecified";
    public string Sensitivity { get; set; } = "None";
    public string Notes { get; set; } = "";
    public int RelatedComponentId { get; set; }
    public string RelatedComponentIdsJson { get; set; } = "[]";
}

[BsonIgnoreExtraElements]
public sealed class DesignNoteDoc
{
    public int Id { get; set; }
    public int Kind { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
}

[BsonIgnoreExtraElements]
public sealed class ControlDoc
{
    public int Id { get; set; }
    public string StableId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceTagsJson { get; set; } = "[]";
    public string Description { get; set; } = "";
    public string ImplementationGuidance { get; set; } = "";
    public string LinkedThreatStableId { get; set; } = "";
    public string LinkedRequirementStableIdsJson { get; set; } = "[]";
    public int Status { get; set; }
    public string StatusNotes { get; set; } = "";
    public string LibraryDefinitionId { get; set; } = "";
    public string LinkedComponentIdsJson { get; set; } = "[]";
}

[BsonIgnoreExtraElements]
public sealed class EntryPointDoc
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int RelatedComponentId { get; set; }
    public string Notes { get; set; } = "";
    public string ExposureNotes { get; set; } = "";
}

[BsonIgnoreExtraElements]
public sealed class SensitiveDataDoc
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public int RelatedComponentId { get; set; }
    public string StorageLocation { get; set; } = "";
    public string Notes { get; set; } = "";
}

[BsonIgnoreExtraElements]
public sealed class ReviewItemDoc
{
    public int Id { get; set; }
    public int SubjectKind { get; set; }
    public string SubjectStableId { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public int Status { get; set; }
    public string Notes { get; set; } = "";
    public string Rationale { get; set; } = "";
    public string Owner { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class SnapshotDoc
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string SnapshotJson { get; set; } = "";
}

[BsonIgnoreExtraElements]
public sealed class ThreatDoc
{
    public string StableId { get; set; } = "";
    public string? RuleFingerprint { get; set; }
    public int Origin { get; set; }
    public bool UserModified { get; set; }
    public string Title { get; set; } = "";
    public int StrideCategory { get; set; }
    public int Severity { get; set; }
    public int Likelihood { get; set; }
    public int Impact { get; set; }
    public int Status { get; set; }
    public string? StatusChangedAtUtc { get; set; }
    public string StatusChangedBy { get; set; } = "";
    public string StatusChangeNote { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Description { get; set; } = "";
    public string GenerationReason { get; set; } = "";
    public string MitigationsJson { get; set; } = "[]";
    public string AffectedComponentsJson { get; set; } = "[]";
    public string AffectedAssetsJson { get; set; } = "[]";
    public string TriggerKeysJson { get; set; } = "[]";
    public string ExplanationJson { get; set; } = "{}";
    public string RelatedDesignNoteIdsJson { get; set; } = "[]";
    public string SourceAttributionJson { get; set; } = "{}";
}

[BsonIgnoreExtraElements]
public sealed class RequirementDoc
{
    public string StableId { get; set; } = "";
    public string? RuleFingerprint { get; set; }
    public int Origin { get; set; }
    public bool UserModified { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceTagsJson { get; set; } = "[]";
    public int Priority { get; set; }
    public int Status { get; set; }
    public string? StatusChangedAtUtc { get; set; }
    public string StatusChangedBy { get; set; } = "";
    public string StatusChangeNote { get; set; } = "";
    public string Notes { get; set; } = "";
    public string PlainExplanation { get; set; } = "";
    public string WhyApplies { get; set; } = "";
    public string ImplementationDirection { get; set; } = "";
    public string TriggerKeysJson { get; set; } = "[]";
    public string LinkedThreatIdsJson { get; set; } = "[]";
    public string ExplanationJson { get; set; } = "{}";
    public string RelatedDesignNoteIdsJson { get; set; } = "[]";
    public string SourceAttributionJson { get; set; } = "{}";
}
