# The Assault Wing network protocol

The Assault Wing supports a server mode directly from a client
process and it also has a separate console based dedicated server.

The networking is split in 2 major modes. The game protocol is roughly the same
in both modes, but the low level transport mechnism is different.

## The Steam network mode  

The Steam networking mode is based on
[ISteamNetworkingSockets](https://partner.steamgames.com/doc/api/ISteamNetworkingSockets)
which provides several advanced features out of the box. The server browsing and
joining from the client UI is also available in this mode.

## The "raw" network mode

This is the original 2013 network code which is dubbed `raw` in the code and in
the address format (Example `raw:127.0.0.1:16727:16727`).

The raw mode is built on "raw" TCP and UDP sockets. This mode is useful for
several things and is kept available for these reasons:
- testing without Steam
- a reference implementation
- future proofing in the event Steam is not available for some reason

Currently in the "raw" mode the server browsing is not available and joining
servers can only be done through the command line.

## The game server handshake

This documentation was created on reverse engineering the code to support the
development of the Steam based network layer. As such it is not authoritative
and many not be accurate. Hopefully this can be improved later.

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
