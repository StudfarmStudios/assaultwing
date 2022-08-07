# Assault Wing - Galactic Battlefront

[assaultwing.com](assaultwing.com) / [GitHub](https://github.com/StudfarmStudios/assaultwing)

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

### Using the `build.sh` script

The `scripts/build.sh` is a more complex build script intended to provide
automation and improve repeatability when making the official builds.

Currently the Windows builds using the `build.sh` rely on the WSL2.

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
### Notes on Steam builds

The Steamworks SDK is only needed when developing Steam features or uploading a
build to the Steam. In this case the SDK needs to be manually downloaded from
[partner.steamgames.com](https://partner.steamgames.com/doc/sdk).

Generally we have just followed the [Steam partner
documentation](https://partner.steamgames.com/doc/sdk/uploading).

Download the latest Steamworks SDK ZIP and extract it to a suitable location.
(As of this writing the latest version of the SDK is 154):

Then the environment variable `STEAMWORKS_SDK_PATH` must point to the unzipped
SDK. The `scripts/builds.sh` takes care of copying certain files from the SDK.
Run something like the following to get the Steam API library, `steam_appid.txt`
and the `steambuild` folder to correct places for Steam development.

```bash
./scripts/build.sh MY_PLATFORM_HERE assault_wing debug skip_clean_and_steam
```

## Notes on quick local testing

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
