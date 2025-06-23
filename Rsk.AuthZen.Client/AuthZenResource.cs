using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents a resource in an AuthZen evaluation request, including its identifier, type, and additional properties.
    /// </summary>
    public class AuthZenResource
    {
        /// <summary>
        /// Gets the unique identifier of the resource.
        /// </summary>
        public string Id { get; internal set; }
    
        /// <summary>
        /// Gets the type of the resource.
        /// </summary>
        public string Type { get; internal set; }
    
        /// <summary>
        /// Gets additional properties associated with the resource.
        /// </summary>
        public Dictionary<string, object> Properties { get; internal set; }
    }
}