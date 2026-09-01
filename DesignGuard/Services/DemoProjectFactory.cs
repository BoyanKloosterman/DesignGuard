using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>
/// Uitgebreid voorbeeld: webshop met trust boundaries, datastromen en bewust gemengde reviewstatus (fictief scenario).
/// Component-Id's 1..n: zelfde volgorde als in <see cref="ProjectModel.Components"/> (nodig voor koppeling assets/entry points bij eerste save).
/// </summary>
public static class DemoProjectFactory
{
    /// <summary>Naam in projectlijst; repositories slaan alleen op als deze naam nog niet bestaat.</summary>
    public const string DemoProjectDisplayName = "Demo — Webshop (uitgebreid)";

    // Stabiele sleutels voor eisen, dreigingen, review (niet wijzigen: koppelingen in seed-data).
    private const string ThreatSession = "d3m0threat0000000000000000000001";
    private const string ThreatSqlInj = "d3m0threat0000000000000000000002";
    private const string ThreatDdos = "d3m0threat0000000000000000000003";
    private const string ThreatWebhook = "d3m0threat0000000000000000000004";

    private const string ReqDbLeast = "d3m0req000000000000000000000001";
    private const string ReqWebhookSig = "d3m0req000000000000000000000002";
    private const string ReqHeaders = "d3m0req000000000000000000000003";
    private const string ReqSecretsRot = "d3m0req000000000000000000000004";

