using Eng.Com2vPilotVolume.Types;
using Eng.WinCoreAudioApiLib;
using ESystem;
using ESystem.Logging;
using ESystem.Miscelaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Eng.Com2vPilotVolume.Services
{
  public class VPilotService : BaseService
  {
    public class StateViewModel : NotifyPropertyChanged
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

    private const string VPILOT_PROCESS_NAME = "vPilot";

    private readonly System.Timers.Timer connectionTimer;
    private readonly Mixer mixer;
    private readonly System.Timers.Timer readVolumeTimer;
    private readonly TaskCompletionSource<bool> stopTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool isStopping = false;

    public StateViewModel State { get; } = new();

    public VPilotService(AppVPilotConfig settings)
    {
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

    protected async override Task StartInternalAsync()
    {
      await Task.Run(() =>
      {
        this.SetVolume(new Volume(1));
        connectionTimer.Enabled = true;
      });
    }

    protected async override Task StopInternalAsync()
    {
      await Task.Run(async () =>
      {
        this.isStopping = true;
        await this.stopTcs.Task;
      });
    }

    public Action<Volume> GetVolumeUpdateCallback() => (q => this.SetVolume(q));

    public void SetVolume(Volume volume)
    {
      if (this.State.VPilotProcess == null)
      {
        this.logger.Log(LogLevel.WARNING, "SetVolume requested, but vPilot not connected. Value will not be set.");
        return;
      }
      this.logger.Log(LogLevel.INFO, $"SetVolume requested with value {volume}.");
      try
      {
        this.mixer.SetVolume(this.State.VPilotProcess!.Id, volume);
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

    private static Process? TryGetProcessById(int id)
    {
      try
      {
        var ret = Process.GetProcessById(id);
        return ret;
      }
      catch (Exception)
      {
        return null;
      }
    }

    private void ConnectionTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      if (this.isStopping)
      {
        ProcessStop();
        return;
      }

      this.logger.Log(LogLevel.INFO, "Reconnecting...");
      var tmp = this.mixer.GetProcessIds()
        .Select(q => TryGetProcessById(q))
        .Where(q => q != null)
        .TapEach(q => this.logger.Log(LogLevel.DEBUG, $"Found process {q?.ProcessName ?? "(null)"}"))
        .FirstOrDefault(q => q?.ProcessName == VPILOT_PROCESS_NAME);
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

    private void ProcessStop()
    {
      this.connectionTimer.Enabled = false;
      this.readVolumeTimer.Enabled = false;
      this.stopTcs.TrySetResult(true);
    }

    private void ReadVolumeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
      if (this.isStopping)
      {
        ProcessStop();
        return;
      }

      try
      {
        Volume volume = this.mixer.GetVolume(this.State.VPilotProcess!.Id);
        this.logger.Debug($"Read volume: {volume}");
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
  }
}
