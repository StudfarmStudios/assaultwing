@echo off
setlocal

echo Usage: steam_build.bat [Configuration]
echo Configuration: "Release" for release. If "Debug", do developer publish.
$echo.

pause

SET ROOT=%~dp0%..
set FLAVOUR=%1

echo dotnet clean
call dotnet clean "%ROOT%\AssaultWing.sln" --nologo --verbosity=quiet
if ERRORLEVEL 1 goto :error

:: It seems we need to define build profiles to build for Linux & Mac here. https://github.com/dotnet/sdk/issues/14281#issuecomment-863499124
call dotnet build "%ROOT%\AssaultWing.sln" --nologo --verbosity=quiet /target:Publish /p:Configuration=%FLAVOUR% "/p:Platform=Any Cpu"
if ERRORLEVEL 1 goto :error

goto :eof

:error
echo Steam build failed
