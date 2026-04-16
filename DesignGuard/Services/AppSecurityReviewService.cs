using System.IO;
using System.Text.Json;
using DesignGuard.ViewModels;

namespace DesignGuard.Services;

public sealed class AppSecurityReviewService
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public IReadOnlyList<AppSecurityReviewRowViewModel> LoadChecklist()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "app_security_checklist.json");
        try
        {
            if (!File.Exists(path)) return Array.Empty<AppSecurityReviewRowViewModel>();
            var json = File.ReadAllText(path);
            var doc = JsonSerializer.Deserialize<ChecklistFileDto>(json, Opts);
            if (doc?.Items == null) return Array.Empty<AppSecurityReviewRowViewModel>();
            return doc.Items.Select(x => new AppSecurityReviewRowViewModel
            {
                Domain = x.Domain,
                Item = x.Item,
                Status = x.Status,
                Rationale = x.Rationale,
                Recommendation = x.Recommendation,
                Evidence = x.Evidence,
                SourceTag = x.SourceTag
            }).ToList();
        }
        catch
        {
            return Array.Empty<AppSecurityReviewRowViewModel>();
        }
    }

    private sealed class ChecklistFileDto
    {
        public List<ChecklistItemDto>? Items { get; set; }
    }

    private sealed class ChecklistItemDto
    {
        public string Domain { get; set; } = "";
        public string Item { get; set; } = "";
        public string Status { get; set; } = "";
        public string Rationale { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public string Evidence { get; set; } = "";
        public string SourceTag { get; set; } = "";
    }
}
