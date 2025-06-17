using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    public class AuthZenAction
    {
        public string Name { get; internal set; }
        public Dictionary<string, object> Properties { get; internal set; }
    }
}