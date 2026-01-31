using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ProcessEngine.Worker;
using ProcessEngine.Worker.Application;
using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Application.Rules;
using ProcessEngine.Worker.Infrastructure.Audit;
using ProcessEngine.Worker.Infrastructure.File;
using ProcessEngine.Worker.Infrastructure.Ingestion;
using ProcessEngine.Worker.Infrastructure.Persistence;
using ProcessEngine.Worker.Infrastructure.Queue;
using ProcessEngine.Worker.Infrastructure.Rules;

//CRITICAL FIX: enable snake_case → PascalCase mapping for Dapper
DefaultTypeMap.MatchNamesWithUnderscores = true;

/*
 * 
 * A Host is created

        Dependency Injection container is built

        Logging, config, lifetime services are initialized

        Services are registered

        Hosted services are discovered
 * 
 */
Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // DB
        services.AddSingleton<DbConnectionFactory>();
        services.AddSingleton<INotificationRepository, MySqlNotificationRepository>();

        // Rules
        services.AddSingleton(new FileRulesetProvider("rules/active-ruleset.json"));
        services.AddSingleton<IRuleValidator, RuleValidator>();

        // Audit
        services.AddSingleton<IAuditLogger>(
            new TextAuditLogger("audit/rule-engine.log"));

        // Queue & Processing
        services.AddSingleton<INotificationQueue, InMemoryNotificationQueue>();
        services.AddSingleton<FileNotificationHandler>();
        services.AddSingleton<INotificationProcessor, NotificationProcessor>();

        // Ingestion
        services.AddSingleton<FileIngestionService>();
        // services.AddSingleton<SftpIngestionService>();
        services.AddSingleton<RabbitMqIngestionService>();

        services.AddSingleton<ISqlProvider>(new FileSqlProvider("sql/notifications"));

        // Worker is a background service. Start it when the application starts
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run(); //Start all registered background services and manage their lifecycle.
