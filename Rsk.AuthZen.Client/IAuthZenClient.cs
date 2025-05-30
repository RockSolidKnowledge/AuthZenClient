using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rsk.AuthZen.Client.DTOs;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenClient
    {
        Task<AuthZenResponse> Evaluate(AuthZenEvaluationRequest evaluationRequest);
        Task<IEnumerable<AuthZenResponse>> Evaluate(IEnumerable<AuthZenEvaluationRequest> evaluationRequests, AuthZenEvaluationRequest requestDefaults);
        Task<IEnumerable<AuthZenResponse>> Evaluate(IEnumerable<AuthZenEvaluationRequest> evaluationRequests, AuthZenEvaluationRequest requestDefaults, AuthZenBoxcarOptions boxcarOptions);
    }
    
    public enum Decision
    {
        Permit,
        Deny
    }
    
    public class AuthZenResponse
    {
        public Decision Decision { get; set; }
        public string Context { get; set; }
        public string CorrelationId { get; set; }
    }

    public class AuthZenEvaluationRequest
    {
        internal AuthZenEvaluationRequest()
        {
            
        }
        
        public AuthZenSubject Subject { get;  internal set; }
        public AuthZenResource Resource { get;  internal set; }
        public AuthZenAction Action { get; internal set; }
        public Dictionary<string, object> Context { get; internal set; }
        public string CorrelationId { get; internal set; }
        
        internal AuthZenRequestMessageDto ToDto()
        {
            var dto = new AuthZenRequestMessageDto();

            if (Subject != null)
            {
                dto.Subject = new AuthZenSubjectDto
                {
                    Id = Subject.Id,
                    Type = Subject.Type,
                    Properties = Subject.Properties
                };
            }

            if (Resource != null)
            {
                dto.Resource = new AuthZenResourceDto
                {
                    Id = Resource.Id,
                    Type = Resource.Type,
                    Properties = Resource.Properties
                };
            }

            if (Action != null)
            {
                dto.Action = new AuthZenActionDto
                {
                    Name = Action.Name,
                    Properties = Action.Properties
                };
            }

            dto.Context = Context;
            
            return dto;
        }
    }

    public class AuthZenSubject
    {
        public string Id { get; internal set; }
        public string Type { get; internal set; }
        public Dictionary<string, object> Properties { get; internal set; }
    }
    
    public class AuthZenResource
    {
        public string Id { get; internal set; }
        public string Type { get; internal set; }
        public Dictionary<string, object> Properties { get; internal set; }
    }
    
    public class AuthZenAction
    {
        public string Name { get; internal set; }
        public Dictionary<string, object> Properties { get; internal set; }
    }
    
    public enum BoxcarSemantics
    {
        ExecuteAll,
        DenyOnFirstDeny,
        PermitOnFirstPermit
    }
    
    // execute_all
    // deny_on_first_deny
    // permit_on_first_permit
    public class AuthZenBoxcarOptions
    {
        public BoxcarSemantics Semantics { get; set; }


        public AuthZenBoxcarOptionsDto ToDto()
        {
            return new AuthZenBoxcarOptionsDto
            {
                Evaluation_semantics = ConvertSemantics(Semantics)
            };
        }

        private static string ConvertSemantics(BoxcarSemantics semantics)
        {
            return semantics switch
            {
                BoxcarSemantics.ExecuteAll => "execute_all",
                BoxcarSemantics.DenyOnFirstDeny => "deny_on_first_deny",
                BoxcarSemantics.PermitOnFirstPermit => "permit_on_first_permit",
                _ => throw new ArgumentException($"Semantics value {semantics} is not supported ")
            };
        }
    }
}