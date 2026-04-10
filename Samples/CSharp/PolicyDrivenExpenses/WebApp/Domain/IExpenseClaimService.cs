namespace WebApp.Domain;

public interface IExpenseClaimService
{
    Task<IExpenseClaimSubmission> Create(
        string submitterUserId,
        string description,
        DateOnly claimDate,
        decimal grossCost,
        decimal tax,
        string costCentre);

    Task<IExpenseClaimSubmission?> GetById(string submissionId);

    Task<IReadOnlyList<IExpenseClaimSubmission>> GetBySubmitterUserId(string submitterUserId);

    Task<IReadOnlyList<IExpenseClaimSubmission>> GetByApproverUserId(string approverUserId);

    Task<IExpenseClaimSubmission> UpdateStatus(
        string submissionId,
        ExpenseClaimStatus status);
}
