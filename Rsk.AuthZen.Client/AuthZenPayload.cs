namespace Rsk.AuthZen.Client
{
    public class AuthZenPayload<T>
    {
        public string CorrelationId { get; internal set; }
        public T Payload { get; internal set; }
    }
}