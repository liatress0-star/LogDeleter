using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogDeleter.Configuration;
using LogDeleter.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogDeleter.Services
{
    public class LogDeletionService
    {
        private readonly LogDeleterSettings _settings;
        private readonly ILogger<LogDeletionService> _logger;
        private readonly IActivityLogger _activity;

        public LogDeletionService(
            IOptions<LogDeleterSettings> settings,
            ILogger<LogDeletionService> logger,
            IActivityLogger activity)
        {
            _settings = settings.Value;
            _logger   = logger;
            _activity = activity;
        }

        /// <summary>
        /// 각 대상 폴더에서 DeleteAfterDays 이전의 모든 로그/압축 파일을 삭제합니다.
        /// </summary>
        public void DeleteOldFiles()
        {
            var cutoffTime = DateTime.Now.AddDays(-_settings.DeleteAfterDays);

            _logger.LogInformation("삭제 기준 시각: {CutoffTime:yyyy-MM-dd HH:mm:ss}", cutoffTime);
            _activity.Info($"[삭제 시작] 기준 시각: {cutoffTime:yyyy-MM-dd HH:mm:ss} " +
                           $"(대상 확장자: {string.Join(", ", _settings.DeletionExtensions)})");

            int  totalDeleted    = 0;
            long totalBytesFreed = 0;

            foreach (var folder in _settings.TargetFolders)
            {
                if (!Directory.Exists(folder))
                {
                    _logger.LogWarning("폴더를 찾을 수 없습니다: {Folder}", folder);
                    _activity.Warn($"[삭제] 폴더 없음: {folder}");
                    continue;
                }

                var searchOption = _settings.SearchSubDirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                // DistinctBy 대신 GroupBy로 중복 제거 (net48 호환)
                var oldFiles = _settings.DeletionExtensions
                    .SelectMany(ext => Directory.EnumerateFiles(folder, $"*{ext}", searchOption))
                    .GroupBy(path => path)
                    .Select(g => new FileInfo(g.Key))
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
                        _logger.LogDebug("삭제: {File} ({Size:N0} bytes)", file.FullName, size);
                        _activity.Info($"[파일 삭제] {file.FullName} | {FormatBytes(size)} | " +
                                       $"마지막 수정: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "파일 삭제 실패: {File}", file.FullName);
                        _activity.Error($"[파일 삭제 실패] {file.FullName} | {ex.Message}");
                    }
                }

                if (_settings.SearchSubDirectories)
                    CleanEmptyDirectories(folder);
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation("삭제 완료: {Count}개 파일, {MB:F2} MB 확보",
                    totalDeleted, totalBytesFreed / 1024.0 / 1024.0);
                _activity.Info($"[삭제 완료] 총 {totalDeleted}개 파일, {FormatBytes(totalBytesFreed)} 확보");
            }
            else
            {
                _logger.LogInformation("삭제할 파일 없음");
                _activity.Info("[삭제 완료] 삭제할 파일 없음");
            }
        }

        private void CleanEmptyDirectories(string rootFolder)
        {
            foreach (var dir in Directory.EnumerateDirectories(rootFolder, "*", SearchOption.AllDirectories)
                         .OrderByDescending(d => d.Length))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                        _logger.LogDebug("빈 폴더 삭제: {Dir}", dir);
                        _activity.Info($"[폴더 삭제] {dir} (빈 폴더)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "빈 폴더 삭제 실패: {Dir}", dir);
                }
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F2} MB";
            if (bytes >= 1024)        return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}
