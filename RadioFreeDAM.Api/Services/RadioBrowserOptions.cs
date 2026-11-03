namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Configuración de Radio Browser. Se lee de la sección "RadioBrowser" de appsettings.json.
/// </summary>
public class RadioBrowserOptions
{
    public const string SectionName = "RadioBrowser";

    /// <summary>Servidor de Radio Browser. Hay varios espejos (de1, de2, fi1...).</summary>
    public string BaseUrl { get; set; } = "https://de1.api.radio-browser.info/";

    /// <summary>Radio Browser pide que cada aplicación se identifique con su User-Agent.</summary>
    public string UserAgent { get; set; } = "RadioFreeDAM/1.0";

    /// <summary>Código ISO del país de las emisoras de Inicio.</summary>
    public string CountryCode { get; set; } = "ES";

    /// <summary>Emisoras populares que se piden. Son más de las que se muestran porque al agrupar por cadenas se juntan varias.</summary>
    public int TopLimit { get; set; } = 80;

    public int SearchLimit { get; set; } = 60;

    /// <summary>Máximo de emisoras al listar todas las de una cadena.</summary>
    public int NetworkLimit { get; set; } = 100;

    public int TimeoutSeconds { get; set; } = 8;
}
