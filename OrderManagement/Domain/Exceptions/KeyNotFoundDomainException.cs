using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Domain.Exceptions;

[ExcludeFromCodeCoverage]
public class KeyNotFoundDomainException : DomainException
{
    public KeyNotFoundDomainException() : base() { }

    public KeyNotFoundDomainException(string message) : base(message) { }

    public KeyNotFoundDomainException(string message, Exception innerException) : base(message, innerException) { }

    public KeyNotFoundDomainException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
