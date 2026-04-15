using AuthZenPolicyServer;
using Rsk.Enforcer;
using Rsk.Enforcer.AuthZen;
using Rsk.Enforcer.PAP.Store;
using Rsk.Enforcer.PEP;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddRazorPages();
        builder.Services
            .AddEnforcer("acmeCorp.global",options =>
            {
                options.Licensee = "DEMO";
                options.LicenseKey = "Get a free license from https://www.identityserver.com/products/enforcer";
            })
            .AddPolicyEnforcementPoint(o => o.Bias = PepBias.Deny)
            .AddAuthZen()
            .AddAuthZenAdvice()
            .AddPolicyAttributeProvider<SubjectAttributeProvider>()
            .AddEmbeddedPolicyStore(typeof(Program).Assembly, "AuthZenPolicyServer.Policies");
        
        var app = builder.Build();

        app.UseEnforcerAuthZen();
        app.UseStaticFiles();
        app.UseRouting();
        app.MapRazorPages();
        app.MapGet("/", context =>
        {
            context.Response.Redirect("/Home");
            return Task.CompletedTask;
        });
        app.Run();
    }
}