// Remote sync: HTTPS-manifest, hash-verificatie, herladen packs.
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Knowledge;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task SyncKnowledgePacksRemoteAsync()
    {
        if (string.IsNullOrWhiteSpace(KnowledgePackManifestUrl))
        {
            StatusMessage = "Vul een HTTPS manifest-URL in.";
            return;
        }

        IsBusy = true;
        BusyMessage = "Knowledge packs synchroniseren…";
        try
        {
            var r = await _packRemoteSync.SyncAsync(
                KnowledgePackManifestUrl.Trim(),
                string.IsNullOrWhiteSpace(KnowledgePackSyncTrustedHostExtra)
                    ? null
                    : KnowledgePackSyncTrustedHostExtra.Trim(),
                CancellationToken.None);
            if (r.Ok)
            {
                _knowledgePacks.Reload();
                RefreshKnowledgePackRows();
            }

            StatusMessage = r.Message;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Knowledge pack sync mislukt: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Null = overgeslagen; anders sync-resultaattekst voor statusbalk.</summary>
    private async Task<string?> TrySyncKnowledgePacksOnStartupAsync()
    {
        if (!KnowledgePackRemoteSyncEnabled || !KnowledgePackSyncOnStartup ||
            string.IsNullOrWhiteSpace(KnowledgePackManifestUrl))
            return null;

        try
        {
            var r = await _packRemoteSync.SyncAsync(
                KnowledgePackManifestUrl.Trim(),
                string.IsNullOrWhiteSpace(KnowledgePackSyncTrustedHostExtra)
                    ? null
                    : KnowledgePackSyncTrustedHostExtra.Trim(),
                CancellationToken.None);
            if (r.Ok)
            {
                _knowledgePacks.Reload();
                RefreshKnowledgePackRows();
            }

            return r.Message;
        }
        catch (Exception ex)
        {
            return $"Knowledge pack sync (start): {ex.Message}";
        }
    }
}
