using Microsoft.AspNetCore.Identity;

namespace WebApp.Domain;

public sealed class InMemoryManagerLookupService(UserManager<IdentityUser> userManager) : IManagerLookupService
{
    private static readonly IReadOnlyDictionary<string, string> ManagerUserNameMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["alice"] = "bob",
            ["bob"] = "alice"
        };

    public async Task<string> GetManagerUserId(string submitterUserId)
    {
        if (string.IsNullOrWhiteSpace(submitterUserId))
        {
            throw new ArgumentException("Submitter user id is required.", nameof(submitterUserId));
        }

        var submitter = await userManager.FindByIdAsync(submitterUserId.Trim());
        if (submitter is null || string.IsNullOrWhiteSpace(submitter.UserName))
        {
            throw new InvalidOperationException($"Submitter user '{submitterUserId}' was not found.");
        }

        if (!ManagerUserNameMap.TryGetValue(submitter.UserName, out var managerUserName))
        {
            throw new InvalidOperationException($"No manager mapping exists for '{submitter.UserName}'.");
        }

        var manager = await userManager.FindByNameAsync(managerUserName);
        if (manager is null)
        {
            throw new InvalidOperationException($"Mapped manager '{managerUserName}' was not found.");
        }

        return manager.Id;
    }
}
