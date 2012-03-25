@echo off
setlocal
set count=1
if "%1"=="" echo You can specify the number of AW instances as a parameter.
if not "%1"=="" set count=%1
:: Note that this doesn't work: for /L %%i in (1,1,%%count) 
set i=0
:loop
if %i% GEQ %count% goto :eof
set /a i=i+1
echo Launching Assault Wing instance %i%...
%SystemRoot%\system32\presentationhost.exe -launchapplication "http://koti.welho.com/vvnurmi/aw2/install/AssaultWingDev.application?quickstart=&server_name=Test%20Server&server=192.168.11.3:16727:16727,82.181.247.216:16727:16727&login_token=4f6df88f38226c6376000088&ship=Plissken&weapon=rockets&mod=repulsor"
timeout /t 5
goto :loop

:: http://assaultwing.com:4001/login?username=Chapelier&password=xxx
