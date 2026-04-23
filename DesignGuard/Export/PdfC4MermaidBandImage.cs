namespace DesignGuard.Export;

/// <summary>Eén C4-band (C1–C4) als PNG voor het security-PDF.</summary>
public sealed record PdfC4MermaidBandImage(string Caption, byte[] Png);
