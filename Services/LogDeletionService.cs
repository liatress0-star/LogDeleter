using LogDeleter.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogDeleter.Services;

public class LogDeletionService
{
    private readonly LogDeleterSettings _settings;
    private readonly ILogger<LogDeletionService> _logger;

    // 삭제 대상 확장자: 로그 파일 + 압축 파일
    private static readonly string[] DeletionExtensions = { ".txt", ".log", ".zip" };

    public LogDeletionService(
        IOptions<LogDeleterSettings> settings,
        ILogger<LogDeletionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 각 대상 폴더에서 DeleteAfterDays 이전의 모든 로그/압축 파일을 삭제합니다.
    /// </summary>
    public void DeleteOldFiles()
    {
        var cutoffTime = DateTime.Now.AddDays(-_settings.DeleteAfterDays);
        _logger.LogInformation("삭제 기준 시각: {CutoffTime:yyyy-MM-dd HH:mm:ss}", cutoffTime);

        int totalDeleted = 0;
        long totalBytesFreed = 0;

        foreach (var folder in _settings.TargetFolders)
        {
            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("폴더를 찾을 수 없습니다: {Folder}", folder);
                continue;
            }

            var searchOption = _settings.SearchSubDirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var oldFiles = DeletionExtensions
                .SelectMany(ext => Directory.EnumerateFiles(folder, $"*{ext}", searchOption))
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTime < cutoffTime)
                .ToList();

            foreach (var file in oldFiles)
            {
                try
                {
                    long size = file.Length;
                    file.Delete();
                    totalDeleted++;
                    totalBytesFreed += size;
                    _logger.LogDebug(
                        "삭제: {File} ({Size:N0} bytes)",
                        file.FullName, size);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "파일 삭제 실패: {File}", file.FullName);
                }
            }

            // 빈 폴더 정리 (대상 폴더 자체는 제외)
            if (_settings.SearchSubDirectories)
            {
                CleanEmptyDirectories(folder);
            }
        }

        if (totalDeleted > 0)
        {
            _logger.LogInformation(
                "삭제 완료: {Count}개 파일, {MB:F2} MB 확보",
                totalDeleted, totalBytesFreed / 1024.0 / 1024.0);
        }
        else
        {
            _logger.LogInformation("삭제할 파일 없음");
        }
    }

    private void CleanEmptyDirectories(string rootFolder)
    {
        // 하위 폴더부터 순회하여 빈 폴더 삭제 (루트 폴더는 제외)
        foreach (var dir in Directory.EnumerateDirectories(rootFolder, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Length)) // 깊은 경로 먼저
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    _logger.LogDebug("빈 폴더 삭제: {Dir}", dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "빈 폴더 삭제 실패: {Dir}", dir);
            }
        }
    }
}
