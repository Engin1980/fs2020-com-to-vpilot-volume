using ESystem.Asserting;
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
    public record Settings(string? MaxVolumeFile, string? MinVolumeFile, string? FrequencyChangedFile, string? ComChangedFile);

    private readonly Settings settings;
    private readonly ELogging.Logger logger;
    public Sounds(Settings settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      this.settings = settings;
      this.logger = ELogging.Logger.Create(this);
    }

    public void PlayVolumeMax()
    {
      TryPlayMP3File(settings.MaxVolumeFile);
    }

    public void PlayVolumeMin()
    {
      TryPlayMP3File(settings.MinVolumeFile);
    }

    public void PlayFrequencyChanged()
    {
      TryPlayMP3File(settings.FrequencyChangedFile);
    }

    public void PlayComChanged()
    {
      TryPlayMP3File(settings.ComChangedFile);
    }

    private void TryPlayMP3File(string? fileName)
    {
      if (fileName == null) return;

      if (System.IO.File.Exists(fileName) == false)
      {
        logger.Log(ELogging.LogLevel.WARNING, $"File {fileName} not found, playing skipped.");
      }
      try
      {
        var reader = new Mp3FileReader(fileName);
        var waveOut = new WaveOut();
        waveOut.Init(reader);
        waveOut.Play();
      }
      catch (Exception ex)
      {
        logger.Log(ELogging.LogLevel.WARNING, $"File {fileName} cannot be played. Reason: {ex.Message}");
      }
    }
  }
}
