using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Com2vPilotVolume
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : System.Windows.Application
  {
    public static IConfiguration Configuration { get; private set; } = null!;
    
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
    }
  }

}
