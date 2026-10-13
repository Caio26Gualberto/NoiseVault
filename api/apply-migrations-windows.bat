@echo off

echo ==========================================
echo        NOISEVAULT MIGRATOR
echo ==========================================
echo.

echo Applying EF Core migrations...
echo.

cd /d %~dp0

dotnet ef database update ^
  --project src\NoiseVault.Infra.Data ^
  --startup-project src\NoiseVault.Api

echo.
echo Done.
pause
