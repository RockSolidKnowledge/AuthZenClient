using WebApp.Domain;

namespace WebApp.Authorization;

public interface IAuthorizeExpenseClaimActions
{
    Task<AuthorizeResult> CanCreateClaim(string submitterUserId);
    Task<AuthorizeResult> CanSubmitClaim(string submitterUserId, decimal grossCost);

    Task<AuthorizeResult> CanApproveAndRejectClaims(string approverUserId);
    Task<AuthorizeResult> CanApproveClaims(string approverUserId , IEnumerable<IExpenseClaimSubmission> submission);
    Task<AuthorizeResult> CanRejectClaims(string approverUserId , IEnumerable<IExpenseClaimSubmission> submission);
}