namespace RadioFreeDAM.Api.Services;

/// <summary>
/// El catálogo externo (Radio Browser) no está disponible: sin red, caído o con una respuesta inválida.
/// El controlador la traduce a un 502 Bad Gateway para la app.
/// </summary>
public class StationCatalogUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
