using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using LogDeleter.Configuration;
using Microsoft.Extensions.Options;

namespace LogDeleter.Logging
{
    /// <summary>
    /// log4net 기반 동작 이력 로거.
    /// ActivityLogFolder에 날짜별 롤링 파일로 기록합니다.
    /// 파일명: activity_yyyy-MM-dd.log
    /// </summary>
    public class Log4NetActivityLogger : IActivityLogger
    {
        private const string LoggerName = "LogDeleter.Activity";

        // 싱글턴이므로 한 번만 구성되지만, 안전하게 플래그로 중복 방지
        private static bool _configured;
        private static readonly object _lock = new object();

        private readonly ILog _log;

        public Log4NetActivityLogger(IOptions<LogDeleterSettings> settings)
        {
            var logFolder = settings.Value.ActivityLogFolder;

            if (string.IsNullOrWhiteSpace(logFolder))
                logFolder = @"C:\Logs\LogDeleter";

            Directory.CreateDirectory(logFolder);

            ConfigureLog4Net(logFolder);

            _log = LogManager.GetLogger(Assembly.GetEntryAssembly(), LoggerName);
        }

        private static void ConfigureLog4Net(string logFolder)
        {
            lock (_lock)
            {
                if (_configured)
                    return;

                var hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());

                var layout = new PatternLayout(
                    "[%date{yyyy-MM-dd HH:mm:ss}] [%-5level] %message%newline");
                layout.ActivateOptions();

                // 날짜별 롤링 파일 어펜더: activity_2024-03-01.log
                var appender = new RollingFileAppender
                {
                    Name              = "ActivityFileAppender",
                    File              = Path.Combine(logFolder, "activity_"),
                    AppendToFile      = true,
                    RollingStyle      = RollingFileAppender.RollingMode.Date,
                    DatePattern       = "yyyy-MM-dd'.log'",
                    StaticLogFileName = false,
                    LockingModel      = new FileAppender.MinimalLock(),
                    Layout            = layout,
                };
                appender.ActivateOptions();

                var logger = (Logger)hierarchy.GetLogger(LoggerName);
                logger.Level      = Level.All;
                logger.Additivity = false; // 루트 로거로 전파 안 함
                logger.AddAppender(appender);

                hierarchy.Configured = true;
                _configured = true;
            }
        }

        public void Info(string message) => _log.Info(message);
        public void Warn(string message) => _log.Warn(message);
        public void Error(string message, Exception? ex = null)
        {
            if (ex != null) _log.Error(message, ex);
            else            _log.Error(message);
        }
    }
}
