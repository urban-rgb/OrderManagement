using System.Runtime.Serialization;

namespace WebApplication1.Domain.Exceptions;

public class KeyNotFoundDomainException : DomainException
{
    public KeyNotFoundDomainException() : base() { }

    public KeyNotFoundDomainException(string message) : base(message) { }

    public KeyNotFoundDomainException(string message, Exception innerException) : base(message, innerException) { }

    public KeyNotFoundDomainException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
