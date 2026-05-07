using System.Runtime.Serialization;

namespace backend.Domain.Exceptions;


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
