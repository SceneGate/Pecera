# SceneGate.PC

File format support for the PC platform.

## Build

Requirements:

- .NET Core 3.1 SDK

Steps:

```sh
# Run these command only the first time to get the build tools
dotnet tool restore
dotnet cake --bootstrap

# Build, test and stage artifacts
dotnet cake

# Just for building and testing
dotnet cake --target=BuildTest
```
