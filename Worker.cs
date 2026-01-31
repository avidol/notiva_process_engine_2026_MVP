using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessEngine.Worker.Application;
using ProcessEngine.Worker.Infrastructure.Persistence;
using ProcessEngine.Worker.Infrastructure.Ingestion;
using ProcessEngine.Worker.Application.Rules;
using ProcessEngine.Worker.Domain.Rules;

using Microsoft.Extensions.Configuration;

namespace ProcessEngine.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly INotificationRepository _repository;
    private readonly INotificationQueue _queue;
    private readonly INotificationProcessor _processor;
    private readonly RabbitMqIngestionService _rabbitMq;
    private readonly IRuleValidator _ruleValidator;

    private readonly int _maxParallelism;
    private readonly SemaphoreSlim _semaphore;

    
    // Tune this later via configuration
    //private const int MaxParallelism = 5;

    public Worker(
        ILogger<Worker> logger,
        INotificationRepository repository,
        INotificationQueue queue,
        INotificationProcessor processor,
        RabbitMqIngestionService rabbitMq,
        IRuleValidator ruleValidator,
        IConfiguration configuration)
    {
        _logger = logger;
        _repository = repository;
        _queue = queue;
        _processor = processor;
        _rabbitMq = rabbitMq;
        _ruleValidator = ruleValidator;

        _maxParallelism = configuration.GetValue<int>("Worker:MaxParallelism", 5);
        _semaphore = new SemaphoreSlim(_maxParallelism);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started with MaxParallelism={MaxParallelism}", _maxParallelism);

        // Start RabbitMQ ingestion ONCE
        _rabbitMq.Start();

        // 🔁 Background processing loop (parallel)
        var processingTask = Task.Run(async () =>
        {
            await foreach (var notification in _queue.DequeueAsync(stoppingToken))
            {
                await _semaphore.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _repository.MarkProcessingAsync(notification.Id);

                        await _processor.ProcessAsync(notification, stoppingToken);

                        await _repository.MarkCompletedAsync(notification.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Processing failed for notification {Id}",
                            notification.Id);

                        await _repository.MarkFailedAsync(
                            notification.Id,
                            ex.Message,
                            permanent: false);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, stoppingToken);
            }
        }, stoppingToken);

        // 🔁 Validation + enqueue loop (single-threaded, predictable)
        while (!stoppingToken.IsCancellationRequested)
        {
            var pending = await _repository.FetchPendingAsync();

            foreach (var notification in pending)
            {
                var result = _ruleValidator.Validate(
                    new RuleContext(notification.Id, notification.PayloadJson)
                );

                if (!result.IsValid)
                {
                    await _repository.MarkFailedPermAsync(
                        notification.Id,
                        JsonSerializer.Serialize(result.Violations)
                    );
                    continue;
                }

                await _queue.EnqueueAsync(notification, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        // Wait for in-flight processing to complete
        await processingTask;
    }
}

//===========================================================OLD CODE=======================================================


//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
///*
// * These are core .NET namespaces.

//   System.Threading / System.Threading.Tasks are required for:

//    - async execution

//    - cooperative cancellation via CancellationToken

//    - Microsoft.Extensions.Hosting provides the BackgroundService abstraction.

//    - Microsoft.Extensions.Logging enables structured, provider-agnostic logging.
// * 
// */

//using Notiva.Notification.Worker.Application;
//using Notiva.Notification.Worker.Infrastructure.Persistence;
//using Notiva.Notification.Worker.Infrastructure.Ingestion;
///*
// * These imports bring in application-layer and infrastructure-layer abstractions:

//    Application → orchestration interfaces (queues, processors)

//    Persistence → database access

//    Ingestion → external input sources (RabbitMQ, File, SFTP, etc.)

//    This reinforces Clean Architecture separation:

//    Worker orchestrates

//    Infrastructure executes details
// * 
// */

//using Notiva.Notification.Worker.Domain.Rules;
//using Notiva.Notification.Worker.Application.Rules;
///*
// * These namespaces relate to the rule engine:

//    Domain.Rules → pure domain objects (RuleContext, RuleViolation)

//    Application.Rules → rule execution logic (IRuleValidator)

//    Keeps rule logic independent of transport or persistence.
// * 
// */

//using System.Text.Json;
///*
//    Used to serialize rule violations into JSON before persisting them.

//    Chosen for:

//        performance

//        native .NET support

//        deterministic output

//*/

//namespace Notiva.Notification.Worker;

///*
// *  Defines the logical boundary of the Worker service.

//    This class represents the long-running background process of the application.
// * 
// */


///*
// * Worker inherits from BackgroundService.

//    BackgroundService is the standard .NET abstraction for:

//    Windows Services

//    Linux daemons

//    Containerized workers

//    It provides:

//    lifecycle management

//    graceful startup/shutdown

//    cancellation handling
// * 
// */


////Host calls ExecuteAsync() automatically. Because Worker inherits from BackgroundService
//public class Worker : BackgroundService
//{

//    /*
//     * Typed logger for this class.

//        Enables structured, contextual logging.
//     */
//    private readonly ILogger<Worker> _logger;

//    /*
//     * Abstraction over database access.

//        Used to:

//        fetch pending notifications

//        update processing state

//        record failures

//        Keeps Worker independent of MySQL or Dapper.
//     * 
//     */
//    private readonly INotificationRepository _repository;
//    /*
//     * Represents an internal processing queue.

//        Decouples:

//        fetching notifications

//        actual processing execution

//        Allows future replacement (e.g., in-memory → channel → distributed queue).
//     * 
//     */
//    private readonly INotificationQueue _queue;

//    /*
//     * Represents the business processing logic.

//        Worker does not process notifications itself.

//        This enforces single responsibility:

//        Worker orchestrates

//        Processor executes business logic
//     * 
//     */
//    private readonly INotificationProcessor _processor;

//    /*
//     * Concrete ingestion service for RabbitMQ.

//        Responsible for:

//        consuming messages

//        normalizing payloads

//        persisting them

//        Started once at worker startup.
//     * 
//     */
//    private readonly RabbitMqIngestionService _rabbitMq;

//    /*
//     * Concrete ingestion service for RabbitMQ.

//        Responsible for:

//        consuming messages

//        normalizing payloads

//        persisting them

//        Started once at worker startup.
//     * 
//     */

//    private readonly IRuleValidator _ruleValidator;


//    /*
//     * Constructor receives all dependencies via DI.

//            No new keyword used inside the class.

//            This allows:

//            testability

//            configurability

//            replacement of implementations
//     * 
//     * 
//     */
//    public Worker(
//        ILogger<Worker> logger,
//        INotificationRepository repository,
//        INotificationQueue queue,
//        INotificationProcessor processor,
//        RabbitMqIngestionService rabbitMq,
//        IRuleValidator ruleValidator)
//    {

//        /*
//         * Assigns injected services to private fields.

//           These references are used throughout the worker lifecycle.
//         * 
//         */

//        _logger = logger;
//        _repository = repository;
//        _queue = queue;
//        _processor = processor;
//        _rabbitMq = rabbitMq;
//        _ruleValidator = ruleValidator;
//    }

//    /*
//     * Entry point of the background service.

//        Called automatically by the hosting framework.

//        stoppingToken is used for graceful shutdown.
//     * 
//     */

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("Worker started");

//        // Start RabbitMQ ingestion ONCE
//        _rabbitMq.Start();

//        // 🔁 Start background processing task
//        var processingTask = Task.Run(async () =>
//        {
//            await foreach (var notification in _queue.DequeueAsync(stoppingToken))
//            {
//                try
//                {
//                    await _repository.MarkProcessingAsync(notification.Id);

//                    await _processor.ProcessAsync(notification, stoppingToken);

//                    await _repository.MarkCompletedAsync(notification.Id);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Processing failed for {Id}", notification.Id);

//                    await _repository.MarkFailedAsync(
//                        notification.Id,
//                        ex.Message,
//                        permanent: false
//                    );
//                }
//            }
//        }, stoppingToken);

//        // 🔁 Validation + enqueue loop
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            var pending = await _repository.FetchPendingAsync();

//            foreach (var notification in pending)
//            {
//                var result = _ruleValidator.Validate(
//                    new RuleContext(notification.Id, notification.PayloadJson)
//                );

//                if (!result.IsValid)
//                {
//                    await _repository.MarkFailedPermAsync(
//                        notification.Id,
//                        JsonSerializer.Serialize(result.Violations)
//                    );
//                    continue;
//                }

//                await _queue.EnqueueAsync(notification, stoppingToken);
//            }

//            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//        }

//        await processingTask;
//    }





////    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
////    {
////        //Emits a startup log.
////        /* Useful for:
////               operational monitoring
////                troubleshooting service restarts
////        */
////        _logger.LogInformation("Worker started");

////        // Start RabbitMQ consumer ONCE
////        /*
////         * Starts the RabbitMQ ingestion service.

////            Important design decision:

////            ingestion runs independently

////            worker loop should not restart consumers

////            Prevents duplicate consumers or message reprocessing.
////         * 
////         */
////        _rabbitMq.Start();


////        /*
////         * Infinite loop that runs until:

////            application shutdown

////            service stop

////            container termination

////            Uses cooperative cancellation instead of forced termination.
////         * 
////         * 
////         */

////        while (!stoppingToken.IsCancellationRequested)
////        {

////            /*
////             * Retrieves notifications that are:

////                new

////                retryable

////                eligible for processing

////                Database acts as the source of truth for work state.
////             * 
////             * 
////             */

////            var pending = await _repository.FetchPendingAsync();


////            /*
////             * Each pending notification is evaluated individually.

////                Prevents one bad message from blocking others.
////             *  
////             */
////            foreach (var notification in pending)
////            {
////                /*
////                 * Creates a RuleContext containing:

////                        notification identifier

////                        raw payload JSON

////                        Passes it to the rule engine.

////                        Rule engine is pure logic:

////                        no DB

////                        no queues

////                        no side effects except audit logging
////                 * 
////                 */

////                var result = _ruleValidator.Validate(new RuleContext(notification.Id, notification.PayloadJson));

////                /*
////                 * Checks whether rule validation succeeded.

////                    IsValid == false means:

////                    at least one rule violation occurred

////                    processing must not continue
////                 * 
////                 */


////                if (!result.IsValid)
////                {

////                    /*
////                     * Logs a warning-level event.

////                            Helps operators understand why processing stopped.
////                     * 
////                     */

////                    _logger.LogWarning("Rule validation failed for notification {Id}", notification.Id);

////                    /*
////                     * Marks the notification as permanently failed.

////                        Stores structured rule violations in the database.

////                        This makes failures:

////                        auditable

////                        explainable

////                        queryable
////                     * 
////                     */
////                    await _repository.MarkFailedPermAsync(notification.Id,JsonSerializer.Serialize(result.Violations)
////                    );

////                    /*
////                     * Skips processing for this notification.

////                        Ensures invalid data never reaches downstream processors.
////                     * 
////                     */

////                    continue;
////                }

////                /*
////                 * Valid notifications are pushed to the internal queue.

////                    Actual processing happens asynchronously elsewhere.

////                    This allows:

////                    parallelism

////                    back-pressure

////                    future scaling 
////                 */

////                await _queue.EnqueueAsync(notification, stoppingToken);
////            }
////            /*
////             * Introduces a polling interval.

////                    Prevents:

////                    tight CPU loops

////                    excessive DB load

////                    Delay respects cancellation for fast shutdown.
////             * 
////             */
////            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
////        }
////    }
//}
