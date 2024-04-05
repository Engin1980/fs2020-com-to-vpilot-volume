using ELogging;
using Eng.WinCoreAudioApiLib;
using ESystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
  public class AppVPilot
  {
    public record Settings(int ConnectionTimerInterval, int ReadVolumeTimerInterval, double VolumeMultiplier);

    #region Public Classes

    public class StateViewModel : NotifyPropertyChangedBase
    {

      #region Public Properties

      public bool IsConnected
      {
        get => base.GetProperty<bool>(nameof(IsConnected));
        set => base.UpdateProperty(nameof(IsConnected), value);
      }

      public Volume Volume
      {
        get => base.GetProperty<Volume>(nameof(Volume));
        set => base.UpdateProperty(nameof(Volume), value);
      }


      public Process? VPilotProcess
      {
        get => base.GetProperty<Process?>(nameof(VPilotProcess));
        set => base.UpdateProperty(nameof(VPilotProcess), value);
      }

      #endregion Public Properties

      #region Public Constructors

      public StateViewModel()
      {
        this.Volume = 0;
        this.IsConnected = false;
      }

      #endregion Public Constructors

    }

    #endregion Public Classes

    #region Private Fields

    private const string VPILOT_PROCESS_NAME = "vPilot";

    private readonly System.Timers.Timer connectionTimer;
    private readonly Logger logger;
    private readonly Mixer mixer;
    private readonly System.Timers.Timer readVolumeTimer;
    private readonly double volumeMultiplier;

    #endregion Private Fields

    #region Public Properties

    public StateViewModel State { get; } = new StateViewModel();

    #endregion Public Properties

    #region Public Constructors

    public AppVPilot(Settings settings)
    {
      this.logger = Logger.Create(this, nameof(AppVPilot));

      this.volumeMultiplier = settings.VolumeMultiplier;

      this.connectionTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = settings.ConnectionTimerInterval,
        Enabled = false
      };
      this.connectionTimer.Elapsed += ConnectionTimer_Elapsed;

      this.readVolumeTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = settings.ReadVolumeTimerInterval,
        Enabled = false
      };
      this.readVolumeTimer.Elapsed += ReadVolumeTimer_Elapsed;

      this.mixer = new();
    }

    #endregion Public Constructors

    #region Public Methods

    public Action<Volume> GetVolumeUpdateCallback() => (q => this.SetVolume(q));

    public void SetVolume(Volume volume)
    {
      Volume multipliedVolume = volume * this.volumeMultiplier;
      this.logger.Log(LogLevel.INFO, $"SetVolume requested with value {volume} mutliplied to {multipliedVolume}.");
      try
      {
        this.mixer.SetVolume(this.State.VPilotProcess!.Id, multipliedVolume);
      }
      catch (Exception ex)
      {
        this.readVolumeTimer.Enabled = false;
        this.State.VPilotProcess = null;
        this.State.IsConnected = false;
        this.connectionTimer.Enabled = true;
        this.logger.Log(LogLevel.ERROR, "Error setting volume of vPilot process, disconnected");
        this.logger.Log(LogLevel.ERROR, "Error info: " + ex.Message);
        this.logger.Log(LogLevel.INFO, "Reconnecting after a while.");
      }
    }

    public void Start()
    {
      StartIfNotConnected();
    }

    #endregion Public Methods

    #region Private Methods

    private void ConnectionTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      this.logger.Log(LogLevel.INFO, "Reconnecting...");
      var tmp = this.mixer.GetProcessIds()
        .Select(q => Process.GetProcessById(q))
        .TapEach(q => this.logger.Log(LogLevel.DEBUG, $"Found process {q.ProcessName}"))
        .FirstOrDefault(q => q.ProcessName == VPILOT_PROCESS_NAME);
      if (tmp is not null)
      {
        this.State.VPilotProcess = tmp;
        this.State.IsConnected = true;
        this.connectionTimer.Enabled = false;
        this.logger.Log(LogLevel.INFO, "VPilot found, connected");
        this.readVolumeTimer.Enabled = true;
      }
      else
      {
        this.logger.Log(LogLevel.INFO, "Connection failed, will retry after a while...");
      }
    }

    private void StartIfNotConnected()
    {
      if (connectionTimer.Enabled) return;
      connectionTimer.Enabled = true;
    }

    private void ReadVolumeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      try
      {
        Volume volume = this.mixer.GetVolume(this.State.VPilotProcess!.Id);
        this.State.Volume = volume;
      }
      catch (Exception ex)
      {
        this.readVolumeTimer.Enabled = false;
        this.State.VPilotProcess = null;
        this.State.IsConnected = false;
        this.connectionTimer.Enabled = true;
        this.logger.Log(LogLevel.WARNING, "Error reading volume of vPilot process, disconnected");
        this.logger.Log(LogLevel.WARNING, "Error info: " + ex.Message);
        this.logger.Log(LogLevel.INFO, "Reconnecting after a while.");
      }
    }

    #endregion Private Methods
  }
}
