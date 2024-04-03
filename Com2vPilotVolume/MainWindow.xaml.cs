using eng.com2vPilotVolume.Types;
using Eng.WinCoreAudioApiLib;
using Microsoft.Extensions.Logging;
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
using Eng.WinCoreAudioApiLib;

namespace Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public class ViewModel
    {
      public AppSimCon.StateViewModel SimConState { get; set; }
      public AppVolumeManager.StateViewModel VPilotState { get; set; }
    }

    private readonly AppSimCon appSimCon = new();
    private readonly AppVolumeManager appVolumeManager = new();

    public ViewModel Model { get; } = new();
    private readonly ILogger logger = LoggingProvider.CreateLogger<MainWindow>();

    public MainWindow()
    {
      InitializeComponent();

      this.Model.SimConState = this.appSimCon.State;
      this.Model.VPilotState = this.appVolumeManager.State;

      this.DataContext = this.Model;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      this.logger.LogInformation("Window_Loaded invoked");
    }
  }
}