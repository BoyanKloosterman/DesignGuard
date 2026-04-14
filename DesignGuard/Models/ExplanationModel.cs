namespace DesignGuard.Models;

/// <summary>
/// Menselijk leesbare uitleg bij dreigingen en eisen.
/// </summary>
public sealed class ExplanationModel
{
    public string WhatItMeans { get; set; } = "";
    public string WhyItMatters { get; set; } = "";
    public string WhyIncluded { get; set; } = "";
}
