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
            PersonalDataProcessed = e.PersonalDataProcessed,
            HasAuthentication = e.HasAuthentication,
            HasAdmin = e.HasAdmin,
            ExternalApis = e.ExternalApis,
            FileUpload = e.FileUpload,
            SensitiveDataStored = e.SensitiveDataStored,
            Components = e.Components.Select(c => new ComponentModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
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
            }).ToList()
        };
    }
}
