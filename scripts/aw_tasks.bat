@echo off

:: Creates Windows tasks for managing an official Assault Wing dedicated server.
:: Note that Windows tasks persist over reboots. Therefore it suffices to run this
:: batch file just once for a server. See schtasks /? for more info. To delete a task,
:: type e.g. schtasks /delete /tn "AW config"
::
:: "AW keepalive" relaunches the dedicated server if AssaultWing.exe crashes.
:: "AW config" switches bots off for Wednesdays.

set AW_LAUNCH_BAT=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Studfarm Studios\Assault Wing Dedicated Server.cmd
set AW_CRASH_EVENT_XPATH=*[System[Provider[@Name='Application Error'] and (EventID=1000)] and EventData[Data='AssaultWing.exe']]
set AW_UPDATE_CONFIG=%~dp0%aw_update_config.bat

schtasks /create /f /tn "AW keepalive" /tr "\"%AW_LAUNCH_BAT%\"" /sc ONEVENT /delay 0000:05 /ec Application /mo "%AW_CRASH_EVENT_XPATH%"

schtasks /create /f /tn "AW config" /tr "\"%AW_UPDATE_CONFIG%\"" /sc HOURLY /mo 1
