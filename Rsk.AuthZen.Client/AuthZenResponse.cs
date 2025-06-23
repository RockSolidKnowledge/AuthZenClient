namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents the response from an AuthZen evaluation, including the decision, context, and correlation identifier.
    /// </summary>
    public class AuthZenResponse
    {
        /// <summary>
        /// Gets the decision result of the evaluation.
        /// </summary>
        public Decision Decision { get; internal set; }
    
        /// <summary>
        /// Gets the context information associated with the evaluation response.
        /// </summary>
        public string Context { get; internal set; }
    
        /// <summary>
        /// Gets the correlation identifier for tracking the evaluation request and response.
        /// </summary>
        public string CorrelationId { get; internal set; }
    }
}