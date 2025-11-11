using Eng.Com2vPilotVolume.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace Eng.Com2vPilotVolume;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
  internal static AppSettings AppSettings { get; set; } = new AppSettings();
  internal static IConfigurationRoot Configuration { get; set; } = null!;

  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);
  }
}
