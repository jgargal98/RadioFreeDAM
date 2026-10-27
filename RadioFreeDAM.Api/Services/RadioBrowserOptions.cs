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

    public int TopLimit { get; set; } = 20;

    public int SearchLimit { get; set; } = 30;

    public int TimeoutSeconds { get; set; } = 8;
}
