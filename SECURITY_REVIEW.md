# DesignGuard v4 — security review (engineering note)

This document summarizes a **self-assessment** of the DesignGuard desktop application. It is not an independent audit or penetration test.

## Scope

- Local-first WPF (.NET 10), SQLite, file-based knowledge packs, export (Markdown, HTML, JSON, PDF).
- Threat and requirement models persisted with optional `SourceAttributionJson`.

## Identified risks (high level)

| Area | Risk | Severity (app context) |
|------|------|-------------------------|
| Local database | Project data readable by the OS user and malware running as the same user | Medium (typical desktop) |
| Import / packs | Malformed or oversized JSON could cause denial-of-service or confusing content | Low–medium |
| Export paths | User-chosen paths could target unexpected locations if combined with future features | Low |
| Dependencies | Transitive NuGet vulnerabilities | Low–medium (monitor) |
| PDF / HTML export | User-generated text reflected into documents (expected); no HTML sanitizer for “safe” web hosting | Low for local reports |

## Implemented mitigations (v4)

- **SQLite schema patching** for new attribution columns without requiring a full database reset for typical upgrades.
- **Knowledge pack loading**: relative paths only, `..` rejected, file size cap, JSON parse failures swallowed per file.
- **Export path check**: `SafeExportPath.TryGetSafeWritePath` validates full path and parent directory existence before write.
- **Disclaimers** in UI-oriented copy, exports, and knowledge pack JSON (no compliance/certification wording).
- **Traceability**: `SourceAttributionModel` on threats and requirements; packs drive mapping where rules match.
- **Operational checklist**: built-in “App security review” panel backed by `Resources/app_security_checklist.json` (editable).

## Remaining gaps (recommended next steps)

1. **EF Core migrations** instead of ad hoc `ALTER TABLE` for cleaner, versioned schema evolution.
2. **JSON Schema validation** for knowledge packs and optional user-imported packs (e.g. `System.Text.Json` + schema file).
3. **Structured logging** (optional, file-based, opt-in) with redaction rules instead of only status bar text.
4. **Backup / export reminder** in UI for the SQLite file path.
5. **Automated dependency scanning** in CI (`dotnet list package --vulnerable`).
6. **QuestPDF / licensing**: confirm QuestPDF Community license terms match your distribution model before shipping broadly.

## Dependency notes (manual)

Review periodically:

- `Microsoft.EntityFrameworkCore.Sqlite`
- `CommunityToolkit.Mvvm`
- `QuestPDF`
- Transitive packages reported by `dotnet list package --include-transitive`

## Serialization

- Project and snapshot JSON use `System.Text.Json` with camelCase for exports.
- Avoid deserializing untrusted types; packs deserialize into explicit DTOs only.

---

*Last updated: 2026-04-16 — align with product version in use.*
