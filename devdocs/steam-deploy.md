# The Steam deployment of Assault Wing

The version number comes from tag. To tag a version do: ```
git tag v1.27.0.0
```

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

# On Linux perform a clean debug build uploaded to Steam for both apps and all platforms
./scripts/steam_build.sh linux all all debug

# On Mac perform an incremenetal debug build for both apps and all platforms, but stop before uploading to steam
./scripts/steam_build.sh mac all all debug skip_clean_and_steam

# On Linux do a clean debug build of the dedicated server for Linux uploaded to Steam
./scripts/build.sh linux linux dedicated_server debug
```

Finally these are the "official" build commands:
```bash
# On Windows (from WSL2 environment) perform a clean steam debug build for both apps and all platforms
./scripts/steam_build.sh windows_wsl2 all all debug

# On Windows (from WSL2 environment) perform a clean steam release build for both apps and all platforms
./scripts/steam_build.sh windows_wsl2 all all release
```

## About Release and Debug builds and Steam Depots

The Debug and Release builds use _different_ steam depot IDs. (The content depot is always the same.)
The release binary depots are 1971372, 1971373, 1971374 for Windows, Linux and Mac
respectively. The debug binary depots are similarly 1971375, 1971376 and 1971377.
Dedicated server has an identical setup.

There are few things to note:
  - Unfortunately the steam build script does not build both release and debug builds now at the same time.
  - If building first debug and then release they appear as separate builds in the steam admin.
  - They can be merged in steam admin by first setting eg. debug to default branch and then release.
  - *However*, the _packages_ in steam can be configured to refer either debug or release _depots_ independently.
    - Care must be taken that:
      - Either both release and debug are both built and then merged in admin.
      - _Or_ all packages must be checked that they refer to the release or debug depots depending on which has been built for the version we want to have live.
  - This setup is a bit complicated, but on the other hand it allows for having a slightly different version of the
    app live for say just one platform

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

### Logging in to steam command line builder client

This is not necessary, but may help at least to verify that login is possible.

```
./steambuild/builder/steamcmd.exe
Redirecting stderr to 'C:\data\limppu_winworkspace\assaultwing\steambuild\builder\logs\stderr.txt'
...

Steam>login awbuilder PASSWORDHERE
```