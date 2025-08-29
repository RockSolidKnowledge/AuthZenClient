using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents a subject in an AuthZen evaluation request, including its identifier, type, and additional properties.
    /// </summary>
    public class AuthZenSubject
    {
        /// <summary>
        /// Gets the unique identifier of the subject.
        /// </summary>
        public string Id { get; internal set; }
    
        /// <summary>
        /// Gets the type of the subject.
        /// </summary>
        public string Type { get; internal set; }
    
        /// <summary>
        /// Gets additional properties associated with the subject.
        /// </summary>
        public Dictionary<string, object> Properties { get; internal set; }
    }
}