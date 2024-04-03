using Eng.WinCoreAudioApiLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
  public class AppVolumeManager
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

    private const string VPILOT_PROCESS_NAME = "vpilot.exe";

    private readonly System.Timers.Timer connectionTimer;
    private readonly Mixer mixer;

    #endregion Private Fields

    #region Public Properties

    public StateViewModel State { get; } = new StateViewModel();

    #endregion Public Properties

    #region Public Constructors

    public AppVolumeManager()
    {
      this.connectionTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = 5000,
        Enabled = false
      };
      this.connectionTimer.Elapsed += ConnectionTimer_Elapsed;

      this.mixer = new();
    }

    #endregion Public Constructors

    #region Public Methods

    public void Start()
    {
      StartIfNotConnected();
    }

    public Action<Volume> GetVolumeUpdateCallback() => (q => this.UpdateVolume(q));

    #endregion Public Methods

    #region Private Methods

    private void UpdateVolume(Volume value)
    {
      throw new NotImplementedException();
    }

    private void ConnectionTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      var tmp = this.mixer.GetProcessIds()
        .Select(q => Process.GetProcessById(q))
        .FirstOrDefault(q => q.ProcessName == VPILOT_PROCESS_NAME);
      if (tmp is not null)
      {
        this.State.VPilotProcess = tmp;
        this.State.IsConnected = true;
        this.connectionTimer.Enabled = false;
      }
    }
    private void StartIfNotConnected()
    {
      if (connectionTimer.Enabled) return;
      connectionTimer.Enabled = true;
    }

    #endregion Private Methods
  }
}
