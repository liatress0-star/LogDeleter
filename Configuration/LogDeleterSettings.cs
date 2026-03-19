namespace LogDeleter.Configuration;

public class LogDeleterSettings
{
    public const string SectionName = "LogDeleterSettings";

    /// <summary>
    /// 로그가 쌓이는 대상 폴더 목록 (하위 폴더 포함)
    /// </summary>
    public List<string> TargetFolders { get; set; } = new();

    /// <summary>
    /// 이 시간(시간 단위) 이전의 로그를 압축합니다. 기본값: 10시간
    /// </summary>
    public int CompressAfterHours { get; set; } = 10;

    /// <summary>
    /// 이 시간(일 단위) 이전의 파일(압축 포함)을 삭제합니다. 기본값: 14일
    /// </summary>
    public int DeleteAfterDays { get; set; } = 14;

    /// <summary>
    /// Worker 실행 주기 (분 단위). 기본값: 60분
    /// </summary>
    public int WorkerIntervalMinutes { get; set; } = 60;

    /// <summary>
    /// 압축 대상 파일 확장자 목록
    /// </summary>
    public List<string> LogExtensions { get; set; } = new() { ".txt", ".log" };

    /// <summary>
    /// 하위 폴더를 재귀적으로 탐색할지 여부
    /// </summary>
    public bool SearchSubDirectories { get; set; } = true;
}
