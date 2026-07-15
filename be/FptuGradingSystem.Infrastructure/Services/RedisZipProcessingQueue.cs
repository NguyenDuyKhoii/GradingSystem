using FptuGradingSystem.Application.Common.Interfaces;
using StackExchange.Redis;
using System.Threading;
using System.Threading.Tasks;

namespace FptuGradingSystem.Infrastructure.Services
{
    public class RedisZipProcessingQueue : IZipProcessingQueue
    {
        private readonly IConnectionMultiplexer _redis;
        private const string StreamKey = "zip-processing-stream";

        public RedisZipProcessingQueue(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task EnqueueAsync(
            int examClassId,
            string zipFilePath,
            string extractFolderPath,
            CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();

            var values = new[]
            {
                new NameValueEntry("examClassId", examClassId),
                new NameValueEntry("zipFilePath", zipFilePath),
                new NameValueEntry("extractFolderPath", extractFolderPath)
            };

            await db.StreamAddAsync(StreamKey, values);
        }
    }
}
