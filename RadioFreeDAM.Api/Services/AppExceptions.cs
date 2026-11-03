namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Errores de negocio. Los servicios los lanzan y ApiExceptionHandler los convierte en la respuesta HTTP adecuada,
/// así los controladores no tienen que repetir try/catch.
/// </summary>
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
}

/// <summary>400: los datos no cumplen una regla.</summary>
public class ValidationFailedException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

/// <summary>401: credenciales incorrectas o usuario que ya no existe.</summary>
public class UnauthorizedAppException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
}

/// <summary>404: no existe lo que se pide.</summary>
public class NotFoundAppException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

/// <summary>409: choca con algo que ya existe (correo repetido, favorita duplicada).</summary>
public class ConflictAppException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
}
