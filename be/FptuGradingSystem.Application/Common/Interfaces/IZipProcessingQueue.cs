using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IZipProcessingQueue
    {
        Task EnqueueAsync(int examClassId, string zipFilePath, string extractFolderPath, CancellationToken cancellationToken = default);
    }
}
