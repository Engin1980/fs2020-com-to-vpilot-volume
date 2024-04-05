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
using ELogging.Model;

namespace Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    #region Public Classes

    public class ViewModel
    {
      #region Public Properties

      public AppSimCon.StateViewModel SimConState { get; private set; }

      public AppVPilotManager.StateViewModel VPilotState { get; private set; }

      #endregion Public Properties

      #region Public Constructors

      public ViewModel(AppSimCon.StateViewModel simConState, AppVPilotManager.StateViewModel vpilotState)
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

    private readonly AppSimCon appSimCon = new();
    private readonly AppVPilotManager appVPilotManager = new();

    private readonly ELogging.Logger logger;

    #endregion Private Fields

    #region Public Properties

    public ViewModel Model { get; private set; }

    #endregion Public Properties

    #region Public Constructors

    public MainWindow()
    {
      InitializeComponent();
      this.Model = new ViewModel(this.appSimCon.State, this.appVPilotManager.State);
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
        txtOut.AppendText($"\n{DateTime.Now}\t{li.SenderName,-20}\t{li.Level,-12}\t{li.Message}");
        txtOut.ScrollToEnd();
      }
    }

    private void InitLog()
    {
      List<LogRule> rules = new()
      {
        new LogRule(".*", true, true, true, true)
      };
      ELogging.Logger.RegisterLogAction(li => ExtendLog(li), rules);

      this.logger.Log(ELogging.LogLevel.INFO, "Log initialized");
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      this.logger.Log(ELogging.LogLevel.INFO, "Window_Loaded invoked");

      this.appSimCon.Start();
      this.appVPilotManager.Start();
    }

    #endregion Private Methods

    private void btnV_Click(object sender, RoutedEventArgs e)
    {
      Button btn = (Button)sender;
      double v = double.Parse((string) btn.Tag) /  100;

      this.appVPilotManager.SetVolume(v);
    }
  }
}