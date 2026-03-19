using LogDeleter;
using LogDeleter.Configuration;
using LogDeleter.Logging;
using LogDeleter.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;

// app.config에서 설정 로드
var settings = AppConfigReader.Read();

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => options.ServiceName = "LogDeleter")
    .ConfigureServices((context, services) =>
    {
        services.Configure<EventLogSettings>(s => s.SourceName = "LogDeleter");

        // app.config에서 읽은 설정을 IOptions<LogDeleterSettings>로 등록
        services.AddSingleton(Options.Create(settings));

        // log4net 기반 동작 이력 로거
        services.AddSingleton<IActivityLogger, Log4NetActivityLogger>();

        // 서비스 등록
        services.AddSingleton<LogCompressionService>();
        services.AddSingleton<LogDeletionService>();
        services.AddHostedService<Worker>();
    });

var host = builder.Build();
host.Run();
