using System;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Provides a fluent builder for constructing single AuthZen evaluation requests,
    /// allowing configuration of subject, resource, action, context, and correlation ID.
    /// </summary>
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
    
        /// <summary>
        /// Sets the correlation identifier for the evaluation request.
        /// </summary>
        /// <param name="id">The correlation ID to associate with the request.</param>
        /// <returns>The current builder instance.</returns>
        public IAuthZenSingleRequestBuilder SetCorrelationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Correlation ID must be provided", nameof(id));
            correlationId = id;
            return this;
        }
    
        /// <summary>
        /// Sets the subject for the evaluation request.
        /// </summary>
        /// <param name="id">The subject identifier.</param>
        /// <param name="type">The subject type.</param>
        /// <returns>A property bag for adding subject properties.</returns>
        public IAuthZenPropertyBag SetSubject(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));
            subjectId = id;
            subjectType = type;
            subjectProperties = new AuthZenPropertyBag();
            return subjectProperties;
        }
    
        /// <summary>
        /// Sets the resource for the evaluation request.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <param name="type">The resource type.</param>
        /// <returns>A property bag for adding resource properties.</returns>
        public IAuthZenPropertyBag SetResource(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));
            resourceId = id;
            resourceType = type;
            resourceProperties = new AuthZenPropertyBag();
            return resourceProperties;
        }
    
        /// <summary>
        /// Sets the action for the evaluation request.
        /// </summary>
        /// <param name="name">The action name.</param>
        /// <returns>A property bag for adding action properties.</returns>
        public IAuthZenPropertyBag SetAction(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must be provided", nameof(name));
            actionName = name;
            actionProperties = new AuthZenPropertyBag();
            return actionProperties;
        }
    
        /// <summary>
        /// Sets the context for the evaluation request.
        /// </summary>
        /// <returns>A property bag for adding context properties.</returns>
        public IAuthZenPropertyBag SetContext()
        {
            contextProperties = new AuthZenPropertyBag();
            return contextProperties;
        }
    
        /// <summary>
        /// Builds the <see cref="AuthZenEvaluationRequest"/> instance using the configured values.
        /// </summary>
        /// <returns>The constructed <see cref="AuthZenEvaluationRequest"/>.</returns>
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
    
                if (!subjectProperties.IsEmpty)
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
    
                if (!actionProperties.IsEmpty)
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