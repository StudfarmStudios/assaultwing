@echo off
echo Usage: publish.bat [FLAVOUR]
echo FLAVOUR: If omitted, do end-user publish. If "Dev", do developer publish.
echo.
pause
set FLAVOUR=%1
call msbuild ..\AssaultWing.sln /target:Publish /p:Configuration=Release%FLAVOUR% /p:Platform=x86
start ..\AssaultWing\bin\x86\Release%FLAVOUR%\app.publish
echo TODO: Increment revision number in AssaultWing.csproj
pause
set FLAVOUR=
