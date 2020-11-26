using System;

namespace SwaggerValidationPoc
{
    public class RequestDoesNotMatchSpecException : Exception
    {
        public RequestDoesNotMatchSpecException(string message) : base(message) { }
    }
}
