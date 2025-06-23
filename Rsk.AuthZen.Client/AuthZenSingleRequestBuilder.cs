using System;

namespace Rsk.AuthZen.Client
{
    public class AuthZenSingleRequestBuilder : IAuthZenSingleRequestBuilder
    {
        private string correlationId;
        
        private string subjectId;
        private string subjectType;
        private string resourceId;
        private string resourceType;
        private string actionName;

        private AuthZenPropertyBag subjectProperties;
        private AuthZenPropertyBag resourceProperties;
        private AuthZenPropertyBag actionProperties;
        private AuthZenPropertyBag contextProperties;
        
        public IAuthZenSingleRequestBuilder SetCorrelationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Correlation ID must be provided", nameof(id));
            
            correlationId = id;

            return this;
        }
        
        public IAuthZenPropertyBag SetSubject(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));
            
            subjectId = id;
            subjectType = type;
            subjectProperties = new AuthZenPropertyBag();
            return subjectProperties;
        }

        public IAuthZenPropertyBag SetResource(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));

            resourceId = id;
            resourceType = type;
            resourceProperties = new AuthZenPropertyBag();
            return resourceProperties;
        }

        public IAuthZenPropertyBag SetAction(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must be provided", nameof(name));

            actionName = name;
            actionProperties = new AuthZenPropertyBag();
            return actionProperties;
        }

        public IAuthZenPropertyBag SetContext()
        {
            contextProperties = new AuthZenPropertyBag();
            return contextProperties;
        }

        public AuthZenEvaluationRequest Build()
        {
            var body = new AuthZenEvaluationBody();

            if (subjectId != null)
            {
                body.Subject = new AuthZenSubject
                {
                    Id = subjectId,
                    Type = subjectType,
                };
                
                if(!subjectProperties.IsEmpty)
                {
                    body.Subject.Properties = subjectProperties.Build();
                }
            }

            if (resourceId != null)
            {
                body.Resource = new AuthZenResource
                {
                    Id = resourceId,
                    Type = resourceType,
                };

                if (!resourceProperties.IsEmpty)
                {
                    body.Resource.Properties = resourceProperties.Build();
                }
            }

            if (actionName != null)
            {
                body.Action = new AuthZenAction
                {
                    Name = actionName,
                };
                
                if(!actionProperties.IsEmpty)
                {
                    body.Action.Properties = actionProperties.Build();
                }
            }

            if (contextProperties != null && !contextProperties.IsEmpty)
            {
                body.Context = contextProperties.Build();
            }

            return new AuthZenEvaluationRequest
            {
                Body = body,
                CorrelationId = correlationId
            };
        }
    }
}