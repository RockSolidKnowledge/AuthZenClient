using WebApp.Domain;

namespace WebApp.Authorization;

public sealed class AllowAllAuthorizeExpenseClaimActions : IAuthorizeExpenseClaimActions
{
    private static readonly AuthorizeResult Success = new AuthorizeResult(true);
    
    public Task<AuthorizeResult> CanCreateClaim(string submitterUserId)
    {
        return Task.FromResult(Success);
    }

    public Task<AuthorizeResult> CanSubmitClaim(string submitterUserId, decimal grossCost)
    {
        return Task.FromResult(Success);
    }

    public Task<AuthorizeResult> CanApproveAndRejectClaims(string approverUserId)
    {
        return Task.FromResult(Success);
    }

    public Task<AuthorizeResult> CanApproveClaims(string approverUserId, IEnumerable<IExpenseClaimSubmission> submission)
    {
        return Task.FromResult(Success);
    }

    public Task<AuthorizeResult> CanRejectClaims(string approverUserId, IEnumerable<IExpenseClaimSubmission> submission)
    {
        return Task.FromResult(Success);
    }
}

