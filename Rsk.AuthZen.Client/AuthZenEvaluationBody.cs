using System.Collections.Generic;
using Rsk.AuthZen.Client.DTOs;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents the body of an evaluation request in AuthZen, including subject, resource, action, and context.
    /// </summary>
    public class AuthZenEvaluationBody
    {
        /// <summary>
        /// Gets the subject for the evaluation request.
        /// </summary>
        public AuthZenSubject Subject { get; internal set; }
    
        /// <summary>
        /// Gets the resource for the evaluation request.
        /// </summary>
        public AuthZenResource Resource { get; internal set; }
    
        /// <summary>
        /// Gets the action to be evaluated.
        /// </summary>
        public AuthZenAction Action { get; internal set; }
    
        /// <summary>
        /// Gets the context data for the evaluation request.
        /// </summary>
        public Dictionary<string, object> Context { get; internal set; }
        
        /// <summary>
        /// Converts this instance to a <see cref="AuthZenRequestMessageDto"/> for transmission.
        /// </summary>
        /// <returns>The corresponding <see cref="AuthZenRequestMessageDto"/>.</returns>
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