// Knowledge packs en app security checklist.
using System.Collections.ObjectModel;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    private void RefreshKnowledgePackRows()
    {
        _knowledgePacks.Reload();
        var discovered = _knowledgePacks.DiscoverPacksIgnoringUserDisabled();
        KnowledgePackRows.Clear();
        foreach (var p in discovered.OrderBy(x => x.Dto.DisplayLabel))
        {
            var disabled = _userSettings.Current.DisabledPackIds.Contains(p.Dto.PackId);
            var stale = _knowledgePacks.IsPackStale(p, _userSettings.Current.PackStaleWarningDays);
            KnowledgePackRows.Add(new KnowledgePackToggleRow(
                p.Dto.PackId,
                p.Dto.DisplayLabel,
                p.Dto.VersionLabel,
                p.Dto.SourceName,
                stale,
                !disabled,
                OnKnowledgePackRowToggled));
        }
    }

    private void OnKnowledgePackRowToggled(string packId, bool enabled)
    {
        _userSettings.SetPackDisabled(packId, !enabled);
        _knowledgePacks.Reload();
        StatusMessage = enabled ? $"Pack ingeschakeld: {packId}" : $"Pack uitgeschakeld: {packId}";
    }

    private void RefreshAppSecurityReview()
    {
        AppSecurityReviewRows = new ObservableCollection<AppSecurityReviewRowViewModel>(_appSecurityReview.LoadChecklist());
    }
}
