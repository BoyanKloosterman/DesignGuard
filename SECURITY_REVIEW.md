# DesignGuard v5 — security review (engineering note)

This document summarizes a **self-assessment** of the DesignGuard desktop application. It is not an independent audit or penetration test.

## Scope

- Local-first WPF (.NET 10), **MongoDB** persistence (network database), file-based knowledge packs, export (Markdown, HTML, JSON, PDF).
- Threat and requirement models persisted with optional `SourceAttributionJson`.
- **Environment-based configuration** for MongoDB; optional dev-only `.env` (see CONFIGURATION.md).

## Identified risks (high level)

| Area | Risk | Severity (app context) |
|------|------|-------------------------|
| MongoDB credentials | Connection string exposure via logs, screenshots, or committed `.env` | **High** if mishandled; mitigated by masking + no full string in UI/logs by design |
| Network database | Data in transit to MongoDB; server compromise exposes project data | Medium–high (depends on deployment hardening) |
| Local client | Project data in memory; malware as same user can read UI/export | Medium (typical desktop) |
| Import / packs | Malformed or oversized JSON could cause denial-of-service or confusing content | Low–medium |
| Export paths | User-chosen paths could target unexpected locations if combined with future features | Low |
| Dependencies | Transitive NuGet vulnerabilities | Low–medium (monitor) |
| PDF / HTML export | User-generated text reflected into documents (expected); no HTML sanitizer for “safe” web hosting | Low for local reports |

## Implemented mitigations (v5)

- **Configuration**: required settings from environment variables; warnings in UI if incomplete; **no hardcoded connection strings**.
- **Secret display**: connection string **masked** in diagnostics; do not log full connection strings (avoid `Console`/file logging of config).
- **MongoDB driver**: explicit timeouts when `DESIGNGUARD_MONGODB_TIMEOUT_SECONDS` is set; optional TLS flag and read preference for server deployments.
- **Persistence model**: stable BSON documents with `[BsonIgnoreExtraElements]` on project root to reduce breakage from stray fields; mapping via explicit DTOs / entity conversion (no arbitrary `dynamic` document graphs for domain load).
- **SQLite import**: user-selected `.db` path only; imports through the same `ProjectMapper` + `ProjectDocumentBuilder` path as normal saves (consistent domain mapping).
- **Knowledge pack loading**: relative paths only, `..` rejected, file size cap, JSON parse failures swallowed per file.
- **Export path check**: `SafeExportPath.TryGetSafeWritePath` validates full path and parent directory existence before write.
- **Disclaimers** in UI-oriented copy, exports, and knowledge pack JSON (no compliance/certification wording).
- **Traceability**: `SourceAttributionModel` on threats and requirements; packs drive mapping where rules match.
- **Operational checklist**: built-in “App security review” panel backed by `Resources/app_security_checklist.json` (editable).

## MongoDB — usage checklist

1. Prefer **TLS** for remote clusters (Atlas uses `mongodb+srv` with TLS).
2. Use **least-privilege** database users (read/write on application DB only).
3. **Rotate** credentials if a connection string may have leaked.
4. Do not enable **debug logging** of MongoDB commands in production builds without redaction.
5. Validate **network** access (firewall / VPN) for self-hosted MongoDB.

## Remaining gaps (recommended next steps)

1. **JSON Schema validation** for knowledge packs and optional user-imported packs.
2. **Structured logging** (optional, file-based, opt-in) with redaction rules instead of only status bar text.
3. **Automated dependency scanning** in CI (`dotnet list package --vulnerable`).
4. **QuestPDF / licensing**: confirm QuestPDF Community license terms match your distribution model before shipping broadly.
5. **EF Core** retained only for SQLite import path; consider isolating import in a separate tool/assembly if attack surface is a concern.

## Dependency notes (manual)

Review periodically:

- `MongoDB.Driver`
- `Microsoft.EntityFrameworkCore.Sqlite` (import only)
- `CommunityToolkit.Mvvm`
- `QuestPDF`
- Transitive packages reported by `dotnet list package --include-transitive`

## Serialization

- Project and snapshot JSON use `System.Text.Json` with camelCase for exports.
- Avoid deserializing untrusted types; packs deserialize into explicit DTOs only.
- MongoDB documents map to explicit C# types before exposing domain models to the UI.

---

*Last updated: 2026-04-16 — v5 MongoDB / configuration focus.*
