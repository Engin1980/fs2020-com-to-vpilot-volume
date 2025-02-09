using ESimConnect;
using ESystem.Asserting;
using ESystem.ValidityChecking;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace eng.com2vPilotVolume.Types
{
  public class AppSimCon
  {
    public record Settings(int NumberOfComs, int ConnectionTimerInterval, string InitializedCheckVar,
      string ComVolumeVar, string ComTransmitVar,
      int[] InitComTransmit, double[] InitComVolume, double[] InitComFrequency);

    public enum EConnectionStatus
    {
      NotConnected,
      ConnectedNoData,
      ConnectedWithData
    }

    private class SimVar<T>
    {
      public SimVar(string name)
      {
        this.Name = name;
      }

      public string Name { get; set; }
      public string RegInfo => $"{Name} (tp:{TypeId}, req:{RequestId})";
      public RequestId RequestId { get; set; } = REQUEST_EMPTY;
      public TypeId TypeId { get; set; } = TYPE_EMPTY;
      public T? Value { get; set; }
    }

    #region Public Classes

    public class StateViewModel : NotifyPropertyChangedBase
    {

      #region Public Properties

      public double ActiveComFrequency
      {
        get => base.GetProperty<double>(nameof(ActiveComFrequency))!;
        set => base.UpdateProperty(nameof(ActiveComFrequency), value);
      }

      public int ActiveComIndex
      {
        get => base.GetProperty<int>(nameof(ActiveComIndex))!;
        set => base.UpdateProperty(nameof(ActiveComIndex), value);
      }

      public Volume ActiveComVolume
      {
        get => base.GetProperty<Volume>(nameof(ActiveComVolume))!;
        set => base.UpdateProperty(nameof(ActiveComVolume), value);
      }

      public EConnectionStatus ConnectionStatus
      {
        get => base.GetProperty<EConnectionStatus>(nameof(ConnectionStatus))!;
        set
        {
          base.UpdateProperty(nameof(ConnectionStatus), value);
          this.ConnectionStatusText = value == EConnectionStatus.NotConnected
            ? "No connection to FS2020"
            : value == EConnectionStatus.ConnectedNoData
            ? "Connected to FS2020, but not initialized"
            : "Connected to FS2020";
          this.IsConnected = value == EConnectionStatus.ConnectedWithData;
        }
      }

      public string ConnectionStatusText
      {
        get => base.GetProperty<string>(nameof(ConnectionStatusText))!;
        private set => base.UpdateProperty(nameof(ConnectionStatusText), value);
      }

      public bool IsConnected
      {
        get => base.GetProperty<bool>(nameof(IsConnected))!;
        private set => base.UpdateProperty(nameof(IsConnected), value);
      }
      #endregion Public Properties

      #region Public Constructors

      public StateViewModel()
      {
        this.ActiveComIndex = -1;
        this.ActiveComVolume = 0;
        this.ConnectionStatus = EConnectionStatus.NotConnected;
      }

      #endregion Public Constructors

    }

    #endregion Public Classes

    #region Private Fields

    private const string COM_FREQUENCY_VAR = "COM ACTIVE FREQUENCY:{i}";
    private static readonly RequestId REQUEST_EMPTY = new RequestId(-1);
    private static readonly TypeId TYPE_EMPTY = new TypeId(-1);
    private const double MAX_COM_FREQUENCY = 136.975;
    private const double MIN_COM_FREQUENCY = 118.000;
    private readonly SimVar<double>[] comFrequencies;
    private readonly SimVar<bool>[] comTransmits;
    private readonly SimVar<Volume>[] comVolumes;
    private readonly System.Timers.Timer connectionTimer;
    private readonly ESimConnect.ESimConnect eSimCon;
    private readonly ELogging.Logger logger;
    private readonly Settings settings;
    private RequestId latRequestId = REQUEST_EMPTY;
    private TypeId latTypeId = TYPE_EMPTY;
    #endregion Private Fields

    #region Public Properties

    public Action<int>? ActiveComChangedCallback { get; set; }
    public Action<double>? FrequencyChangedCallback { get; set; }
    public StateViewModel State { get; } = new();
    public Action<Volume>? VolumeUpdateCallback { get; set; }
    #endregion Public Properties

    #region Public Constructors

    public AppSimCon(AppSimCon.Settings settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      EAssert.Argument.IsTrue(settings.NumberOfComs >= 1);
      EAssert.Argument.IsTrue(settings.ConnectionTimerInterval > 500);

      this.settings = settings;

      this.comTransmits = new SimVar<bool>[settings.NumberOfComs];
      this.comVolumes = new SimVar<Volume>[settings.NumberOfComs];
      this.comFrequencies = new SimVar<double>[settings.NumberOfComs];
      for (int i = 0; i < settings.NumberOfComs; i++)
      {
        this.comVolumes[i] = new SimVar<Volume>(settings.ComVolumeVar.Replace("{i}", (i + 1).ToString()));
        this.comTransmits[i] = new SimVar<bool>(settings.ComTransmitVar.Replace("{i}", (i + 1).ToString()));
        this.comFrequencies[i] = new SimVar<double>(COM_FREQUENCY_VAR.Replace("{i}", (i + 1).ToString()));
      }

      this.logger = ELogging.Logger.Create(this, nameof(AppSimCon));
      this.eSimCon = new();
      this.eSimCon.Disconnected += ESimCon_Disconnected;

      this.connectionTimer = new System.Timers.Timer()
      {
        AutoReset = true,
        Interval = settings.ConnectionTimerInterval,
        Enabled = false
      };
      this.connectionTimer.Elapsed += ConnectionTimer_Elapsed;
    }

    private void ESimCon_Disconnected(ESimConnect.ESimConnect sender)
    {
      this.logger.Log(ELogging.LogLevel.WARNING, "FS2020 disconnected. Retrying connection...");
      this.State.ConnectionStatus = EConnectionStatus.NotConnected;
      this.connectionTimer.Enabled = true;
    }

    #endregion Public Constructors

    #region Public Methods

    public void Start()
    {
      StartIfNotConnected();
    }

    #endregion Public Methods

    #region Private Methods

    private static string GetComRadioSetHzNameForComIndex(int index) =>
      index switch
      {
        1 => "COM_RADIO_SET_HZ",
        2 => "COM2_RADIO_SET_HZ",
        3 => "COM3_RADIO_SET_HZ",
        _ => throw new NotImplementedException("Unexpected radio COM index " + index)
      };

    private void ConnectionTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
      switch (this.State.ConnectionStatus)
      {
        case EConnectionStatus.NotConnected:
          this.logger.Log(ELogging.LogLevel.INFO, "Reconnecting...");
          InitSimConConnection(); break;
        case EConnectionStatus.ConnectedNoData:
          this.logger.Log(ELogging.LogLevel.INFO, "Checking simvar status...");
          InitSimConCheckLatitude();
          break;
        default:
          this.logger.Log(ELogging.LogLevel.INFO, "Ignored connection status: " + this.State.ConnectionStatus);
          break;
      }
    }

    private void ESimCon_DataReceived(ESimConnect.ESimConnect sender, ESimConnect.ESimConnect.ESimConnectDataReceivedEventArgs e)
    {
      this.logger.Log(ELogging.LogLevel.INFO, $"Received data {e.RequestId}={e.Data}");
      if (this.latRequestId != REQUEST_EMPTY && this.latRequestId == e.RequestId)
        ProcessLatDataReceived(e);
      else if (this.comFrequencies.Any(q => q.RequestId == e.RequestId))
        ProcessFreqDataReceived(e);
      else if (this.comVolumes.Any(q => q.RequestId == e.RequestId))
        ProcessVolumeDataReceived(e);
      else if (this.comTransmits.Any(q => q.RequestId == e.RequestId))
        ProcessTransmitDataReceived(e);
      else
        this.logger.Log(ELogging.LogLevel.WARNING, $"Received data with unknown requestId={e.RequestId}.");
    }

    private void InitializeComTypesToSimCon()
    {
      // IMPORTANT NOTE ABOUT "... / 2" below
      // There is a bug in binding configuration to records causing array has double length
      // https://github.com/dotnet/runtime/issues/83803

      if (this.settings.InitComTransmit != null)
      {
        if (this.settings.InitComTransmit.Length / 2 > this.settings.NumberOfComs)
          this.logger.Log(ELogging.LogLevel.WARNING, $"Settings error: Init COM transmit vector is longer ({this.settings.InitComTransmit.Length / 2}) than number of COMs ({this.settings.NumberOfComs}). Initialization skipped.");
        else
          for (int i = 0; i < this.settings.InitComTransmit.Length / 2; i++)
          {
            int val = this.settings.InitComTransmit[i];
            if (val == -1) continue;
            if (val != 0 && val != 1)
            {
              this.logger.Log(ELogging.LogLevel.WARNING, $"Config says transmit of COM {i + 1} should be set to {val}, but valid values are only -1/0/1. Skipping.");
              continue;
            }
            this.logger.Log(ELogging.LogLevel.INFO, $"Initializing COM {i + 1} transmit to {val}");
            this.eSimCon.Values.Send(this.comTransmits[i].TypeId, val);
          }
      }

      if (this.settings.InitComVolume != null)
      {
        if (this.settings.InitComVolume.Length / 2 > this.settings.NumberOfComs)
          this.logger.Log(ELogging.LogLevel.WARNING, $"Settings error: Init COM volume vector is longer ({this.settings.InitComVolume.Length / 2}) than number of COMs ({this.settings.NumberOfComs}). Initialization skipped.");
        else
          for (int i = 0; i < this.settings.InitComVolume.Length / 2; i++)
          {
            double val = this.settings.InitComVolume[i];
            if (val == -1) continue;
            if (val < 0 || val > 1)
            {
              this.logger.Log(ELogging.LogLevel.WARNING, $"Config says volume of COM {i + 1} should be set to {val}, but valid values are only -1 or 0..1. Skipping.");
              continue;
            }
            this.logger.Log(ELogging.LogLevel.INFO, $"Initializing COM {i + 1} volume to {val}");
            this.eSimCon.Values.Send(this.comVolumes[i].TypeId, val);
          }
      }

      if (this.settings.InitComFrequency != null)
      {
        if (this.settings.InitComFrequency.Length / 2 > this.settings.NumberOfComs)
          this.logger.Log(ELogging.LogLevel.WARNING, $"Settings error: Init COM frequency vector is longer ({this.settings.InitComFrequency.Length / 2}) than number of COMs ({this.settings.NumberOfComs}). Initialization skipped.");
        else
          for (int i = 0; i < this.settings.InitComFrequency.Length / 2; i++)
          {
            double val = this.settings.InitComFrequency[i];
            if (val == -1) continue;
            if (val < MIN_COM_FREQUENCY || val > MAX_COM_FREQUENCY)
            {
              this.logger.Log(ELogging.LogLevel.WARNING, $"Config says frequency of COM {i + 1} should be set to {val}, but valid values are only -1 or {MIN_COM_FREQUENCY}..{MAX_COM_FREQUENCY}. Skipping.");
              continue;
            }
            this.logger.Log(ELogging.LogLevel.INFO, $"Initializing COM {i + 1} frequency to {val}");
            string name = GetComRadioSetHzNameForComIndex(i + 1);
            uint value = (uint)(val * 1000000);
            this.eSimCon.ClientEvents.Invoke(name, value);
          }
      }
    }

    private void InitSimConConnection()
    {
      try
      {
        this.logger.Log(ELogging.LogLevel.DEBUG, "Openning connection to ESimCon.");
        this.eSimCon.Open();
        this.logger.Log(ELogging.LogLevel.DEBUG, "Connection to ESimCon opened.");

        // on success:
        try
        {
          this.logger.Log(ELogging.LogLevel.DEBUG, "Registering location types to ESimCon.");
          RegisterLocationTypesToSim();
          this.logger.Log(ELogging.LogLevel.DEBUG, "Registering communication types to ESimCon.");
          RegisterComTypesToSimCon();
          this.logger.Log(ELogging.LogLevel.INFO, "Connection enabled.");
          this.logger.Log(ELogging.LogLevel.DEBUG, "Updating status to Connected-No-Data");
          this.State.ConnectionStatus = EConnectionStatus.ConnectedNoData;
        }
        catch (Exception ex)
        {
          this.logger.Log(ELogging.LogLevel.ERROR, "Fatal error registering simcon variables: " + ex.Message);
          throw new ApplicationException("Failed to register simcon definitions.", ex);
        }
      }
      catch
      {
        // intentionally blank
        this.logger.Log(ELogging.LogLevel.INFO, "Connection failed, will retry after a while...");
      }
    }

    private void InitSimConCheckLatitude()
    {
      EAssert.IsTrue(this.latTypeId != TYPE_EMPTY);
      this.logger.Log(ELogging.LogLevel.DEBUG, "Requesting latitude from ESimCon.");
      this.latRequestId = this.eSimCon.Values.Request(this.latTypeId);
    }

    private void ProcessFreqDataReceived(ESimConnect.ESimConnect.ESimConnectDataReceivedEventArgs e)
    {
      this.logger.Log(ELogging.LogLevel.DEBUG, $"Frequency changed data received as {e.Data}");
      SimVar<double> cf = this.comFrequencies.First(q => q.RequestId == e.RequestId);
      cf.Value = (double)e.Data / 1e6;
      if (this.FrequencyChangedCallback is not null)
      {
        int index = Array.IndexOf(this.comFrequencies, cf);
        if (this.comTransmits[index].Value)
        {
          this.State.ActiveComFrequency = cf.Value;
          this.FrequencyChangedCallback(cf.Value);
        }

      }
    }

    private void ProcessLatDataReceived(ESimConnect.ESimConnect.ESimConnectDataReceivedEventArgs e)
    {
      this.logger.Log(ELogging.LogLevel.DEBUG, $"Init-check-var value obtained as {e.Data}");
      if (e.Data is double val && val != 0)
      {
        this.logger.Log(ELogging.LogLevel.INFO, $"Init-check-var value valid, confirming connection.");
        this.State.ConnectionStatus = EConnectionStatus.ConnectedWithData;
        this.connectionTimer.Enabled = false;

        InitializeComTypesToSimCon();
      }
    }

    private void ProcessTransmitDataReceived(ESimConnect.ESimConnect.ESimConnectDataReceivedEventArgs e)
    {
      var ct = this.comTransmits.First(q => q.RequestId == e.RequestId);
      int index = Array.IndexOf(this.comTransmits, ct);
      ct.Value = ((double)e.Data) != 0;
      this.logger.Log(ELogging.LogLevel.INFO, $"COM{index + 1} transmit changed to {ct.Value}");
      if (ct.Value)
      {
        this.logger.Log(ELogging.LogLevel.INFO, $"Sending new volume {this.comVolumes[index].Value} to vPilot");
        this.State.ActiveComIndex = index + 1;
        this.State.ActiveComVolume = this.comVolumes[index].Value;
        this.State.ActiveComFrequency = this.comFrequencies[index].Value;
        this.VolumeUpdateCallback?.Invoke(this.comVolumes[index].Value);
      }
    }

    private void ProcessVolumeDataReceived(ESimConnect.ESimConnect.ESimConnectDataReceivedEventArgs e)
    {
      var cv = this.comVolumes.First(q => q.RequestId == e.RequestId);
      int index = Array.IndexOf(comVolumes, cv);

      double volumeDouble = (double)e.Data;
      cv.Value = volumeDouble;
      this.logger.Log(ELogging.LogLevel.INFO, $"COM{index + 1} volume changed to {cv.Value}");
      if (this.comTransmits[index].Value)
      {
        this.logger.Log(ELogging.LogLevel.INFO, $"Sending new volume {cv.Value} to vPilot");
        this.State.ActiveComVolume = cv.Value;
        this.VolumeUpdateCallback?.Invoke(cv.Value);
      }
    }
    private void RegisterComTypesToSimCon()
    {
      this.eSimCon.DataReceived += ESimCon_DataReceived;

      RequestId requestId;
      for (int i = 0; i < this.settings.NumberOfComs; i++)
      {
        var cv = this.comVolumes[i];
        this.logger.Log(ELogging.LogLevel.INFO, $"COM {i + 1} VOLUME registering via {cv.Name}.");
        this.comVolumes[i].TypeId = this.eSimCon.Values.Register<double>(cv.Name);
        requestId = this.eSimCon.Values.RequestRepeatedly(cv.TypeId, SimConnectPeriod.SIM_FRAME, true);
        cv.RequestId = requestId;
        this.logger.Log(ELogging.LogLevel.DEBUG, $"COM {i + 1} VOLUME registered via {cv.RegInfo}");

        var ct = this.comTransmits[i];
        this.logger.Log(ELogging.LogLevel.INFO, $"COM {i + 1} TRANSMIT registering via {ct.Name}.");
        ct.TypeId = this.eSimCon.Values.Register<double>(ct.Name);
        requestId = this.eSimCon.Values.RequestRepeatedly(ct.TypeId, SimConnectPeriod.SIM_FRAME, true);
        ct.RequestId = requestId;
        this.logger.Log(ELogging.LogLevel.DEBUG, $"COM {i + 1} TRANSMIT registered via {ct.RegInfo}.");

        var cf = comFrequencies[i];
        this.logger.Log(ELogging.LogLevel.INFO, $"COM {i + 1} FREQ registering via {cf.Name}");
        cf.TypeId = this.eSimCon.Values.Register<double>(cf.Name);
        requestId = this.eSimCon.Values.RequestRepeatedly(cf.TypeId, SimConnectPeriod.SECOND, true);
        cf.RequestId = requestId;
        this.logger.Log(ELogging.LogLevel.DEBUG, $"COM {i + 1} FREQ registered via {cf.RegInfo}");
      }
    }

    private void RegisterLocationTypesToSim()
    {
      EAssert.IsTrue(this.latTypeId == TYPE_EMPTY);

      this.logger.Log(ELogging.LogLevel.INFO, $"Registering init-check-var, confirming connection.");
      string name = this.settings.InitializedCheckVar;
      this.latTypeId = this.eSimCon.Values.Register<double>(name);
    }
    private void StartIfNotConnected()
    {
      if (connectionTimer.Enabled) return;
      connectionTimer.Enabled = true;
    }
    #endregion Private Methods
  }
}
