namespace RadioFreeDAM.Shared.Dtos;

/// <summary>
/// Respuesta del endpoint de estado. Permite a la app comprobar que el servidor está vivo.
/// </summary>
public class HealthDto
{
    public string Status { get; set; } = string.Empty;

    public DateTime ServerTime { get; set; }
}
