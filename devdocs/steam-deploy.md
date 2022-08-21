# The Steam deployment of Assault Wing

There are 2 scripts to work with Steam builds:
- The `scripts/build.sh` sets up builds for local development of the Steam features.
- The `scripts/steam_build.sh` builds and uploads official builds to Steam.

The `scripts/build.sh` basically just copies some necessary files for Steam testing to
the target folders of development builds.

NOTE: Currently the build scripts on Windows expect to be run in the WSL2 environment.

NOTE: Run the scripts without parameters to get further help on the possible options

Here are some examples of using the scripts:

```bash
# Incremental debug build on windows
./scripts/build.sh windows_wsl2 debug

# Clean debug build on Linux
./scripts/build.sh linux debug clean

# Incremental debug build on Linux
./scripts/build.sh linux debug

# On Windows perform a clean debug Windows only build of AssaultWing and stop before uploading to steam
./scripts/steam_build.sh windows_wsl2 windows assault_wing debug skip_steam

# On Windows perform a clean debug build for both apps and all platforms
./scripts/steam_build.sh windows_wsl2 all all debug

# On Linux perform a clean debug build uploaded to Steam for both apps and all platforms
./scripts/steam_build.sh linux all all debug

# On Mac perform an incremenetal debug build for both apps and all platforms, but stop before uploading to steam
./scripts/steam_build.sh mac all all debug skip_clean_and_steam

# On Linux do a clean debug build of the dedicated server for Linux uploaded to Steam
./scripts/build.sh linux linux dedicated_server debug
```

## About the Steamworks SDK

The Steamworks SDK is only needed when developing Steam features or uploading a
build to the Steam. In this case the SDK needs to be manually downloaded from
[partner.steamgames.com](https://partner.steamgames.com/doc/sdk).

Generally we have just followed the [Steam partner
documentation](https://partner.steamgames.com/doc/sdk/uploading).

Download the latest Steamworks SDK ZIP and extract it to a suitable location.
(As of this writing the latest version of the SDK is 154):

## About the Steam credentials

To upload to steam `AW_STEAM_BUILD_ACCOUNT` and `AW_STEAM_BUILD_PASSWORD` must be set.

## The environment variable `STEAMWORKS_SDK_PATH`

Then the environment variable `STEAMWORKS_SDK_PATH` must point to the unzipped
SDK. The `scripts/builds.sh` takes care of copying certain files from the SDK.

Run something like the following to get the Steam API library, `steam_appid.txt`
and the `steambuild` folder to correct places for Steam development.
### Setting builds to default branch

Once all platform builds are done, they must be set to default branch
for the [main app builds page](https://partner.steamgames.com/apps/builds/1971370)
and the [dedicated server builds page](https://partner.steamgames.com/apps/builds/2103880).

### Depots and Steam App IDs

The steam App IDs are:
- Assault Wing 1971370
- Assault Wing Dedicated Server 2103880

The binaries are in separate depots depending on the platform and whether it is
a debug build or not. The content depot is shared between everything.

The logic is encoded in `scripts/build.sh`.
p
The depot id of the binaries is `APP_ID` +
- 1 is content (can only be built on windows)
- 2 is windows release
- 3 is linux release
- 4 is mac release
- 5 is windows debug
- 6 is linux debug
- 7 is mac debug
