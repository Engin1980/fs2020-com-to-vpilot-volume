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

namespace Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public record Settings(int[] StartupWindowSize);

    #region Public Classes

    public class ViewModel
    {
      #region Public Properties

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
      }

      #endregion Public Constructors
    }

    #endregion Public Classes

    #region Private Fields

    private readonly AppSimCon appSimCon;
    private readonly AppVPilot appVPilot;

    private readonly ELogging.Logger logger;

    #endregion Private Fields

    #region Public Properties

    public ViewModel Model { get; private set; }

    #endregion Public Properties

    #region Public Constructors

    public MainWindow()
    {
      InitializeComponent();

      var cfg = App.Configuration;
      this.appSimCon = new(cfg.GetSection("AppSimCon").Get<AppSimCon.Settings>() ?? throw new ApplicationException("Invalid config file - AppSimCon config missing."));
      this.appVPilot = new(cfg.GetSection("AppVPilot").Get<AppVPilot.Settings>() ?? throw new ApplicationException("Invalid config file - VPilot config missing."));

      var sett = cfg.GetSection("MainWindow").Get<MainWindow.Settings>() ?? throw new ApplicationException("Invalid config file - MainWindow config missing.");
      this.Width = sett.StartupWindowSize[0];
      this.Height = sett.StartupWindowSize[1];

      this.Model = new ViewModel(this.appSimCon.State, this.appVPilot.State);
      this.DataContext = this.Model;
      this.logger = ELogging.Logger.Create(this, "MainWindow", false);

      InitLog();
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
      var configRules = App.Configuration.GetSection("Logging:MainWindowOut:Rules");
      List<ConfigLogRule> configLogRules = configRules.Get<List<ConfigLogRule>>() ?? throw new ApplicationException("Failed to load config - logging.");
      List<LogRule> rules = configLogRules.Select(q => new LogRule(q.Pattern, q.Level)).ToList();
      Logger.RegisterLogAction(li => ExtendLog(li), rules);

      this.logger.Log(LogLevel.INFO, "Log initialized");
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      this.logger.Log(LogLevel.INFO, "Window_Loaded invoked");

      this.appSimCon.Start();
      this.appVPilot.Start();
    }

    #endregion Private Methods

    private void btnV_Click(object sender, RoutedEventArgs e)
    {
      Button btn = (Button)sender;
      double v = double.Parse((string)btn.Tag) / 100;

      this.appVPilot.SetVolume(v);
    }
  }
}