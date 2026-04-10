using System.Collections.Concurrent;

namespace WebApp.Domain;

public sealed class InMemoryExpenseClaimService(IManagerLookupService managerLookupService) : IExpenseClaimService
{
    private readonly IManagerLookupService managerLookupService = managerLookupService;
    private static readonly ConcurrentDictionary<string, ExpenseClaimSubmission> claims = new();

    public async Task<IExpenseClaimSubmission> Create(
        string submitterUserId,
        string description,
        DateOnly claimDate,
        decimal grossCost,
        decimal tax,
        string costCentre)
    {
        ValidateCreateInput(submitterUserId, description, grossCost, tax, costCentre);

        var approverUserId = await managerLookupService.GetManagerUserId(submitterUserId);
        if (string.IsNullOrWhiteSpace(approverUserId))
        {
            throw new InvalidOperationException($"No approver found for submitter '{submitterUserId}'.");
        }

        var claim = new ExpenseClaimSubmission(
            SubmissionId: Guid.NewGuid().ToString("N"),
            SubmitterUserId: submitterUserId.Trim(),
            ApproverUserId: approverUserId.Trim(),
            Description: description.Trim(),
            ClaimDate: claimDate,
            GrossCost: grossCost,
            Tax: tax,
            CostCentre: costCentre.Trim(),
            Status: ExpenseClaimStatus.Submitted);

        claims[claim.SubmissionId] = claim;
        return claim;
    }

    public Task<IExpenseClaimSubmission?> GetById(string submissionId)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
        {
            return Task.FromResult<IExpenseClaimSubmission?>(null);
        }

        claims.TryGetValue(submissionId, out var claim);
        return Task.FromResult<IExpenseClaimSubmission?>(claim);
    }

    public Task<IReadOnlyList<IExpenseClaimSubmission>> GetBySubmitterUserId(string submitterUserId)
    {
        if (string.IsNullOrWhiteSpace(submitterUserId))
        {
            return Task.FromResult<IReadOnlyList<IExpenseClaimSubmission>>([]);
        }

        var normalized = submitterUserId.Trim();
        var results = claims.Values
            .Where(c => string.Equals(c.SubmitterUserId, normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.ClaimDate)
            .Cast<IExpenseClaimSubmission>()
            .ToList();

        return Task.FromResult<IReadOnlyList<IExpenseClaimSubmission>>(results);
    }

    public Task<IReadOnlyList<IExpenseClaimSubmission>> GetByApproverUserId(string approverUserId)
    {
        if (string.IsNullOrWhiteSpace(approverUserId))
        {
            return Task.FromResult<IReadOnlyList<IExpenseClaimSubmission>>([]);
        }

        var normalized = approverUserId.Trim();
        var results = claims.Values
            .Where(c => string.Equals(c.ApproverUserId, normalized, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.ClaimDate)
            .Cast<IExpenseClaimSubmission>()
            .ToList();

        return Task.FromResult<IReadOnlyList<IExpenseClaimSubmission>>(results);
    }

    public Task<IExpenseClaimSubmission> UpdateStatus(string submissionId, ExpenseClaimStatus status)
    {
        if (string.IsNullOrWhiteSpace(submissionId))
        {
            throw new ArgumentException("Submission id is required.", nameof(submissionId));
        }

        if (!claims.TryGetValue(submissionId, out var existing))
        {
            throw new KeyNotFoundException($"No expense claim found with id '{submissionId}'.");
        }

        var updated = existing with { Status = status };
        claims[submissionId] = updated;

        return Task.FromResult<IExpenseClaimSubmission>(updated);
    }

    private static void ValidateCreateInput(
        string submitterUserId,
        string description,
        decimal grossCost,
        decimal tax,
        string costCentre)
    {
        if (string.IsNullOrWhiteSpace(submitterUserId))
        {
            throw new ArgumentException("Submitter user id is required.", nameof(submitterUserId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (grossCost <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grossCost), "Gross cost must be greater than zero.");
        }

        if (tax < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tax), "Tax cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(costCentre))
        {
            throw new ArgumentException("Cost centre is required.", nameof(costCentre));
        }
    }

    private sealed record ExpenseClaimSubmission(
        string SubmissionId,
        string SubmitterUserId,
        string ApproverUserId,
        string Description,
        DateOnly ClaimDate,
        decimal GrossCost,
        decimal Tax,
        string CostCentre,
        ExpenseClaimStatus Status) : IExpenseClaimSubmission;
}
