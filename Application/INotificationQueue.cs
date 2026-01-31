using System.Threading;
using System.Threading.Tasks;
using ProcessEngine.Worker.Domain;
using System.Collections.Generic;


namespace ProcessEngine.Worker.Application;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationItem notification, CancellationToken ct);
    IAsyncEnumerable<NotificationItem> DequeueAsync(CancellationToken ct);
}
