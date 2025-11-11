using Com2vPilotVolume;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
  internal static class SettingsProvider
  {
    public static string UserConfigFilePath => System.IO.Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "Com2VPilotVolume",
      "appsettings.json");

    public static string DefaultConfigFilePath => "appsettings.json";
    public record SettingsAndConfig(AppSettings? AppSettings, IConfigurationRoot Configuration);

    public static void LoadAppSettings(out List<string> errors)
    {
      errors = [];
      SettingsAndConfig sac = TryLoadUserSettings(ref errors);
      if (sac.AppSettings == null)
        sac = LoadDefaultSettings(ref errors);
      App.AppSettings = sac.AppSettings!;
      App.Configuration = sac.Configuration;
    }

    private static SettingsAndConfig LoadDefaultSettings(ref List<string> errors)
    {
      var cb = new ConfigurationBuilder();
      cb.AddJsonFile(DefaultConfigFilePath, false);
      var configuration = cb.Build();
      AppSettings? ret = configuration.Get<AppSettings>();
      if (ret == null)
      {
        errors.Add($"Failed to load default app settings from {DefaultConfigFilePath}. Initial settings will be used.");
        ret = new();
      }
      return new(ret, configuration);
    }

    private static SettingsAndConfig TryLoadUserSettings(ref List<string> errors)
    {
      if (System.IO.File.Exists(UserConfigFilePath) == false)
        CreateUserSettings(ref errors);

      var cb = new ConfigurationBuilder();
      cb.AddJsonFile(UserConfigFilePath, false);
      var configuration = cb.Build();
      AppSettings? ret = configuration.Get<AppSettings>();
      if (ret == null)
        errors.Add($"Failed to load user app settings from {UserConfigFilePath}. Default app settings are used.");
      return new(ret, configuration);
    }

    private static void CreateUserSettings(ref List<string> errors)
    {
      try
      {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(UserConfigFilePath)!);
        System.IO.File.Copy(DefaultConfigFilePath, UserConfigFilePath);
      }
      catch (Exception ex)
      {
        errors.Add($"Failed to create user settings file at {UserConfigFilePath}: {ex}. Default app settings are used.");
      }
    }
  }
}
