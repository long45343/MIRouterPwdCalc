@echo off
setlocal
cd /d "%~dp0"

echo [1/2] 查找 Windows 内置的 .NET Framework VB 编译器...
set "VBC_PATH=%windir%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe"
if not exist "%VBC_PATH%" (
    set "VBC_PATH=%windir%\Microsoft.NET\Framework\v4.0.30319\vbc.exe"
)

if not exist "%VBC_PATH%" (
    echo [错误] 未在系统中检测到 .NET Framework 4.0/4.8 vbc.exe
    pause
    exit /b 1
)

echo [2/2] 正在编译 MainForm.vb -> XiaomiRouterSshCalc.exe...
"%VBC_PATH%" /target:winexe /out:"XiaomiRouterSshCalc.exe" /win32icon:"Resources\app.ico" /win32manifest:"app.manifest" /reference:"System.dll","System.Windows.Forms.dll","System.Drawing.dll","System.Core.dll" "MainForm.vb"

if %errorlevel% equ 0 (
    echo.
    echo ===================================================
    echo 编译成功！生成文件：XiaomiRouterSshCalc.exe
    echo ===================================================
) else (
    echo.
    echo [失败] 编译出错，请检查上方日志。
)

pause
