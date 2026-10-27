namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Textos de presentación de una emisora compartidos por varias pantallas.
/// </summary>
public static class StationText
{
    /// <summary>"País · etiqueta1, etiqueta2…". Radio Browser puede traer muchas etiquetas, así que se limitan.</summary>
    public static string Describe(string country, string tags, int maxTags)
    {
        var tagList = tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(maxTags)
            .ToList();

        if (tagList.Count == 0)
            return country;

        var tagText = string.Join(", ", tagList);
        return string.IsNullOrWhiteSpace(country) ? tagText : $"{country} · {tagText}";
    }
}
