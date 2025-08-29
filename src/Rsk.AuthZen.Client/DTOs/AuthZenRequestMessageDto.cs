using System.Collections.Generic;

namespace Rsk.AuthZen.Client.DTOs
{
    internal class AuthZenRequestMessageDto
    {
        public AuthZenSubjectDto Subject { get;  set; }
        public AuthZenResourceDto Resource { get;  set; }
        public AuthZenActionDto Action { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }

    internal class AuthZenSubjectDto
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
    
    internal class AuthZenResourceDto
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
    
    internal class AuthZenActionDto
    {
        public string Name { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}