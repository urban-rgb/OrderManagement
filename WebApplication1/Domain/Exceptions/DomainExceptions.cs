namespace WebApplication1.Domain.Exceptions;

// TODO Один файл - один класс, разделить
// TODO + создать разные конструкторы для каждой из ошибок (ссылка)
public class DomainException : Exception
{
    public DomainException() : base() { }

    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}