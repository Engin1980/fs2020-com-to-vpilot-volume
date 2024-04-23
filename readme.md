# FS2020 COM Volume to vPilot Volume

![Application Image](Wiki/Imgs/App.jpg)

### What?

A simple tool synchronizing in-plane COM radio volume with vPilot output volume.

### Why?

VPilot offers no possibility to directly adjust output volume when ATC or another pilot is
speaking over VATSIM - only via the Settings panel.

### How?

Fortunately, Windows OS can adjust output volume per application.
This app gets the %&nbsp;of COM volume from FS2020 and sets it as %&nbsp;of
volume of VPilot in Windows Audio Mixing Panel.

## Instalation

Simply unzip the release package to your custom folder.
[.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) is required to run the application (commonly installed on Windows
OS by default).

## Usage

For simple usage, just start the application using `Com2vPilotVolume.exe`.

The app automatically looks for and connects to FS2020 and also looks for vPilot
running instance. If both were found, the app synchronizes FS2020 COM volume to
vPilot volume via Windows Audio Mixing panel.

## Principle in more detail

### FS2020 Connection

In the plane, there are up to 3 communication radios - COM1, COM2 and COM3. A pilot can
receive all of them at once, but transmit only via one selected. The information
about the selected transmit radio and output volume of all three radios is read out via
SimConnect library and appropriate variables (see below if more detail is needed).

### vPilot Volume Adjustment

This app does not access vPilot directly. Instead, it uses Windows OS functionality called
*Volume Mixer* panel, where an OS user can adjust output volume per application. To adjust
vPilot volume, this app accesses *Volume Mixer* functionality via Windows API (Windows
Audio Core API), looks for the vPilot and then adjusts its mixed output volume.

vPilot also uses only COM1 and COM2 capabilities.

### Synchronization principle

vPilot offers in *Volume Mixer* only one audio stream volume for both COM1/2 receiving.
Concerning that, **it is not possible to adjust output volume per COM1/2 radio
separately**, what might be useful for example for listening to ATIS at low volume on COM2
when receiving COM1 as a main channel.

Therefore, the app looks for the active transmit radio preset in FS2020. Once transmit radio
is changed, or the volume of the transmit radio is changed, the output volume of vPilot is
synchronized.


## More detailed info & adjustments

All the configuration of the app is available in the `appsettings.json` file located in the
application folder. Please backup this file before making any changes.

### Connection interval to FS2020 and vPilot

For both tasks - connection to the FS2020 and checking if vPilot is running - apply
the same rules. The app tries to connect repeatedly in the preset interval.
Once connected, if something fails (FS2020 is exited or vPilot is exited), the app
returns to the unconnected state and starts the connection process again.

The interval (in milliseconds) for both can be adjusted via:

* `AppSimCon.ConnectionTimerInterval` for FS2020 connection, or
* `AppVPilot.ConnectionTimerInterval` for vPilot.

Values must be equal to or greater than 1000 ms (1 second).

### Number of handled COM radios

FS2020 supports up to 3 COM radios (COM1/2/3). vPilot supports 2 COM radios (COM1/2).
The app can by default handle any number of radios, but the default value is 3.
The number of handled radios can be set via `AppSimCon.NumberOfComs`.

### Adjusting FS2020 connection and read-out

The app connects to FS2020 via the SimConnect library and reads out values for COM volume and
transmit flag. If not changed, default SimVars to read out COM volume and transmit flag are used (see below).

Those simvars are predefined and used by all default FS2020 airplanes and most of the
add-ons. However, in some cases, you might need to adjust those simvars, e.g.:
* You would like to keep the original behavior as lowering the in-game COM radio volume
  to 0 causes FS2020 AI ATC is not heard. When connected via this app, lowering the COM
  radio volume to 0 causes also the vPilot volume to be muted.
* Some planes/addons use different simvars to control the volume/transmit COM and you
  would like to monitor them instead.

**Monitored SimVars**

As mentioned, the app is monitoring several FS2020 SimVars to provide its functionality:
* COM radio volume 1-3 - to read out the current volume of the COM radio in the simulation. By default, this property is represented by `COM VOLUME:{i}`.
* Active COM transmiting radio 1-3 - to read out the currently selected COM radio used for transmission. By default, this property is represented by `COM TRANSMIT:{i}`.
* COM radio active frequency 1-3 - to read out the current active frequency for each COM radio. This property is represented by `COM ACTIVE FREQUENCY:{i}` (cannot be changed). 

In the config file, you can change the monitored FS2020 vars from the default ones to different ones via keys `AppSimCon.ComVolumeVar` for volume or `AppSimCon.ComTransmitVar`
for transmit. For example, you can adjust the values to use so-called L-vars like:
```
{
  "AppSimCon": {
    ...
    "ComVolumeVar": "L:COM_VOLUME:{i}",
    "ComTransmitVar": "L:COM_TRANSMIT:{i}",
    ...
...
```

**Note**: Sequence `{i}` is always replaced with the respective COM number.

**Note**: Be sure you are not overriding some L-var used by another addon as this may cause
unexpected behavior in the sim.

When set in this way, you may now change `L:COM_VOLUME:1` variable to control the COM1
volume. In the volume variable, expected values are decimals between 0..1 defining volume %.
In the transmit variable, expected values are decimal values 0 (not active) or 1 (active).

