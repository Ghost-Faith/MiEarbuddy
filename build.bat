@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

echo Compilando Mi Earbuddy...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"app.ico" /resource:case.png,case.png /out:"MiEarbuddy.exe" /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll src\Common\*.cs src\Bluetooth\*.cs src\UI\*.cs src\Program.cs

if %ERRORLEVEL% equ 0 (
    echo [SUCESSO] MiEarbuddy.exe gerado com sucesso!
) else (
    echo [ERRO] Falha na compilacao. Codigo: %ERRORLEVEL%
)
