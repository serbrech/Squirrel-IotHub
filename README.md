# Squirrel IoT Hub Updates

This is a sample app that demonstrate how to auto-update .net software runnin on IoT devices using Squirrel and IoT Hub.

## Build

For simplicity, I hooked the nuget package creation and the squirrel release command in the console application AfterBuild target.

## Run

1. Change the `releaselocation` setting in the app.config
2. Build the solution. squirrel will create a Releases folder in the console project.
3. run Setup.exe from the release folder. the console application will monitor your `releaselocation` for updates
4. update the application version in the AssemblyInfo.cs file
5. Rebuild. squirrel update the Releases/RELEASES file and drops the new nuget packages.
6. Drop the delta package and the RELEASES file at the `releaselocation`.
7. Watch the console update and restart with the new binaries


## TODO

 - Listen to a IoTHub message to trigger the update instead of periodically checking.