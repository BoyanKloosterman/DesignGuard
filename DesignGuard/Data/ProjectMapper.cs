using System.Globalization;
using DesignGuard.Data.Entities;
using DesignGuard.Models;

namespace DesignGuard.Data;

internal static class ProjectMapper
{
    internal static DateTime? ParseOptionalUtc(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    internal static string? FormatOptionalUtc(DateTime? dt) =>
        dt == null ? null : dt.Value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    public static ProjectModel ToModel(ProjectEntity e)
    {
        return new ProjectModel
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc,
            SystemName = e.SystemName,
            SystemType = Enum.TryParse<SystemType>(e.SystemType, out var st) ? st : SystemType.WebApp,
            DeploymentContext = Enum.TryParse<DeploymentContext>(e.DeploymentContext, out var dc)
                ? dc
                : DeploymentContext.Cloud,
            InternetExposed = e.InternetExposed,
            PersonalDataProcessed = e.PersonalDataProcessed,
            HasAuthentication = e.HasAuthentication,
            HasAdmin = e.HasAdmin,
            ExternalApis = e.ExternalApis,
            FileUpload = e.FileUpload,
            SensitiveDataStored = e.SensitiveDataStored,
            LoggingMonitoringPresent = e.LoggingMonitoringPresent,
            CriticalBusinessFunction = e.CriticalBusinessFunction,
            OpenIssuesSummary = e.OpenIssuesSummary,
            GovernanceSecurityOwner = e.GovernanceSecurityOwner,
            GovernanceTechnicalOwner = e.GovernanceTechnicalOwner,
            GovernanceComplianceStakeholder = e.GovernanceComplianceStakeholder,
            GovernanceReviewCadence = e.GovernanceReviewCadence,
            C4Elements = JsonBlobs.C4ElementList(e.C4ElementsJson),
            DismissedSuggestionKeys = JsonBlobs.StringList(e.DismissedSuggestionKeysJson),
            TrustBoundaries = e.TrustBoundaries.OrderBy(t => t.Id).Select(t => new TrustBoundaryModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Notes = t.Notes,
                ColorHint = t.ColorHint
            }).ToList(),
            Components = e.Components.Select(c => new ComponentModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag,
                TrustBoundaryId = c.TrustBoundaryId,
                TrustBoundaryName = c.TrustBoundary?.Name,
                IsEntryPoint = c.IsEntryPoint,
                AssetClassification = string.IsNullOrWhiteSpace(c.AssetClassification)
                    ? nameof(AssetClassification.Unspecified)
                    : c.AssetClassification,
                StoresOrProcesses = string.IsNullOrWhiteSpace(c.DataSensitivity)
                    ? nameof(DataSensitivity.None)
                    : c.DataSensitivity,
                Notes = c.Notes,
                VisualX = c.VisualX,
                VisualY = c.VisualY
            }).ToList(),
            DataFlows = e.DataFlows.Select(f => new DataFlowModel
            {
                Id = f.Id,
                FromComponentId = f.FromComponentId,
                ToComponentId = f.ToComponentId,
                Label = f.Label,
                Notes = f.Notes
            }).ToList(),
            UserRoles = e.UserRoles.Select(r => new UserRoleModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            }).ToList(),
            Assets = e.Assets.Select(a => new AssetModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Classification = string.IsNullOrWhiteSpace(a.Classification)
                    ? nameof(AssetClassification.Unspecified)
                    : a.Classification,
                Sensitivity = string.IsNullOrWhiteSpace(a.Sensitivity)
                    ? nameof(DataSensitivity.None)
                    : a.Sensitivity,
                Notes = a.Notes,
                RelatedComponentId = a.RelatedComponentId
            }).ToList(),
            DesignNotes = e.DesignNotes.Select(n => new DesignNoteModel
            {
                Id = n.Id,
                Kind = (DesignNoteKind)n.Kind,
                Title = n.Title,
                Description = n.Description,
                Notes = n.Notes
            }).ToList(),
            Controls = e.Controls.Select(c => new ControlModel
            {
                Id = c.Id,
                StableId = c.StableId,
                Title = c.Title,
                Category = c.Category,
                SourceTags = JsonBlobs.StringList(c.SourceTagsJson),
                Description = c.Description,
                ImplementationGuidance = c.ImplementationGuidance,
                LinkedThreatStableId = c.LinkedThreatStableId,
                LinkedRequirementStableIds = JsonBlobs.StringList(c.LinkedRequirementStableIdsJson),
                Status = Enum.IsDefined(typeof(ControlLifecycleStatus), c.Status)
                    ? (ControlLifecycleStatus)c.Status
                    : ControlLifecycleStatus.Draft,
                StatusNotes = c.StatusNotes,
                LibraryDefinitionId = c.LibraryDefinitionId,
                LinkedComponentIds = JsonBlobs.IntList(c.LinkedComponentIdsJson)
            }).ToList(),
            EntryPoints = e.EntryPoints.OrderBy(x => x.Id).Select(x => new EntryPointModel
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                RelatedComponentId = x.RelatedComponentId,
                Notes = x.Notes,
                ExposureNotes = x.ExposureNotes
            }).ToList(),
            SensitiveDataItems = e.SensitiveDataItems.OrderBy(x => x.Id).Select(x => new SensitiveDataModel
            {
                Id = x.Id,
                Name = x.Name,
                Category = x.Category,
                Description = x.Description,
                RelatedComponentId = x.RelatedComponentId,
                StorageLocation = x.StorageLocation,
                Notes = x.Notes
            }).ToList(),
            ReviewItems = e.ReviewItems.OrderBy(x => x.Id).Select(x => new ReviewItemModel
            {
                Id = x.Id,
                SubjectKind = Enum.IsDefined(typeof(ReviewSubjectKind), x.SubjectKind)
                    ? (ReviewSubjectKind)x.SubjectKind
                    : ReviewSubjectKind.OpenQuestion,
                SubjectStableId = x.SubjectStableId,
                SubjectTitle = x.SubjectTitle,
                Status = Enum.IsDefined(typeof(ReviewWorkflowStatus), x.Status)
                    ? (ReviewWorkflowStatus)x.Status
                    : ReviewWorkflowStatus.Draft,
                Notes = x.Notes,
                Rationale = x.Rationale,
                Owner = x.Owner,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList(),
            Snapshots = e.Snapshots.OrderByDescending(x => x.CreatedAtUtc).Select(x => new SnapshotModel
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAtUtc = x.CreatedAtUtc,
                SnapshotJson = x.SnapshotJson
            }).ToList(),
            Threats = e.Threats.Select(ToThreat).ToList(),
            Requirements = e.Requirements.Select(ToRequirement).ToList()
        };
    }

    public static ThreatModel ToThreat(ThreatEntity e) => new()
    {
        Id = e.StableId,
        RuleFingerprint = e.RuleFingerprint,
        Origin = (ThreatOrigin)e.Origin,
        UserModified = e.UserModified,
        Title = e.Title,
        StrideCategory = (StrideCategory)e.StrideCategory,
        Severity = (SeverityEstimate)e.Severity,
        Status = (ThreatStatus)e.Status,
        StatusChangedAtUtc = ParseOptionalUtc(e.StatusChangedAtUtc),
        StatusChangedBy = e.StatusChangedBy ?? "",
        StatusChangeNote = e.StatusChangeNote ?? "",
        Notes = e.Notes,
        Description = e.Description,
        GenerationReason = e.GenerationReason,
        SuggestedMitigations = JsonBlobs.StringList(e.MitigationsJson),
        AffectedComponents = JsonBlobs.StringList(e.AffectedComponentsJson),
        AffectedAssets = JsonBlobs.StringList(e.AffectedAssetsJson),
        TriggerKeys = JsonBlobs.StringList(e.TriggerKeysJson),
        Explanation = JsonBlobs.Explanation(e.ExplanationJson),
        RelatedDesignNoteIds = JsonBlobs.IntList(e.RelatedDesignNoteIdsJson),
        SourceAttribution = JsonBlobs.SourceAttribution(e.SourceAttributionJson)
    };

    public static RequirementModel ToRequirement(RequirementEntity e) => new()
    {
        Id = e.StableId,
        RuleFingerprint = e.RuleFingerprint,
        Origin = (RequirementOrigin)e.Origin,
        UserModified = e.UserModified,
        Title = e.Title,
        Category = e.Category,
        SourceTags = JsonBlobs.StringList(e.SourceTagsJson),
        Priority = (RequirementPriority)e.Priority,
        Status = (RequirementStatus)e.Status,
        StatusChangedAtUtc = ParseOptionalUtc(e.StatusChangedAtUtc),
        StatusChangedBy = e.StatusChangedBy ?? "",
        StatusChangeNote = e.StatusChangeNote ?? "",
        Notes = e.Notes,
        PlainExplanation = e.PlainExplanation,
        WhyApplies = e.WhyApplies,
        ImplementationDirection = e.ImplementationDirection,
        TriggerKeys = JsonBlobs.StringList(e.TriggerKeysJson),
        LinkedThreatIds = JsonBlobs.StringList(e.LinkedThreatIdsJson),
        Explanation = JsonBlobs.Explanation(e.ExplanationJson),
        RelatedDesignNoteIds = JsonBlobs.IntList(e.RelatedDesignNoteIdsJson),
        SourceAttribution = JsonBlobs.SourceAttribution(e.SourceAttributionJson)
    };

    public static ThreatEntity ToThreatEntity(ThreatModel m, int projectId) => new()
    {
        ProjectId = projectId,
        StableId = string.IsNullOrWhiteSpace(m.Id) ? Guid.NewGuid().ToString("N") : m.Id,
        RuleFingerprint = m.RuleFingerprint,
        Origin = (int)m.Origin,
        UserModified = m.UserModified,
        Title = m.Title,
        StrideCategory = (int)m.StrideCategory,
        Severity = (int)m.Severity,
        Status = (int)m.Status,
        StatusChangedAtUtc = FormatOptionalUtc(m.StatusChangedAtUtc),
        StatusChangedBy = m.StatusChangedBy ?? "",
        StatusChangeNote = m.StatusChangeNote ?? "",
        Notes = m.Notes,
        Description = m.Description,
        GenerationReason = m.GenerationReason,
        MitigationsJson = JsonBlobs.Serialize(m.SuggestedMitigations),
        AffectedComponentsJson = JsonBlobs.Serialize(m.AffectedComponents),
        AffectedAssetsJson = JsonBlobs.Serialize(m.AffectedAssets),
        TriggerKeysJson = JsonBlobs.Serialize(m.TriggerKeys),
        ExplanationJson = JsonBlobs.Serialize(m.Explanation),
        RelatedDesignNoteIdsJson = JsonBlobs.Serialize(m.RelatedDesignNoteIds),
        SourceAttributionJson = JsonBlobs.Serialize(m.SourceAttribution)
    };

    public static RequirementEntity ToRequirementEntity(RequirementModel m, int projectId) => new()
    {
        ProjectId = projectId,
        StableId = string.IsNullOrWhiteSpace(m.Id) ? Guid.NewGuid().ToString("N") : m.Id,
        RuleFingerprint = m.RuleFingerprint,
        Origin = (int)m.Origin,
        UserModified = m.UserModified,
        Title = m.Title,
        Category = m.Category,
        SourceTagsJson = JsonBlobs.Serialize(m.SourceTags),
        Priority = (int)m.Priority,
        Status = (int)m.Status,
        StatusChangedAtUtc = FormatOptionalUtc(m.StatusChangedAtUtc),
        StatusChangedBy = m.StatusChangedBy ?? "",
        StatusChangeNote = m.StatusChangeNote ?? "",
        Notes = m.Notes,
        PlainExplanation = m.PlainExplanation,
        WhyApplies = m.WhyApplies,
        ImplementationDirection = m.ImplementationDirection,
        TriggerKeysJson = JsonBlobs.Serialize(m.TriggerKeys),
        LinkedThreatIdsJson = JsonBlobs.Serialize(m.LinkedThreatIds),
        ExplanationJson = JsonBlobs.Serialize(m.Explanation),
        RelatedDesignNoteIdsJson = JsonBlobs.Serialize(m.RelatedDesignNoteIds),
        SourceAttributionJson = JsonBlobs.Serialize(m.SourceAttribution)
    };
}
