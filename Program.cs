using LogDeleter;
using LogDeleter.Configuration;
using LogDeleter.Logging;
using LogDeleter.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);

// Windows 서비스로 실행 지원
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LogDeleter";
});

// Windows EventLog 로깅 설정
builder.Services.Configure<EventLogSettings>(settings =>
{
    settings.SourceName = "LogDeleter";
});

// 설정 바인딩
builder.Services.Configure<LogDeleterSettings>(
    builder.Configuration.GetSection(LogDeleterSettings.SectionName));

// log4net 기반 동작 이력 로거 등록
builder.Services.AddSingleton<IActivityLogger, Log4NetActivityLogger>();

// 서비스 등록
builder.Services.AddSingleton<LogCompressionService>();
builder.Services.AddSingleton<LogDeletionService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
