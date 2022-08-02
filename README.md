# Assault Wing - Galactic Battlefront

[assaultwing.com](assaultwing.com) / [GitHub](github.com/vvnurmi/assaultwing)

© Studfarm Studios

_Assault Wing is a fast-paced physics-based shooter for many players over the
internet._

## Dependencies

Assault Wing uses [MonoGame](https://www.monogame.net/) which is an open source
implementation of the Microsoft XNA 4 Framework. Assault Wing is currently using
the OpenGL backend of the MonoGame on all platforms. Assault Wing builds and
runs at least on Windows, Linux and Mac.

To compile, use the .NET 6 or later (6 is the current LTS version). Other
dependencies are fetched from NuGet, except the optional Steamworks SDK.

As an IDE either a recent Visual Studio or Visual Studio Code can be used.

## Building

On windows simple `dotnet build` should work, however there is also a more
complex build script `scripts/build.sh` to provide some automation.

Here are some examples:
```bash
# Linux incremental debug build
./scripts/build.sh linux assault_wing debug skip_clean_and_steam
# Linux clean debug build uploaded to Steam
./scripts/build.sh linux assault_wing debug
# Linux clean debug build of the dedicated server uploaded to Steam
./scripts/build.sh linux dedicated_server debug
```

The build script without parameters outputs further help.                                                                                                                                         

## Setting up Steam builds

Writing some notes here about what was done to create the steam build,
although mostly it is just basic stuff from the documentation at 
https://partner.steamgames.com/doc/sdk/uploading

Get the Steamworks SDK zip and unzip in a suitable location. Currently used version:
  
  - steamworks_sdk_153a.zip

Copy everything from the folder from the SDK `sdk/tools/ContentBuilder/` to `steambuild/` in the project folder.
For example depending where the unzipped SDK is:

    rsync -av ../../steamworks_sdk/tools/ContentBuilder/ steambuild/` 

## Notes on quick local testing

With the following the game can be made to quickly connect to a local
`DedicatedServer` process running in another terminal.

When Steam is not present:

    AssaultWing.exe --quickstart --server_name local --server raw:127.0.0.1:16727:16727

With Steam networking engine using direct connection:

    AssaultWing.exe --quickstart --server_name local --server direct:127.0.0.1:16727

With Steam networking engine using Steam Relay:

    AssaultWing.exe --quickstart --server_name local --server ip:127.0.0.1:16727


## Misc TODOs

- Disable the StatsSender or set up the backend.

## The "raw" network protocol handshake

This is just my rough idea based on looking at code how it goes.
Documenting this here to be able to replicate relevant parts using Steam networking.

- Client connects to server
  - it can attempt to connect to multiple servers at once (array of AWEndPoint)
  - First one to respond causes any later ones responding to be ignored
  - Probably used (or intended to be used) in the quick connect functionality
- Server accepts connection, adds client connection to list
- Client sends GameServerHandshakeRequestTCP
- Server checks version compatibility (DropClient etc if not ok)
- Server sets ConnectionStatus for the client connection to Active
- Server runs DoClientUdpHandshake and sends a series of UDP packets to try to break NAT (not needed on steam)
- Client runs HandleConnectionHandshakingOnClient and periodically sends GameServerHandshakeRequestUDP
  - Server handles this with HandleGameServerHandshakeRequestUDP:
    - Server updates RemoteUDPEndPoint based on the message if ClientKey matches
  - Not sure about this GameServerHandshakeRequestUDP (probably this is not needed on Steam)
  
## The Steam network protocol handshake

Based on ISteamNetworkingSockets
https://partner.steamgames.com/doc/api/ISteamNetworkingSockets

Client connects either by ConnectByIpAddress or ConnectP2P
