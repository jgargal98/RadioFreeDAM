using System.ComponentModel.DataAnnotations;

namespace RadioFreeDAM.Shared.Dtos;

public class AddFavoriteRequest
{
    /// <summary>Id de la emisora en Radio Browser (stationuuid).</summary>
    [Required(ErrorMessage = "Falta la emisora.")]
    [StringLength(64, ErrorMessage = "El identificador de la emisora no es válido.")]
    public string StationId { get; set; } = string.Empty;
}

/// <summary>
/// Una favorita del usuario. En la base de datos solo se guarda el Id; los datos de la emisora
/// los completa la API consultando Radio Browser.
/// </summary>
public class FavoriteDto
{
    public string StationId { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; }

    /// <summary>Datos actuales de la emisora. Null si Radio Browser ya no la tiene.</summary>
    public StationDto? Station { get; set; }
}
