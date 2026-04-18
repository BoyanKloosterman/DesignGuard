using DesignGuard.Data.Mongo.Documents;
using DesignGuard.Models;

namespace DesignGuard.Data.Mongo;

/// <summary>Bouwt een <see cref="ProjectDocument"/> vanuit het domeinmodel (zelfde remapping als SQLite-save).</summary>
internal static class ProjectDocumentBuilder
{
    public static ProjectDocument Build(ProjectModel m, int projectId, DateTime createdAtUtc)
    {
        var now = DateTime.UtcNow;
        var doc = new ProjectDocument
        {
            Id = projectId,
            Name = m.Name,
            Description = m.Description,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = now,
            SystemName = m.SystemName,
            SystemType = m.SystemType.ToString(),
            DeploymentContext = m.DeploymentContext.ToString(),
            InternetExposed = m.InternetExposed,
            PersonalDataProcessed = m.PersonalDataProcessed,
            HasAuthentication = m.HasAuthentication,
            HasAdmin = m.HasAdmin,
            ExternalApis = m.ExternalApis,
            FileUpload = m.FileUpload,
            SensitiveDataStored = m.SensitiveDataStored,
            LoggingMonitoringPresent = m.LoggingMonitoringPresent,
            CriticalBusinessFunction = m.CriticalBusinessFunction,
            OpenIssuesSummary = m.OpenIssuesSummary,
            GovernanceSecurityOwner = m.GovernanceSecurityOwner,
            GovernanceTechnicalOwner = m.GovernanceTechnicalOwner,
            GovernanceComplianceStakeholder = m.GovernanceComplianceStakeholder,
            GovernanceReviewCadence = m.GovernanceReviewCadence,
            C4ElementsJson = JsonBlobs.Serialize(m.C4Elements),
            DismissedSuggestionKeysJson = JsonBlobs.Serialize(m.DismissedSuggestionKeys)
        };

        foreach (var tb in m.TrustBoundaries)
        {
            doc.TrustBoundaries.Add(new TrustBoundaryDoc
            {
                Name = tb.Name,
                Description = tb.Description,
                Notes = tb.Notes,
                ColorHint = string.IsNullOrWhiteSpace(tb.ColorHint) ? "#4472C4" : tb.ColorHint
            });
        }

        AssignIds(doc.TrustBoundaries);
        var boundaryByName = doc.TrustBoundaries.ToDictionary(b => b.Name, b => b.Id, StringComparer.Ordinal);

        foreach (var c in m.Components)
        {
            int? tbId = null;
            if (c.TrustBoundaryId is { } oldTbId && oldTbId != 0)
            {
                var tbName = m.TrustBoundaries.FirstOrDefault(t => t.Id == oldTbId)?.Name;
                if (tbName != null && boundaryByName.TryGetValue(tbName, out var nid))
                    tbId = nid;
            }

            if (tbId == null && !string.IsNullOrWhiteSpace(c.TrustBoundaryName) &&
                boundaryByName.TryGetValue(c.TrustBoundaryName, out var byName))
                tbId = byName;

            doc.Components.Add(new ComponentDoc
            {
                TrustBoundaryId = tbId,
                IsEntryPoint = c.IsEntryPoint,
                AssetClassification = string.IsNullOrWhiteSpace(c.AssetClassification)
                    ? nameof(AssetClassification.Unspecified)
                    : c.AssetClassification,
                DataSensitivity = string.IsNullOrWhiteSpace(c.StoresOrProcesses)
                    ? nameof(DataSensitivity.None)
                    : c.StoresOrProcesses,
                Notes = c.Notes,
                VisualX = c.VisualX,
                VisualY = c.VisualY,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
            });
        }

        AssignIds(doc.Components);
        var nameToCompId = doc.Components.ToDictionary(c => c.Name, c => c.Id, StringComparer.Ordinal);

        foreach (var r in m.UserRoles)
        {
            doc.UserRoles.Add(new UserRoleDoc { Name = r.Name, Description = r.Description });
        }

        AssignIds(doc.UserRoles);

        foreach (var f in m.DataFlows)
        {
            ResolveFlowEndpoints(f, nameToCompId);
            if (f.FromComponentId == 0 || f.ToComponentId == 0) continue;
            doc.DataFlows.Add(new DataFlowDoc
            {
                FromComponentId = f.FromComponentId,
                ToComponentId = f.ToComponentId,
                Label = f.Label,
                Notes = f.Notes
            });
        }

        AssignIds(doc.DataFlows);

        foreach (var a in m.Assets)
        {
            var related = 0;
            if (a.RelatedComponentId != 0)
            {
                var compName = m.Components.FirstOrDefault(c => c.Id == a.RelatedComponentId)?.Name;
                if (compName != null && nameToCompId.TryGetValue(compName, out var cid))
                    related = cid;
            }

            doc.Assets.Add(new AssetDoc
            {
                Name = a.Name,
                Description = a.Description,
                Classification = string.IsNullOrWhiteSpace(a.Classification)
                    ? nameof(AssetClassification.Unspecified)
                    : a.Classification,
                Sensitivity = string.IsNullOrWhiteSpace(a.Sensitivity)
                    ? nameof(DataSensitivity.None)
                    : a.Sensitivity,
                Notes = a.Notes,
                RelatedComponentId = related
            });
        }

        AssignIds(doc.Assets);

        foreach (var n in m.DesignNotes)
        {
            doc.DesignNotes.Add(new DesignNoteDoc
            {
                Kind = (int)n.Kind,
                Title = n.Title,
                Description = n.Description,
                Notes = n.Notes
            });
        }

        AssignIds(doc.DesignNotes);

        foreach (var c in m.Controls)
        {
            var linkedIds = (c.LinkedComponentIds ?? new List<int>())
                .Select(oldId => RemapComponentId(m, oldId, nameToCompId))
                .Where(id => id != 0)
                .Distinct()
                .ToList();
            doc.Controls.Add(new ControlDoc
            {
                StableId = string.IsNullOrWhiteSpace(c.StableId) ? Guid.NewGuid().ToString("N") : c.StableId,
                Title = c.Title,
                Category = c.Category,
                SourceTagsJson = JsonBlobs.Serialize(c.SourceTags),
                Description = c.Description,
                ImplementationGuidance = c.ImplementationGuidance,
                LinkedThreatStableId = c.LinkedThreatStableId,
                LinkedRequirementStableIdsJson = JsonBlobs.Serialize(c.LinkedRequirementStableIds),
                Status = (int)c.Status,
                StatusNotes = c.StatusNotes,
                LibraryDefinitionId = c.LibraryDefinitionId,
                LinkedComponentIdsJson = JsonBlobs.Serialize(linkedIds)
            });
        }

        AssignIds(doc.Controls);

        foreach (var ep in m.EntryPoints)
        {
            var related = RemapComponentId(m, ep.RelatedComponentId, nameToCompId);
            doc.EntryPoints.Add(new EntryPointDoc
            {
                Name = ep.Name,
                Description = ep.Description,
                RelatedComponentId = related,
                Notes = ep.Notes,
                ExposureNotes = ep.ExposureNotes
            });
        }

        AssignIds(doc.EntryPoints);

        foreach (var s in m.SensitiveDataItems)
        {
            var related = RemapComponentId(m, s.RelatedComponentId, nameToCompId);
            doc.SensitiveDataItems.Add(new SensitiveDataDoc
            {
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                RelatedComponentId = related,
                StorageLocation = s.StorageLocation,
                Notes = s.Notes
            });
        }

        AssignIds(doc.SensitiveDataItems);

        foreach (var r in m.ReviewItems)
        {
            doc.ReviewItems.Add(new ReviewItemDoc
            {
                SubjectKind = (int)r.SubjectKind,
                SubjectStableId = r.SubjectStableId,
                SubjectTitle = r.SubjectTitle,
                Status = (int)r.Status,
                Notes = r.Notes,
                Rationale = r.Rationale,
                Owner = r.Owner,
                CreatedAtUtc = r.CreatedAtUtc == default ? DateTime.UtcNow : r.CreatedAtUtc
            });
        }

        AssignIds(doc.ReviewItems);

        foreach (var s in m.Snapshots)
        {
            doc.Snapshots.Add(new SnapshotDoc
            {
                Name = s.Name,
                CreatedAtUtc = s.CreatedAtUtc == default ? DateTime.UtcNow : s.CreatedAtUtc,
                SnapshotJson = s.SnapshotJson
            });
        }

        AssignIds(doc.Snapshots);

        var noteMap = BuildDesignNoteIdMap(m.DesignNotes, doc.DesignNotes);
        foreach (var t in m.Threats)
        {
            doc.Threats.Add(ToThreatDoc(t, JsonBlobs.Serialize(RemapIds(t.RelatedDesignNoteIds, noteMap))));
        }

        foreach (var r in m.Requirements)
        {
            doc.Requirements.Add(ToRequirementDoc(r, JsonBlobs.Serialize(RemapIds(r.RelatedDesignNoteIds, noteMap))));
        }

        return doc;
    }

