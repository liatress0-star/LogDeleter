using System;

namespace LogDeleter.Logging
{
    /// <summary>
    /// LogDeleter 동작 이력(압축/삭제)을 파일로 기록하는 로거 인터페이스
    /// </summary>
    public interface IActivityLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception? ex = null);
    }
}
