@echo off
:: LogDeleter 빌드 및 배포 스크립트

SET PROJECT_DIR=%~dp0..
SET OUTPUT_DIR=%PROJECT_DIR%\publish

echo =============================================
echo  LogDeleter 빌드 및 배포
echo =============================================

:: 기존 publish 폴더 정리
if exist "%OUTPUT_DIR%" (
    echo 기존 publish 폴더를 정리합니다...
    rmdir /s /q "%OUTPUT_DIR%"
)

:: 빌드 및 발행 (단일 실행 파일, self-contained)
echo 빌드 중...
dotnet publish "%PROJECT_DIR%\LogDeleter.csproj" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT_DIR%"

if %errorLevel% NEQ 0 (
    echo [오류] 빌드에 실패했습니다.
    pause
    exit /b 1
)

echo.
echo [완료] 빌드 성공: %OUTPUT_DIR%
echo.
echo 다음 단계: install-service.bat 을 관리자 권한으로 실행하세요.
pause
