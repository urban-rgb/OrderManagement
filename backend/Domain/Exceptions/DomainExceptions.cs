using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace backend.Domain.Exceptions;

[Serializable]
public class DomainException : Exception
{
    public DomainException() : base() { }

    [JsonConstructor]
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }

    protected DomainException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}