using DesignGuard.Data.Entities;
using DesignGuard.Data.Mongo.Documents;

namespace DesignGuard.Data.Mongo;

/// <summary>Converteert BSON-document naar bestaande EF-entities zodat <see cref="ProjectMapper"/> hergebruikt wordt.</summary>
internal static class ProjectDocumentEntityConverter
{
    public static ProjectEntity ToEntity(ProjectDocument d)
    {
        var boundaries = d.TrustBoundaries.Select(t => new TrustBoundaryEntity
        {
            Id = t.Id,
            ProjectId = d.Id,
            Name = t.Name,
            Description = t.Description,
            Notes = t.Notes,
            ColorHint = t.ColorHint
        }).ToList();

        var boundaryById = boundaries.ToDictionary(b => b.Id);

        var components = d.Components.Select(c =>
        {
            var e = new ComponentEntity
            {
                Id = c.Id,
                ProjectId = d.Id,
                TrustBoundaryId = c.TrustBoundaryId,
                IsEntryPoint = c.IsEntryPoint,
                AssetClassification = c.AssetClassification,
                DataSensitivity = c.DataSensitivity,
                Notes = c.Notes,
                VisualX = c.VisualX,
                VisualY = c.VisualY,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
            };
            if (c.TrustBoundaryId is { } tid && boundaryById.TryGetValue(tid, out var tb))
                e.TrustBoundary = tb;
            return e;
        }).ToList();

        return new ProjectEntity
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            CreatedAtUtc = d.CreatedAtUtc,
            UpdatedAtUtc = d.UpdatedAtUtc,
            SystemName = d.SystemName,
            SystemType = d.SystemType,
            DeploymentContext = d.DeploymentContext,
            InternetExposed = d.InternetExposed,
            PersonalDataProcessed = d.PersonalDataProcessed,
            HasAuthentication = d.HasAuthentication,
            HasAdmin = d.HasAdmin,
            ExternalApis = d.ExternalApis,
            FileUpload = d.FileUpload,
            SensitiveDataStored = d.SensitiveDataStored,
            LoggingMonitoringPresent = d.LoggingMonitoringPresent,
            CriticalBusinessFunction = d.CriticalBusinessFunction,
            OpenIssuesSummary = d.OpenIssuesSummary,
            GovernanceSecurityOwner = d.GovernanceSecurityOwner,
            GovernanceTechnicalOwner = d.GovernanceTechnicalOwner,
            GovernanceComplianceStakeholder = d.GovernanceComplianceStakeholder,
            GovernanceReviewCadence = d.GovernanceReviewCadence,
            C4ElementsJson = d.C4ElementsJson,
            DismissedSuggestionKeysJson = d.DismissedSuggestionKeysJson,
            TrustBoundaries = boundaries,
            Components = components,
            DataFlows = d.DataFlows.Select(f => new DataFlowEntity
            {
                Id = f.Id,
                ProjectId = d.Id,
                FromComponentId = f.FromComponentId,
                ToComponentId = f.ToComponentId,
                Label = f.Label,
                Notes = f.Notes
            }).ToList(),
            UserRoles = d.UserRoles.Select(r => new UserRoleEntity
            {
                Id = r.Id,
                ProjectId = d.Id,
                Name = r.Name,
                Description = r.Description
            }).ToList(),
            Assets = d.Assets.Select(a => new AssetEntity
            {
                Id = a.Id,
                ProjectId = d.Id,
                Name = a.Name,
                Description = a.Description,
                Classification = a.Classification,
                Sensitivity = a.Sensitivity,
                Notes = a.Notes,
                RelatedComponentId = a.RelatedComponentId
            }).ToList(),
            DesignNotes = d.DesignNotes.Select(n => new DesignNoteEntity
            {
                Id = n.Id,
                ProjectId = d.Id,
                Kind = n.Kind,
                Title = n.Title,
                Description = n.Description,
                Notes = n.Notes
            }).ToList(),
            Controls = d.Controls.Select(c => new ControlEntity
            {
                Id = c.Id,
                ProjectId = d.Id,
                StableId = c.StableId,
                Title = c.Title,
                Category = c.Category,
                SourceTagsJson = c.SourceTagsJson,
                Description = c.Description,
                ImplementationGuidance = c.ImplementationGuidance,
                LinkedThreatStableId = c.LinkedThreatStableId,
                LinkedRequirementStableIdsJson = c.LinkedRequirementStableIdsJson,
                Status = c.Status,
                StatusNotes = c.StatusNotes,
                LibraryDefinitionId = c.LibraryDefinitionId,
                LinkedComponentIdsJson = string.IsNullOrWhiteSpace(c.LinkedComponentIdsJson)
                    ? "[]"
                    : c.LinkedComponentIdsJson
            }).ToList(),
            EntryPoints = d.EntryPoints.Select(x => new EntryPointEntity
            {
                Id = x.Id,
                ProjectId = d.Id,
                Name = x.Name,
                Description = x.Description,
                RelatedComponentId = x.RelatedComponentId,
                Notes = x.Notes,
                ExposureNotes = x.ExposureNotes
            }).ToList(),
            SensitiveDataItems = d.SensitiveDataItems.Select(x => new SensitiveDataEntity
            {
                Id = x.Id,
                ProjectId = d.Id,
                Name = x.Name,
                Category = x.Category,
                Description = x.Description,
                RelatedComponentId = x.RelatedComponentId,
                StorageLocation = x.StorageLocation,
                Notes = x.Notes
            }).ToList(),
            ReviewItems = d.ReviewItems.Select(x => new ReviewItemEntity
            {
                Id = x.Id,
                ProjectId = d.Id,
                SubjectKind = x.SubjectKind,
                SubjectStableId = x.SubjectStableId,
                SubjectTitle = x.SubjectTitle,
                Status = x.Status,
                Notes = x.Notes,
                Rationale = x.Rationale,
                Owner = x.Owner,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList(),
            Snapshots = d.Snapshots.Select(x => new SnapshotEntity
            {
                Id = x.Id,
                ProjectId = d.Id,
                Name = x.Name,
                CreatedAtUtc = x.CreatedAtUtc,
                SnapshotJson = x.SnapshotJson
            }).ToList(),
            Threats = d.Threats.Select(t => new ThreatEntity
            {
                Id = 0,
                ProjectId = d.Id,
                StableId = t.StableId,
                RuleFingerprint = t.RuleFingerprint,
                Origin = t.Origin,
                UserModified = t.UserModified,
                Title = t.Title,
                StrideCategory = t.StrideCategory,
                Severity = t.Severity,
                Status = t.Status,
                StatusChangedAtUtc = t.StatusChangedAtUtc,
                StatusChangedBy = t.StatusChangedBy ?? "",
                StatusChangeNote = t.StatusChangeNote ?? "",
                Notes = t.Notes,
                Description = t.Description,
                GenerationReason = t.GenerationReason,
                MitigationsJson = t.MitigationsJson,
                AffectedComponentsJson = t.AffectedComponentsJson,
                AffectedAssetsJson = t.AffectedAssetsJson,
                TriggerKeysJson = t.TriggerKeysJson,
                ExplanationJson = t.ExplanationJson,
                RelatedDesignNoteIdsJson = t.RelatedDesignNoteIdsJson,
                SourceAttributionJson = t.SourceAttributionJson
            }).ToList(),
            Requirements = d.Requirements.Select(r => new RequirementEntity
            {
                Id = 0,
                ProjectId = d.Id,
                StableId = r.StableId,
                RuleFingerprint = r.RuleFingerprint,
                Origin = r.Origin,
                UserModified = r.UserModified,
                Title = r.Title,
                Category = r.Category,
                SourceTagsJson = r.SourceTagsJson,
                Priority = r.Priority,
                Status = r.Status,
                StatusChangedAtUtc = r.StatusChangedAtUtc,
                StatusChangedBy = r.StatusChangedBy ?? "",
                StatusChangeNote = r.StatusChangeNote ?? "",
                Notes = r.Notes,
                PlainExplanation = r.PlainExplanation,
                WhyApplies = r.WhyApplies,
                ImplementationDirection = r.ImplementationDirection,
                TriggerKeysJson = r.TriggerKeysJson,
                LinkedThreatIdsJson = r.LinkedThreatIdsJson,
                ExplanationJson = r.ExplanationJson,
                RelatedDesignNoteIdsJson = r.RelatedDesignNoteIdsJson,
                SourceAttributionJson = r.SourceAttributionJson
            }).ToList()
        };
    }
}
