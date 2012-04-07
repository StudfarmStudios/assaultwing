@echo off
setlocal
set max_tester=16
set tester_from=1
set tester_to=1
if "%2"=="" (
    echo You can specify tester START and END indices as parameter, e.g. "%~n0 1 %max_tester%"
) else (
    set tester_from=%1
    set tester_to=%2
)
if %tester_from% LSS 1 goto :invalid_range
if %tester_to% GTR %max_tester% goto :invalid_range
if %tester_from% GTR %tester_to% goto :invalid_range

:: Note that this doesn't work: for /L %%i in (%%tester_from,1,%%tester_to) 
set i=%tester_from%
:loop
if %i% GTR %tester_to% goto :eof
echo Launching Assault Wing instance %i%...
if %i% EQU 1 set login_token=4f7f03bfb42ade8c5c000018
if %i% EQU 2 set login_token=4f7f03e6b42ade8c5c000019
if %i% EQU 3 set login_token=4f7f0400b42ade8c5c00001a
if %i% EQU 4 set login_token=4f7f040bb42ade8c5c00001b
if %i% EQU 5 set login_token=4f7f041ab42ade8c5c00001c
if %i% EQU 6 set login_token=4f7f0425b42ade8c5c00001d
if %i% EQU 7 set login_token=4f7f042fb42ade8c5c00001e
if %i% EQU 8 set login_token=4f7f043ab42ade8c5c00001f
if %i% EQU 9 set login_token=4f7f0446b42ade8c5c000020
if %i% EQU 10 set login_token=4f7f0453b42ade8c5c000021
if %i% EQU 11 set login_token=4f7f045db42ade8c5c000022
if %i% EQU 12 set login_token=4f7f046eb42ade8c5c000023
if %i% EQU 13 set login_token=4f7f099cb42ade8c5c000027
if %i% EQU 14 set login_token=4f7f09c0b42ade8c5c000028
if %i% EQU 15 set login_token=4f7f09d1b42ade8c5c000029
if %i% EQU 16 set login_token=4f7f1369b42ade8c5c00002b
%SystemRoot%\system32\presentationhost.exe -launchapplication "http://koti.welho.com/vvnurmi/aw2/install/AssaultWingDev.application?quickstart=&server_name=Test%20Server&server=62.75.224.66:16728:16728&login_token=%login_token%&ship=Plissken&weapon=rockets&mod=repulsor"
timeout /t 5
set /a i=i+1
goto :loop

:invalid_range
echo Invalid range: %tester_from%...%tester_to%
goto :eof

:: http://assaultwing.com:4001/login?username=Tester%20A&password=tester
:: Chapelier: 4f6df88f38226c6376000088
:: Tester A: 4f7f03bfb42ade8c5c000018
:: Tester B: 4f7f03e6b42ade8c5c000019
:: Tester C: 4f7f0400b42ade8c5c00001a
:: Tester D: 4f7f040bb42ade8c5c00001b
:: Tester E: 4f7f041ab42ade8c5c00001c
:: Tester F: 4f7f0425b42ade8c5c00001d
:: Tester G: 4f7f042fb42ade8c5c00001e
:: Tester H: 4f7f043ab42ade8c5c00001f
:: Tester I: 4f7f0446b42ade8c5c000020
:: Tester J: 4f7f0453b42ade8c5c000021
:: Tester K: 4f7f045db42ade8c5c000022
:: Tester L: 4f7f046eb42ade8c5c000023
:: Tester M: 4f7f099cb42ade8c5c000027
:: Tester N: 4f7f09c0b42ade8c5c000028
:: Tester O: 4f7f09d1b42ade8c5c000029
:: Tester P: 4f7f1369b42ade8c5c00002b
