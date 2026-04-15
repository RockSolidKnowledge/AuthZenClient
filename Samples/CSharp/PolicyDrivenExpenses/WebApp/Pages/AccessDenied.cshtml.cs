using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Domain;

namespace WebApp.Pages;

public class AccessDeniedModel(IGenerateAccessDeniedContent accessDeniedContent) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Reason { get; set; }

    public IEnumerable<string> Messages => accessDeniedContent.Messages(Reason ?? "");

    public void OnGet()
    {
    }
}
