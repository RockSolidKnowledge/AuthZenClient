using System.Collections.Generic;
using Rsk.AuthZen.Client.DTOs;

namespace Rsk.AuthZen.Client
{
    public class AuthZenEvaluationRequest
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