    public static ProjectModel CreateDemoProject()
    {
        var p = new ProjectModel
        {
            Name = DemoProjectDisplayName,
            Description =
                "Scenario: SPA-webshop met API-gateway, aparte admin-API, PostgreSQL, Redis, object storage en externe PSP/mail. " +
                "Dit project is expres deels afgerond (sommige dreigingen/eisen/controls klaar, andere nog open) om een realistische werkbank te tonen. Geen compliance-claim.",
            SystemName = "Demo Webshop",
            SystemType = SystemType.WebApp,
            DeploymentContext = DeploymentContext.Cloud,
            InternetExposed = true,
            PersonalDataProcessed = true,
            HasAuthentication = true,
            HasAdmin = true,
            ExternalApis = true,
            FileUpload = true,
            SensitiveDataStored = true,
            LoggingMonitoringPresent = true,
            CriticalBusinessFunction = true,
            OpenIssuesSummary =
                "• Webhook HMAC: implementatie klaar in staging; productie-cutover week 16.\n" +
                "• Admin SPA: nog geen volledige device-flow getest met nieuwe sessiestore.\n" +
                "• Object storage: bucket policy 'private + signed URL' — IAM-review door infra gepland.\n" +
                "• DDoS-risico: geaccepteerd t.o.v. CDN; geen extra actie tenzij SLA wijzigt.",
            AssessmentGoal = "Grey-box assessment van de webshop: misbruik van sessies, admin, webhooks en dataopslag in kaart brengen.",
            AssessmentTestType = AssessmentTestType.GreyBox,
            ScopeIn = "Shop SPA, admin-API, API-gateway, PostgreSQL, Redis, object storage, PSP-webhooks (testomgeving).",
            ScopeOut = "Productie-writes, fysieke toegang, social engineering, derde-partij PSP-platform zelf.",
            RulesOfEngagementNotes = "Alleen testomgeving. Geen DoS-loadtests. Escalatie via security-eigenaar."
        };

        p.TrustBoundaries.Add(new TrustBoundaryModel
        {
            Id = 1,
            Name = "Internet / clients",
            Description = "Browser, mobiele clients, eventueel CDN naar de SPA.",
            ColorHint = "#2B6CB8",
            Notes = "Publiek onvertrouwd verkeer."
        });
        p.TrustBoundaries.Add(new TrustBoundaryModel
        {
            Id = 2,
            Name = "Edge-netwerk",
            Description = "API-gateway, TLS-terminatie, eerste verdedigingslinie.",
            ColorHint = "#D69E2E",
            Notes = "Hier landen ook inkomende webhooks (PSP)."
        });
        p.TrustBoundaries.Add(new TrustBoundaryModel
        {
            Id = 3,
            Name = "Backend-kern",
            Description = "Domein-API's, database, cache en blob — niet direct vanaf internet.",
            ColorHint = "#38A169",
            Notes = "VPC / private subnets."
        });
        p.TrustBoundaries.Add(new TrustBoundaryModel
        {
            Id = 4,
            Name = "Externe partners",
            Description = "Betaalprovider, e-mail SaaS — buiten jouw trust zone.",
            ColorHint = "#805AD5",
            Notes = "Contractueel en technisch begrenzen."
        });

        // TrustBoundaryId + Name: beide gezet zodat diagram-overlays en editors consistent zijn.
        p.Components.Add(new ComponentModel
        {
            Id = 1,
            Name = "Shop SPA",
            Description = "React storefront; klant ziet catalogus en checkout.",
            Tag = "frontend",
            TrustBoundaryId = 1,
            TrustBoundaryName = "Internet / clients",
            IsEntryPoint = true,
            Notes = "Publiek entry; strikte CSP nog in backlog (zie eis security headers)."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 2,
            Name = "Admin SPA",
            Description = "Beheerpaneel voor catalogus, orders en gebruikers.",
            Tag = "admin-ui",
            TrustBoundaryId = 1,
            TrustBoundaryName = "Internet / clients",
            IsEntryPoint = true,
            Notes = "Alleen bereikbaar na SSO; MFA rollout Q3."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 3,
            Name = "API-gateway",
            Description = "Routeert verkeer naar shop- of admin-service; centrale authz-check.",
            Tag = "edge",
            TrustBoundaryId = 2,
            TrustBoundaryName = "Edge-netwerk",
            IsEntryPoint = true,
            Notes = "Rate limits actief op /api/*; webhook-pad nog documenteren."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 4,
            Name = "Shop-service",
            Description = "Orderflow, catalogus, checkout-sessies, integratie PSP.",
            Tag = "service",
            TrustBoundaryId = 3,
            TrustBoundaryName = "Backend-kern",
            IsEntryPoint = false,
            Notes = "Bevat checkout-logica; geen kaartdata op schijf."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 5,
            Name = "Admin-service",
            Description = "Beheer-API; bulk-operaties en rapportages.",
            Tag = "service",
            TrustBoundaryId = 3,
            TrustBoundaryName = "Backend-kern",
            IsEntryPoint = false,
            Notes = "Strengere RBAC dan shop-API."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 6,
            Name = "PostgreSQL",
            Description = "Orders, klantprofielen, productdata.",
            Tag = "database",
            TrustBoundaryId = 3,
            TrustBoundaryName = "Backend-kern",
            IsEntryPoint = false,
            StoresOrProcesses = nameof(DataSensitivity.Personal),
            Notes = "Back-ups versleuteld; least-privilege user (afgerond)."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 7,
            Name = "Redis (sessies)",
            Description = "Sessies, winkelmand, idempotency keys.",
            Tag = "cache",
            TrustBoundaryId = 3,
            TrustBoundaryName = "Backend-kern",
            IsEntryPoint = false,
            StoresOrProcesses = nameof(DataSensitivity.Sensitive),
            Notes = "Geen TLS tussen app en Redis in dev — prod wel; assumptie vastgelegd."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 8,
            Name = "Object storage (S3)",
            Description = "Productafbeeldingen, geüploade tickets, factuur-PDF's.",
            Tag = "storage",
            TrustBoundaryId = 3,
            TrustBoundaryName = "Backend-kern",
            IsEntryPoint = false,
            StoresOrProcesses = nameof(DataSensitivity.Sensitive),
            Notes = "Signed URLs; virusscan op uploads — deels geïmplementeerd."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 9,
            Name = "PSP (Stripe)",
            Description = "Hosted checkout / webhooks betalingsstatus.",
            Tag = "external",
            TrustBoundaryId = 4,
            TrustBoundaryName = "Externe partners",
            IsEntryPoint = false,
            Notes = "PCI DSS bij provider; wij slaan geen PAN op."
        });
        p.Components.Add(new ComponentModel
        {
            Id = 10,
            Name = "E-mailprovider",
            Description = "Transactionele mail (orderbevestiging, reset).",
            Tag = "external",
            TrustBoundaryId = 4,
            TrustBoundaryName = "Externe partners",
            IsEntryPoint = false,
            Notes = "SPF/DKIM door marketing beheerd."
        });

        void Flow(string from, string to, string label) =>
            p.DataFlows.Add(new DataFlowModel { SourceComponentName = from, TargetComponentName = to, Label = label });

        Flow("Shop SPA", "API-gateway", "HTTPS JSON (winkelmand, checkout)");
        Flow("Admin SPA", "API-gateway", "HTTPS JSON (beheer)");
        Flow("API-gateway", "Shop-service", "Intern (mTLS)");
        Flow("API-gateway", "Admin-service", "Intern (mTLS)");
        Flow("Shop-service", "PostgreSQL", "SQL (orders, PII)");
        Flow("Admin-service", "PostgreSQL", "SQL (beheerdata)");
        Flow("Shop-service", "Redis (sessies)", "Sessies / locks");
        Flow("Shop-service", "Object storage (S3)", "Uploads / PDF-facturen");
        Flow("Shop-service", "PSP (Stripe)", "Betaling + webhooks");
        Flow("Shop-service", "E-mailprovider", "SMTP/API mail");

        p.UserRoles.Add(new UserRoleModel { Name = "Klant", Description = "Bestelt, beheert profiel en adressen." });
        p.UserRoles.Add(new UserRoleModel { Name = "Shop-beheerder", Description = "Catalogus, prijzen, promoties." });
        p.UserRoles.Add(new UserRoleModel { Name = "Support", Description = "Orderinzage, refunds (beperkte rechten)." });

        p.Assets.Add(new AssetModel
        {
            Id = 1,
            Name = "Order- en klantdossier",
            Description = "Orders, NAW, orderhistorie.",
            Classification = nameof(AssetClassification.Confidential),
            Sensitivity = nameof(DataSensitivity.Personal),
            RelatedComponentId = 6,
            Notes = "Hoofdbron voor AVG-verzoeken."
        });
        p.Assets.Add(new AssetModel
        {
            Id = 2,
            Name = "Factuur-PDF",
            Description = "Financiële documenten in blob.",
            Classification = nameof(AssetClassification.Restricted),
            Sensitivity = nameof(DataSensitivity.Sensitive),
            RelatedComponentId = 8,
            Notes = "Retention 7 jaar — fiscaliteit."
        });
        p.Assets.Add(new AssetModel
        {
            Id = 3,
            Name = "Productmedia",
            Description = "Publieke afbeeldingen + soms leveranciers-PDF.",
            Classification = nameof(AssetClassification.Internal),
            Sensitivity = nameof(DataSensitivity.Low),
            RelatedComponentId = 8,
            Notes = "Niet alle uploads zijn publiek."
        });

        p.SensitiveDataItems.Add(new SensitiveDataModel
        {
            Id = 1,
            Name = "Klant-PII in orders",
            Category = "PII",
            Description = "NAW, e-mail, telefoon in orderregels.",
            RelatedComponentId = 6,
            StorageLocation = "PostgreSQL — schema orders",
            Notes = "Pseudonimisering exports: nog niet."
        });
        p.SensitiveDataItems.Add(new SensitiveDataModel
        {
            Id = 2,
            Name = "Sessie- en mandtokens",
            Category = "Auth tokens",
            Description = "Server-side sessies en idempotency keys.",
            RelatedComponentId = 7,
            StorageLocation = "Redis cluster prod",
            Notes = "TTL 24u; invalidatie bij logout deels."
        });
        p.SensitiveDataItems.Add(new SensitiveDataModel
        {
            Id = 3,
            Name = "Factuur-PDF inhoud",
            Category = "Financieel",
            Description = "Kan PII + bedragen bevatten.",
            RelatedComponentId = 8,
            StorageLocation = "S3 bucket invoices/",
            Notes = "Encryptie at rest provider-default."
        });
        p.SensitiveDataItems.Add(new SensitiveDataModel
        {
            Id = 4,
            Name = "Webhook payload PSP",
            Category = "Integratie",
            Description = "Betaalstatus, metadata.",
            RelatedComponentId = 3,
            StorageLocation = "Kort in gateway-logs",
            Notes = "Log-minimalisatie nog open."
        });

        p.DesignNotes.Add(new DesignNoteModel
        {
            Id = 1,
            Kind = DesignNoteKind.Assumption,
            Title = "Redis alleen bereikbaar vanuit kern-VPC",
            Description = "Geen directe route van edge naar Redis; verkeer loopt via services.",
            Notes = "Te herzien bij nieuwe cache-topologie."
        });
        p.DesignNotes.Add(new DesignNoteModel
        {
            Id = 2,
            Kind = DesignNoteKind.Decision,
            Title = "Geen PAN bij ons; checkout via PSP",
            Description = "Kaartdata blijft bij Stripe hosted fields / redirect.",
            Notes = "SAQ-scope beperkt — geen formele claim in deze app."
        });
        p.DesignNotes.Add(new DesignNoteModel
        {
            Id = 3,
            Kind = DesignNoteKind.OpenQuestion,
            Title = "Webhook rate limit productie?",
            Description = "Is het PSP webhook-endpoint al voorzien van per-source throttling in prod?",
            Notes = "Hangt samen met replay/HMAC-werk."
        });

        p.Controls.Add(new ControlModel
        {
            Id = 1,
            StableId = "dgdemoctrl0000000000000000000001",
            Title = "Database least privilege + parameterized queries",
            Category = "Data",
            Description = "Aparte DB-user per service; geen dynamische SQL-concat.",
            ImplementationGuidance = "Migrations reviewen; alleen prepared statements in data-laag.",
            LinkedThreatStableId = ThreatSqlInj,
            LinkedRequirementStableIds = new List<string> { ReqDbLeast },
            Status = ControlLifecycleStatus.Implemented,
            StatusNotes = "Code review sprint 11; spot-check queries.",
            SourceTags = new List<string> { "demo", "sql" }
        });

        p.Controls.Add(new ControlModel
        {
            Id = 2,
            StableId = "dgdemoctrl0000000000000000000002",
            Title = "WAF / bot-baseline op edge",
            Category = "Netwerk",
            Description = "OWASP Top 10 basisregels op gateway.",
            ImplementationGuidance = "False positives monitoren; logging naar SIEM.",
            LinkedThreatStableId = ThreatDdos,
            Status = ControlLifecycleStatus.UnderReview,
            StatusNotes = "Regelset v3 in staging; productie nog oude set.",
            SourceTags = new List<string> { "demo", "edge" }
        });
        p.Controls.Add(new ControlModel
        {
            Id = 3,
            StableId = "dgdemoctrl0000000000000000000003",
            Title = "PSP webhook HMAC + timestamp tolerantie",
            Category = "Integratie",
            Description = "Handtekening valideren; reject oude timestamps.",
            ImplementationGuidance = "Secret in vault; rotate bij incident.",
            LinkedThreatStableId = ThreatWebhook,
            LinkedRequirementStableIds = new List<string> { ReqWebhookSig },
            Status = ControlLifecycleStatus.Proposed,
            StatusNotes = "Implementatie klaar in branch; PR open.",
            SourceTags = new List<string> { "demo", "webhook" }
        });

        p.ReviewItems.Add(new ReviewItemModel
        {
            SubjectKind = ReviewSubjectKind.Threat,
            SubjectStableId = ThreatWebhook,
            SubjectTitle = "Webhook replay / tampering",
            Status = ReviewWorkflowStatus.UnderReview,
            Owner = "AppSec",
            Notes = "Pentest-achtige replay-test nog uit te voeren.",
            Rationale = "Impact hoog op orderstatus."
        });
        p.ReviewItems.Add(new ReviewItemModel
        {
            SubjectKind = ReviewSubjectKind.Requirement,
            SubjectStableId = ReqSecretsRot,
            SubjectTitle = "Secrets rotation automation",
            Status = ReviewWorkflowStatus.Draft,
            Owner = "Platform",
            Notes = "Afhankelijk van nieuwe vault-versie.",
            Rationale = ""
        });
        p.ReviewItems.Add(new ReviewItemModel
        {
            SubjectKind = ReviewSubjectKind.DesignNote,
            SubjectStableId = "3",
            SubjectTitle = "Open vraag webhook rate limit",
            Status = ReviewWorkflowStatus.NeedsClarification,
            Owner = "Infra",
            Notes = "Afstemmen met PSP documentatie.",
            Rationale = ""
        });

        p.Threats.Add(new ThreatModel
        {
            Id = ThreatSession,
            Origin = ThreatOrigin.Custom,
            UserModified = true,
            Title = "Session hijacking / fixation (shop cookie)",
            StrideCategory = StrideCategory.Spoofing,
            Severity = SeverityEstimate.Medium,
            Status = ThreatStatus.Open,
            Description = "Gestolen of vastgezette sessie geeft toegang tot mand en checkout als klant.",
            Notes = "Nog te testen met nieuwe SameSite-instellingen.",
            AffectedComponents = new List<string> { "Shop SPA", "Redis (sessies)" },
            AffectedAssets = new List<string> { "Order- en klantdossier" },
            TriggerKeys = new List<string> { "internet_exposed", "has_authentication" },
            SuggestedMitigations = new List<string>
            {
                "Secure + HttpOnly + SameSite cookies",
                "Sessie rotatie na login",
                "Korte TTL + server-side invalidatie"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Iemand kan zich voordoen als een ingelogde klant.",
                WhyItMatters = "Orders en adressen kunnen worden gemanipuleerd.",
                WhyIncluded = "SPA met sessies in Redis en publiek entry."
            }
        });

        p.Threats.Add(new ThreatModel
        {
            Id = ThreatSqlInj,
            Origin = ThreatOrigin.Custom,
            UserModified = true,
            Title = "SQL-injectie op order- of zoek-endpoint",
            StrideCategory = StrideCategory.Tampering,
            Severity = SeverityEstimate.High,
            Status = ThreatStatus.Mitigated,
            Description = "Onveilige query-samenstelling zou data kunnen lekken of wijzigen.",
            Notes = "Alle kritieke paden geaudit; parameterized queries afgerond sprint 12.",
            AffectedComponents = new List<string> { "Shop-service", "Admin-service", "PostgreSQL" },
            AffectedAssets = new List<string> { "Order- en klantdossier" },
            TriggerKeys = new List<string> { "sql_surface" },
            SuggestedMitigations = new List<string> { "ORM/prepared statements", "Least privilege DB-user", "Input validation" },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Aanvaller voegt SQL-code toe aan invoer.",
                WhyItMatters = "Volledige database kan worden blootgesteld.",
                WhyIncluded = "Rijke CRUD op orders en admin-rapportages."
            }
        });

