using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Domain;

public interface IGenerateAccessDeniedContent
{
    ActionResult Redirect(IEnumerable<string> messages);
    IEnumerable<string> Messages(string reasonCode);
}



public class InMemoryGeneratedAccessDeniedContent : IGenerateAccessDeniedContent
{
    private ConcurrentDictionary<string, IEnumerable<string>> messagesMap = new();
    
    public ActionResult Redirect(IEnumerable<string> messages)
    {
        string reasonCode = Guid.NewGuid().ToString();
        messagesMap.TryAdd(reasonCode, messages);
        return new RedirectToPageResult("/AccessDenied",  null , new
        {
            reason = reasonCode
        }, null);
    }

    public IEnumerable<string> Messages(string reasonCode)
    {
        messagesMap.TryRemove(reasonCode, out IEnumerable<string>? messages);
        return messages ?? [];
    }
}