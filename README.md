# Assault Wing - Galactic Battlefront

[assaultwing.com](http://assaultwing.com) / [GitHub](https://github.com/StudfarmStudios/assaultwing)

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

A simple `dotnet build` should work. Use `dotnet test` to run the test suite.

The content building only works on Windows at the moment. To make code building
work on other platforms, the content building is disabled for non Windows
platforms (Linux, Mac) in the `AssaultWingContent.csproj` and
`AssaultWingCoreContent.csproj` files. In this case the data folders (`Content`
and `CoreContent`) must be made accessible in the working directory when the
application is run.

### About Steam builds and the Steamworks SDK

The Steamworks SDK is only needed when developing Steam features or uploading a
build to the Steam. In this case the SDK needs to be manually downloaded from
[partner.steamgames.com](https://partner.steamgames.com/doc/sdk).

See [steam-deploy.md](docs/steam-deploy.md) for more details on Steam builds.

## Notes on quick local testing

After `dotnet build`, simply run one of the following:

```bash
  cd DedicatedServer/bin/x64/Debug/netcoreapp6.0
  cd AssaultWing/bin/x64/Debug/netcoreapp6.0
```

And then run the `AssaultWing` or the `DedicatedServer` executable in that
folder. Note that on other platforms than Windows you will have to copy
`Content` and `CoreContent` folders to these folders manually.

With the following the game can be made to quickly connect to a local
`DedicatedServer` process running in another terminal. This provides a quick way
to test the game in the full network mode. Multiple client windows can be
connected to the same server locally.

When Steam is not present:

    AssaultWing.exe --quickstart --server_name local --server raw:127.0.0.1:16727:16727

When testing with Steam your Steam account must be added to the the Steam partner account for Assault Wing. Additionally Steam Client must be running.

With the Steam networking engine using direct connection:

    AssaultWing.exe --quickstart --server_name local --server direct:127.0.0.1:16727

With Steam networking engine using Steam Relay:

    AssaultWing.exe --quickstart --server_name local --server ip:127.0.0.1:16727

## Developer Documentation

- [The TODO list](docs/TODO.md)
- [Notes about deploying to Steam](docs/steam-deploy.md)
- [The Assault Wing network protocol](docs/network-protocol.md)
