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
}
