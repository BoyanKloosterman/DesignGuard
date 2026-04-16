// Handmatige dreigingen en eisen.
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private void AddCustomThreat()
    {
        Threats.Add(new ThreatModel
        {
            Origin = ThreatOrigin.Custom,
            Title = "Handmatige dreiging",
            Description = "Beschrijf het scenario.",
            StrideCategory = StrideCategory.Tampering,
            Severity = SeverityEstimate.Medium,
            Status = ThreatStatus.Open,
            GenerationReason = "Toegevoegd door gebruiker.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "",
                WhyItMatters = "",
                WhyIncluded = "Handmatig toegevoegd."
            }
        });
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveThreat(ThreatModel? t)
    {
        if (t == null) return;
        Threats.Remove(t);
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void AddCustomRequirement()
    {
        Requirements.Add(new RequirementModel
        {
            Origin = RequirementOrigin.Custom,
            Title = "Handmatige eis",
            Category = "Algemeen",
            PlainExplanation = "",
            WhyApplies = "Toegevoegd door gebruiker.",
            ImplementationDirection = "",
            Priority = RequirementPriority.Medium,
            Status = RequirementStatus.Proposed,
            Explanation = new ExplanationModel { WhatItMeans = "", WhyItMatters = "", WhyIncluded = "" }
        });
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveRequirement(RequirementModel? r)
    {
        if (r == null) return;
        Requirements.Remove(r);
        RefreshFilters();
        UpdateDashboard();
    }
}
