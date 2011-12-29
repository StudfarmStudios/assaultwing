@echo off

:: This batch file runs in an infinite loop, checking periodically
:: that an Assault Wing dedicated server is running. If not, it is
:: re-launched. Uses grep.exe that you can get from UnxUtils at
:: http://sourceforge.net/projects/unxutils/

set WAIT_SECS=60
set AW_LAUNCH_BAT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Studfarm Studios\Assault Wing Dedicated Server.cmd
set GREP_EXE=c:\Program Files (x86)\UnxUtils\grep.exe

:loop
:: Grep returns 0 if match, 1 if no match, and 2 if trouble.
tasklist | "%GREP_EXE%" AssaultWing.exe > NUL
set GREP_RETURN=%ERRORLEVEL%
if %GREP_RETURN%==1 echo Assault Wing not running, re-launching!
if %GREP_RETURN%==1 call "%AW_LAUNCH_BAT%"
choice /m "Wanna [C]heck again (automatically in %WAIT_SECS% seconds) or [Q]uit?" /C CQ /d C /t %WAIT_SECS%
if %ERRORLEVEL%==1 goto loop
