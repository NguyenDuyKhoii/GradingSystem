namespace FptuGradingSystem.Application.Common.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync(string channel, string message);
    }
}
