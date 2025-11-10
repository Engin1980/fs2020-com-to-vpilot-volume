using eng.com2vPilotVolume.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Com2vPilotVolume;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
  public static IConfiguration Configuration { get; private set; } = null!;
  internal static AppSettings AppSettings { get; private set; } = new AppSettings();

  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    InitConfig();
  }

  private void InitConfig()
  {
    var cb = new ConfigurationBuilder();
    cb.AddJsonFile("appsettings.json", false);
    App.Configuration = cb.Build();
    AppSettings = App.Configuration.Get<AppSettings>() ?? new AppSettings();
    EnsureSettingsSanity();
  }

  private void EnsureSettingsSanity()
  {
    try
    {
      var tmp = AppSettings.KeyboardMappings
        .Where(q => q.Adjust == null || q.Set == null)
        .ToList();
      if (tmp.Any())
        throw new ApplicationException("Every keyboard mapping entry must have at least one of 'Set' or 'Adjust' defined.");
    }
    catch (Exception ex)
    {
      System.Windows.MessageBox.Show($"Error in configuration: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
      Environment.Exit(1);
    }
  }
}
