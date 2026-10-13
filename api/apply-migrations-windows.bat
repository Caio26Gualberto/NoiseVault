@echo off

echo ==========================================
echo        HEADBANGER MIGRATOR
echo ==========================================
echo.

echo Applying EF Core migrations...
echo.

cd /d %~dp0

dotnet ef database update ^
  --project src\Headbanger.Infra.Data ^
  --startup-project src\Headbanger.Api

echo.
echo Done.
pause
