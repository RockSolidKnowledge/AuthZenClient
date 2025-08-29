namespace Rsk.AuthZen.Client.DTOs
{
    internal class AuthZenBoxcarRequestMessageDto : AuthZenRequestMessageDto
    {
        public AuthZenBoxcarOptionsDto Options { get; set; }
        public AuthZenRequestMessageDto[] Evaluations { get; set; }
    }
    
    internal class AuthZenBoxcarOptionsDto
    {
        public string Evaluations_semantic { get; set; }
    }
}