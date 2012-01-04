@echo off
echo Running %~dpf0
ruby "%~dp0%aw_update_config.rb"
timeout /t 10
