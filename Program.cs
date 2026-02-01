using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using ProcessEngine.Worker;
using ProcessEngine.Worker.Application;
using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Application.Processing;
using ProcessEngine.Worker.Application.Rules;

using ProcessEngine.Worker.Infrastructure.Audit;
using ProcessEngine.Worker.Infrastructure.File;
using ProcessEngine.Worker.Infrastructure.Ingestion;
using ProcessEngine.Worker.Infrastructure.Persistence;
using ProcessEngine.Worker.Infrastructure.Processing;
using ProcessEngine.Worker.Infrastructure.Queue;
using ProcessEngine.Worker.Infrastructure.Rules;

// ------------------------------------------------------------
// CRITICAL FIX: enable snake_case → PascalCase mapping for Dapper
// ------------------------------------------------------------
DefaultTypeMap.MatchNamesWithUnderscores = true;

// ------------------------------------------------------------
// HOST BOOTSTRAP
// ------------------------------------------------------------
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        // ========================================================
        // DATABASE
        // ========================================================
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<INotificationRepository, MySqlNotificationRepository>();
        services.AddSingleton<ISqlProvider>(
            new FileSqlProvider("sql/notifications"));

        // ========================================================
        // RULE ENGINE
        // ========================================================
        services.AddSingleton(new FileRulesetProvider("rules/active-ruleset.json"));
        services.AddSingleton<IRuleValidator, RuleValidator>();

        // ========================================================
        // AUDIT LOGGING
        // ========================================================
        services.AddSingleton<IAuditLogger>(
            new TextAuditLogger("audit/rule-engine.log"));

        // ========================================================
        // QUEUE & CORE PROCESSING
        // ========================================================
        services.AddSingleton<INotificationQueue, InMemoryNotificationQueue>();

        // 🔑 🔑 🔑 MISSING REGISTRATION (THIS WAS THE BUG)
        services.AddSingleton<ProcessingPipeline>();

        services.AddSingleton<INotificationProcessor, NotificationProcessor>();

        // ========================================================
        // INGESTION CHANNELS (PLUGGABLE)
        // ========================================================
        services.AddSingleton<FileIngestionService>();
        services.AddSingleton<RabbitMqIngestionService>();
        // services.AddSingleton<SftpIngestionService>(); // optional

        // ========================================================
        // PROCESSING STEPS (ORDERED + CONDITIONAL)
        // ========================================================

        if (config.GetValue<bool>("ProcessingSteps:XmlConversion:Enabled"))
        {
            services.AddSingleton<IProcessingStep, XmlConversionStep>();
        }

        if (config.GetValue<bool>("ProcessingSteps:FileOutput:Enabled"))
        {
            services.AddSingleton<IProcessingStep, FileOutputStep>();
        }

        // ========================================================
        // WORKER (BACKGROUND SERVICE)
        // ========================================================
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
