using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenRequestBuilder
    {
        IAuthZenPropertyBag SetSubject(string id, string type);
        IAuthZenPropertyBag SetResource(string id, string type);
        IAuthZenPropertyBag SetAction(string name);
        IAuthZenPropertyBag SetContext();
    }
    
    public interface IAuthZenSingleRequestBuilder : IAuthZenRequestBuilder
    {
        IAuthZenSingleRequestBuilder SetCorrelationId(string correlationId);
        
        AuthZenEvaluationRequest Build();
    }
    
    public class AuthZenEvaluationRequest
    {
        public string CorrelationId { get; internal set; }
        public AuthZenEvaluationBody Body { get; internal set; }
    }
    
    public interface IAuthZenPropertyBag
    {
        IAuthZenPropertyBag Add(string name, object value);
        bool IsEmpty { get; }
    }

    internal interface IAuthZenPropertyBuilder : IAuthZenPropertyBag
    {
        Dictionary<string, object> Build();
    }

    public class AuthZenPropertyBag : IAuthZenPropertyBuilder
    {
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
        
        public bool IsEmpty => properties.Count == 0;

        public IAuthZenPropertyBag Add(string name, object value)
        {
            properties[name] = value;
            
            return this;
        }

        public Dictionary<string, object> Build()
        {
            return properties;
        }
    }
}