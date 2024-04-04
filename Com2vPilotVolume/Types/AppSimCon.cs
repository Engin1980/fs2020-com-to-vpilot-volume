using ESimConnect;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static ESimConnect.SimUnits;

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
    private readonly int[] comVolumeRequestIds = new int[3] { INT_EMPTY, INT_EMPTY, INT_EMPTY };
    private readonly int[] comTransmitRequestIds = new int[3] { INT_EMPTY, INT_EMPTY, INT_EMPTY };
    private readonly bool[] comTransmit = new bool[3] { false,false,false};
    private readonly Volume[] comVolumes = new Volume[3] { 0, 0, 0 };

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
      this.logger.Log(ELogging.LogLevel.VERBOSE, $"Invoked data {e.RequestId}={e.Data}");
      for (int i = 0; i <= 2; i++)
      {
        if (e.RequestId == this.comVolumeRequestIds[i])
        {
          double volumeDouble = (double)e.Data;
          this.comVolumes[i] = volumeDouble;
          this.logger.Log(ELogging.LogLevel.INFO, $"COM{i + 1} volume changed to {this.comVolumes[i]}");
          if (this.comTransmit[i])
          {            
            this.State.Volume = this.comVolumes[i];
            this.VolumeUpdateCallback?.Invoke(this.comVolumes[i]);
          }
        }
        else if (e.RequestId == this.comTransmitRequestIds[i])
        {
          this.comTransmit[i] = ((double)e.Data) != 0;
          this.logger.Log(ELogging.LogLevel.INFO, $"COM{i + 1} transmit changed to {this.comTransmit[i]}");
          if (this.comTransmit[i])
          {
            this.State.ActiveComIndex = i + 1;
            this.State.Volume = this.comVolumes[i];
            this.VolumeUpdateCallback?.Invoke(this.comVolumes[i]);
          }
        }
      }
    }

    private void RegisterToSimCon()
    {
      this.eSimCon.DataReceived += ESimCon_DataReceived;

      for (int i = 0; i <= 2; i++)
      {
        string name = $"COM VOLUME:{i + 1}";
        int typeId = this.eSimCon.RegisterPrimitive<double>(name);
        this.eSimCon.RequestPrimitiveRepeatedly(typeId, out int requestId, Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME, true);
        this.comVolumeRequestIds[i] = requestId;
        this.logger.Log(ELogging.LogLevel.VERBOSE, $"COM {i + 1} VOLUME registered via {name} as request {requestId}");

        name = $"COM TRANSMIT:{i + 1}";
        typeId = this.eSimCon.RegisterPrimitive<double>(name);
        this.eSimCon.RequestPrimitiveRepeatedly(typeId, out requestId, Microsoft.FlightSimulator.SimConnect.SIMCONNECT_PERIOD.SIM_FRAME, true);
        this.comTransmitRequestIds[i] = requestId;
        this.logger.Log(ELogging.LogLevel.VERBOSE, $"COM {i + 1} TRANSMIT registered via {name} as request {requestId}");
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
