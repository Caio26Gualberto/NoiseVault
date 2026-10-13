@echo off
echo ==========================================
echo        NOISEVAULT MIGRATION CREATOR
echo ==========================================
echo.

REM Vai para a pasta onde está o .bat
cd /d %~dp0

REM Pergunta o nome da migration
set /p MIGRATION_NAME=Enter migration name:

REM Validação simples
if "%MIGRATION_NAME%"=="" (
    echo.
    echo Migration name cannot be empty.
    pause
    exit /b
)

echo.
echo Creating migration "%MIGRATION_NAME%"...
echo.

dotnet ef migrations add %MIGRATION_NAME% ^
  --project src\NoiseVault.Infra.Data ^
  --startup-project src\NoiseVault.Api

echo.
echo Done.
pause
