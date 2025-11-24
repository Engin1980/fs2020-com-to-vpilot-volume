using ESystem;
using ESystem.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Eng.WinCoreAudioApiLib
{
  public class Mixer
  {
    private readonly Logger logger;

    public Mixer()
    {
      this.logger = Logger.Create(this, "WCAA-Mixer");
    }

    public IEnumerable<int> GetProcessIds()
    {
      this.logger.Log(LogLevel.INFO, "GetProcessIds() invoked");

      // get the speakers (1st render + multimedia) device
      IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
      deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out IMMDevice speakers);

      // activate the session manager. we need the enumerator
      Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
      speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out object o);
      IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

      // enumerate sessions for on this device
      mgr.GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);
      sessionEnumerator.GetCount(out int count);

      for (int i = 0; i < count; i++)
      {
        sessionEnumerator.GetSession(i, out IAudioSessionControl2 ctl);
        ctl.GetProcessId(out int id);
        yield return id;
        Marshal.ReleaseComObject(ctl);
      }
      Marshal.ReleaseComObject(sessionEnumerator);
      Marshal.ReleaseComObject(mgr);
      Marshal.ReleaseComObject(speakers);
      Marshal.ReleaseComObject(deviceEnumerator);

      this.logger.Log(LogLevel.INFO, "GetProcessIds() completed.");
    }

    public double GetVolume(int processId)
    {
      ISimpleAudioVolume volume = TryGetVolumeObject(processId)
        ?? throw new MixerException($"Failed to get volume for '{processId}'.");

      volume.GetMasterVolume(out float level);

      double ret = level;
      return ret;
    }

    public bool IsMute(int processId)
    {
      ISimpleAudioVolume volume = TryGetVolumeObject(processId)
        ?? throw new MixerException($"Failed to get mute-flag for '{processId}'.");
      volume.GetMute(out bool ret);

      logger.Log(LogLevel.INFO, $"Get mute for {processId} returned {ret}");
      return ret;
    }

    public void SetVolume(int processId, double level)
    {
      logger.Log(LogLevel.INFO, $"Set Volume for {processId} setting {level}");

      level = NormalizeLevel(level);
      logger.Log(LogLevel.DEBUG, $"Normalized level = {level}");

      ISimpleAudioVolume volume = TryGetVolumeObject(processId)
        ?? throw new MixerException($"Failed to get volume for '{processId}'.");

      Guid guid = Guid.Empty;
      _ = volume.SetMasterVolume((float)level, ref guid);
    }

    public double GetMasterVolume()
    {
      // get the speakers (1st render + multimedia) device
      IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
      deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out IMMDevice speakers);

      // activate the endpoint volume interface
      Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
      speakers.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out object o);
      IAudioEndpointVolume endpoint = (IAudioEndpointVolume)o;

      // get the volume level (0.0 - 1.0)
      endpoint.GetMasterVolumeLevelScalar(out float level);

      // clean up
      Marshal.ReleaseComObject(endpoint);
      Marshal.ReleaseComObject(speakers);
      Marshal.ReleaseComObject(deviceEnumerator);
      return level;
    }

    public void SetMasterVolume(double level)
    {
      level = Math.Max(0, Math.Min(1, level));
      IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
      deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out IMMDevice speakers);

      Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
      speakers.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out object obj);
      var endpointVolume = (IAudioEndpointVolume)obj;

      Guid guid = Guid.Empty;
      _ = endpointVolume.SetMasterVolumeLevelScalar((float)level, ref guid);

      Marshal.ReleaseComObject(endpointVolume);
      Marshal.ReleaseComObject(speakers);
      Marshal.ReleaseComObject(deviceEnumerator);
    }

    private static double NormalizeLevel(double level)
    {
      return level
        .Select(q => Math.Round(q, 2))
        .Select(q => Math.Max(q, 0))
        .Select(q => Math.Min(q, 1));
    }

    public void SetMute(int processId, bool isMuted)
    {
      logger.Log(LogLevel.INFO, $"Set Mute for {processId} setting {isMuted}");

      ISimpleAudioVolume? volume = TryGetVolumeObject(processId);
      if (volume == null)
        throw new MixerException($"Failed to set mute-flag for '{processId}'.");

      Guid guid = Guid.Empty;
      volume.SetMute(isMuted, ref guid);
    }

    private static ISimpleAudioVolume? TryGetVolumeObject(int processId)
    {
      // get the speakers (1st render + multimedia) device
      IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
      deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out IMMDevice speakers);

      // activate the session manager. we need the enumerator
      Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
      speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out object o);
      IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

      // enumerate sessions for on this device
      mgr.GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);
      sessionEnumerator.GetCount(out int count);

      // search for an audio session with the required name
      // NOTE: we could also use the process id instead of the app name (with IAudioSessionControl2)
      ISimpleAudioVolume? volumeControl = null;
      for (int i = 0; i < count; i++)
      {
        IAudioSessionControl2 ctl;
        sessionEnumerator.GetSession(i, out ctl);
        ctl.GetProcessId(out int pid);
        if (processId == pid)
        {
          volumeControl = (ISimpleAudioVolume)ctl;
          break;
        }
        Marshal.ReleaseComObject(ctl);
      }
      Marshal.ReleaseComObject(sessionEnumerator);
      Marshal.ReleaseComObject(mgr);
      Marshal.ReleaseComObject(speakers);
      Marshal.ReleaseComObject(deviceEnumerator);
      return volumeControl;
    }
  }
}
