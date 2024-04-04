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
  public class AppVPilotManager
  {

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
    private readonly System.Timers.Timer updateTimer;
    private readonly Logger logger;
    private readonly Mixer mixer;

    #endregion Private Fields

    #region Public Properties

    public StateViewModel State { get; } = new StateViewModel();

    #endregion Public Properties

    #region Public Constructors

    public AppVPilotManager()
    {
      this.logger = Logger.Create(this, nameof(AppVPilotManager));

      this.connectionTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = 5000,
        Enabled = false
      };
      this.connectionTimer.Elapsed += ConnectionTimer_Elapsed;

      this.updateTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = 250,
        Enabled = false
      };
      this.updateTimer.Elapsed += UpdateTimer_Elapsed;

      this.mixer = new();
    }

    private void UpdateTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      try
      {
        Volume volume = this.mixer.GetVolume(this.State.VPilotProcess!.Id);
        this.State.Volume = volume;
      }
      catch (Exception ex)
      {
        this.updateTimer.Enabled = false;
        this.State.VPilotProcess = null;
        this.State.IsConnected = false;
        this.connectionTimer.Enabled = true;
        this.logger.Log(LogLevel.WARNING, "Error reading volume of vPilot process, disconnected");
        this.logger.Log(LogLevel.WARNING, "Error info: " + ex.Message);
        this.logger.Log(LogLevel.INFO, "Reconnecting after a while.");
      }
    }

    #endregion Public Constructors

    #region Public Methods

    public Action<Volume> GetVolumeUpdateCallback() => (q => this.UpdateVolume(q));

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
        .TapEach(q => this.logger.Log(LogLevel.VERBOSE, $"Found process {q.ProcessName}"))
        .FirstOrDefault(q => q.ProcessName == VPILOT_PROCESS_NAME);
      if (tmp is not null)
      {
        this.State.VPilotProcess = tmp;
        this.State.IsConnected = true;
        this.connectionTimer.Enabled = false;
        this.logger.Log(LogLevel.INFO, "VPilot found, connected");
        this.updateTimer.Enabled = true;
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

    private void UpdateVolume(Volume value)
    {
      throw new NotImplementedException();
    }

    #endregion Private Methods
  }
}
