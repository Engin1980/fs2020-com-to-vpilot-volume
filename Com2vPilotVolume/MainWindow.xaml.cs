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
using WinCoreAudioApiLib;

namespace Com2vPilotVolume
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
  {
    public class AppVolume : NotifyPropertyChangedBase
    {
      public Action<int, double>? VolumeChanged;
      public string Name
      {
        get => base.GetProperty<string>(nameof(Name))!;
        set => base.UpdateProperty(nameof(Name), value);
      }

      public int ProcessId
      {
        get => base.GetProperty<int>(nameof(ProcessId))!;
        set => base.UpdateProperty(nameof(ProcessId), value);
      }

      public double Volume
      {
        get => base.GetProperty<double>(nameof(Volume))!;
        set
        {
          double val = Math.Max(0, Math.Min(1, value));
          base.UpdateProperty(nameof(Volume), value);
          this.VolumeChanged?.Invoke(this.ProcessId, val);
        }
      }

      public override string ToString()
      {
        return $"{Name}({ProcessId})={Volume} [AppVolume]";
      }
    }

    public class ViewModel
    {
      public BindingList<AppVolume> Apps { get; } = new BindingList<AppVolume>();
    }

    public ViewModel Model { get; } = new();
    private readonly Mixer mixer = new();
    private readonly ILogger logger = LoggingProvider.CreateLogger<MainWindow>();

    public MainWindow()
    {
      InitializeComponent();
      this.DataContext = this.Model;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      this.logger.LogInformation("Window_Loaded invoked");

      var apps = mixer
        .GetProcessIds()
        .Select(q => CreateAppVolume(q))
        .ToList();

      foreach (var app in apps)
      {
        UpdateVolume(app);
        app.VolumeChanged += AppVolume_VolumeChanged;
      }

      apps
        .ToList()
        .ForEach(q => Model.Apps.Add(q));

      this.logger.LogInformation($"Window_Loaded completed with {Model.Apps.Count} apps.");
    }

    private AppVolume CreateAppVolume(int processId)
    {
      Process p = Process.GetProcessById(processId);
      AppVolume ret = new()
      {
        Name = p.ProcessName,
        ProcessId = processId
      };
      return ret;
    }

    private void AppVolume_VolumeChanged(int processId, double newVolume)
    {
      this.mixer.SetVolume(processId, newVolume);
    }

    private void UpdateVolume(AppVolume appVolume)
    {
      double value = this.mixer.GetVolume(appVolume.ProcessId);
      appVolume.Volume = value;
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
      this.Model.Apps.ToList().ForEach(q => UpdateVolume(q));
    }
  }
}