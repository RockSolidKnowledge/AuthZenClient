using System;

namespace Rsk.AuthZen.Client
{
    public class AuthZenRequestFailureException : Exception
    {
        public AuthZenRequestFailureException()
        {
        }

        public AuthZenRequestFailureException(string message) : base(message)
        {
        }

        public AuthZenRequestFailureException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}