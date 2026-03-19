using System.Collections.Generic;

namespace LogDeleter.Configuration
{
    public class LogDeleterSettings
    {
        public const string SectionName = "LogDeleterSettings";

        /// <summary>
        /// 하위 폴더 전체를 감시할 루트 폴더.
        /// 이 폴더의 직계 하위 폴더가 압축/삭제 대상이 됩니다.
        /// 예) C:\Logs → C:\Logs\App1, C:\Logs\App2, ...
        /// </summary>
        public string RootFolder { get; set; } = string.Empty;

        /// <summary>
        /// 이 시간(시간 단위) 이전의 로그를 압축합니다. 기본값: 10시간
        /// </summary>
        public int CompressAfterHours { get; set; } = 10;

        /// <summary>
        /// 이 기간(일 단위) 이전의 파일(압축 포함)을 삭제합니다. 기본값: 14일
        /// </summary>
        public int DeleteAfterDays { get; set; } = 14;

        /// <summary>
        /// Worker 실행 주기 (분 단위). 기본값: 60분
        /// </summary>
        public int WorkerIntervalMinutes { get; set; } = 60;

        /// <summary>
        /// 압축 대상 파일 확장자 목록. 기본값: .txt, .log
        /// </summary>
        public List<string> LogExtensions { get; set; } = new List<string> { ".txt", ".log" };

        /// <summary>
        /// 삭제 대상 파일 확장자 목록. 기본값: .txt, .log, .zip
        /// </summary>
        public List<string> DeletionExtensions { get; set; } = new List<string> { ".txt", ".log", ".zip" };

        /// <summary>
        /// 각 하위 폴더 안에서 재귀적으로 탐색할지 여부. 기본값: true
        /// </summary>
        public bool SearchSubDirectories { get; set; } = true;

        /// <summary>
        /// LogDeleter 자체 동작 로그를 저장할 폴더 경로
        /// </summary>
        public string ActivityLogFolder { get; set; } = @"C:\Logs\LogDeleter";
    }
}
