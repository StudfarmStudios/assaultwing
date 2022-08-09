# Notes on `build.sh`

```
dotnet publish ./AssaultWing/AssaultWing.csproj --framework netcoreapp6.0 --runtime osx-x64 --output publish-output --self-contained 
```

## Notes about the "cross-compiling" version of the script

- No hard clean
- Clean once
- 2 build options
-   standard debug build
-   all platforms and both apps
-     only really works on windows that can build the content, but if old content depot is ok, other platforms work too
-     followed by steam upload of all at once
-        iterated generation of steam files
