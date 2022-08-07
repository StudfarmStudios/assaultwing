# The Steam deployment of Assault Wing

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
