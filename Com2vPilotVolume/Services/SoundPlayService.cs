using Eng.Com2vPilotVolume.Types;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.Com2vPilotVolume.Services
{
  public class SoundPlayService(SoundsConfig settings) : BaseService
  {
    private readonly SoundsConfig settings = settings;

    private void ValidateSoundFiles()
    {
      if (settings.MaxVolumeFile != null && System.IO.File.Exists(settings.MaxVolumeFile) == false)
      {
        logger.Log(ESystem.Logging.LogLevel.ERROR, $"Max volume sound file {settings.MaxVolumeFile} not found. No sound will be played.");
      }
      if (settings.MinVolumeFile != null && System.IO.File.Exists(settings.MinVolumeFile) == false)
      {
        logger.Log(ESystem.Logging.LogLevel.ERROR, $"Min volume sound file {settings.MinVolumeFile} not found. No sound will be played.");
      }
      if (settings.FrequencyChangedFile != null && System.IO.File.Exists(settings.FrequencyChangedFile) == false)
      {
        logger.Log(ESystem.Logging.LogLevel.ERROR, $"Frequency changed sound file {settings.FrequencyChangedFile} not found. No sound will be played.");
      }
      if (settings.ComChangedFile != null && System.IO.File.Exists(settings.ComChangedFile) == false)
      {
        logger.Log(ESystem.Logging.LogLevel.ERROR, $"COM changed sound file {settings.ComChangedFile} not found. No sound will be played.");
      }
    }

    protected async override Task StartInternalAsync()
    {
      await Task.Run(() => { ValidateSoundFiles(); });
    }

    protected async override Task StopInternalAsync()
    {
      await Task.Run(() => { });
    }

    public void PlayVolumeMax()
    {
      TryPlayMP3File(settings.MaxVolumeFile, settings.MaxVolumeFileVolume);
    }

    public void PlayVolumeMin()
    {
      TryPlayMP3File(settings.MinVolumeFile, settings.MinVolumeFileVolume);
    }

    public void PlayFrequencyChanged()
    {
      TryPlayMP3File(settings.FrequencyChangedFile, settings.FrequencyChangedFileVolume);
    }

    public void PlayComChanged()
    {
      TryPlayMP3File(settings.ComChangedFile, settings.ComChangedFileVolume);
    }

    private void TryPlayMP3File(string? fileName, Volume volume)
    {
      logger.Log(ESystem.Logging.LogLevel.DEBUG, $"Request to play {fileName} at volume {volume}.");

      if (fileName == null) return;

      if (System.IO.File.Exists(fileName) == false)
      {
        logger.Log(ESystem.Logging.LogLevel.WARNING, $"File {fileName} not found, playing skipped.");
      }
      else
        try
        {
          var reader = new Mp3FileReader(fileName);
          var waveOut = new WaveOut();
          waveOut.Init(reader);
          waveOut.Volume = (float)volume;
          waveOut.Play();
        }
        catch (Exception ex)
        {
          logger.Log(ESystem.Logging.LogLevel.WARNING, $"File {fileName} cannot be played. Reason: {ex.Message}");
        }
    }
  }
}
