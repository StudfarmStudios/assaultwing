@echo off
setlocal
echo Usage: publish.bat [FLAVOUR]
echo FLAVOUR: If omitted, do end-user publish. If "Dev", do developer publish.
echo.
pause
SET ROOT=%~dp0%..
set FLAVOUR=%1
call msbuild "%ROOT%\AssaultWing.sln" /target:Publish /p:Configuration=Release%FLAVOUR% /p:Platform=x86
if ERRORLEVEL 1 goto :error
start "" "%ROOT%\AssaultWing\bin\x86\Release%FLAVOUR%\app.publish"
ruby "%~dp0%aw_project_file.rb" //ApplicationRevision ++
goto :eof
echo Publish succeeded

:error
echo Publish failed
