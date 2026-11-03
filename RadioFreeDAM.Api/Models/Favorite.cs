namespace RadioFreeDAM.Api.Models;

/// <summary>
/// Relación usuario–emisora en la tabla rfd_favorites.
/// Solo se guarda el Id de Radio Browser; los datos de la emisora se consultan al listar.
/// </summary>
public class Favorite
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    /// <summary>stationuuid de Radio Browser.</summary>
    public string StationId { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; }
}
