using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents an action in AuthZen, including its name and associated properties.
    /// </summary>
    public class AuthZenAction
    {
        /// <summary>
        /// Gets the name of the action.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the properties associated with the action.
        /// </summary>
        public Dictionary<string, object> Properties { get; internal set; }
    }
}