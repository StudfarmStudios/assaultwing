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
