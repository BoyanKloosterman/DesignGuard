using DesignGuard.Data.Entities;
using DesignGuard.Models;

namespace DesignGuard.Data;

internal static class ProjectMapper
{
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
                AssetClassification = Enum.TryParse<AssetClassification>(c.AssetClassification, out var ac)
                    ? ac
                    : AssetClassification.Unspecified,
                StoresOrProcesses = Enum.TryParse<DataSensitivity>(c.DataSensitivity, out var ds)
                    ? ds
                    : DataSensitivity.None,
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
                Classification = Enum.TryParse<AssetClassification>(a.Classification, out var cl)
                    ? cl
                    : AssetClassification.Unspecified,
                Sensitivity = Enum.TryParse<DataSensitivity>(a.Sensitivity, out var se)
                    ? se
                    : DataSensitivity.None,
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
                Title = c.Title,
                Description = c.Description,
                LinkedThreatStableId = c.LinkedThreatStableId,
                StatusNotes = c.StatusNotes
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
        Notes = e.Notes,
        Description = e.Description,
        GenerationReason = e.GenerationReason,
        SuggestedMitigations = JsonBlobs.StringList(e.MitigationsJson),
        AffectedComponents = JsonBlobs.StringList(e.AffectedComponentsJson),
        AffectedAssets = JsonBlobs.StringList(e.AffectedAssetsJson),
        TriggerKeys = JsonBlobs.StringList(e.TriggerKeysJson),
        Explanation = JsonBlobs.Explanation(e.ExplanationJson),
        RelatedDesignNoteIds = JsonBlobs.IntList(e.RelatedDesignNoteIdsJson)
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
        Notes = e.Notes,
        PlainExplanation = e.PlainExplanation,
        WhyApplies = e.WhyApplies,
        ImplementationDirection = e.ImplementationDirection,
        TriggerKeys = JsonBlobs.StringList(e.TriggerKeysJson),
        LinkedThreatIds = JsonBlobs.StringList(e.LinkedThreatIdsJson),
        Explanation = JsonBlobs.Explanation(e.ExplanationJson),
        RelatedDesignNoteIds = JsonBlobs.IntList(e.RelatedDesignNoteIdsJson)
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
        Notes = m.Notes,
        Description = m.Description,
        GenerationReason = m.GenerationReason,
        MitigationsJson = JsonBlobs.Serialize(m.SuggestedMitigations),
        AffectedComponentsJson = JsonBlobs.Serialize(m.AffectedComponents),
        AffectedAssetsJson = JsonBlobs.Serialize(m.AffectedAssets),
        TriggerKeysJson = JsonBlobs.Serialize(m.TriggerKeys),
        ExplanationJson = JsonBlobs.Serialize(m.Explanation),
        RelatedDesignNoteIdsJson = JsonBlobs.Serialize(m.RelatedDesignNoteIds)
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
        Notes = m.Notes,
        PlainExplanation = m.PlainExplanation,
        WhyApplies = m.WhyApplies,
        ImplementationDirection = m.ImplementationDirection,
        TriggerKeysJson = JsonBlobs.Serialize(m.TriggerKeys),
        LinkedThreatIdsJson = JsonBlobs.Serialize(m.LinkedThreatIds),
        ExplanationJson = JsonBlobs.Serialize(m.Explanation),
        RelatedDesignNoteIdsJson = JsonBlobs.Serialize(m.RelatedDesignNoteIds)
    };
}
