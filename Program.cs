using LogDeleter;
using LogDeleter.Configuration;
using LogDeleter.Logging;
using LogDeleter.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "LogDeleter";
    })
    .ConfigureServices((context, services) =>
    {
        // Windows EventLog 로깅 설정
        services.Configure<EventLogSettings>(settings =>
        {
            settings.SourceName = "LogDeleter";
        });

        // 설정 바인딩
        services.Configure<LogDeleterSettings>(
            context.Configuration.GetSection(LogDeleterSettings.SectionName));

        // log4net 기반 동작 이력 로거 등록
        services.AddSingleton<IActivityLogger, Log4NetActivityLogger>();

        // 서비스 등록
        services.AddSingleton<LogCompressionService>();
        services.AddSingleton<LogDeletionService>();
        services.AddHostedService<Worker>();
    });

var host = builder.Build();
host.Run();
