using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Authorization;
using WebApp.Domain;

namespace WebApp.Pages.Expenses;

[Authorize]
public class CreateModel(
    IExpenseClaimService expenseClaimService,
    IAuthorizeExpenseClaimActions authorizeExpenseClaimActions,
    IGenerateAccessDeniedContent accessDeniedService) : PageModel
{
    [BindProperty]
    public ExpenseClaimSubmissionModel Submission { get; set; } = new();

    public bool Submitted { get; private set; }

    public IReadOnlyList<OpenExpenseClaimViewModel> ExistingClaims { get; private set; } = [];

    public IEnumerable<SelectListItem> CostCentres =>
    [
        new("G&A", "G&A"),
        new("R&D", "R&D"),
        new("Sales", "Sales"),
        new("PS", "PS"),
        new("Maintenance", "Maintenance")
    ];

    public async Task<IActionResult> OnGet()
    {
        Submission.ClaimDate = DateOnly.FromDateTime(DateTime.Today);

        var submitterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(submitterUserId))
        {
            return Challenge();
        }


        AuthorizeResult canCreateAuthorizationResult = await authorizeExpenseClaimActions.CanCreateClaim(submitterUserId);
        if (!canCreateAuthorizationResult.Success)
        {
            return accessDeniedService.Redirect(canCreateAuthorizationResult.Messages);
        }

        await LoadExistingClaims(submitterUserId);
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var submitterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(submitterUserId))
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadExistingClaims(submitterUserId);
            return Page();
        }

        var canSubmit = await authorizeExpenseClaimActions.CanSubmitClaim(
            submitterUserId,
            Submission.GrossCost!.Value);
        if (!canSubmit.Success)
        {
            return accessDeniedService.Redirect(canSubmit.Messages);
        }

        await expenseClaimService.Create(
            submitterUserId,
            Submission.Description,
            Submission.ClaimDate!.Value,
            Submission.GrossCost!.Value,
            Submission.Tax!.Value,
            Submission.CostCentre);

        Submitted = true;
        ModelState.Clear();
        Submission = new ExpenseClaimSubmissionModel
        {
            ClaimDate = DateOnly.FromDateTime(DateTime.Today)
        };

        await LoadExistingClaims(submitterUserId);
        return Page();
    }

    private async Task LoadExistingClaims(string submitterUserId)
    {
        var claims = await expenseClaimService.GetBySubmitterUserId(submitterUserId);
        ExistingClaims = claims
            .Where(c => c.Status != ExpenseClaimStatus.Approved)
            .OrderByDescending(c => c.ClaimDate)
            .Select(c => new OpenExpenseClaimViewModel(
                c.SubmissionId,
                c.Description,
                c.ClaimDate,
                c.GrossCost,
                c.Tax,
                c.CostCentre,
                c.Status))
            .ToList();
    }

    public sealed class ExpenseClaimSubmissionModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Claim Date")]
        public DateOnly? ClaimDate { get; set; }

        [Required]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        [Display(Name = "Gross Cost")]
        public decimal? GrossCost { get; set; }

        [Required]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        [Display(Name = "Tax")]
        public decimal? Tax { get; set; }

        [Required]
        [Display(Name = "Cost Centre")]
        public string CostCentre { get; set; } = string.Empty;
    }

    public sealed record OpenExpenseClaimViewModel(
        string SubmissionId,
        string Description,
        DateOnly ClaimDate,
        decimal GrossCost,
        decimal Tax,
        string CostCentre,
        ExpenseClaimStatus Status);
}
