using SlotRace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlotRace.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken ct);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct);
        Task<bool> EmailExistsAsync(string email, CancellationToken ct);
        Task AddAsync(User user, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
