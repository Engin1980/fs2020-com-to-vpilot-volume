using Eng.Com2vPilotVolume.Services;
using Eng.Com2vPilotVolume.Types;
using Eng.WinCoreAudioApiLib;
using ESystem.Asserting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Services
{
  public class ProcessVolumeInitService : BaseService
  {
    private readonly VolumeInitializationConfig config;

    public ProcessVolumeInitService(VolumeInitializationConfig config)
    {
      EAssert.Argument.IsNotNull(config, nameof(config));
      this.config = config;
    }

    public Task ApplyProcessVolumeInitializationsAsync()
    {
      return Task.Run(() => ApplyProcessVolumeInitialization());
    }

    private void ApplyProcessVolumeInitialization()
    {
      this.logger.Info("Applying process volume initializations...");

      Mixer m = new();

      this.logger.Info($"Setting up master volume level to {this.config.MasterVolume} %");
      try
      {
        m.SetMasterVolume(this.config.MasterVolume / 100);
      }
      catch (Exception ex)
      {
        this.logger.Error($"Failed to set master volume level: {ex.Message}");
      }

      var allProcesses = System.Diagnostics.Process.GetProcesses();

      var processInfos = m.GetProcessIds()
        .Select(q => new { Id = q, Process = allProcesses.FirstOrDefault(p => p.Id == q), Volume = m.GetVolume(q) })
        .Where(q => q.Process != null)
        .Select(q => new { q.Id, Process = q.Process!, q.Volume })
        .Where(q => q.Process.HasExited == false);

      this.logger.Debug($"Found {processInfos.Count()} audio processes.");
      foreach (var pi in processInfos)
      {
        this.logger.Debug($" - Process '{pi.Process.ProcessName}' (ID: {pi.Id}) with current volume {pi.Volume * 100:F1}%");
      }

      foreach (var vi in config.ProcessVolumes)
      {
        foreach (var pi in processInfos)
        {
          if (System.Text.RegularExpressions.Regex.IsMatch(pi.Process.ProcessName, vi.ProcessNameRegex))
          {
            this.logger.Info($"Setting initial volume for process '{pi.Process.ProcessName}' (ID: {pi.Id}) to {vi.Volume}% (was {pi.Volume * 100:F1}%)");
            try
            {
              m.SetVolume(pi.Id, vi.Volume / 100);
            }
            catch (Exception ex)
            {
              this.logger.Error($"Failed to set volume for process '{pi.Process.ProcessName}' (ID: {pi.Id}): {ex.Message}");
            }
          }
        }

        this.logger.Info("Process volume initializations completed.");
      }
    }

    protected override Task StartInternalAsync()
    {
      // intentionally blank
      return Task.CompletedTask;
    }

    protected override Task StopInternalAsync()
    {
      // intentionally blank
      return Task.CompletedTask;
    }
  }
}
