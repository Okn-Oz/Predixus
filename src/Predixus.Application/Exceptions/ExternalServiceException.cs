namespace Predixus.Application.Exceptions;

public class ExternalServiceException(string service, string message)
    : Exception($"{service} servisine ulaşılamadı: {message}")
{
    public string Service { get; } = service;
}
