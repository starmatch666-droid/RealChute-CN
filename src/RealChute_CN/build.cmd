@echo off
rem 重新编译 RealChute_CN 插件
rem 用法：双击本文件，或在本目录执行 build.cmd

set KSP=C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program
set MANAGED=%KSP%\KSP_x64_Data\Managed
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [错误] 找不到 C# 编译器：%CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /target:library /optimize+ ^
  /out:"%~dp0RealChute_CN.dll" ^
  /r:"%MANAGED%\Assembly-CSharp.dll" ^
  /r:"%MANAGED%\UnityEngine.dll" ^
  /r:"%MANAGED%\UnityEngine.CoreModule.dll" ^
  /r:"%KSP%\GameData\000_Harmony\0Harmony.dll" ^
  "%~dp0RealChute_CN.cs"

if errorlevel 1 (
    echo.
    echo [失败] 编译出错，请查看上面的信息。
) else (
    echo.
    echo [成功] 已生成 RealChute_CN.dll
    copy /y "%~dp0RealChute_CN.dll" "%KSP%\GameData\RealChute_CN\RealChute_CN.dll" >nul
    echo [成功] 已复制到 GameData\RealChute_CN\
)
pause
