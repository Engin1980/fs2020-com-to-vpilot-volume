using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
  public class AppSettings
  {
    public AppSimConConfig AppSimCon { get; set; } = new AppSimConConfig();
    public AppVPilotConfig AppVPilot { get; set; } = new AppVPilotConfig();
    public VolumeMappingConfig VolumeMapping { get; set; } = new VolumeMappingConfig();
    public SoundsConfig Sounds { get; set; } = new SoundsConfig();
    public MainWindowConfig MainWindow { get; set; } = new MainWindowConfig();
    public KeyboardMappingsConfig KeyboardMappings { get; set; } = new KeyboardMappingsConfig();
  }

  public class AppSimConConfig
  {
    public int NumberOfComs { get; set; }
    public int ConnectionTimerInterval { get; set; }
    public string InitializedCheckVar { get; set; } = string.Empty;
    public string ComVolumeVar { get; set; } = string.Empty;
    public string ComTransmitVar { get; set; } = string.Empty;
    public int[] InitComTransmit { get; set; } = Array.Empty<int>();
    public int[] InitComVolume { get; set; } = Array.Empty<int>();
    public double[] InitComFrequency { get; set; } = Array.Empty<double>();
  }

  public class AppVPilotConfig
  {
    public int ConnectionTimerInterval { get; set; }
    public int ReadVolumeTimerInterval { get; set; }
  }

  public class VolumeMappingConfig
  {
    // Map of volume mapping pairs, each inner array contains two integers [input, mapped]
    public double[][] Map { get; set; } = Array.Empty<double[]>();
    public double MinimumThreshold { get; set; }
  }

  public class SoundsConfig
  {
    public string MaxVolumeFile { get; set; } = string.Empty;
    public double MaxVolumeFileVolume { get; set; }
    public string MinVolumeFile { get; set; } = string.Empty;
    public double MinVolumeFileVolume { get; set; }
    public string FrequencyChangedFile { get; set; } = string.Empty;
    public double FrequencyChangedFileVolume { get; set; }
    public string ComChangedFile { get; set; } = string.Empty;
    public double ComChangedFileVolume { get; set; }
  }

  public class MainWindowConfig
  {
    public int[] StartupWindowSize { get; set; } = Array.Empty<int>();
  }

  public class KeyboardMappingEntry
  {
    public double? Set { get; set; }
    public double? Adjust { get; set; }
    public string Keys { get; set; } = string.Empty;
  }

  public class KeyboardMappingsConfig : List<KeyboardMappingEntry>
  {
  }
}
