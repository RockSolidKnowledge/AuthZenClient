namespace Rsk.AuthZen.Client
{
    public class AuthZenPayload<T>
    {
        public string CorrelationId { get; set; }
        public T Payload { get; set; }
    }
}