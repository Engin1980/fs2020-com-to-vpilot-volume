# FS2020 COM Volume to vPilot Volume

![Application Image](Wiki/Imgs/App.jpgx)

### What?

A simple tool synchronizing in-plane COM radio volume with vPilot output volume.

### Why?

VPilot offers no possibility to directly adjust output volume when ATC or other pilot is speaking over VATSIM - only via Settings panel. 

### How?

Fortunately, Windows OS is able to adjust output volume per application. This app gets the % of COM volume from FS2020 and sets it as % of volume of VPilot in Windows Audio Mixing Panel.

## Instalation

Simple unzip the release package to your custom folder. [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) is required to run the application (commonly installed on Windows OS by default).

## Usage

For a simple usage, just start the application using `Com2vPilotVolume.exe`. 

The app starts looking for a running instance of FS2020. Once found, it connects and reads out information about transmiting COM device and its volume.
At the same time, the apps starts looking for a running instance of VPilot. If found, it automatically adjusts VPilot volume w.r.t. the FS2020 data.

## More detalied info & adjustments

### Connecting to FS2020 + custom L-vars

App connects via SimConnect to the simvars specified in the configuration file `appsettings.json`. By default, those simvars are `COM VOLUME:{index}` (COM volume) and `COM TRANSMIT:{index}` (transmiting COM). Index is one of `1,2,3`. 

However, sometimes it may be useful to build custom volume and transmit management. Therefore, in configuration file, variables can be changed to custom L-vars, e.g.,: `L:COM_VOL:{index}` and `L:COM_VOL:{index}`.

**Note:** In configuration file, only prefixes are defined. To every prefix, the postfix with the number of COM device is automatically added when connecting to SimConnect.

### Connecting to Windows Mixing Device to adjust VPilot volume

App uses WIN API (Windows Audio Core API, specifically) to read and write values of volume for the specific application. The app recognizes "vPilot" applicaton via its "vPilot" name in thread name. If (in future) the name of VPilot changes, the app will not be able to connect to the vPilot and the update will be required.

The application **does not** affect, interact or access vPilot itself in any way.

**Volume Multiplicating Transformation**

As seen by tests, the real output volume changes very slightly for the big range of vPilot mixing volume - for ranges 30%-100% the output is almost the same. For lower values, the output become more quiet, and for 0%, the output is muted. Threfore the mapping can be adjusted by multiplier - see `appsettings.json`. Multiplication factor defines, how the final value is adjusted before set to vPilot mixer volume. Example: Multiplicator value `0.25` causes the 100% FS2020 volume be transformed to 25% vPilot volume. You can adjust the multiplier at your wish, to "disable" the behavior set the multiplier to `1`.

## FAQ

None yet

## Credits

Created by Marek Vajgl.

Report issues via `Issues` tab at the project GitHub pages.
