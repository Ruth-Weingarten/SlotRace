using Microsoft.EntityFrameworkCore;
using SlotRace.Application.Interfaces;
using SlotRace.Domain.Entities;
using SlotRace.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
namespace SlotRace.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        
        public async Task<User?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        }

       
        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        }

        
        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        {
            return await _context.Users.AnyAsync(u => u.Email == email, ct);
        }

        
        public async Task AddAsync(User user, CancellationToken ct)
        {
            await _context.Users.AddAsync(user, ct);
        }

        
        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }
    }
}
