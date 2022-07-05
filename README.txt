Assault Wing - Galactic Battlefront

assaultwing.com

github.com/vvnurmi/assaultwing

(c) Studfarm Studios

Assault Wing is a fast-paced physics-based shooter for many players over the internet.

To compile, use Microsoft Visual C# 2010 Express and XNA Game Studio 4.0 Refresh.


## Notes on Steam builds

Writing some notes here about what was done to create the steam build,
although mostly it is just basic stuff from the documentation at 
https://partner.steamgames.com/doc/sdk/uploading

Get the steam SDK zip and unzip in a suitable location. Currently used version:
  
  - steamworks_sdk_153a.zip

Copy everything from the folder from the SDK `sdk/tools/ContentBuilder/` to `steambuild/` in the project folder.
For example depending where the unzipped SDK is:

    rsync -av ../../steamworks_sdk/tools/ContentBuilder/ steambuild/` 

## Notes on quick local testing

With the following the game can be made to quickly connect to a local
`DedicatedServer` process running in another terminal.

    AssaultWing.exe --quickstart --server_name local --server 127.0.0.1:16727:16727

## Misc TODOs

- Disable the StatsSender or set up the backend.
