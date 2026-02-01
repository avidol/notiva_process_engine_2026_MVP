using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ProcessEngine.Worker.Application.Processing;

public class ProcessingPipeline
{
    private readonly IEnumerable<IProcessingStep> _steps;
    private readonly ILogger<ProcessingPipeline> _logger;

    public ProcessingPipeline(
        IEnumerable<IProcessingStep> steps,
        ILogger<ProcessingPipeline> logger)
    {
        _steps = steps;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        foreach (var step in _steps)
        {
            _logger.LogInformation(
                "Executing processing step: {Step}",
                step.Name);

            await step.ExecuteAsync(context, cancellationToken);
        }
    }
}
