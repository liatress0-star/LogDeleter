using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace LogDeleter.Configuration
{
    /// <summary>
    /// app.config의 appSettings 섹션에서 LogDeleterSettings를 읽어옵니다.
    /// 값이 없으면 LogDeleterSettings의 기본값이 유지됩니다.
    /// </summary>
    public static class AppConfigReader
    {
        public static LogDeleterSettings Read()
        {
            var settings = new LogDeleterSettings();
            var app = ConfigurationManager.AppSettings;

            if (!string.IsNullOrWhiteSpace(app["RootFolder"]))
                settings.RootFolder = app["RootFolder"]!;

            if (int.TryParse(app["CompressAfterHours"], out int compressHours))
                settings.CompressAfterHours = compressHours;

            if (int.TryParse(app["DeleteAfterDays"], out int deleteDays))
                settings.DeleteAfterDays = deleteDays;

            if (int.TryParse(app["WorkerIntervalMinutes"], out int interval))
                settings.WorkerIntervalMinutes = interval;

            if (!string.IsNullOrWhiteSpace(app["LogExtensions"]))
                settings.LogExtensions = SplitValues(app["LogExtensions"]!);

            if (!string.IsNullOrWhiteSpace(app["DeletionExtensions"]))
                settings.DeletionExtensions = SplitValues(app["DeletionExtensions"]!);

            if (bool.TryParse(app["SearchSubDirectories"], out bool searchSub))
                settings.SearchSubDirectories = searchSub;

            if (!string.IsNullOrWhiteSpace(app["ActivityLogFolder"]))
                settings.ActivityLogFolder = app["ActivityLogFolder"]!;

            return settings;
        }

        private static List<string> SplitValues(string value)
        {
            return value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
        }
    }
}
