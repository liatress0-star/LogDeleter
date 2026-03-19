using LogDeleter.Configuration;
using LogDeleter.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogDeleter;

public class Worker : BackgroundService
{
    private readonly LogCompressionService _compressionService;
    private readonly LogDeletionService _deletionService;
    private readonly LogDeleterSettings _settings;
    private readonly ILogger<Worker> _logger;

    public Worker(
        LogCompressionService compressionService,
        LogDeletionService deletionService,
        IOptions<LogDeleterSettings> settings,
        ILogger<Worker> logger)
    {
        _compressionService = compressionService;
        _deletionService = deletionService;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "LogDeleter 서비스 시작 - 실행 주기: {Interval}분, " +
            "압축 기준: {CompressHours}시간, 삭제 기준: {DeleteDays}일",
            _settings.WorkerIntervalMinutes,
            _settings.CompressAfterHours,
            _settings.DeleteAfterDays);

        // 서비스 시작 시 즉시 한 번 실행
        await RunAsync(stoppingToken);

        var interval = TimeSpan.FromMinutes(_settings.WorkerIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("다음 실행까지 대기: {Interval}분", _settings.WorkerIntervalMinutes);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunAsync(stoppingToken);
        }

        _logger.LogInformation("LogDeleter 서비스 종료");
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        _logger.LogInformation("===== LogDeleter 작업 시작: {Time:yyyy-MM-dd HH:mm:ss} =====", DateTime.Now);

        try
        {
            // 1단계: 오래된 로그 압축
            _logger.LogInformation("[1/2] 로그 압축 시작");
            await Task.Run(() => _compressionService.CompressOldLogs(), stoppingToken);
            _logger.LogInformation("[1/2] 로그 압축 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "로그 압축 중 오류 발생");
        }

        if (stoppingToken.IsCancellationRequested)
            return;

        try
        {
            // 2단계: 기준 초과 파일 삭제
            _logger.LogInformation("[2/2] 오래된 파일 삭제 시작");
            await Task.Run(() => _deletionService.DeleteOldFiles(), stoppingToken);
            _logger.LogInformation("[2/2] 오래된 파일 삭제 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "파일 삭제 중 오류 발생");
        }

        _logger.LogInformation("===== LogDeleter 작업 완료: {Time:yyyy-MM-dd HH:mm:ss} =====", DateTime.Now);
    }
}
