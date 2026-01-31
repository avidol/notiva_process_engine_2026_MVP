using System.Threading;
using System.Threading.Tasks;
using ProcessEngine.Worker.Domain;

namespace ProcessEngine.Worker.Application;

public interface INotificationProcessor
{
    Task ProcessAsync(NotificationItem notification, CancellationToken ct);
}
