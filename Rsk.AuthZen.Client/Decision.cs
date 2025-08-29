namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Specifies the possible decisions returned by an AuthZen evaluation.
    /// </summary>
    public enum Decision
    {
        /// <summary>
        /// The request is permitted.
        /// </summary>
        Permit,
    
        /// <summary>
        /// The request is denied.
        /// </summary>
        Deny
    }
}