namespace DesignGuard.Models;

/// <summary>Levenscyclus van een maatregel in het project (geen compliance-claim).</summary>
public enum ControlLifecycleStatus
{
    Draft = 0,
    Proposed = 1,
    UnderReview = 2,
    Accepted = 3,
    Implemented = 4,
    Deferred = 5,
    Rejected = 6
}
