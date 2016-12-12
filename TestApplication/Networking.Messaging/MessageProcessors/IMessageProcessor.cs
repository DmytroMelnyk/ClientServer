using System.Threading.Tasks;

namespace Networking.Messaging.MessageProcessors
{
    public interface IMessageProcessor
    {
        Task ProcessAsync(object message);
    }
}