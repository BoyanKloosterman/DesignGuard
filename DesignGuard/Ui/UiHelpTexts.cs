namespace DesignGuard.Ui;

/// <summary>Gedeelde korte hulpteksten (één bron, minder herhaling in XAML).</summary>
public static class UiHelpTexts
{
    public static string ThreatComponentNameMatch =>
        "Open dreigingen op C4-rijen tellen mee als de rij-naam exact overeenkomt met een naam in «Affected components» bij een dreiging (tab Dreigingen).";

    public static string DesignArchitectureDiagram =>
        "Tip: groene rand = ingang, oranje streep = gevoelige data. Mermaid-diagram top-down — compact op scherm en in PDF. Trust boundary = vertrouwensgrens (bijv. internet vs intern).";

    public static string C4Intro =>
        "C4 staat los van het architectuurdiagram op Ontwerp. Mermaid C4 (C1–C4) komt uit de tabellen; relaties als Rel(van, naar, label) alleen als beide knooppunten in dat band bestaan (id 0 = systeem in scope, alleen C1). Personen: trefwoorden in naam/omschrijving; DB/queue uit tech.";

    public static string C4PanelSummary =>
        "Mermaid C4 (C1–C4) uit de tabellen; los van het architectuurdiagram op Ontwerp.";

    public static string C4TooltipBody => C4Intro + " " + ThreatComponentNameMatch;
}