    private static void AssignIds(List<TrustBoundaryDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<ComponentDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<DataFlowDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<UserRoleDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<AssetDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<DesignNoteDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<ControlDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<EntryPointDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<SensitiveDataDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<ReviewItemDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIds(List<SnapshotDoc> items) => AssignIdsCore(items, (x, id) => x.Id = id);

    private static void AssignIdsCore<T>(List<T> items, Action<T, int> setId)
    {
        for (var i = 0; i < items.Count; i++)
            setId(items[i], i + 1);
    }

    private static ThreatDoc ToThreatDoc(ThreatModel t, string relatedNoteIdsJson) => new()
    {
        StableId = string.IsNullOrWhiteSpace(t.Id) ? Guid.NewGuid().ToString("N") : t.Id,
        RuleFingerprint = t.RuleFingerprint,
        Origin = (int)t.Origin,
        UserModified = t.UserModified,
        Title = t.Title,
        StrideCategory = (int)t.StrideCategory,
        Severity = (int)t.Severity,
        Status = (int)t.Status,
        Notes = t.Notes,
        Description = t.Description,
        GenerationReason = t.GenerationReason,
        MitigationsJson = JsonBlobs.Serialize(t.SuggestedMitigations),
        AffectedComponentsJson = JsonBlobs.Serialize(t.AffectedComponents),
        AffectedAssetsJson = JsonBlobs.Serialize(t.AffectedAssets),
        TriggerKeysJson = JsonBlobs.Serialize(t.TriggerKeys),
        ExplanationJson = JsonBlobs.Serialize(t.Explanation),
        RelatedDesignNoteIdsJson = relatedNoteIdsJson,
        SourceAttributionJson = JsonBlobs.Serialize(t.SourceAttribution)
    };

    private static RequirementDoc ToRequirementDoc(RequirementModel r, string relatedNoteIdsJson) => new()
    {
        StableId = string.IsNullOrWhiteSpace(r.Id) ? Guid.NewGuid().ToString("N") : r.Id,
        RuleFingerprint = r.RuleFingerprint,
        Origin = (int)r.Origin,
        UserModified = r.UserModified,
        Title = r.Title,
        Category = r.Category,
        SourceTagsJson = JsonBlobs.Serialize(r.SourceTags),
        Priority = (int)r.Priority,
        Status = (int)r.Status,
        Notes = r.Notes,
        PlainExplanation = r.PlainExplanation,
        WhyApplies = r.WhyApplies,
        ImplementationDirection = r.ImplementationDirection,
        TriggerKeysJson = JsonBlobs.Serialize(r.TriggerKeys),
        LinkedThreatIdsJson = JsonBlobs.Serialize(r.LinkedThreatIds),
        ExplanationJson = JsonBlobs.Serialize(r.Explanation),
        RelatedDesignNoteIdsJson = relatedNoteIdsJson,
        SourceAttributionJson = JsonBlobs.Serialize(r.SourceAttribution)
    };

    private static Dictionary<int, int> BuildDesignNoteIdMap(
        IReadOnlyList<DesignNoteModel> oldNotes,
        List<DesignNoteDoc> newDocs)
    {
        var map = new Dictionary<int, int>();
        var used = new HashSet<int>();
        foreach (var old in oldNotes.Where(n => n.Id != 0))
        {
            var match = newDocs.FirstOrDefault(n =>
                !used.Contains(n.Id) &&
                n.Kind == (int)old.Kind &&
                n.Title == old.Title);
            if (match != null)
            {
                map[old.Id] = match.Id;
                used.Add(match.Id);
            }
        }

        return map;
    }

    private static List<int> RemapIds(IReadOnlyList<int> ids, IReadOnlyDictionary<int, int> map)
    {
        var list = new List<int>();
        foreach (var id in ids)
        {
            if (map.TryGetValue(id, out var n))
                list.Add(n);
        }

        return list;
    }

    private static void ResolveFlowEndpoints(DataFlowModel f, IReadOnlyDictionary<string, int> nameToId)
    {
        if (!string.IsNullOrWhiteSpace(f.SourceComponentName) &&
            nameToId.TryGetValue(f.SourceComponentName, out var fromId))
            f.FromComponentId = fromId;
        if (!string.IsNullOrWhiteSpace(f.TargetComponentName) &&
            nameToId.TryGetValue(f.TargetComponentName, out var toId))
            f.ToComponentId = toId;
    }

    private static int RemapComponentId(ProjectModel m, int oldId, IReadOnlyDictionary<string, int> nameToCompId)
    {
        if (oldId == 0) return 0;
        var compName = m.Components.FirstOrDefault(c => c.Id == oldId)?.Name;
        if (compName != null && nameToCompId.TryGetValue(compName, out var cid))
            return cid;
        return 0;
    }
}
