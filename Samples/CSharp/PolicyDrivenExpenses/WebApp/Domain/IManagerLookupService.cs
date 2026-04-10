namespace WebApp.Domain;

public interface IManagerLookupService
{
    Task<string> GetManagerUserId(string submitterUserId);
}
