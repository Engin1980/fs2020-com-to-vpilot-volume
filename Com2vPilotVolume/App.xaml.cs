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

  //private void InitConfig()
  //{
  //  EnsureLocalConfigExists();

  //  var cb = new ConfigurationBuilder();
  //  cb.AddJsonFile("appsettings.json", false);
  //  var configuration = cb.Build();
  //  AppSettings = configuration.Get<AppSettings>() ?? new AppSettings();
  //  //EnsureSettingsSanity(); //TODO remove if not used
  //}

  //private static string UserConfigFilePath => Path.Combine(
  //  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
  //  "Com2VPilotVolume",
  //  "appsettings.json");

  //public static void EnsureLocalConfigExists()
  //{
  //  if (File.Exists(UserConfigFilePath) == false)
  //  {
  //    Directory.CreateDirectory(Path.GetDirectoryName(UserConfigFilePath)!);
  //    File.Copy("appsettings.json", UserConfigFilePath);
  //  }

  //  Directory.CreateDirectory(folder);
  //  string configPath = Path.Combine(folder, "config.json");
  //}
}
