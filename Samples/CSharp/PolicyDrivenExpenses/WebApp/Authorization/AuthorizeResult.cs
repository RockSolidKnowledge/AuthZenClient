namespace WebApp.Authorization;

public struct AuthorizeResult(bool success, IEnumerable<string> messages)
{
    public IEnumerable<string> Messages { get; } = messages;
    private static readonly IEnumerable<string> EmptyMessages = [];
    
    public AuthorizeResult(bool success) : this(success,EmptyMessages)
    {
        
    }

    public bool Success { get; } = success;
}