namespace Rsk.AuthZen.Client
{
    public class AuthZenMetadataResponse
    {
        public string PolicyDecisionPoint { get; set; } = null!;

        public string AccessEvaluationEndpoint { get; set; } = null!;
    
        public string AccessEvaluationsEndpoint { get; set; }
    
        public string SearchSubjectEndpoint { get; set; }
    
        public string SearchActionEndpoint { get; set; }
    
        public string SearchResourceEndpoint { get; set; }
    }
}