        p.Threats.Add(new ThreatModel
        {
            Id = ThreatDdos,
            Origin = ThreatOrigin.Custom,
            UserModified = false,
            Title = "Volume-gebaseerde uitval storefront (DDoS)",
            StrideCategory = StrideCategory.DenialOfService,
            Severity = SeverityEstimate.Medium,
            Status = ThreatStatus.Accepted,
            Description = "Publieke shop kan worden overspoeld met verkeer.",
            Notes = "Geaccepteerd: CDN + provider-DDoS; geen dedicated 'always-on' mitigatie.",
            AffectedComponents = new List<string> { "Shop SPA", "API-gateway" },
            TriggerKeys = new List<string> { "internet_exposed" },
            SuggestedMitigations = new List<string> { "CDN caching", "Rate limits", "CAPTCHA bij abuse" },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Dienst wordt traag of onbereikbaar door verkeersvolume.",
                WhyItMatters = "Omzetverlies en reputatieschade.",
                WhyIncluded = "Internet-facing storefront."
            }
        });

        p.Threats.Add(new ThreatModel
        {
            Id = ThreatWebhook,
            Origin = ThreatOrigin.Custom,
            UserModified = true,
            Title = "Webhook replay of tampering (PSP)",
            StrideCategory = StrideCategory.Tampering,
            Severity = SeverityEstimate.High,
            Status = ThreatStatus.Open,
            Description = "Ongesigneerde of herhaalde webhooks kunnen orderstatus corrupten.",
            Notes = "HMAC-implementatie in review; zie open issues.",
            AffectedComponents = new List<string> { "API-gateway", "Shop-service", "PSP (Stripe)" },
            RelatedDesignNoteIds = new List<int> { 3 },
            TriggerKeys = new List<string> { "external_webhook" },
            SuggestedMitigations = new List<string>
            {
                "HMAC of signed payload",
                "Idempotency keys per event",
                "Timestamp + skew check"
            },
            Explanation = new ExplanationModel
            {
                WhatItMeans = "Vals webhook-event wordt als echt verwerkt.",
                WhyItMatters = "Financiële en orderdata raakt inconsistent.",
                WhyIncluded = "Inbound callbacks van externe PSP."
            }
        });

        foreach (var t in p.Threats)
            RiskScoring.EnsureScores(t);

        p.Requirements.Add(new RequirementModel
        {
            Id = ReqDbLeast,
            Origin = RequirementOrigin.Custom,
            UserModified = true,
            Title = "Least privilege database-accounts en parameterized access",
            Category = "Data",
            Priority = RequirementPriority.High,
            Status = RequirementStatus.Implemented,
            PlainExplanation = "Elke service heeft alleen de rechten die nodig zijn; SQL-injectie beperkt blast radius.",
            WhyApplies = "Meerdere services praten met dezelfde PostgreSQL.",
            LinkedThreatIds = new List<string> { ThreatSqlInj },
            SourceTags = new List<string> { "demo", "database" }
        });

        p.Requirements.Add(new RequirementModel
        {
            Id = ReqWebhookSig,
            Origin = RequirementOrigin.Custom,
            UserModified = false,
            Title = "Valideer PSP-webhooks (handtekening + anti-replay)",
            Category = "Integratie",
            Priority = RequirementPriority.High,
            Status = RequirementStatus.Accepted,
            PlainExplanation = "Alleen cryptografisch geverifieerde events verwerken; dubbele events idempotent afhandelen.",
            WhyApplies = "Webhook-pad is publiek bereikbaar op de gateway.",
            LinkedThreatIds = new List<string> { ThreatWebhook },
            ImplementationDirection = "Library van PSP gebruiken + eigen replay-testset.",
            SourceTags = new List<string> { "demo", "webhook" }
        });

        p.Requirements.Add(new RequirementModel
        {
            Id = ReqHeaders,
            Origin = RequirementOrigin.Custom,
            UserModified = false,
            Title = "Security headers en CSP op SPA’s",
            Category = "Web",
            Priority = RequirementPriority.Medium,
            Status = RequirementStatus.Proposed,
            PlainExplanation = "Verklein XSS/clickjacking-risico in browser.",
            WhyApplies = "Beide SPA’s zijn publiek of breed intern.",
            Notes = "CSP nog geen report-only in prod.",
            SourceTags = new List<string> { "demo", "headers" }
        });

        p.Requirements.Add(new RequirementModel
        {
            Id = ReqSecretsRot,
            Origin = RequirementOrigin.Custom,
            UserModified = false,
            Title = "Geautomatiseerde rotatie API-keys en webhook secrets",
            Category = "Operatie",
            Priority = RequirementPriority.Medium,
            Status = RequirementStatus.Deferred,
            PlainExplanation = "Secrets vernieuwen zonder downtime.",
            WhyApplies = "Meerdere externe integraties (PSP, mail).",
            Notes = "Wacht op vault-upgrade Q3.",
            SourceTags = new List<string> { "demo", "ops" }
        });

        AddDemoC4Model(p);
        return p;
    }

    /// <summary>C4-demo: zelfde namen als Components waar dreigingen op koppelen (Shop SPA, Redis (sessies), …).</summary>
    private static void AddDemoC4Model(ProjectModel p)
    {
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 1,
            Level = C4Level.Context,
            Name = "Klant",
            Description = "Eindgebruiker storefront en checkout.",
            Technology = ""
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 2,
            Level = C4Level.Context,
            Name = "Shop-beheerder",
            Description = "Interne beheerder catalogus en orders.",
            Technology = ""
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 3,
            Level = C4Level.Context,
            Name = "PSP (Stripe)",
            Description = "Externe betaalprovider en webhooks.",
            Technology = ""
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 4,
            Level = C4Level.Context,
            Name = "E-mailprovider",
            Description = "Transactionele mail SaaS.",
            Technology = ""
        });

        p.C4Elements.Add(new C4ElementModel
        {
            Id = 5,
            Level = C4Level.Container,
            Name = "Shop SPA",
            Description = "React storefront.",
            Technology = "React",
            ParentId = 1
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 6,
            Level = C4Level.Container,
            Name = "Admin SPA",
            Description = "Beheerpaneel.",
            Technology = "React",
            ParentId = 2
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 7,
            Level = C4Level.Container,
            Name = "API-gateway",
            Description = "TLS, routing, authz.",
            Technology = "Gateway",
            ParentId = null
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 8,
            Level = C4Level.Container,
            Name = "Shop-service",
            Description = "Orders, checkout, integraties.",
            Technology = ".NET",
            ParentId = null
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 9,
            Level = C4Level.Container,
            Name = "Admin-service",
            Description = "Beheer-API en rapportages.",
            Technology = ".NET",
            ParentId = null
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 10,
            Level = C4Level.Container,
            Name = "PostgreSQL",
            Description = "Orders en klantdata.",
            Technology = "PostgreSQL",
            ParentId = null
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 11,
            Level = C4Level.Container,
            Name = "Redis (sessies)",
            Description = "Sessies en winkelmand.",
            Technology = "Redis",
            ParentId = null
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 12,
            Level = C4Level.Container,
            Name = "Object storage (S3)",
            Description = "Uploads en factuur-PDF.",
            Technology = "S3",
            ParentId = null
        });

        p.C4Elements.Add(new C4ElementModel
        {
            Id = 13,
            Level = C4Level.Component,
            Name = "Checkout API",
            Description = "Checkout en PSP-callbacks.",
            Technology = "REST",
            ParentId = 8
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 14,
            Level = C4Level.Component,
            Name = "Catalogus API",
            Description = "Producten en prijzen.",
            Technology = "REST",
            ParentId = 8
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 15,
            Level = C4Level.Component,
            Name = "Beheer API",
            Description = "Bulk en rapportage.",
            Technology = "REST",
            ParentId = 9
        });

        p.C4Elements.Add(new C4ElementModel
        {
            Id = 16,
            Level = C4Level.Code,
            Name = "WebhookController",
            Description = "Inbound PSP-webhooks.",
            Technology = "ASP.NET",
            ParentId = 13
        });
        p.C4Elements.Add(new C4ElementModel
        {
            Id = 17,
            Level = C4Level.Code,
            Name = "OrderService",
            Description = "Orderdomänlogica.",
            Technology = "C#",
            ParentId = 13
        });

        void Rel(int id, int from, int to, string label, C4MermaidRelLineKind lineKind = C4MermaidRelLineKind.Default) =>
            p.C4Relations.Add(new C4RelationModel
            {
                Id = id,
                FromElementId = from,
                ToElementId = to,
                Label = label,
                LineKind = lineKind
            });

        var n = 1;
        Rel(n++, 1, 0, "Gebruikt storefront en checkout");
        Rel(n++, 2, 0, "Beheert via admin");
        Rel(n++, 0, 3, "Betaling en webhooks");
        Rel(n++, 0, 4, "Orderbevestiging en resets");

        Rel(n++, 1, 5, "HTTPS");
        Rel(n++, 2, 6, "HTTPS");
        Rel(n++, 5, 7, "API-aanroepen");
        Rel(n++, 6, 7, "API-aanroepen");
        Rel(n++, 7, 8, "Routeert naar shop");
        Rel(n++, 7, 9, "Routeert naar admin");
        Rel(n++, 8, 10, "SQL orders en PII");
        Rel(n++, 9, 10, "SQL beheerdata");
        Rel(n++, 8, 11, "Sessies en mand");
        Rel(n++, 8, 12, "Uploads en facturen");
        Rel(n++, 8, 3, "Betaling");
        Rel(n++, 8, 4, "Mail");

        Rel(n++, 13, 14, "Catalogus en prijzen");

        Rel(n++, 16, 17, "Webhook naar orderlogica");
    }
}
