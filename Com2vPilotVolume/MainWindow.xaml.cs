using eng.com2vPilotVolume.Types;
using Eng.WinCoreAudioApiLib;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ESystem.Asserting;
using Microsoft.Extensions.Configuration;
using ELogging;
using ESystem.Miscelaneous;
using System.Reflection;
using System.Linq.Expressions;
using System.CodeDom;
using System.Timers;

namespace Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {

    public record Settings(int[] StartupWindowSize, bool ShowSimpleAdjustButtons);

    private record ConfigLogRule(string Pattern, string Level);


    #region Public Classes + Structs + Interfaces

    public class ViewModel : NotifyPropertyChanged
    {

      #region Public Properties

      public bool ShowSimpleAdjustButtons
      {
        get => base.GetProperty<bool>(nameof(ShowSimpleAdjustButtons))!;
        set => base.UpdateProperty(nameof(ShowSimpleAdjustButtons), value);
      }

      public AppSimCon.StateViewModel SimConState { get; private set; }
      public AppVPilot.StateViewModel VPilotState { get; private set; }

      #endregion Public Properties

      #region Public Constructors

      public ViewModel(AppSimCon.StateViewModel simConState, AppVPilot.StateViewModel vpilotState)
      {
        EAssert.Argument.IsNotNull(simConState, nameof(simConState));
        EAssert.Argument.IsNotNull(vpilotState, nameof(vpilotState));

        this.SimConState = simConState;
        this.VPilotState = vpilotState;
        this.ShowSimpleAdjustButtons = false;
      }

      #endregion Public Constructors

    }

    #endregion Public Classes + Structs + Interfaces

    #region Private Fields

    private readonly AppSimCon appSimCon = null!;
    private readonly AppVPilot appVPilot = null!;
    private readonly Logger logger;
    private readonly Sounds sounds = null!;
    private readonly System.Timers.Timer? tmrMinVolumeAlert;
    private double lastActiveComFrequency = 0;
    private int lastActiveComIndex = 1;

    #endregion Private Fields

    #region Public Properties

    public ViewModel Model { get; private set; } = null!;

    #endregion Public Properties

    #region Public Constructors

    public MainWindow()
    {
      int repeatIntervalOfMinVolumeAlert;
      InitializeComponent();

      // log init
      this.logger = Logger.Create(this, "MainWindow", false);
      InitLog();

      // app init
      MainWindow.Settings sett;
      try
      {
        var cfg = App.Configuration;
        this.appVPilot = new(cfg.GetSection("AppVPilot").Get<AppVPilot.Settings>() ?? throw new ConfigLoadFailedException("AppVPilot"));
        this.appSimCon = new(cfg.GetSection("AppSimCon").Get<AppSimCon.Settings>() ?? throw new ConfigLoadFailedException("AppSimCon"));
        this.sounds = new(cfg.GetSection("Sounds").Get<Sounds.Settings>() ?? throw new ConfigLoadFailedException("Sounds"));
        repeatIntervalOfMinVolumeAlert = cfg.GetValue<int>("RepeatIntervalOfMinVolumeAlert", -1);
        sett = cfg.GetSection("MainWindow").Get<MainWindow.Settings>() ?? throw new ConfigLoadFailedException("MainWindow");
      }
      catch (Exception ex)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to initialize from config file.");
        this.logger.Log(LogLevel.ERROR, ex.ToString());
        this.logger.Log(LogLevel.ERROR, "Application will now quit.");
        Application.Current.Shutdown();
        return;
      }

      this.appSimCon.VolumeUpdateCallback += appSimCon_VolumeUpdateCallback;
      this.appSimCon.ActiveComChangedCallback += appSimCon_ActiveComChangedCallback;
      this.appSimCon.FrequencyChangedCallback += appSimCon_FrequencyChangedCallback;

      this.Width = sett.StartupWindowSize[0];
      this.Height = sett.StartupWindowSize[1];

