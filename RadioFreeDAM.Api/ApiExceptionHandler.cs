using Microsoft.AspNetCore.Diagnostics;
using RadioFreeDAM.Api.Services;

namespace RadioFreeDAM.Api;

/// <summary>
/// Punto único donde las excepciones se convierten en respuestas HTTP con formato ProblemDetails
/// ({ "title": "mensaje para el usuario", "status": 409, ... }).
/// </summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title) = exception switch
        {
            AppException app => (app.StatusCode, app.Message),

            // Nuestra API funciona, pero el servicio externo del que depende no.
            StationCatalogUnavailableException catalog => (StatusCodes.Status502BadGateway, catalog.Message),

            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title }
        });
    }
}
