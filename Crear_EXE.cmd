@echo off
setlocal
cd /d "%~dp0"
dotnet publish "src\Asclepio.Escritorio\Asclepio.Escritorio.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false -o "publicado"
if errorlevel 1 (
  echo.
  echo No se pudo publicar. Revisa el mensaje anterior y que el SDK de .NET 8 este instalado.
  pause
  exit /b 1
)
echo.
echo Listo: publicado\Asclepio.Prototipo.exe
pause
