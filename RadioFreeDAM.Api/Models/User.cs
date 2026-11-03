namespace RadioFreeDAM.Api.Models;

/// <summary>Usuario guardado en la tabla rfd_users.</summary>
public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Siempre en minúsculas y sin espacios, para que el índice único no distinga mayúsculas.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña (PBKDF2). La contraseña nunca se guarda en claro.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<Favorite> Favorites { get; set; } = [];
}
