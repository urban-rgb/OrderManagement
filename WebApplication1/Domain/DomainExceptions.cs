namespace WebApplication1.Domain;

public class DomainException(string message) : Exception(message);
public class KeyNotFoundDomainException(string message) : DomainException(message);
public class ConflictDomainException(string message) : DomainException(message);