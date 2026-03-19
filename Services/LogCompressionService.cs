using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using LogDeleter.Configuration;
using LogDeleter.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogDeleter.Services
{
    public class LogCompressionService
    {
        private readonly LogDeleterSettings _settings;
        private readonly ILogger<LogCompressionService> _logger;
        private readonly IActivityLogger _activity;

        public LogCompressionService(
            IOptions<LogDeleterSettings> settings,
            ILogger<LogCompressionService> logger,
            IActivityLogger activity)
        {
            _settings = settings.Value;
            _logger   = logger;
            _activity = activity;
        }

        /// <summary>
        /// RootFolder의 직계 하위 폴더들에서 CompressAfterHours 이전 파일을 시간 단위로 묶어 압축합니다.
        /// </summary>
        public void CompressOldLogs()
        {
            var cutoffTime = DateTime.Now.AddHours(-_settings.CompressAfterHours);

            _logger.LogInformation("압축 기준 시각: {CutoffTime:yyyy-MM-dd HH:mm:ss}", cutoffTime);
            _activity.Info($"[압축 시작] 기준 시각: {cutoffTime:yyyy-MM-dd HH:mm:ss} " +
                           $"(대상 확장자: {string.Join(", ", _settings.LogExtensions)})");

            if (!Directory.Exists(_settings.RootFolder))
            {
                _logger.LogWarning("루트 폴더를 찾을 수 없습니다: {RootFolder}", _settings.RootFolder);
                _activity.Warn($"[압축] 루트 폴더 없음: {_settings.RootFolder}");
                return;
            }

            // RootFolder의 직계 하위 폴더 목록을 대상으로 처리
            foreach (var folder in Directory.GetDirectories(_settings.RootFolder, "*", SearchOption.TopDirectoryOnly))
            {
                CompressFolderLogs(folder, cutoffTime);

                if (_settings.SearchSubDirectories)
                {
                    foreach (var subDir in Directory.GetDirectories(folder, "*", SearchOption.AllDirectories))
                        CompressFolderLogs(subDir, cutoffTime);
                }
            }

            _activity.Info("[압축 완료]");
        }

        private void CompressFolderLogs(string folderPath, DateTime cutoffTime)
        {
            var logFiles = _settings.LogExtensions
                .SelectMany(ext => Directory.EnumerateFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly))
                .Select(f => new FileInfo(f))
                .Where(f => f.LastWriteTime < cutoffTime)
                .ToList();

            if (logFiles.Count == 0)
                return;

            // 파일을 시간(hour) 단위로 그룹화
            var hourlyGroups = logFiles.GroupBy(f => new DateTime(
                f.LastWriteTime.Year, f.LastWriteTime.Month, f.LastWriteTime.Day,
                f.LastWriteTime.Hour, 0, 0));

            foreach (var group in hourlyGroups)
            {
                var hourLabel = group.Key.ToString("yyyy-MM-dd_HH00");
                var zipPath   = Path.Combine(folderPath, $"{hourLabel}.zip");
                var files     = group.ToList();

                if (File.Exists(zipPath))
                {
                    _logger.LogDebug("이미 압축 파일 존재, 건너뜀: {ZipPath}", zipPath);
                    _activity.Info($"[압축 건너뜀] 이미 존재: {zipPath}");
                    DeleteOriginalFiles(files, zipPath);
                    continue;
                }

                _logger.LogInformation("압축 중: {ZipPath} ({Count}개 파일)", zipPath, files.Count);

                try
                {
                    long originalBytes = files.Sum(f => f.Length);

                    using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        foreach (var file in files)
                        {
                            zipArchive.CreateEntryFromFile(
                                file.FullName, file.Name, CompressionLevel.Optimal);
                        }
                    }

                    long compressedBytes = new FileInfo(zipPath).Length;
                    double ratio = originalBytes > 0
                        ? (1.0 - (double)compressedBytes / originalBytes) * 100
                        : 0;

                    _logger.LogInformation("압축 완료: {ZipPath} ({Count}개 파일)", zipPath, files.Count);
                    _activity.Info(
                        $"[압축 생성] {zipPath} | " +
                        $"파일 수: {files.Count}개 | " +
                        $"원본: {FormatBytes(originalBytes)} → 압축: {FormatBytes(compressedBytes)} " +
                        $"(절감률 {ratio:F1}%)");

                    DeleteOriginalFiles(files, zipPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "압축 실패: {ZipPath}", zipPath);
                    _activity.Error($"[압축 실패] {zipPath} | {ex.Message}", ex);

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
            if (!File.Exists(zipPath))
                return;

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                    _logger.LogDebug("원본 삭제: {File}", file.FullName);
                    _activity.Info($"[원본 삭제] {file.FullName}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "원본 파일 삭제 실패: {File}", file.FullName);
                    _activity.Error($"[원본 삭제 실패] {file.FullName} | {ex.Message}");
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
