namespace DesignGuard.Models;

public static class C4LevelFormatting
{
    public static string ShortLabel(C4Level level) => level switch
    {
        C4Level.Context => "C1 — Context",
        C4Level.Container => "C2 — Containers",
        C4Level.Component => "C3 — Components",
        C4Level.Code => "C4 — Code",
        _ => level.ToString()
    };

    /// <summary>Wat dit abstractieniveau in het C4-model betekent (o.a. PDF).</summary>
    public static string LevelScopeExplanation(C4Level level) => level switch
    {
        C4Level.Context =>
            "Personen en externe systemen rond jouw oplossing: wie en wat communiceert met het systeem, zonder technische inners.",
        C4Level.Container =>
            "Deploybare eenheden met eigen proces of runtime: bijv. webapp, SPA, mobiele app, API, database, message broker.",
        C4Level.Component =>
            "Hoofdbouwstenen binnen één container: bijv. controllers, domeinservices, integratiefacades — achter duidelijke interfaces.",
        C4Level.Code =>
            "Implementatiezoom: klassen, interfaces en packages (UML-achtig); alleen relevant waar detail nodig is.",
        _ => ""
    };
}
