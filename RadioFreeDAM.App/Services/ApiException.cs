using System.Net;
using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

/// <summary>
/// La API ha respondido con un error (400, 401, 409...). El mensaje es el "title" que manda la API,
/// ya pensado para mostrárselo al usuario.
/// </summary>
public class ApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>Lanza ApiException si la respuesta no es correcta.</summary>
    public static async Task ThrowIfErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        string? title = null;
        try
        {
            title = (await response.Content.ReadFromJsonAsync<ProblemResponse>())?.Title;
        }
        catch
        {
            // Respuesta sin cuerpo JSON (por ejemplo un 401 del middleware de autenticación).
        }

        var message = !string.IsNullOrWhiteSpace(title)
            ? title
            : response.StatusCode == HttpStatusCode.Unauthorized
                ? "Tu sesión ha caducado. Vuelve a iniciar sesión."
                : "El servidor ha devuelto un error. Inténtalo de nuevo.";

        throw new ApiException(message, response.StatusCode);
    }

    /// <summary>Mensaje para el usuario según el tipo de error.</summary>
    public static string ToUserMessage(Exception ex) => ex switch
    {
        ApiException api => api.Message,
        HttpRequestException { StatusCode: null } =>
            $"No se puede conectar con la API en {ApiConfig.BaseUrl}. Comprueba que está arrancada.",
        TaskCanceledException => "El servidor está tardando demasiado en responder. Inténtalo de nuevo.",
        _ => "Ha ocurrido un error inesperado. Inténtalo de nuevo."
    };

    private sealed class ProblemResponse
    {
        public string? Title { get; set; }
    }
}
