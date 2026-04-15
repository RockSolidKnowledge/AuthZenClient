namespace WebApp.Domain;

public enum ExpenseClaimStatus
{
    Submitted,
    Rejected,
    Approved
}

public interface IExpenseClaimSubmission
{
    string SubmissionId { get; }
    string SubmitterUserId { get; }
    string ApproverUserId { get; }
    string Description { get; }
    DateOnly ClaimDate { get; }
    decimal GrossCost { get; }
    decimal Tax { get; }
    string CostCentre { get; }
    ExpenseClaimStatus Status { get; }
}
