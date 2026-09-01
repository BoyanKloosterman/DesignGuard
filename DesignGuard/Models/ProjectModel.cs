namespace DesignGuard.Models;

/// <summary>
/// Volledige project- en ontwerpstatus voor generatie en export.
/// </summary>
public sealed class ProjectModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string SystemName { get; set; } = "";
    public SystemType SystemType { get; set; } = SystemType.WebApp;
    public DeploymentContext DeploymentContext { get; set; } = DeploymentContext.Cloud;

    /// <summary>Bereikbaar vanaf internet (publiek of remote).</summary>
    public bool InternetExposed { get; set; } = true;

    public bool PersonalDataProcessed { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasAdmin { get; set; }
    public bool ExternalApis { get; set; }
    public bool FileUpload { get; set; }
    public bool SensitiveDataStored { get; set; }
    public bool LoggingMonitoringPresent { get; set; } = true;
    public bool CriticalBusinessFunction { get; set; }

    public List<ComponentModel> Components { get; set; } = new();
    public List<DataFlowModel> DataFlows { get; set; } = new();
    public List<UserRoleModel> UserRoles { get; set; } = new();
    public List<TrustBoundaryModel> TrustBoundaries { get; set; } = new();
    public List<AssetModel> Assets { get; set; } = new();
    public List<DesignNoteModel> DesignNotes { get; set; } = new();
    public List<ControlModel> Controls { get; set; } = new();

    public List<EntryPointModel> EntryPoints { get; set; } = new();
    public List<SensitiveDataModel> SensitiveDataItems { get; set; } = new();
    public List<ReviewItemModel> ReviewItems { get; set; } = new();
    public List<SnapshotModel> Snapshots { get; set; } = new();

    public List<ThreatModel> Threats { get; set; } = new();
    public List<RequirementModel> Requirements { get; set; } = new();

    public string OpenIssuesSummary { get; set; } = "";

    /// <summary>Eigenaar security (mens/team), voor governance en export.</summary>
    public string GovernanceSecurityOwner { get; set; } = "";

    /// <summary>Eigenaar techniek / product.</summary>
    public string GovernanceTechnicalOwner { get; set; } = "";

    /// <summary>Compliance / privacy aanspreekpunt of RACI-notitie.</summary>
    public string GovernanceComplianceStakeholder { get; set; } = "";

    /// <summary>Reviewritme (bv. kwartaal) of planning-notitie.</summary>
    public string GovernanceReviewCadence { get; set; } = "";

    /// <summary>Testdoel / opdrachtformulering (kick-off).</summary>
    public string AssessmentGoal { get; set; } = "";

    public AssessmentTestType AssessmentTestType { get; set; } = AssessmentTestType.Unspecified;

    /// <summary>In-scope samenvatting (systemen, omgevingen, accounts).</summary>
    public string ScopeIn { get; set; } = "";

    /// <summary>Expliciet buiten scope.</summary>
    public string ScopeOut { get; set; } = "";

    /// <summary>Afspraken / rules of engagement (geen juridisch contract).</summary>
    public string RulesOfEngagementNotes { get; set; } = "";

    /// <summary>Opdrachtgever-contact en escalatie (geen secrets).</summary>
    public string AssessmentContact { get; set; } = "";

    /// <summary>Testvenster, bijv. 1–12 sep, alleen kantooruren.</summary>
    public string AssessmentWindow { get; set; } = "";

    /// <summary>test / acc / prod of een vrije toelichting.</summary>
    public string AssessmentEnvironment { get; set; } = "";

    /// <summary>Testaccounts en rollen. Geen wachtwoorden.</summary>
    public string AssessmentAccounts { get; set; } = "";

    /// <summary>Beperkingen: WAF, testdata, verboden acties.</summary>
    public string AssessmentLimitations { get; set; } = "";

    /// <summary>Vrije toelichting rest-risico voor het rapport.</summary>
    public string AssessmentResidualNotes { get; set; } = "";

    /// <summary>Afgevinkte playbook-items (stabiele id's).</summary>
    public List<string> CompletedPlaybookItemIds { get; set; } = new();

    public List<PentestFindingModel> Findings { get; set; } = new();

    public List<CoverageItemModel> CoverageItems { get; set; } = new();

    public List<AttackSurfaceItemModel> AttackSurface { get; set; } = new();

    public List<TestBlockerModel> TestBlockers { get; set; } = new();

    /// <summary>Afgewezen suggesties (rule keys), lokaal opgeslagen.</summary>
    public List<string> DismissedSuggestionKeys { get; set; } = new();

    /// <summary>C4-elementen voor threatmodel-tab (los van architectuurdiagram).</summary>
    public List<C4ElementModel> C4Elements { get; set; } = new();

    /// <summary>C4-relaties (Mermaid Rel) tussen elementen; id 0 = systeem in scope op C1.</summary>
    public List<C4RelationModel> C4Relations { get; set; } = new();
}
