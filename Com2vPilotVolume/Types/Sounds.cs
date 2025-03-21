﻿using ESystem.Asserting;
using NAudio.Wave;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
  public class Sounds
  {
    public record Settings(string? MaxVolumeFile, double MaxVolumeFileVolume,
      string? MinVolumeFile, double MinVolumeFileVolume,
      string? FrequencyChangedFile, double FrequencyChangedFileVolume,
      string? ComChangedFile, double ComChangedFileVolume);

    private readonly Settings settings;
    private readonly ESystem.Logging.Logger logger;
    public Sounds(Settings settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      this.settings = settings;
      this.logger = ESystem.Logging.Logger.Create(this, "Sound");
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
