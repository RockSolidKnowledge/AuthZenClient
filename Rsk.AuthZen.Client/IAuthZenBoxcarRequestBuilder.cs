using System;
using System.Collections.Generic;
using System.Linq;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenBoxcarRequestBuilder
    {
        IAuthZenBoxcarRequestBuilder SetCorrelationId(string correlationId);
        
        IAuthZenPropertyBag SetDefaultSubject(string id, string type);
        IAuthZenPropertyBag SetDefaultResource(string id, string type);
        IAuthZenPropertyBag SetDefaultAction(string name);
        IAuthZenPropertyBag SetDefaultContext();

        IAuthZenRequestBuilder AddRequest();
        
        AuthZenBoxcarEvaluationRequest Build();
    }

    public class AuthZenBoxcarEvaluationRequest
    {
        public string CorrelationId { get; internal set; }
        public AuthZenBoxcarEvaluationBody Body { get; internal set; }
    }
    
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

        public IAuthZenBoxcarRequestBuilder SetCorrelationId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Correlation ID must be provided", nameof(id));
            
            correlationId = id;

            return this;
        }

        public IAuthZenPropertyBag SetDefaultSubject(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));

            defaultSubjectId = id;
            defaultSubjectType = type;
            
            defaultSubjectProperties = new AuthZenPropertyBag();
            return defaultSubjectProperties;
        }

        public IAuthZenPropertyBag SetDefaultResource(string id, string type)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID must be provided", nameof(id));
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type must be provided", nameof(type));

            defaultResourceId = id;
            defaultResourceType = type;
            
            defaultResourceProperties = new AuthZenPropertyBag();
            return defaultResourceProperties;
        }

        public IAuthZenPropertyBag SetDefaultAction(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must be provided", nameof(name));

            defaultActionName = name;
            defaultActionProperties = new AuthZenPropertyBag();
            return defaultActionProperties;
        }

        public IAuthZenPropertyBag SetDefaultContext()
        {
            defaultContextProperties = new AuthZenPropertyBag();
            return defaultContextProperties;
        }

        public IAuthZenRequestBuilder AddRequest()
        {
            var evaluationBuilder = new AuthZenBoxcarEvaluationRequestBuilder();
            
            evaluationBuilders.Add(evaluationBuilder);
            
            return evaluationBuilder;
        }
        
        public IAuthZenBoxcarRequestBuilder SetEvaluationSemantics(BoxcarSemantics semantics)
        {
            options = new AuthZenBoxcarOptions()
            {
                Semantics = semantics
            };
            
            return this;
        }

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
        public bool HasValuesSet => 
            !string.IsNullOrWhiteSpace(subjectId) || 
            !string.IsNullOrWhiteSpace(resourceId) || 
            !string.IsNullOrWhiteSpace(actionName) || 
            (contextProperties?.IsEmpty == false);

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