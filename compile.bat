@echo off
chcp 65001 >nul

set APP_NAME=文档水印工具
set APP_VERSION=1.2
set OUTPUT=%APP_NAME%_v%APP_VERSION%.exe

set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

set LIBDIR=
for %%F in ("%CSC%") do set LIBDIR=%%~dpF
set LIBDIR=%LIBDIR%WPF

echo 正在编译 %APP_NAME% v%APP_VERSION% ...
echo 编译器: %CSC%
echo 输出文件: %OUTPUT%
echo.

"%CSC%" /nologo /codepage:65001 /target:winexe /win32icon:assets\app_icon.ico /out:"%OUTPUT%" /lib:"%LIBDIR%" ^
/r:System.Drawing.dll ^
/r:System.Windows.Forms.dll ^
/r:System.Xml.dll ^
/r:System.Xml.Linq.dll ^
/r:System.Core.dll ^
/r:WindowsBase.dll ^
/r:PdfSharp.dll ^
Program.cs WatermarkSettings.cs WatermarkBitmapGenerator.cs DocxWatermarker.cs PdfWatermarker.cs MainForm.cs

if %errorlevel% neq 0 (
    echo.
    echo 编译失败，请检查错误信息。
    pause
    exit /b 1
)

echo.
echo 编译成功：%OUTPUT%
echo 版本规则：主版本.维护版本；维护更新递增后一位，例如 1.0 -> 1.1。
pause
