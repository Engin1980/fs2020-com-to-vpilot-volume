# FS2020 COM Volume to vPilot Volume

![Application Image](Wiki/Imgs/App.jpg)

### What?

A simple tool synchronizing in-plane COM radio volume with vPilot output volume.

### Why?

VPilot offers no possibility to directly adjust output volume when ATC or another pilot is
speaking over VATSIM - only via the Settings panel.

### How?

Fortunately, Windows OS can adjust output volume per application.
This app gets the %&nbsp;of COM volume from FS2020 and sets it as a percentage of
volume of VPilot in Windows Audio Mixing Panel.

## Instalation

App is provided as ZIP file.

The newest releases are available at the [Releases](https://github.com/Engin1980/fs2020-com-to-vpilot-volume/releases) page. Simply download and unzip the release package to your custom folder.

[.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) is required to run the application (commonly installed on Windows
OS by default).

## Usage

For simple usage, just start the application using `Com2vPilotVolume.exe`.

The app automatically looks for and connects to FS2020 and also looks for vPilot
running instance. If both were found, the app synchronizes FS2020 COM volume to
vPilot volume via Windows Audio Mixing panel.

## More Info

For more info about principle, implementation or configuration see [Project Wiki](https://github.com/Engin1980/fs2020-com-to-vpilot-volume/wiki).

## Version history

**v0.3.2 - 2024-04-23**
* Improved logging and configuration file.
* Added support for initial COM frequency tuning

**v0.3.0 - 2024-04-22**
* Added support for sound notifications

**v0.2.0 - 2024-04-21**
* Added postponed custom variable initialization when FS2020 is ready

**v0.1.1-beta - 2024-04-08**
* Initial release
* Can connect to FS2020
* Can use custom variables
* Multiplier added to adjust vPilot volume mapping

## Credits

Created by Marek Vajgl.

Project pages: https://github.com/Engin1980/fs2020-com-to-vpilot-volume

Report issues via the [Issues](https://github.com/Engin1980/fs2020-com-to-vpilot-volume/issues) tab on the project GitHub pages.
