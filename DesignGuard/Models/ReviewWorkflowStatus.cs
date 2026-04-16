namespace DesignGuard.Models;

/// <summary>Status in het reviewboard; lichtgewicht workflow.</summary>
public enum ReviewWorkflowStatus
{
    Draft = 0,
    UnderReview = 1,
    Accepted = 2,
    Implemented = 3,
    Deferred = 4,
    Rejected = 5,
    NeedsClarification = 6
}
