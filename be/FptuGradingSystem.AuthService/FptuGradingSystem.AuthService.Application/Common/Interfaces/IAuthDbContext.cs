using FptuGradingSystem.AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FptuGradingSystem.AuthService.Application.Common.Interfaces
{
    public interface IAuthDbContext
    {
        DbSet<User> Users { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
