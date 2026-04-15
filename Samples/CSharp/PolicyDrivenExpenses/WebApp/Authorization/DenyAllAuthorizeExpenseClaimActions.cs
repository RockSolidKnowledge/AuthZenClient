    using WebApp.Domain;

    namespace WebApp.Authorization;

public sealed class DenyAllAuthorizeExpenseClaimActions : IAuthorizeExpenseClaimActions
{
    private static readonly AuthorizeResult Failure = new AuthorizeResult(false, ["All requests will be denied"]);
    
    public Task<AuthorizeResult> CanCreateClaim(string submitterUserId)
    {
        return Task.FromResult(Failure);
    }

    public Task<AuthorizeResult> CanSubmitClaim(string submitterUserId, decimal grossCost)
    {
        return Task.FromResult(Failure);
    }

    public Task<AuthorizeResult> CanApproveAndRejectClaims(string approverUserId)
    {
        return Task.FromResult(Failure);
    }

    public Task<AuthorizeResult> CanApproveClaims(string approverUserId, IEnumerable<IExpenseClaimSubmission> submission)
    {
        return Task.FromResult(Failure);
    }

    public Task<AuthorizeResult> CanRejectClaims(string approverUserId, IEnumerable<IExpenseClaimSubmission> submission)
    {
        return Task.FromResult(Failure);
    }
    
}

