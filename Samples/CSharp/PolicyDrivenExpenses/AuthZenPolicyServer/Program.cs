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
                options.LicenseKey = "eyJhdXRoIjoiREVNTyIsImV4cCI6IjIwMjYtMDQtMzBUMDA6MDA6MDAiLCJpYXQiOiIyMDI2LTAzLTMwVDExOjUwOjA1LjQxMDU5NTFaIiwib3JnIjoiREVNTyIsImF1ZCI6N30=.mtYt37KGtQFJ5je0XJGckWrOx6lqCF5QwraMPJyGFgzYOq8sAFARoIjCKpJ0JpbpCRbCcaTFFhekfHU6NLvJka/ZzfsOYM4JHBSQpol2Z38PwkR4p8J6ONBi/SYOIvXrTk48Tf09Tvo2WHeoiZ9/MLu4IN7+w8sib0fUdkt/cY1PKHHzofBBHPsOT4/LOyxZoVIFLsINC5IOCkGf1vkmCADVFTszOY5nwUf3CNBs+C6UwfpHnvggnMnZpanW45WoWDDcQHgxwS13LgH6k+0XBUrPdcFhTR9mlSuboDspctvVeNASUBWcSLLdGY7GhK2RAWEAf9bbsTrSHErqIK+gx0XcDaq+n94q/qW3swJGGjUlcj+PaGPhmoEojYfwFWWZU6y4dz45XC941GpsYZEGYSVos5+oJMdreCOZqoPXhjEiqmRDgNT7llQ4bixr9voW3N1WKrfy6Ftr2ZYPv/tSOZb3wofGkpLSpPAw/XiyWUOkIiuVajR9CM8//pWQCOZodL1/xuXlioW8EVECXoGDhreDaGhc5BIEycJC/Fv0rgrnFxrPbStm8z+jmigGhN7G7quXaZr+VHhr+WEgjqbB3MSUhR1f/jwjKtiQMEoU7EDiC9BsNkV+KmGKr+o23HlvM2mwE5/rOa/ORgJ3LZmad2yBi6CYge8lwmSLWABMEmc=";
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