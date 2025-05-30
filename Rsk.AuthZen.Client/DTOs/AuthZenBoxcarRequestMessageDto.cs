using System.Collections.Generic;

namespace Rsk.AuthZen.Client.DTOs
{
    public class AuthZenBoxcarRequestMessageDto : AuthZenRequestMessageDto
    {
        public AuthZenBoxcarOptionsDto Options { get; set; }
        public AuthZenRequestMessageDto[] Evaluations { get; set; }
    }
    
    public class AuthZenBoxcarOptionsDto
    {
        public string Evaluation_semantics { get; set; }
    }
}