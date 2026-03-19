# LogDeleter

오래된 로그 파일을 자동으로 압축·삭제하는 Windows 서비스입니다.

## 동작 방식

1시간 주기로 실행되며 두 가지 작업을 수행합니다.

| 작업 | 기준 | 동작 |
|------|------|------|
| 압축 | `CompressAfterHours` 이전 | `.txt` / `.log` 파일을 시간 단위 `.zip`으로 묶음 |
| 삭제 | `DeleteAfterDays` 이전 | 압축 파일 포함 모든 로그 파일 삭제 |

### 압축 파일 네이밍

같은 시간대의 로그는 하나의 zip으로 묶입니다.

```
C:\Logs\App1\
  ├── 2024-03-01_0900.zip   ← 09:00 ~ 09:59 로그 묶음
  ├── 2024-03-01_1000.zip
  └── ...
```

## 설정 (appsettings.json)

```json
{
  "LogDeleterSettings": {
    "TargetFolders": [
      "C:\\Logs\\App1",
      "C:\\Logs\\App2"
    ],
    "CompressAfterHours": 10,
    "DeleteAfterDays": 14,
    "WorkerIntervalMinutes": 60,
    "LogExtensions": [ ".txt", ".log" ],
    "SearchSubDirectories": true
  }
}
```

| 항목 | 기본값 | 설명 |
|------|--------|------|
| `TargetFolders` | - | 대상 폴더 목록 (필수) |
| `CompressAfterHours` | `10` | 이 시간(시간 단위) 이전 로그 압축 |
| `DeleteAfterDays` | `14` | 이 기간(일 단위) 이전 파일 삭제 |
| `WorkerIntervalMinutes` | `60` | 실행 주기 (분) |
| `LogExtensions` | `.txt`, `.log` | 압축 대상 확장자 |
| `SearchSubDirectories` | `true` | 하위 폴더 재귀 탐색 여부 |

## 빌드 및 설치

### 1. 빌드

```bat
scripts\build-publish.bat
```

또는 직접:

```bat
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

### 2. 설정 수정

`publish\appsettings.json`에서 `TargetFolders` 및 기준 값을 환경에 맞게 수정합니다.

### 3. 서비스 설치

관리자 권한으로 실행:

```bat
scripts\install-service.bat
```

### 4. 서비스 제거

```bat
scripts\uninstall-service.bat
```

## 로그 확인

Windows 이벤트 뷰어 → Windows 로그 → 응용 프로그램 → 소스: `LogDeleter`

## 요구 사항

- .NET 8 Runtime (또는 self-contained 빌드 사용)
- Windows OS
- 서비스 설치 시 관리자 권한 필요