      if (repeatIntervalOfMinVolumeAlert > 0)
      {
        this.tmrMinVolumeAlert = new()
        {
          Interval = repeatIntervalOfMinVolumeAlert * 1000,
          AutoReset = true,
          Enabled = false
        };
        this.tmrMinVolumeAlert.Elapsed += tmrMinVolumeAlert_Elapsed;
      }
      this.Model = new ViewModel(this.appSimCon.State, this.appVPilot.State)
      {
        ShowSimpleAdjustButtons = sett.ShowSimpleAdjustButtons
      };
      this.DataContext = this.Model;
    }

    #endregion Public Constructors

    #region Private Methods

    private void appSimCon_ActiveComChangedCallback(int comIndex)
    {
      if (this.lastActiveComIndex != comIndex)
      {
        this.lastActiveComFrequency = 0;
        this.lastActiveComIndex = comIndex;
        this.sounds.PlayComChanged();
      }
    }

    private void appSimCon_FrequencyChangedCallback(double newFrequency)
    {
      if (newFrequency != this.lastActiveComFrequency)
      {
        this.lastActiveComFrequency = newFrequency;
        this.sounds.PlayFrequencyChanged();
      }
    }

    private void appSimCon_VolumeUpdateCallback(Volume volume)
    {
      this.appVPilot.SetVolume(volume);
      if (volume == 0)
      {
        this.sounds.PlayVolumeMin();
        if (this.tmrMinVolumeAlert != null) this.tmrMinVolumeAlert.Enabled = true;
      }
      else
      {
        if (this.tmrMinVolumeAlert != null) this.tmrMinVolumeAlert.Enabled = false;
        if (volume == 1)
          this.sounds.PlayVolumeMax();
      }
    }

    private void btnV_Click(object sender, RoutedEventArgs e)
    {
      Button btn = (Button)sender;
      double v = double.Parse((string)btn.Tag) / 100;

      this.appSimCon_VolumeUpdateCallback(v);
    }

    private void ExtendLog(LogItem li)
    {
      if (!Dispatcher.CheckAccess())
        Dispatcher.Invoke(() => ExtendLog(li));
      else
      {
        txtOut.AppendText($"\n{DateTime.Now,-20}  {li.SenderName,-20}  {li.Level,-12}  {li.Message}");
        txtOut.ScrollToEnd();
      }
    }

    private void InitLog()
    {
      // file log
      string logFileName = App.Configuration.GetSection("Logging:LogFile").GetValue<string?>("Name") ?? "_log.txt";
      bool resetLogFile = App.Configuration.GetSection("Logging:LogFile").GetValue<bool?>("Reset") ?? true;
      string levelString = App.Configuration.GetSection("Logging:LogFile").GetValue<string?>("Level") ?? "debug";
      LogLevel level;
      try
      {
        level = Enum.Parse<LogLevel>(levelString);
      }
      catch (Exception ex)
      {
        level = LogLevel.DEBUG;
      }
      if (resetLogFile && System.IO.File.Exists(logFileName))
        System.IO.File.Delete(logFileName);
      List<LogRule> logEverythingRules = new()
      {
        new LogRule(".+", level)
      };
      Logger.RegisterLogAction(li => LogToFile(li, logFileName), logEverythingRules);

      // Main Window log
      try
      {
        string key = "Logging:MainWindowOut:Rules";
        var configRules = App.Configuration.GetSection(key);
        List<ConfigLogRule> configLogRules = configRules.Get<List<ConfigLogRule>>() ?? throw new ConfigLoadFailedException(key);
        List<LogRule> rules = configLogRules.Select(q => new LogRule(q.Pattern, q.Level)).ToList();
        Logger.RegisterLogAction(li => ExtendLog(li), rules);
      }
      catch (Exception ex)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to initialize local app log.");
        this.logger.Log(LogLevel.ERROR, ex.ToString());
        this.logger.Log(LogLevel.ERROR, "Application will now quit.");
        Application.Current.Shutdown();
        return;
      }

      this.logger.Log(LogLevel.INFO, "Log initialized");
    }

    private void LogToFile(LogItem li, string fileName)
    {
      string s = $"\n{DateTime.Now,-20}  {li.SenderName,-20}  {li.Level,-12}  {li.Message}";
      lock (this)
      {
        System.IO.File.AppendAllText(fileName, s);
      }
    }

    private void PrintAbout()
    {
      this.logger.Log(LogLevel.ALWAYS, " ");

      var assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      this.logger.Log(LogLevel.ALWAYS, fvi.ProductName!);
      this.logger.Log(LogLevel.ALWAYS, " ");
      this.logger.Log(LogLevel.ALWAYS, $"Version: \t{fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}");
      this.logger.Log(LogLevel.ALWAYS, "Author: \tMarek Vajgl (engin@seznam.cz)");
      this.logger.Log(LogLevel.ALWAYS, "Link: \t\thttps://github.com/Engin1980/fs2020-com-to-vpilot-volume");
      this.logger.Log(LogLevel.ALWAYS, " ");
      this.logger.Log(LogLevel.ALWAYS, "! ! ! Use at own risk ! ! !");
      this.logger.Log(LogLevel.ALWAYS, " ");
    }

    private void tmrMinVolumeAlert_Elapsed(object? sender, ElapsedEventArgs e)
    {
      this.sounds.PlayVolumeMin();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
      Application.Current.Shutdown();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      this.logger.Log(LogLevel.INFO, "Window_Loaded invoked");

      this.appSimCon.Start();
      this.appVPilot.Start();

      PrintAbout();
    }

    #endregion Private Methods

  }
}