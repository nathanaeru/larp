@echo off
echo Compiling LARP...

set "csc="
for /d %%a in ("%windir%\Microsoft.NET\Framework\v*") do (
    if exist "%%a\csc.exe" set "csc=%%a\csc.exe"
)

if not defined csc (
    echo [ERROR] Native C# Compiler (csc.exe) not found on this Windows installation.
    pause
    exit /b
)

"%csc%" /target:winexe /out:larp.exe larp.cs

if %errorlevel% equ 0 (
    echo [SUCCESS] larp.exe built successfully!
    echo You can now delete larp.cs and build.bat.
) else (
    echo [ERROR] Compilation failed.
)
pause