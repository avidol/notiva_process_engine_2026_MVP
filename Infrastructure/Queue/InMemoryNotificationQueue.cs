using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ProcessEngine.Worker.Application;
using ProcessEngine.Worker.Domain;
using System.Collections.Generic;


namespace ProcessEngine.Worker.Infrastructure.Queue;

public class InMemoryNotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationItem> _channel =
        Channel.CreateBounded<NotificationItem>(1000);

    public async ValueTask EnqueueAsync(NotificationItem notification, CancellationToken ct)
        => await _channel.Writer.WriteAsync(notification, ct);

    public IAsyncEnumerable<NotificationItem> DequeueAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
