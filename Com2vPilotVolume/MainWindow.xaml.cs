using Eng.Com2vPilotVolume;
using Eng.Com2vPilotVolume.Services;
using Eng.Com2vPilotVolume.Types;
using Eng.WinCoreAudioApiLib;
using ESystem.Asserting;
using ESystem.Logging;
using ESystem.Miscelaneous;
using Microsoft.Extensions.Configuration;
using System.CodeDom;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
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
using static ESimConnect.Definitions.SimEvents.Client.AircraftRadio;

namespace Eng.Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public record Services(SimConService SimConService, VPilotService VPilotService, KeyHookService KeyHookService, SoundService SoundService);

    #region Public Classes

    public class ViewModel : NotifyPropertyChanged
    {
      #region Public Properties

      public SimConService.StateViewModel SimConState { get; private set; }
      public VPilotService.StateViewModel VPilotState { get; private set; }


      public double CustomVolume
      {
        get { return base.GetProperty<double>(nameof(CustomVolume))!; }
        set { base.UpdateProperty(nameof(CustomVolume), value); }
      }

      #endregion Public Properties

      #region Public Constructors

      public ViewModel(SimConService.StateViewModel simConState, VPilotService.StateViewModel vpilotState)
      {
        EAssert.Argument.IsNotNull(simConState, nameof(simConState));
        EAssert.Argument.IsNotNull(vpilotState, nameof(vpilotState));

        this.SimConState = simConState;
        this.VPilotState = vpilotState;
      }

      #endregion Public Constructors
    }

    #endregion Public Classes

    #region Private Fields

    private readonly Services services = null!;
    private readonly VolumeMapper volumeMapper = null!;
    private readonly Logger logger = null!;
    private double lastActiveComFrequency = 0;
    private int lastActiveComIndex = 1;
    private bool isInitialized = false;
    private readonly NotifyIcon? notifyIcon = null;

    #endregion Private Fields

    #region Public Properties

    public ViewModel Model { get; private set; } = null!;

    #endregion Public Properties

    #region Public Constructors

    public MainWindow()
    {
      InitializeComponent();

      this.Title = $"FS2020 Com->VPilot Volume (ver. {Assembly.GetExecutingAssembly().GetName().Version})";

      SettingsProvider.LoadAppSettings(out List<string> errors);

      // log init
      this.logger = Logger.Create(this, "Main", false);
      InitLog();
      LogServiceProviderErrors(errors);

      // simconnect.dll existence check
      if (System.IO.File.Exists("simconnect.dll") == false)
      {
        this.logger.Log(LogLevel.ERROR, "SimConnect.dll not found in application folder.");
        this.logger.Log(LogLevel.ERROR, "Application start-up aborted.");
        this.isInitialized = false;
        return;
      }

      // app init
      MainWindowConfig sett;
      try
      {
        sett = App.AppSettings.MainWindow;
      }
      catch (Exception ex)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to initialize from config file.");
        this.logger.Log(LogLevel.ERROR, ex.ToString());
        this.logger.Log(LogLevel.ERROR, "Application start-up aborted.");
        this.isInitialized = false;
        return;
      }

      this.volumeMapper = new(App.AppSettings.VolumeMapping);

      try
      {
        this.services = new Services(
          new SimConService(App.AppSettings.AppSimCon),
          new VPilotService(App.AppSettings.AppVPilot),
          new KeyHookService(App.AppSettings.KeyboardMappings),
          new SoundService(App.AppSettings.Sounds)
          );
      }
      catch (Exception ex)
      {
        this.logger.Log(LogLevel.ERROR, "Failed to initialize services.");
        this.logger.Log(LogLevel.ERROR, ex.ToString());
        this.logger.Log(LogLevel.ERROR, "Application start-up aborted.");
        this.isInitialized = false;
        return;
      }

      try
      {
        this.notifyIcon = CreateNotifyIcon();
      }
      catch (Exception ex)
      {
        this.notifyIcon = null!;
        this.logger.Log(LogLevel.ERROR, "Failed to initialize notify icon.");
        this.logger.Log(LogLevel.ERROR, ex.ToString());
        this.logger.Log(LogLevel.ERROR, "App minimalization may work in a strange way/or crash.");
      }

      this.services.SimConService.VolumeUpdateCallback += appSimCon_VolumeUpdateCallback;
      this.services.SimConService.ActiveComChangedCallback += appSimCon_ActiveComChangedCallback;
      this.services.SimConService.FrequencyChangedCallback += appSimCon_FrequencyChangedCallback;

      this.Width = sett.StartupWindowSize[0];
      this.Height = sett.StartupWindowSize[1];

      this.Model = new ViewModel(
        this.services.SimConService.State,
        this.services.VPilotService.State);
      this.DataContext = this.Model;
      this.isInitialized = true;
    }

    private void LogServiceProviderErrors(List<string> errors)
    {
      var logger = Logger.Create("SettingsProvider");
      foreach (var error in errors)
      {
        logger.Log(LogLevel.ERROR, error);
      }
    }

    private NotifyIcon CreateNotifyIcon()
    {
      NotifyIcon ret = new()
      {
        Icon = new Icon("icon.ico"),
        Visible = false,
        Text = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Com2VPilotVolume",
      };
      ret.Click += (s, e) =>
      {
        this.Show();
        this.WindowState = WindowState.Normal;
      };
      return ret;
    }

    private void appSimCon_FrequencyChangedCallback(double newFrequency)
    {
      if (newFrequency != this.lastActiveComFrequency)
      {
        this.lastActiveComFrequency = newFrequency;
        this.services.SoundService.PlayFrequencyChanged();
      }
    }

    private void appSimCon_ActiveComChangedCallback(int comIndex)
    {
      if (this.lastActiveComIndex != comIndex)
      {
        this.lastActiveComFrequency = 0;
        this.lastActiveComIndex = comIndex;
        this.services.SoundService.PlayComChanged();
      }
    }

    private void appSimCon_VolumeUpdateCallback(Volume simVolume)
    {
      Volume winVolume = volumeMapper.Map(simVolume);

      this.services.VPilotService.SetVolume(winVolume);
      if (winVolume == 1)
        this.services.SoundService.PlayVolumeMax();
      else if (winVolume == 0)
        this.services.SoundService.PlayVolumeMin();
    }

    #endregion Public Constructors

    #region Private Methods

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

    private record ConfigLogRule(string Pattern, string Level);

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
      catch (Exception)
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
        System.Windows.Application.Current.Shutdown();
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

    private void Window_Closed(object sender, EventArgs e)
    {
      System.Windows.Application.Current.Shutdown();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
      if (this.isInitialized == false) return;

      this.logger.Log(LogLevel.INFO, "Window_Loaded invoked");

      List<Task> tasks = [];
      Task t;

      t = this.services.VPilotService.StartAsync();
      tasks.Add(t);

      t = this.services.SimConService.StartAsync();
      tasks.Add(t);

      t = this.services.KeyHookService.StartAsync();
      tasks.Add(t);

      t = this.services.SoundService.StartAsync();
      tasks.Add(t);

      await Task.WhenAll(tasks);

      PrintAbout();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
      if (notifyIcon != null && WindowState == WindowState.Minimized && this.Visibility == Visibility.Visible)
      {
        Hide();
      }
    }

    private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (this.notifyIcon != null)
        this.notifyIcon.Visible = this.Visibility != Visibility.Visible;
    }

    private void btnOpenSettingsEditor_Click(object sender, RoutedEventArgs e)
    {
      SettingsEditor frm = new SettingsEditor();
      frm.ShowDialog();
    }

    private void btnInputSet_Click(object sender, RoutedEventArgs e)
    {
      double v = this.Model.CustomVolume / 100d;
      this.appSimCon_VolumeUpdateCallback(v);
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

    #endregion Private Methods
  }
}