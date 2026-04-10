using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages.Account;

public class Logout(SignInManager<IdentityUser> signInManager) : PageModel
{
private readonly SignInManager<IdentityUser> _signInManager = signInManager;

public async Task<IActionResult> OnPostAsync()
{
    await _signInManager.SignOutAsync();
    return RedirectToPage("/Home");
}
}