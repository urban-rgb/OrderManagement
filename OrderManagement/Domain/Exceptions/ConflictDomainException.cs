using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagement.Domain.Exceptions;

[ExcludeFromCodeCoverage]
public class ConflictDomainException : DomainException
{
    public ConflictDomainException() : base() { }

    public ConflictDomainException(string message) : base(message) { }

    public ConflictDomainException(string message, Exception innerException) : base(message, innerException) { }

    protected ConflictDomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
    {
    }
}
