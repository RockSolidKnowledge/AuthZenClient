using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    public class AuthZenResource
    {
        public string Id { get; internal set; }
        public string Type { get; internal set; }
        public Dictionary<string, object> Properties { get; internal set; }
    }
}