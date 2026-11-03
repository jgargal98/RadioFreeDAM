using RadioFreeDAM.Api.Models;

namespace RadioFreeDAM.Api.Repositories;

/// <summary>Acceso a la tabla de usuarios. Solo lee y escribe datos: las reglas están en los servicios.</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
