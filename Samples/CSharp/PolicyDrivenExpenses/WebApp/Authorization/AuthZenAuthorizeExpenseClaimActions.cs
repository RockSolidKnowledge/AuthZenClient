using System.Text.Json;
using Rsk.AuthZen.Client;
using WebApp.Domain;

namespace WebApp.Authorization;

public class AuthZenAuthorizeExpenseClaimActions(IAuthZenClient client) : IAuthorizeExpenseClaimActions
{
    public Task<AuthorizeResult> CanCreateClaim(string submitterUserId)
    {
        AuthZenSingleRequestBuilder requestBuilder = new AuthZenSingleRequestBuilder();

        requestBuilder
            .SetSubject(submitterUserId, "user");
        
        requestBuilder.SetAction("CreateClaim");
        requestBuilder.SetResource("newExpense", "expenses");
        
        var request = requestBuilder.Build();

        return Authorize(request);
    }

    
    public Task<AuthorizeResult> CanSubmitClaim(string submitterUserId, decimal grossCost)
    {
        AuthZenSingleRequestBuilder requestBuilder = new AuthZenSingleRequestBuilder();

        requestBuilder
            .SetSubject(submitterUserId, "user");
        
        requestBuilder.SetAction("SubmitClaim");
        requestBuilder
            .SetResource("newExpense", "expenses")
            .Add("total",grossCost);
        
        var request = requestBuilder.Build();

        return Authorize(request);
    }

    public Task<AuthorizeResult> CanApproveAndRejectClaims(string approverUserId)
    {
        AuthZenSingleRequestBuilder requestBuilder = new AuthZenSingleRequestBuilder();

        requestBuilder
            .SetSubject(approverUserId, "user");
        
        requestBuilder
            .SetAction("ListClaimsToApprove");
        
        requestBuilder
            .SetResource("expenseList", "expenses")
            ;
        
        var request = requestBuilder.Build();

        return Authorize(request);
    }

    public Task<AuthorizeResult> CanApproveClaims(string approverUserId, IEnumerable<IExpenseClaimSubmission> submissions)
    {
        return  AuthorizeClaimSubmissionsAction(approverUserId, submissions,"AcceptClaim");
    }
    
    public Task<AuthorizeResult> CanRejectClaims(string approverUserId, IEnumerable<IExpenseClaimSubmission> submissions)
    {
        return  AuthorizeClaimSubmissionsAction(approverUserId, submissions,"RejectClaim");
    }
    
    private async Task<AuthorizeResult> AuthorizeClaimSubmissionsAction(string approverUserId, 
        IEnumerable<IExpenseClaimSubmission> submissions , string action)
    {
        var boxCarRequestBuilder = new AuthZenBoxcarRequestBuilder();

        foreach (IExpenseClaimSubmission submission in submissions)
        {
            var rb = boxCarRequestBuilder.AddRequest();
            rb.SetAction(action);
            rb.SetSubject(approverUserId, "user");
            rb.SetResource(submission.SubmissionId, "expenses")
                .Add("approver", submission.ApproverUserId);
        }

        AuthZenBoxcarEvaluationRequest? request = boxCarRequestBuilder.Build();

        AuthZenBoxcarResponse? result = await client.Evaluate(request);
        
        bool success =  result.Evaluations.All(e => e.Decision == Decision.Permit);
        return new AuthorizeResult(success);
    }
    
    private async Task<AuthorizeResult> Authorize(AuthZenEvaluationRequest request)
    {
        AuthZenResponse response = await client.Evaluate(request);
        bool success = response.Decision == Decision.Permit;

        if (success == false)
        {
            string? error = null;

            if (JsonSerializer.Deserialize<JsonElement>(response.Context)
                .TryGetProperty("error", out JsonElement errorProperty))
            {
                error = errorProperty.GetString();
            }

            return new AuthorizeResult(false, [error ?? String.Empty]);
        }
        
        return new AuthorizeResult(success);
    }
}