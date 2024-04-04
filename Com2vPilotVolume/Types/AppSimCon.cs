using ESimConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace eng.com2vPilotVolume.Types
{
  public class AppSimCon
  {

    #region Public Classes

    public class StateViewModel : NotifyPropertyChangedBase
    {

      #region Public Properties

      public int ActiveComIndex
      {
        get => base.GetProperty<int>(nameof(ActiveComIndex))!;
        set => base.UpdateProperty(nameof(ActiveComIndex), value);
      }

      public bool IsConnected
      {
        get => base.GetProperty<bool>(nameof(IsConnected))!;
        set => base.UpdateProperty(nameof(IsConnected), value);
      }
      public Volume Volume
      {
        get => base.GetProperty<Volume>(nameof(Volume))!;
        set => base.UpdateProperty(nameof(Volume), value);
      }

      #endregion Public Properties

      #region Public Constructors

      public StateViewModel()
      {
        this.ActiveComIndex = -1;
        this.Volume = 0;
        this.IsConnected = false;
      }

      #endregion Public Constructors

    }

    #endregion Public Classes

    #region Private Fields

    private const int INT_EMPTY = -1;
    private readonly System.Timers.Timer connectionTimer;
    private readonly ESimConnect.ESimConnect eSimCon;
    private readonly ELogging.Logger logger;
    private int comVolumeTypeId = INT_EMPTY;
    private int comVolumeRequestId = INT_EMPTY;
    private int com1ReceiveEventId = INT_EMPTY;
    private int com2ReceiveEventId = INT_EMPTY;
    private int com3ReceiveEventId = INT_EMPTY;

    #endregion Private Fields

    #region Public Properties

    public StateViewModel State { get; } = new();
    public Action<Volume>? VolumeUpdateCallback { get; set; }

    #endregion Public Properties

    #region Public Constructors

    public AppSimCon()
    {
      this.logger = ELogging.Logger.Create(this, nameof(AppSimCon));
      this.eSimCon = new();

      this.connectionTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = 5000,
        Enabled = false
      };
      this.connectionTimer.Elapsed += ConnectionTimer_Elapsed;
    }

    #endregion Public Constructors

    #region Public Methods

    public void Start()
    {
      StartIfNotConnected();
    }

    #endregion Public Methods

    #region Private Methods

    private void ConnectionTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      this.logger.Log(ELogging.LogLevel.INFO, "Reconnecting...");

      try
      {
        this.eSimCon.Open();

        // on success:
        try
        {
          RegisterToSimCon();
          this.connectionTimer.Enabled = false;
          this.logger.Log(ELogging.LogLevel.INFO, "Connection enabled.");
          this.State.IsConnected = true;
        }
        catch (Exception ex)
        {
          this.logger.Log(ELogging.LogLevel.ERROR, "Fatal error registering simcon variables: " + ex.Message);
          throw new ApplicationException("Failed to register simcon definitions.", ex);
        }
      }
      catch (Exception ex)
      {
        // intentionally blank
        this.logger.Log(ELogging.LogLevel.INFO, "Connection failed, will retry after a while...");
      }
    }

    private void ESimCon_DataReceived(ESimConnect.ESimConnect sender, ESimConnect.ESimConnect.ESimConnectDataReceivedEventArgs e)
    {
      if (e.RequestId != this.comVolumeRequestId) return;

      double volumeDouble = (double)e.Data;
      Volume volume = volumeDouble;

      this.VolumeUpdateCallback?.Invoke(volume);
    }

    private void RegisterToSimCon()
    {
      this.eSimCon.DataReceived += ESimCon_DataReceived;
      int typeId = this.eSimCon.RegisterPrimitive<double>(SimVars.Aircraft.RadioAndNavigation.COM_VOLUME);
      this.eSimCon.RequestPrimitiveRepeatedly(typeId, out int requestId, Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME, true);
      this.comVolumeTypeId = typeId;
      this.comVolumeRequestId = requestId;

      this.eSimCon.EventInvoked += ESimCon_EventInvoked;
      this.com1ReceiveEventId = this.eSimCon.RegisterSystemEvent("COM1_RECEIVE_SELECT");
      this.com2ReceiveEventId = this.eSimCon.RegisterSystemEvent("COM2_RECEIVE_SELECT");
      this.com3ReceiveEventId = this.eSimCon.RegisterSystemEvent("COM3_RECEIVE_SELECT");

    }

    private void ESimCon_EventInvoked(ESimConnect.ESimConnect sender, ESimConnect.ESimConnect.ESimConnectEventInvokedEventArgs e)
    {
      if (e.RequestId == this.com1ReceiveEventId)
        this.State.ActiveComIndex = 1;
      else if (e.RequestId == this.com2ReceiveEventId)
        this.State.ActiveComIndex = 2;
      else if (e.RequestId == this.com3ReceiveEventId)
        this.State.ActiveComIndex = 3;
    }

    private void StartIfNotConnected()
    {
      if (connectionTimer.Enabled) return;
      connectionTimer.Enabled = true;
    }

    #endregion Private Methods
  }
}
