# Assault Wing - Galactic Battlefront

[assaultwing.com](https://assaultwing.com) / [GitHub](https://github.com/StudfarmStudios/assaultwing)

© Studfarm Studios

_Assault Wing is a fast-paced physics-based shooter for many players over the
internet._

## Information

- [Quick instructions](docs/instructions.md)
- [Gameplay and scoring introduction](docs/gameplay.md)
- [Credits](docs/credits.md)

## Dependencies

Assault Wing uses [MonoGame](https://www.monogame.net/) which is an open source
implementation of the Microsoft XNA 4 Framework. Assault Wing is currently using
the OpenGL backend of the MonoGame on all platforms. Assault Wing builds and
runs at least on Windows, Linux and Mac.

To compile, use the .NET 7 or later (7 not an LTS version, but at the time of
this writing it has the support deadline beyond the current LTS version). Other
dependencies are fetched from NuGet, except the optional Steamworks SDK.

As an IDE either a recent Visual Studio or Visual Studio Code can be used.

Note that if you previously had Dotnet tool MGCB installed globally, you
may have to remove the global install:

    dotnet tool uninstall -g dotnet-mgcb


## Building

CI Build Status: ![CI status badge](https://github.com/StudfarmStudios/assaultwing/actions/workflows/dotnet.yml/badge.svg "GitHub Actions build status")

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

See [steam-deploy.md](devdocs/steam-deploy.md) for more details on Steam builds.

## Notes on quick local testing

You can build with the usual `dotnet build`, but building with the
following command allows connecting multiple clients from same machine
(remember to enable windowed mode):

    dotnet build -p:DefineConstants="ALLOW_MULTIPLE_CLIENTS_PER_HOST;DEBUG=true"

Then simply run one of the following:

```bash
  cd DedicatedServer/bin/x64/Debug/netcoreapp7.0
  cd AssaultWing/bin/x64/Debug/netcoreapp7.0
```

And then run the `AssaultWing` or the `DedicatedServer` executable in that
folder. Note that on other platforms than Windows you will have to copy
`Content` and `CoreContent` folders to these folders manually (or in some cases
using the script mentioned below).

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

## Local Steam Testing

When debugging in an IDE or similar setups, some files need to be copied to the
default build folders. To help with this there is a script:

    ./scripts/copy_steam_files_to_ide_build.sh mac

(NOTE: replace `mac` with your current platform)

(NOTE: at the moment the copying of content files has a chance of working on Mac
only, but the SDK files should be copied.)

## Developer Documentation

- [The TODO list](devdocs/TODO.md)
- [Notes about deploying to Steam](devdocs/steam-deploy.md)
- [The Assault Wing network protocol](devdocs/network-protocol.md)
- [The Assault Wing Web Site](devdocs/web-site.md)