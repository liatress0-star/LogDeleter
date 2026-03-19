@echo off
:: LogDeleter Windows 서비스 설치 스크립트
:: 관리자 권한으로 실행해야 합니다.

SET SERVICE_NAME=LogDeleter
SET DISPLAY_NAME=Log Deleter Service
SET DESCRIPTION=오래된 로그 파일을 자동으로 압축 및 삭제하는 서비스
SET EXE_PATH=%~dp0..\publish\LogDeleter.exe

echo =============================================
echo  LogDeleter 서비스 설치
echo =============================================

:: 관리자 권한 확인
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo [오류] 관리자 권한으로 실행해야 합니다.
    pause
    exit /b 1
)

:: 실행 파일 존재 확인
if not exist "%EXE_PATH%" (
    echo [오류] 실행 파일을 찾을 수 없습니다: %EXE_PATH%
    echo 먼저 publish 폴더에 빌드 결과물을 배치하세요.
    echo 예) dotnet publish -c Release -r win-x64 --self-contained -o publish
    pause
    exit /b 1
)

:: 기존 서비스 확인 및 삭제
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorLevel% EQU 0 (
    echo 기존 서비스를 중지하고 삭제합니다...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 3 /nobreak >nul
    sc delete "%SERVICE_NAME%"
    timeout /t 2 /nobreak >nul
)

:: 서비스 설치
echo 서비스를 설치합니다: %EXE_PATH%
sc create "%SERVICE_NAME%" ^
    binPath= "\"%EXE_PATH%\"" ^
    DisplayName= "%DISPLAY_NAME%" ^
    start= auto ^
    obj= LocalSystem

if %errorLevel% NEQ 0 (
    echo [오류] 서비스 설치에 실패했습니다.
    pause
    exit /b 1
)

:: 서비스 설명 추가
sc description "%SERVICE_NAME%" "%DESCRIPTION%"

:: 서비스 실패 시 자동 재시작 설정 (1분 후)
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/60000/restart/60000/restart/60000

:: 서비스 시작
echo 서비스를 시작합니다...
sc start "%SERVICE_NAME%"

if %errorLevel% NEQ 0 (
    echo [경고] 서비스 시작에 실패했습니다. 수동으로 시작해주세요.
) else (
    echo [완료] 서비스가 성공적으로 설치 및 시작되었습니다.
)

echo.
echo 서비스 상태 확인: sc query %SERVICE_NAME%
echo 서비스 관리: services.msc
pause
