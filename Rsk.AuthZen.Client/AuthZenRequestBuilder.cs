using System;

namespace Rsk.AuthZen.Client
{
    internal class AuthZenRequestBuilder : IAuthZenRequestBuilder
    {
        private string subjectId;
        private string subjectType;
        private string resourceId;
        private string resourceType;
        private string actionName;
        private string correlationId;

        private AuthZenPropertyBag subjectProperties;
        private AuthZenPropertyBag resourceProperties;
        private AuthZenPropertyBag actionProperties;
        private AuthZenPropertyBag contextProperties;
        
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
            var request = new AuthZenEvaluationRequest();

            if (subjectId != null)
            {
                request.Subject = new AuthZenSubject
                {
                    Id = subjectId,
                    Type = subjectType,
                };
                
                if(!subjectProperties.IsEmpty)
                {
                    request.Subject.Properties = subjectProperties.Build();
                }
            }

            if (resourceId != null)
            {
                request.Resource = new AuthZenResource
                {
                    Id = resourceId,
                    Type = resourceType,
                };

                if (!resourceProperties.IsEmpty)
                {
                    request.Resource.Properties = resourceProperties.Build();
                }
            }

            if (actionName != null)
            {
                request.Action = new AuthZenAction
                {
                    Name = actionName,
                };
                
                if(!actionProperties.IsEmpty)
                {
                    request.Action.Properties = actionProperties.Build();
                }
            }

            if (contextProperties != null && !contextProperties.IsEmpty)
            {
                request.Context = contextProperties.Build();
            }

            if (correlationId != null)
            {
                request.CorrelationId = correlationId;
            }
            
            return request;
        }

        public IAuthZenRequestBuilder SetCorrelationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Correlation ID must be provided", nameof(id));
            
            correlationId = id;

            return this;
        }
    }
}