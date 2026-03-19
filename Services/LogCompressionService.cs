using System.IO.Compression;
using LogDeleter.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogDeleter.Services;

public class LogCompressionService
{
    private readonly LogDeleterSettings _settings;
    private readonly ILogger<LogCompressionService> _logger;

    public LogCompressionService(
        IOptions<LogDeleterSettings> settings,
        ILogger<LogCompressionService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 각 대상 폴더에서 CompressAfterHours 이전의 로그 파일을 시간 단위로 묶어 압축합니다.
    /// </summary>
    public void CompressOldLogs()
    {
        var cutoffTime = DateTime.Now.AddHours(-_settings.CompressAfterHours);
        _logger.LogInformation("압축 기준 시각: {CutoffTime:yyyy-MM-dd HH:mm:ss}", cutoffTime);

        foreach (var folder in _settings.TargetFolders)
        {
            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("폴더를 찾을 수 없습니다: {Folder}", folder);
                continue;
            }

            CompressFolderLogs(folder, cutoffTime);

            if (_settings.SearchSubDirectories)
            {
                foreach (var subDir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                {
                    CompressFolderLogs(subDir, cutoffTime);
                }
            }
        }
    }

    private void CompressFolderLogs(string folderPath, DateTime cutoffTime)
    {
        // 압축 대상 파일 조회 (하위 폴더 제외 - 이미 재귀 처리됨)
        var logFiles = _settings.LogExtensions
            .SelectMany(ext => Directory.EnumerateFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly))
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTime < cutoffTime)
            .ToList();

        if (logFiles.Count == 0)
            return;

        // 파일을 시간(hour) 단위로 그룹화
        var hourlyGroups = logFiles
            .GroupBy(f => new DateTime(
                f.LastWriteTime.Year,
                f.LastWriteTime.Month,
                f.LastWriteTime.Day,
                f.LastWriteTime.Hour,
                0, 0));

        foreach (var group in hourlyGroups)
        {
            var hourLabel = group.Key.ToString("yyyy-MM-dd_HH00");
            var zipPath = Path.Combine(folderPath, $"{hourLabel}.zip");

            // 이미 해당 시간대 zip이 존재하면 추가하지 않음 (중복 방지)
            if (File.Exists(zipPath))
            {
                _logger.LogDebug("이미 압축 파일 존재, 건너뜀: {ZipPath}", zipPath);
                // 이미 zip이 있지만 원본 파일이 남아있는 경우 삭제만 진행
                DeleteOriginalFiles(group.ToList(), zipPath);
                continue;
            }

            var files = group.ToList();
            _logger.LogInformation(
                "압축 중: {ZipPath} ({Count}개 파일)",
                zipPath, files.Count);

            try
            {
                using var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                foreach (var file in files)
                {
                    zipArchive.CreateEntryFromFile(
                        file.FullName,
                        file.Name,
                        CompressionLevel.Optimal);
                }

                _logger.LogInformation(
                    "압축 완료: {ZipPath} ({Count}개 파일)",
                    zipPath, files.Count);

                DeleteOriginalFiles(files, zipPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "압축 실패: {ZipPath}", zipPath);

                // 실패 시 불완전한 zip 파일 제거
                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); }
                    catch { /* ignore */ }
                }
            }
        }
    }

    private void DeleteOriginalFiles(List<FileInfo> files, string zipPath)
    {
        // zip 파일이 정상적으로 생성된 경우에만 원본 삭제
        if (!File.Exists(zipPath))
            return;

        foreach (var file in files)
        {
            try
            {
                file.Delete();
                _logger.LogDebug("원본 삭제: {File}", file.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "원본 파일 삭제 실패: {File}", file.FullName);
            }
        }
    }
}
