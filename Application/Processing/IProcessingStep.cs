using System.Threading;
using System.Threading.Tasks;

namespace ProcessEngine.Worker.Application.Processing;

public interface IProcessingStep
{
    string Name { get; }

    Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken
    );
}
