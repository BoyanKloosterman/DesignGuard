// Snelstart: minimale C4-scope voor nieuwe projecten.
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private void QuickStartMinimalC4()
    {
        if (!HasOpenProject)
        {
            StatusMessage = "Open of maak eerst een project.";
            return;
        }

        if (C4Elements.Count > 0)
        {
            StatusMessage = "C4 bevat al elementen — snelstart overgeslagen.";
            return;
        }

        var id = 1;
        void add(C4Level level, string name, string description, string technology)
        {
            C4Elements.Add(new C4ElementRowViewModel
            {
                Id = id++,
                Level = level,
                Name = name,
                Description = description,
                Technology = technology,
                ParentId = null
            });
        }

        add(C4Level.Context, "Gebruiker / operator", "Actor buiten het systeem (persoon of externe rol).", "");
        add(C4Level.Container, "Webapplicatie", "UI in de browser of SPA die met de backend praat.", "browser / SPA");
        add(C4Level.Container, "Backend API", "Server-side dienst met bedrijfslogica en integraties.", "API");
        add(C4Level.Component, "Authenticatie", "Inloggen, sessies, tokens — af te stemmen op jouw ontwerp.", "OIDC / sessies");

        RefreshC4ThreatLinkCounts();
        StatusMessage =
            "Minimale C4-scope toegevoegd. Pas namen aan zodat ze matchen met componenten/dreigingen; daarna ‘Analyse vernieuwen’.";
    }
}
