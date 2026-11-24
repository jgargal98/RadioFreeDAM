using System.Text.RegularExpressions;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Una cadena de radio y cómo reconocer sus emisoras por el nombre.
/// </summary>
/// <param name="SearchTerms">Textos con los que se buscan sus emisoras en Radio Browser.</param>
/// <param name="Match">El nombre de la emisora tiene que cumplir esta expresión.</param>
/// <param name="Exclude">Emisoras con nombre parecido que son otra cosa (por ejemplo "LOS40 Classic").</param>
/// <param name="National">
/// Nombre de la emisora nacional de la cadena. Radio Browser no la distingue de las locales (la de COPE se llama
/// "Cope Madrid FM"), así que se indica aquí para poder ofrecerla la primera.
/// </param>
public record NetworkDefinition(string Id, string Name, string[] SearchTerms, Regex Match, Regex? Exclude = null,
    Regex? National = null)
{
    /// <summary>Nombre con el que se muestra la emisora nacional, por ejemplo "COPE (nacional)".</summary>
    public string NationalName => $"{Name} (nacional)";

    public bool Contains(string stationName)
    {
        var name = stationName.Trim();
        return Match.IsMatch(name) && (Exclude is null || !Exclude.IsMatch(name));
    }
}

/// <summary>
/// Cadenas españolas conocidas. Radio Browser no dice a qué cadena pertenece cada emisora
/// (para él "COPE Málaga" y "COPE Sevilla" son emisoras distintas), así que la lista se mantiene a mano.
/// Una cadena que no esté aquí simplemente no se agrupa.
/// </summary>
public static class NetworkCatalog
{
    private static Regex Pattern(string pattern) =>
        new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly IReadOnlyList<NetworkDefinition> All =
    [
        new("cope", "COPE", ["cope"], Pattern(@"^cope\b"),
            National: Pattern(@"^cope madrid fm$")),
        new("ser", "Cadena SER", ["cadena ser"], Pattern(@"^cadena\s*ser\b"),
            Exclude: Pattern(@"\+"), // SER+ es otra emisora
            National: Pattern(@"^cadena\s*ser radio madrid$")),
        new("ondacero", "Onda Cero", ["onda cero"], Pattern(@"^onda\s*cero\b"),
            National: Pattern(@"^onda\s*cero( madrid)?$")),
        new("esradio", "esRadio", ["esradio", "es radio"], Pattern(@"^es\s*radio\b"),
            National: Pattern(@"^es\s*radio( madrid)?$")),
        new("radiomarca", "Radio Marca", ["radio marca"], Pattern(@"^radio\s*marca\b"),
            National: Pattern(@"^radio\s*marca$")),
        new("rne", "RNE", ["rne", "radio nacional"], Pattern(@"\brne\b|^radio\s*nacional\b"),
            National: Pattern(@"^rne radio nacional$")),
        new("cadena100", "Cadena 100", ["cadena 100"], Pattern(@"^cadena\s*100\b"),
            National: Pattern(@"^cadena\s*100$")),
        new("cadenadial", "Cadena Dial", ["cadena dial"], Pattern(@"^cadena\s*dial\b"),
            National: Pattern(@"^cadena\s*dial$")),
        new("los40", "LOS40", ["los 40", "los40"], Pattern(@"^los\s*40\b"),
            Exclude: Pattern(@"classic|dance|urban"), // emisoras temáticas distintas
            National: Pattern(@"^los\s*40( nacional)?$")),
        new("kissfm", "KISS FM", ["kiss fm"], Pattern(@"^kiss\s*fm\b"),
            National: Pattern(@"^kiss\s*fm( españa)?$")),
        new("rockfm", "Rock FM", ["rock fm", "rockfm"], Pattern(@"^rock\s*fm\b"),
            National: Pattern(@"^rock\s*fm( \(españa\))?$")),
        new("europafm", "Europa FM", ["europa fm"], Pattern(@"^europa\s*fm\b"),
            National: Pattern(@"^europa\s*fm$")),
        new("locafm", "Loca FM", ["loca fm"], Pattern(@"^loca\s*fm\b"),
            National: Pattern(@"^loca\s*fm$")),
        new("radiole", "Radiolé", ["radiole", "radiolé"], Pattern(@"^radiol[eé]\b"),
            National: Pattern(@"^radiol[eé]$"))
    ];

    /// <summary>Cadena a la que pertenece una emisora por su nombre, o null.</summary>
    public static NetworkDefinition? FindFor(string stationName) =>
        All.FirstOrDefault(network => network.Contains(stationName));

    public static NetworkDefinition? FindById(string id) =>
        All.FirstOrDefault(network => network.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
