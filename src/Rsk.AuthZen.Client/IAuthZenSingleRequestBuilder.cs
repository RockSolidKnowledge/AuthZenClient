using System.Collections.Generic;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Provides a builder interface for constructing an AuthZen evaluation request,
    /// allowing the configuration of subject, resource, action, and context, each with their own properties.
    /// </summary>
    public interface IAuthZenRequestBuilder
    {
        /// <summary>
        /// Sets the subject for the evaluation request.
        /// </summary>
        /// <param name="id">The subject identifier.</param>
        /// <param name="type">The subject type.</param>
        /// <returns>A property bag for adding subject properties.</returns>
        IAuthZenPropertyBag SetSubject(string id, string type);
    
        /// <summary>
        /// Sets the resource for the evaluation request.
        /// </summary>
        /// <param name="id">The resource identifier.</param>
        /// <param name="type">The resource type.</param>
        /// <returns>A property bag for adding resource properties.</returns>
        IAuthZenPropertyBag SetResource(string id, string type);
    
        /// <summary>
        /// Sets the action for the evaluation request.
        /// </summary>
        /// <param name="name">The action name.</param>
        /// <returns>A property bag for adding action properties.</returns>
        IAuthZenPropertyBag SetAction(string name);
    
        /// <summary>
        /// Sets the context for the evaluation request.
        /// </summary>
        /// <returns>A property bag for adding context properties.</returns>
        IAuthZenPropertyBag SetContext();
    }
    
    /// <summary>
    /// Provides a fluent builder interface for constructing a single AuthZen evaluation request,
    /// supporting the configuration of correlation ID, subject, resource, action, and context.
    /// </summary>
    public interface IAuthZenSingleRequestBuilder : IAuthZenRequestBuilder
    {
        /// <summary>
        /// Sets the correlation identifier for the evaluation request.
        /// </summary>
        /// <param name="correlationId">The correlation ID to associate with the request.</param>
        /// <returns>The current builder instance.</returns>
        IAuthZenSingleRequestBuilder SetCorrelationId(string correlationId);
    
        /// <summary>
        /// Builds the <see cref="AuthZenEvaluationRequest"/> instance using the configured values.
        /// </summary>
        /// <returns>The constructed <see cref="AuthZenEvaluationRequest"/>.</returns>
        AuthZenEvaluationRequest Build();
    }
    
    /// <summary>
    /// Represents a single AuthZen evaluation request, including a correlation identifier and the request body.
    /// </summary>
    public class AuthZenEvaluationRequest
    {
        /// <summary>
        /// Gets the correlation identifier for tracking the evaluation request.
        /// </summary>
        public string CorrelationId { get; internal set; }
    
        /// <summary>
        /// Gets the body of the evaluation request, containing evaluation details.
        /// </summary>
        public AuthZenEvaluationBody Body { get; internal set; }
    }
    
    /// <summary>
    /// Represents a property bag for storing key-value pairs used in AuthZen evaluation requests.
    /// Provides methods to add properties and check if the bag is empty.
    /// </summary>
    public interface IAuthZenPropertyBag
    {
        /// <summary>
        /// Adds a property with the specified name and value to the property bag.
        /// </summary>
        /// <param name="name">The name of the property to add.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The current property bag instance for fluent chaining.</returns>
        IAuthZenPropertyBag Add(string name, object value);
    
        /// <summary>
        /// Gets a value indicating whether the property bag is empty.
        /// </summary>
        bool IsEmpty { get; }
    }

    /// <summary>
    /// Extends <see cref="IAuthZenPropertyBag"/> to provide a method for building
    /// the property bag into a dictionary of key-value pairs for use in AuthZen evaluation requests.
    /// </summary>
    internal interface IAuthZenPropertyBuilder : IAuthZenPropertyBag
    {
        /// <summary>
        /// Builds and returns the property bag as a dictionary of key-value pairs.
        /// </summary>
        /// <returns>A dictionary containing all properties added to the bag.</returns>
        Dictionary<string, object> Build();
    }

    /// <summary>
    /// Implements <see cref="IAuthZenPropertyBuilder"/> to provide a property bag for storing
    /// key-value pairs used in AuthZen evaluation requests. Supports adding properties,
    /// checking if the bag is empty, and building the bag into a dictionary.
    /// </summary>
    public class AuthZenPropertyBag : IAuthZenPropertyBuilder
    {
        private readonly Dictionary<string, object> properties = new Dictionary<string, object>();
    
        /// <summary>
        /// Gets a value indicating whether the property bag is empty.
        /// </summary>
        public bool IsEmpty => properties.Count == 0;
    
        /// <summary>
        /// Adds a property with the specified name and value to the property bag.
        /// </summary>
        /// <param name="name">The name of the property to add.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The current property bag instance for fluent chaining.</returns>
        public IAuthZenPropertyBag Add(string name, object value)
        {
            properties[name] = value;
            return this;
        }
    
        /// <summary>
        /// Builds and returns the property bag as a dictionary of key-value pairs.
        /// </summary>
        /// <returns>A dictionary containing all properties added to the bag.</returns>
        public Dictionary<string, object> Build()
        {
            return properties;
        }
    }
}