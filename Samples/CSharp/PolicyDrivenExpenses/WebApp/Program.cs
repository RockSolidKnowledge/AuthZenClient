using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Rsk.AuthZen.Client;
using WebApp;
using WebApp.Authorization;
using WebApp.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("AuthZenIdentity"));

builder.Services
    .AddIdentityApiEndpoints<IdentityUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/AccessDenied";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.AddAuthorization();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IManagerLookupService, InMemoryManagerLookupService>();
builder.Services.AddScoped<IExpenseClaimService, InMemoryExpenseClaimService>();
builder.Services.AddScoped<IAuthorizeExpenseClaimActions, AuthZenAuthorizeExpenseClaimActions>();
builder.Services.AddSingleton<IGenerateAccessDeniedContent, InMemoryGeneratedAccessDeniedContent>();

builder.Services.AddHttpClient();

builder.Services.Configure<AuthZenClientOptions>(options =>
{
    options.AuthorizationUrl = "https://localhost:7064";
});
builder.Services.AddTransient<IAuthZenClient, AuthZenClient>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization(); 

app.MapGet("/", () => Results.Redirect("/Home"));

app.MapRazorPages();
app.MapIdentityApi<IdentityUser>();

await SeedUsersAsync(app.Services);

app.Run();

static async Task SeedUsersAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    await EnsureUserAsync(userManager, "alice", "alice@example.local", "Passw0rd!");
    await EnsureUserAsync(userManager, "bob", "bob@example.local", "Passw0rd!");
}

static async Task EnsureUserAsync(UserManager<IdentityUser> userManager, string userName, string email, string password)
{
    var existing = await userManager.FindByNameAsync(userName);
    if (existing is not null)
    {
        return;
    }

    var user = new IdentityUser
    {
        UserName = userName,
        Email = email,
        EmailConfirmed = true,
        Id = userName
    };

    
    var result = await userManager.CreateAsync(user, password);
    
    if (!result.Succeeded)
    {
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to seed user '{userName}': {errors}");
    }
    
}
