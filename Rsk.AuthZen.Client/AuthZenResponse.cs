namespace Rsk.AuthZen.Client
{
    public class AuthZenResponse
    {
        public Decision Decision { get; internal set; }
        public string Context { get; internal set; }
        public string CorrelationId { get; internal set; }
    }
}