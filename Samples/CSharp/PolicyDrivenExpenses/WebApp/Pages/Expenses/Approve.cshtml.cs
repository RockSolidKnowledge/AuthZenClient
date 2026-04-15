using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Authorization;
using WebApp.Domain;

namespace WebApp.Pages.Expenses;

[Authorize]
public class ApproveModel(IExpenseClaimService expenseClaimService, IGenerateAccessDeniedContent accessDenied, IAuthorizeExpenseClaimActions pep) : PageModel
{
    [BindProperty]
    public List<string> SelectedClaimIds { get; set; } = [];

    [BindProperty]
    public string Action { get; set; } = string.Empty;

    public IReadOnlyList<PendingApprovalViewModel> Claims { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        var approverUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(approverUserId))
        {
            return Challenge();
        }

        AuthorizeResult canApproveAndRejectClaims = await pep.CanApproveAndRejectClaims(approverUserId); 
        if (!canApproveAndRejectClaims.Success)
        {
            return accessDenied.Redirect(canApproveAndRejectClaims.Messages);
        }
        
        await LoadExpenseClaims(approverUserId);
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var approverUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(approverUserId))
        {
            return Challenge();
        }

        var action = Action?.Trim().ToLowerInvariant();
        if (action is not ("approve" or "reject"))
        {
            StatusMessage = "Choose an action.";
            await LoadExpenseClaims(approverUserId);
            return Page();
        }

        var assignedClaims = await expenseClaimService.GetByApproverUserId(approverUserId);
        Dictionary<string,IExpenseClaimSubmission> allowedIds = assignedClaims
            .Where(c => c.Status == ExpenseClaimStatus.Submitted)
            .ToDictionary(e => e.SubmissionId,e=>e);

        List<string> selectedIds = SelectedClaimIds
            .Where(id => !string.IsNullOrWhiteSpace(id) && allowedIds.ContainsKey(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<IExpenseClaimSubmission> expensesToActUpon = assignedClaims.Where(c => selectedIds.Contains(c.SubmissionId)).ToList();

        Func<string, IEnumerable<IExpenseClaimSubmission>, Task<AuthorizeResult>> authorizationCall =
            action == "approve" ? pep.CanApproveClaims : pep.CanRejectClaims;
        
        AuthorizeResult claimActionAuthorizationResult = await authorizationCall(approverUserId, expensesToActUpon );

        if (!claimActionAuthorizationResult.Success)
        {
            return accessDenied.Redirect(claimActionAuthorizationResult.Messages);
        }
        
        foreach (var submissionId in selectedIds)
        {
            var nextStatus = action == "approve" ? ExpenseClaimStatus.Approved : ExpenseClaimStatus.Rejected;
            await expenseClaimService.UpdateStatus(submissionId, nextStatus);
        }

        StatusMessage = selectedIds.Count == 0
            ? "No claims were selected."
            : $"{(action == "approve" ? "Approved" : "Rejected")} {selectedIds.Count} claim(s).";

        SelectedClaimIds.Clear();
        await LoadExpenseClaims(approverUserId);
        return Page();
    }

    private async Task LoadExpenseClaims(string approverUserId)
    {
        var claims = await expenseClaimService.GetByApproverUserId(approverUserId);

        Claims = claims
            .Where(c => c.Status == ExpenseClaimStatus.Submitted)
            .OrderBy(c => c.ClaimDate)
            .Select(c => new PendingApprovalViewModel(
                c.SubmissionId,
                c.SubmitterUserId,
                c.Description,
                c.ClaimDate,
                c.GrossCost,
                c.Tax,
                c.CostCentre,
                c.Status))
            .ToList();
    }

    public sealed record PendingApprovalViewModel(
        string SubmissionId,
        string SubmitterUserId,
        string Description,
        DateOnly ClaimDate,
        decimal GrossCost,
        decimal Tax,
        string CostCentre,
        ExpenseClaimStatus Status);
}
