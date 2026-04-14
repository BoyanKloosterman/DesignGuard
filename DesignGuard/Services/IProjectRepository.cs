using DesignGuard.Models;

namespace DesignGuard.Services;

public interface IProjectRepository
{
    Task<IReadOnlyList<(int Id, string Name, DateTime UpdatedAtUtc)>> ListSummariesAsync(CancellationToken ct = default);
    Task<ProjectModel?> GetAsync(int id, CancellationToken ct = default);
    Task<int> SaveAsync(ProjectModel model, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task EnsureDatabaseAsync(CancellationToken ct = default);
    Task<int> EnsureDemoProjectAsync(CancellationToken ct = default);
}
