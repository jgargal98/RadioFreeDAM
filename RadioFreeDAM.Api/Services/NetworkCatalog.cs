using System.Text.RegularExpressions;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Una cadena de radio y cómo reconocer sus emisoras por el nombre.
/// </summary>
/// <param name="SearchTerms">Textos con los que se buscan sus emisoras en Radio Browser.</param>
/// <param name="Match">El nombre de la emisora tiene que cumplir esta expresión.</param>
/// <param name="Exclude">Emisoras con nombre parecido que son otra cosa (por ejemplo "LOS40 Classic").</param>
public record NetworkDefinition(string Id, string Name, string[] SearchTerms, Regex Match, Regex? Exclude = null)
{
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
        new("cope", "COPE", ["cope"], Pattern(@"^cope\b")),
        new("ser", "Cadena SER", ["cadena ser"], Pattern(@"^cadena\s*ser\b"),
            Exclude: Pattern(@"\+")), // SER+ es otra emisora
        new("ondacero", "Onda Cero", ["onda cero"], Pattern(@"^onda\s*cero\b")),
        new("esradio", "esRadio", ["esradio", "es radio"], Pattern(@"^es\s*radio\b")),
        new("radiomarca", "Radio Marca", ["radio marca"], Pattern(@"^radio\s*marca\b")),
        new("rne", "RNE", ["rne", "radio nacional"], Pattern(@"\brne\b|^radio\s*nacional\b")),
        new("cadena100", "Cadena 100", ["cadena 100"], Pattern(@"^cadena\s*100\b")),
        new("cadenadial", "Cadena Dial", ["cadena dial"], Pattern(@"^cadena\s*dial\b")),
        new("los40", "LOS40", ["los 40", "los40"], Pattern(@"^los\s*40\b"),
            Exclude: Pattern(@"classic|dance|urban")), // emisoras temáticas distintas
        new("kissfm", "KISS FM", ["kiss fm"], Pattern(@"^kiss\s*fm\b")),
        new("rockfm", "Rock FM", ["rock fm", "rockfm"], Pattern(@"^rock\s*fm\b")),
        new("europafm", "Europa FM", ["europa fm"], Pattern(@"^europa\s*fm\b")),
        new("locafm", "Loca FM", ["loca fm"], Pattern(@"^loca\s*fm\b")),
        new("radiole", "Radiolé", ["radiole", "radiolé"], Pattern(@"^radiol[eé]\b"))
    ];

    /// <summary>Cadena a la que pertenece una emisora por su nombre, o null.</summary>
    public static NetworkDefinition? FindFor(string stationName) =>
        All.FirstOrDefault(network => network.Contains(stationName));

    public static NetworkDefinition? FindById(string id) =>
        All.FirstOrDefault(network => network.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
