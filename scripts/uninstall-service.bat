@echo off
:: LogDeleter Windows 서비스 제거 스크립트
:: 관리자 권한으로 실행해야 합니다.

SET SERVICE_NAME=LogDeleter

echo =============================================
echo  LogDeleter 서비스 제거
echo =============================================

:: 관리자 권한 확인
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo [오류] 관리자 권한으로 실행해야 합니다.
    pause
    exit /b 1
)

:: 서비스 존재 확인
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% NEQ 0 (
    echo 서비스가 설치되어 있지 않습니다: %SERVICE_NAME%
    pause
    exit /b 0
)

:: 서비스 중지
echo 서비스를 중지합니다...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 5 /nobreak >nul

:: 서비스 삭제
echo 서비스를 삭제합니다...
sc delete "%SERVICE_NAME%"

if %errorLevel% NEQ 0 (
    echo [오류] 서비스 삭제에 실패했습니다.
    pause
    exit /b 1
)

echo [완료] 서비스가 성공적으로 제거되었습니다.
pause
