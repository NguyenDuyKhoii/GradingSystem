using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IUserGrpcClient
    {
        Task EnsureUserExistsAsync(int userId, CancellationToken cancellationToken = default);
    }
}
