namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando se viola una regla de negocio.
/// </summary>
public class BusinessRuleException : AppException
{
    // Convención: (errorCode, message)
    public BusinessRuleException(string errorCode, string message)
        : base(message, errorCode)
    {
    }
    
}
