using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.CrossCutting.Exceptions;

/// <summary>
/// Excepción que se lanza cuando los datos de entrada no son válidos.
/// Puede incluir múltiples errores de validación.
/// </summary>
public class ValidationException : AppException
{
    public ValidationException()
        : base(
            ErrorCodes.VALIDATION_ERROR,
            nameof(ErrorCodes.VALIDATION_ERROR))
    {
    }

    // Convención: (errorCode, message)
    public ValidationException(string errorCode, string message)
        : base(message, errorCode)
    {
    }
}
