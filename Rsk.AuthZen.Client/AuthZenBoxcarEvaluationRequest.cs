using System;
using System.Collections.Generic;
using Rsk.AuthZen.Client.DTOs;

namespace Rsk.AuthZen.Client
{
    public class AuthZenBoxcarEvaluationRequest
    {
        public List<AuthZenBoxcarEvaluation> Evaluations { get; internal set; }
        public AuthZenBoxcarEvaluation DefaultValues { get; internal set; }
        public AuthZenBoxcarOptions Options { get; internal set; }
        
        internal AuthZenBoxcarRequestMessageDto ToDto()
        {
            var dto = new AuthZenBoxcarRequestMessageDto();

            if (DefaultValues?.Subject != null)
            {
                dto.Subject = new AuthZenSubjectDto
                {
                    Id = DefaultValues.Subject.Id,
                    Type = DefaultValues.Subject.Type,
                    Properties = DefaultValues.Subject.Properties
                };
            }

            if (DefaultValues?.Resource != null)
            {
                dto.Resource = new AuthZenResourceDto
                {
                    Id = DefaultValues.Resource.Id,
                    Type = DefaultValues.Resource.Type,
                    Properties = DefaultValues.Resource.Properties
                };
            }

            if (DefaultValues?.Action != null)
            {
                dto.Action = new AuthZenActionDto
                {
                    Name = DefaultValues.Action.Name,
                    Properties = DefaultValues.Action.Properties
                };
            }

            dto.Context = DefaultValues?.Context;

            if (Evaluations != null && Evaluations.Count > 0)
            {
                dto.Evaluations = new AuthZenRequestMessageDto[Evaluations.Count];
                for (int i = 0; i < Evaluations.Count; i++)
                {
                    dto.Evaluations[i] = Evaluations[i].ToDto();
                }
            }
            
            if (Options != null)
            {
                dto.Options = Options.ToDto();
            }

            return dto;
        }
    }

    public class AuthZenBoxcarResponse
    {
        public string CorrelationId { get; internal set; }
        public List<AuthZenResponse> Evaluations { get; internal set; }
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
        public BoxcarSemantics Semantics { get; internal set; }
        
        internal AuthZenBoxcarOptionsDto ToDto()
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

    public class AuthZenBoxcarEvaluation
    {
        public AuthZenSubject Subject { get; internal set; }
        public AuthZenResource Resource { get; internal set; }
        public AuthZenAction Action { get; internal set; }
        public Dictionary<string, object> Context { get; internal set; }

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
}