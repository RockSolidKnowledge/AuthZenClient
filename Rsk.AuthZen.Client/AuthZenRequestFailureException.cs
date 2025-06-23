using System;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents an error that occurs when a request to the AuthZen service fails.
    /// </summary>
    public class AuthZenRequestFailureException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthZenRequestFailureException"/> class.
        /// </summary>
        public AuthZenRequestFailureException()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthZenRequestFailureException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message</param>
        public AuthZenRequestFailureException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthZenRequestFailureException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// <param name="message">The error message</param>
        /// <param name="inner">The inner exception</param>
        /// </summary>
        public AuthZenRequestFailureException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}