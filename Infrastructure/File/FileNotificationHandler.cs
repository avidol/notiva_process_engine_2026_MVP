using System.IO;
using System.Text;
using System.Threading.Tasks;
using ProcessEngine.Worker.Domain;

namespace ProcessEngine.Worker.Infrastructure.File;

public class FileNotificationHandler
{
    public async Task HandleAsync(NotificationItem notification)
    {
        var payload = notification.PayloadJson;

        Directory.CreateDirectory("output");

        var path = Path.Combine(
            "output",
            $"{notification.Id}.txt"
        );

        await System.IO.File.WriteAllTextAsync(path, payload, Encoding.UTF8);
    }
}