Once done, you can map your controller to work with those values (e.g. via MobiFlight)
to control transmit and volume values.

Additionally:
* By default, plane latitude location is used for validation if FS2020 has loaded already and is ready to initialize SimVars. This value is checked for non-empty and non-zero value. Once detected, the SimVar initialization is executed (see below). The default "plane latitude" SimVar can be changed to a different one with the same behavior using `InitializedCheckVar` key in config file.

**SimVars Init**

This part was created to initialize custom L-vars (defined in the previous step) on
the app startup. However, it can be used also to reset the default values if the original
FS2020 simvars are used.

Why - if you define custom L-vars in the previous step and those variables are not
defined/used by any addon, you have to set their initial values.

To set the variable values when started, use `InitComTransmit`, `InitComVolume` and `InitComFrequency` keys in config:

```
{
  "AppSimCon": {
    ...
    "InitComTransmit": [ 1, 0, -1 ],
    "InitComVolume": [ 0.9, -1, -1 ],
    "InitComFrequency: [122.8, -1, -1],
    ...
...  
```

Rules:
* The number of elements in the list must equal to `AppSimCon.NumberOfComs`.
* Value `-1` always means "do nothing, skip initialization and continue with the next item".
* For volume, float values 0..1 define the initial state. E.g., `0.5` initializes volume
  to 50&nbsp;%.
* For transmit, integer values 0/1 define the initial state: 0 - not active, 1 - active.
* For frequency, decimal values between 118.000 and 136.975 are accepted.

The example above initializes sets COM1 transmit to the active state, COM2 transmit to the inactive state, and skips COM3 transmit initialization. Also sets COM1 volume to 90&nbsp;% and frequency to 122.800 (VATSIM Unicom) and skips the other COM2/3 initializations.

**Note:** Default SimEvent `COM_RADIO_SET_HZ` (or its equivalents for COM2/3) is used to adjust the COM frequency. This cannot be changed.

### vPilot volume read-out

The app shows the current volume set for vPilot in Volume Mixer. Unfortunately, there is
no API automatically informing that the vPilot's volume has changed in Volume Mixer, so
this value is read out automatically in the predefined interval. You can adjust the interval
via `AppVPilot.ReadVolumeTimerInterval`. The default value is 250ms, but you can
significantly increase the value to decrease CPU usage but increase the delay of value
update in the app. This does not affect how volume change is propagated to vPilot.

### Volume Multiplicating Transformation

As seen by tests, the real output volume of vPilot changes very slightly for
the range of the output values between 30&nbsp;% - 100&nbsp;%; here, the output is
almost the same. For lower values, the output becomes more quiet, and for 0%,
the output is muted.

Therefore the mapping can be adjusted by multiplier - see `appVPilot.VolumeMultiplier`.
The multiplication factor defines, how the final value is adjusted (multiplied) before
set to vPilot Volume Mixer. Example: Multiplicator value `0.25` causes the 100&nbsp;%
FS2020 COM volume be transformed to 25% vPilot volume. You can adjust the multiplier
at your wish, to "disable" the behavior set the multiplier to `1`.

### Sounds

The app is able to play sounds at the specific occasions:
* when the volume of the active COM radio is maxed,
* when the active COM radio is muted,
* when the active COM radio has changed,
* when the active frequency of the active COM radio has changed.

Every sound can be adjusted in the relevant section of the configuration file:

```
  "Sounds": {
    "MaxVolumeFile": ".\\sounds\\high.mp3",
    "MaxVolumeFileVolume": 0.5,
    "MinVolumeFile": ".\\sounds\\low.mp3",
    "MinVolumeFileVolume": 0.5,
    "FrequencyChangedFile": ".\\sounds\\freqSwitch.mp3",
    "FrequencyChangedFileVolume": 0.5,
    "ComChangedFile": ".\\sounds\\comSwitch.mp3",
    "ComChangedFileVolume": 0.5
  }
```

For every file holds:
* if the file is empty (value is set to the `null`), the sound will be skipped and not used
* you can adjust the volume of the sound using the `...Volume` key of the respective sound file. The supported values are 0..1.

### Testing buttons

The app can show buttons to set the volume to vPilot directly. They are for testing
purposes. To adjust the visibility of those buttons, set `MainWindow.ShowSimpleAdjustButtons`
to `true` or `false`.

### Logging

For run-time evaluation, logging is introduced into the application. Probably, you should be interested in this section only if you are trying to resolve some issue.

`MainWindowOut` section defines logging written in the Main App Window. You can adjust the level of the logging w.r.t. log rules defined below.

To adjust logging into the file:
* `Logging:LogFile:Name` defines the file-name, in which the log will be written. If no path specified, the working directory is used. If this value is empty or invalid, default value `_log.txt` will be used.
* `Logging:LogFile:Reset` defines the the log file is erased at every app startup. If invalid, `true` value is used. Note - `false` value may lead to **huge** log file size!
* `Logging:LogFile:Level` defines the lowest logged level into the file. If invalid, `debug` value is used.

To define required log levels, you may use following strings: `debug`/`info`/`warning`/`error`/`critical`.

## FAQ

Q: Application does not start (or quits immediatelly) without any notice.

A: Check the log file. Mostly, the cause is invalid `appsettings.json` log file.

---


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

Report issues via the [`Issues`](https://github.com/Engin1980/fs2020-com-to-vpilot-volume/issues) tab on the project GitHub pages.
