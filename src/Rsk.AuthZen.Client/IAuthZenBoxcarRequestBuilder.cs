using System;
using System.Collections.Generic;
using System.Linq;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Provides a fluent builder interface for constructing AuthZen boxcar evaluation requests,
    /// supporting the configuration of default subject, resource, action, context, and the addition of individual requests.
    /// </summary>
    public interface IAuthZenBoxcarRequestBuilder
    {
        /// <summary>
        /// Sets the correlation identifier for the boxcar evaluation request.
        /// </summary>
        /// <param name="correlationId">The correlation ID to associate with the request.</param>
        /// <returns>The current builder instance.</returns>
        IAuthZenBoxcarRequestBuilder SetCorrelationId(string correlationId);
    
        /// <summary>
        /// Sets the default subject for all evaluation requests in the boxcar.
        /// </summary>
        /// <param name="id">The subject identifier.</param>
        /// <param name="type">The subject type.</param>
        /// <returns>A property bag for adding subject properties.</returns>
        IAuthZenPropertyBag SetDefaultSubject(string id, string type);
    
        /// <summary>
        /// Sets the default resource for all evaluation requests in the boxcar.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <param name="type">The resource type.</param>
        /// <returns>A property bag for adding resource properties.</returns>
        IAuthZenPropertyBag SetDefaultResource(string id, string type);
    
        /// <summary>
        /// Sets the default action for all evaluation requests in the boxcar.
        /// </summary>
        /// <param name="name">The action name.</param>
        /// <returns>A property bag for adding action properties.</returns>
        IAuthZenPropertyBag SetDefaultAction(string name);
    
        /// <summary>
        /// Sets the default context for all evaluation requests in the boxcar.
        /// </summary>
        /// <returns>A property bag for adding context properties.</returns>
        IAuthZenPropertyBag SetDefaultContext();
    
        /// <summary>
        /// Adds a new evaluation request to the boxcar.
        /// </summary>
        /// <returns>A builder for configuring the individual evaluation request.</returns>
        IAuthZenRequestBuilder AddRequest();
    
        /// <summary>
        /// Builds the <see cref="AuthZenBoxcarEvaluationRequest"/> instance using the configured values and requests.
        /// </summary>
        /// <returns>The constructed <see cref="AuthZenBoxcarEvaluationRequest"/>.</returns>
        AuthZenBoxcarEvaluationRequest Build();
    }

    /// <summary>
    /// Represents a boxcar evaluation request for AuthZen, including a correlation identifier and the request body.
    /// </summary>
    public class AuthZenBoxcarEvaluationRequest
    {
        /// <summary>
        /// Gets the correlation identifier for tracking the boxcar evaluation request.
        /// </summary>
        public string CorrelationId { get; internal set; }
    
        /// <summary>
        /// Gets the body of the boxcar evaluation request, containing evaluation details.
        /// </summary>
        public AuthZenBoxcarEvaluationBody Body { get; internal set; }
    }
    
    /// <summary>
    /// Provides a fluent builder for constructing AuthZen boxcar evaluation requests,
    /// allowing configuration of default subject, resource, action, context, evaluation semantics,
    /// and the addition of individual evaluation requests.
    /// </summary>
    public class AuthZenBoxcarRequestBuilder : IAuthZenBoxcarRequestBuilder
    {
        private string correlationId;
        private string defaultSubjectId;
        private string defaultSubjectType;
        private string defaultResourceId;
        private string defaultResourceType;
        private string defaultActionName;
    
        private AuthZenPropertyBag defaultSubjectProperties;
        private AuthZenPropertyBag defaultResourceProperties;
        private AuthZenPropertyBag defaultActionProperties;
        private AuthZenPropertyBag defaultContextProperties;
    
        private List<AuthZenBoxcarEvaluationRequestBuilder> evaluationBuilders = new List<AuthZenBoxcarEvaluationRequestBuilder>();
        private AuthZenBoxcarOptions options;
    
        /// <summary>
        /// Sets the correlation identifier for the boxcar evaluation request.
        /// </summary>
        /// <param name="id">The correlation ID to associate with the request.</param>
        /// <returns>The current builder instance.</returns>
        public IAuthZenBoxcarRequestBuilder SetCorrelationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Correlation ID must be provided", nameof(id));
            correlationId = id;
            return this;
        }
    
        /// <summary>
        /// Sets the default subject for all evaluation requests in the boxcar.
        /// </summary>
        /// <param name="id">The subject identifier.</param>
        /// <param name="type">The subject type.</param>
        /// <returns>A property bag for adding subject properties.</returns>
        public IAuthZenPropertyBag SetDefaultSubject(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));
            defaultSubjectId = id;
            defaultSubjectType = type;
            defaultSubjectProperties = new AuthZenPropertyBag();
            return defaultSubjectProperties;
        }
    
        /// <summary>
        /// Sets the default resource for all evaluation requests in the boxcar.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <param name="type">The resource type.</param>
        /// <returns>A property bag for adding resource properties.</returns>
        public IAuthZenPropertyBag SetDefaultResource(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));
            defaultResourceId = id;
            defaultResourceType = type;
            defaultResourceProperties = new AuthZenPropertyBag();
            return defaultResourceProperties;
        }
    
        /// <summary>
        /// Sets the default action for all evaluation requests in the boxcar.
        /// </summary>
        /// <param name="name">The action name.</param>
        /// <returns>A property bag for adding action properties.</returns>
        public IAuthZenPropertyBag SetDefaultAction(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must be provided", nameof(name));
            defaultActionName = name;
            defaultActionProperties = new AuthZenPropertyBag();
            return defaultActionProperties;
        }
    
        /// <summary>
        /// Sets the default context for all evaluation requests in the boxcar.
        /// </summary>
        /// <returns>A property bag for adding context properties.</returns>
        public IAuthZenPropertyBag SetDefaultContext()
        {
            defaultContextProperties = new AuthZenPropertyBag();
            return defaultContextProperties;
        }
    
        /// <summary>
        /// Adds a new evaluation request to the boxcar.
        /// </summary>
        /// <returns>A builder for configuring the individual evaluation request.</returns>
        public IAuthZenRequestBuilder AddRequest()
        {
            var evaluationBuilder = new AuthZenBoxcarEvaluationRequestBuilder();
            evaluationBuilders.Add(evaluationBuilder);
            return evaluationBuilder;
        }
    
        /// <summary>
        /// Sets the evaluation semantics for the boxcar request.
        /// </summary>
        /// <param name="semantics">The evaluation semantics to use.</param>
        /// <returns>The current builder instance.</returns>
        public IAuthZenBoxcarRequestBuilder SetEvaluationSemantics(BoxcarSemantics semantics)
        {
            options = new AuthZenBoxcarOptions()
            {
                Semantics = semantics
            };
            return this;
        }
    
        /// <summary>
        /// Builds the <see cref="AuthZenBoxcarEvaluationRequest"/> instance using the configured values and requests.
        /// </summary>
        /// <returns>The constructed <see cref="AuthZenBoxcarEvaluationRequest"/>.</returns>
        public AuthZenBoxcarEvaluationRequest Build()
        {
            var body = new AuthZenBoxcarEvaluationBody
            {
                Evaluations = new List<AuthZenEvaluationBody>(),
                DefaultValues = new AuthZenEvaluationBody(),
            };
    
            if (!string.IsNullOrWhiteSpace(defaultSubjectId) && !string.IsNullOrWhiteSpace(defaultSubjectType))
            {
                body.DefaultValues.Subject = new AuthZenSubject
                {
                    Id = defaultSubjectId,
                    Type = defaultSubjectType,
                };
    
                if (defaultSubjectProperties is { IsEmpty: false })
                {
                    body.DefaultValues.Subject.Properties = defaultSubjectProperties.Build();
                }
            }
    
            if (!string.IsNullOrWhiteSpace(defaultResourceId) && !string.IsNullOrWhiteSpace(defaultResourceType))
            {
                body.DefaultValues.Resource = new AuthZenResource
                {
                    Id = defaultResourceId,
                    Type = defaultResourceType,
                };
    
                if (defaultResourceProperties is { IsEmpty: false })
                {
                    body.DefaultValues.Resource.Properties = defaultResourceProperties.Build();
                }
            }
    
            if (!string.IsNullOrWhiteSpace(defaultActionName))
            {
                body.DefaultValues.Action = new AuthZenAction
                {
                    Name = defaultActionName,
                };
    
                if (defaultActionProperties is { IsEmpty: false })
                {
                    body.DefaultValues.Action.Properties = defaultActionProperties.Build();
                }
            }
    
            if (defaultContextProperties is { IsEmpty: false })
            {
                body.DefaultValues.Context = defaultContextProperties.Build();
            }
    
            foreach (var builder in evaluationBuilders.Where(eb => eb.HasValuesSet))
            {
                var evaluationRequest = builder.Build();
                body.Evaluations.Add(evaluationRequest);
            }
    
            if (options != null)
            {
                body.Options = options;
            }
    
            return new AuthZenBoxcarEvaluationRequest
            {
                CorrelationId = correlationId,
                Body = body
            };
        }
    }

    /// <summary>
    /// Provides a builder for constructing an individual AuthZen evaluation request,
    /// allowing the configuration of subject, resource, action, and context, each with their own properties.
    /// </summary>
    internal class AuthZenBoxcarEvaluationRequestBuilder : IAuthZenRequestBuilder
    {
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
        /// Gets a value indicating whether any values have been set for this evaluation request.
        /// </summary>
        public bool HasValuesSet => 
            !string.IsNullOrWhiteSpace(subjectId) || 
            !string.IsNullOrWhiteSpace(resourceId) || 
            !string.IsNullOrWhiteSpace(actionName) || 
            (contextProperties?.IsEmpty == false);
    
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
        /// Builds the <see cref="AuthZenEvaluationBody"/> instance using the configured values.
        /// </summary>
        /// <returns>The constructed <see cref="AuthZenEvaluationBody"/>.</returns>
        public AuthZenEvaluationBody Build()
        {
            var request = new AuthZenEvaluationBody();
    
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
    
            return request;
        }
    }
}