using ESystem.Logging;
using System;
using System.Runtime.InteropServices;

namespace Eng.WinCoreAudioApiLib;


public interface IAdaptiveMixer
{
  double GetWindowsMixerVolume();
  void SetWindowsMixerVolume(double vol);
}

/// <summary>
/// Monitors the audio peak level of a Windows application identified by its Process ID (PID)
/// using Windows Core Audio API (WASAPI) through manual COM interop.
/// </summary>
public sealed class AdaptiveMixer : IAdaptiveMixer, IDisposable
{
  private readonly Logger logger;
  private readonly int targetPid;
  private readonly object lockObject = new();

  private IAudioMeterInformation? meterInfo;
  private bool isDisposed;

  /// <summary>
  /// Initializes a new instance of AdaptiveMixer for the specified process ID.
  /// </summary>
  /// <param name="pid">The process ID to monitor.</param>
  public AdaptiveMixer(int pid)
  {
    this.logger = Logger.Create(this, "WCAA-AdaptMixer");
    this.targetPid = pid;
    this.logger.Log(LogLevel.INFO, $"AdaptiveMixer created for PID {pid}");

    // Attempt initial session discovery (but don't fail if not found)
    TryLocateSession();
  }

  /// <summary>
  /// Gets whether an audio session for the target PID is currently available.
  /// </summary>
  public bool IsSessionAvailable
  {
    get
    {
      lock (lockObject)
      {
        return meterInfo != null;
      }
    }
  }

  /// <summary>
  /// Gets the current peak audio level (0.0 to 1.0) for the target process.
  /// If the session is not found, attempts to locate it. Returns 0.0 if unavailable.
  /// </summary>
  public float GetCurrentPeak()
  {
    lock (lockObject)
    {
      if (isDisposed)
      {
        logger.Log(LogLevel.WARNING, "GetCurrentPeak called on disposed object");
        return 0.0f;
      }

      // If we don't have a meter, try to locate the session
      if (meterInfo == null)
      {
        TryLocateSession();
      }

      // If still no meter, return 0
      if (meterInfo == null)
      {
        return 0.0f;
      }

      // Attempt to get the peak value
      try
      {
        meterInfo.GetPeakValue(out float peak);
        return peak;
      }
      catch (COMException ex)
      {
        logger.Log(LogLevel.WARNING, $"Failed to get peak value: {ex.Message}");

        // Session may have been closed, release and retry next time
        ReleaseMeter();
        return 0.0f;
      }
      catch (Exception ex)
      {
        logger.Log(LogLevel.ERROR, $"Unexpected error getting peak value: {ex.Message}");
        ReleaseMeter();
        return 0.0f;
      }
    }
  }

  /// <summary>
  /// Returns the system master volume for the default render device (0.0 - 1.0).
  /// </summary>
  public double GetWindowsMixerVolume()
  {
    lock (lockObject)
    {
      if (isDisposed)
      {
        logger.Log(LogLevel.WARNING, "GetWindowsMixerVolume called on disposed object");
        return 0.0;
      }

      IMMDeviceEnumerator? deviceEnumerator = null;
      IMMDevice? device = null;
      IAudioEndpointVolume? endpointVolume = null;

      try
      {
        // Create device enumerator
        deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

        // Get default render device
        deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);

        // Activate IAudioEndpointVolume on the device
        Guid iidEndpoint = typeof(IAudioEndpointVolume).GUID;
        device.Activate(ref iidEndpoint, 0, IntPtr.Zero, out object epObj);
        endpointVolume = epObj as IAudioEndpointVolume;

        if (endpointVolume == null)
          return 0.0;

        // Get master volume scalar (0.0 - 1.0)
        endpointVolume.GetMasterVolumeLevelScalar(out float level);
        return Math.Clamp((double)level, 0.0, 1.0);
      }
      catch (Exception ex)
      {
        logger.Log(LogLevel.ERROR, $"GetWindowsMixerVolume error: {ex.Message}");
        return 0.0;
      }
      finally
      {
        if (endpointVolume != null)
          Marshal.ReleaseComObject(endpointVolume);
        if (device != null)
          Marshal.ReleaseComObject(device);
        if (deviceEnumerator != null)
          Marshal.ReleaseComObject(deviceEnumerator);
      }
    }
  }

  /// <summary>
  /// Sets the system master volume for the default render device. Accepts values in range 0.0 - 1.0.
  /// Values outside this range will be clamped.
  /// </summary>
  public void SetWindowsMixerVolume(double vol)
  {
    lock (lockObject)
    {
      if (isDisposed)
      {
        logger.Log(LogLevel.WARNING, "SetWindowsMixerVolume called on disposed object");
        return;
      }

      float level = (float)Math.Clamp(vol, 0.0, 1.0);

      IMMDeviceEnumerator? deviceEnumerator = null;
      IMMDevice? device = null;
      IAudioEndpointVolume? endpointVolume = null;

      try
      {
        // Create device enumerator
        deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

        // Get default render device
        deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);

        // Activate IAudioEndpointVolume on the device
        Guid iidEndpoint = typeof(IAudioEndpointVolume).GUID;
        device.Activate(ref iidEndpoint, 0, IntPtr.Zero, out object epObj);
        endpointVolume = epObj as IAudioEndpointVolume;

        if (endpointVolume == null)
          return;

        // Use Guid.Empty as the event context
        Guid guid = Guid.Empty;
        endpointVolume.SetMasterVolumeLevelScalar(level, ref guid);
      }
      catch (Exception ex)
      {
        logger.Log(LogLevel.ERROR, $"SetWindowsMixerVolume error: {ex.Message}");
      }
      finally
      {
        if (endpointVolume != null)
          Marshal.ReleaseComObject(endpointVolume);
        if (device != null)
          Marshal.ReleaseComObject(device);
        if (deviceEnumerator != null)
          Marshal.ReleaseComObject(deviceEnumerator);
      }
    }
  }

  /// <summary>
  /// Attempts to locate the audio session for the target PID and obtain the meter interface.
  /// </summary>
  /// <returns>True if session was found and meter obtained; otherwise false.</returns>
  private bool TryLocateSession()
  {
    IMMDeviceEnumerator? deviceEnumerator = null;
    IMMDevice? device = null;
    IAudioSessionManager2? sessionManager = null;
    IAudioSessionEnumerator? sessionEnumerator = null;

    try
    {
      logger.Log(LogLevel.DEBUG, $"Attempting to locate session for PID {targetPid}");

      // Step 1: Create device enumerator
      deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

      // Step 2: Get the default audio render device (speakers)
      deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);

      // Step 3: Activate the session manager
      Guid iidSessionManager = typeof(IAudioSessionManager2).GUID;
      device.Activate(ref iidSessionManager, 0, IntPtr.Zero, out object sessionManagerObj);
      sessionManager = (IAudioSessionManager2)sessionManagerObj;

      // Step 4: Get session enumerator
      sessionManager.GetSessionEnumerator(out sessionEnumerator);
      sessionEnumerator.GetCount(out int sessionCount);

      logger.Log(LogLevel.DEBUG, $"Found {sessionCount} audio sessions");

      // Step 5: Enumerate all sessions to find the one matching our PID
      for (int i = 0; i < sessionCount; i++)
      {
        IAudioSessionControl? sessionControl = null;
        IAudioSessionControl2? sessionControl2 = null;

        try
        {
          sessionEnumerator.GetSession(i, out sessionControl2);

          // Step 6: Get the process ID for this session
          sessionControl2.GetProcessId(out int sessionPid);

          // Step 7: Check if this is our target process
          if (sessionPid == targetPid)
          {
            logger.Log(LogLevel.INFO, $"Found audio session for PID {targetPid}");

            // Step 8: Query for IAudioMeterInformation interface
            Guid iidMeterInfo = typeof(IAudioMeterInformation).GUID;
            IntPtr meterPtr = IntPtr.Zero;

            // QueryInterface to get the meter from the session control
            sessionControl = (IAudioSessionControl)sessionControl2;
            Marshal.QueryInterface(Marshal.GetIUnknownForObject(sessionControl), ref iidMeterInfo, out meterPtr);

            if (meterPtr != IntPtr.Zero)
            {
              // Release old meter if exists
              if (meterInfo != null)
              {
                Marshal.ReleaseComObject(meterInfo);
              }

              meterInfo = (IAudioMeterInformation)Marshal.GetObjectForIUnknown(meterPtr);
              Marshal.Release(meterPtr);

              logger.Log(LogLevel.INFO, "Successfully obtained IAudioMeterInformation");
              return true;
            }
          }
        }
        finally
        {
          if (sessionControl2 != null)
            Marshal.ReleaseComObject(sessionControl2);
        }
      }

      logger.Log(LogLevel.DEBUG, $"Session for PID {targetPid} not found");
      return false;
    }
    catch (Exception ex)
    {
      logger.Log(LogLevel.ERROR, $"Error locating session: {ex.Message}");
      return false;
    }
    finally
    {
      // Cleanup COM objects
      if (sessionEnumerator != null)
        Marshal.ReleaseComObject(sessionEnumerator);
      if (sessionManager != null)
        Marshal.ReleaseComObject(sessionManager);
      if (device != null)
        Marshal.ReleaseComObject(device);
      if (deviceEnumerator != null)
        Marshal.ReleaseComObject(deviceEnumerator);
    }
  }

  /// <summary>
  /// Releases the meter COM object.
  /// </summary>
  private void ReleaseMeter()
  {
    if (meterInfo != null)
    {
      Marshal.ReleaseComObject(meterInfo);
      meterInfo = null;
      logger.Log(LogLevel.DEBUG, "Released meter interface");
    }
  }

  /// <summary>
  /// Retrieves all process IDs that currently have audio sessions.
  /// </summary>
  public static IEnumerable<int> GetProcessIds()
  {
    IMMDeviceEnumerator? deviceEnumerator = null;
    IMMDevice? speakers = null;
    IAudioSessionManager2? mgr = null;
    IAudioSessionEnumerator? sessionEnumerator = null;

    try
    {
      // Get the speakers (1st render + multimedia) device
      deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
      deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

      // Activate the session manager
      Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
      speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out object o);
      mgr = (IAudioSessionManager2)o;

      // Enumerate sessions on this device
      mgr.GetSessionEnumerator(out sessionEnumerator);
      sessionEnumerator.GetCount(out int count);

      for (int i = 0; i < count; i++)
      {
        IAudioSessionControl2? ctl = null;
        try
        {
          sessionEnumerator.GetSession(i, out ctl);
          ctl.GetProcessId(out int id);
          yield return id;
        }
        finally
        {
          if (ctl != null)
            Marshal.ReleaseComObject(ctl);
        }
      }
    }
    finally
    {
      if (sessionEnumerator != null)
        Marshal.ReleaseComObject(sessionEnumerator);
      if (mgr != null)
        Marshal.ReleaseComObject(mgr);
      if (speakers != null)
        Marshal.ReleaseComObject(speakers);
      if (deviceEnumerator != null)
        Marshal.ReleaseComObject(deviceEnumerator);
    }
  }

  public void Dispose()
  {
    lock (lockObject)
    {
      if (!isDisposed)
      {
        ReleaseMeter();
        isDisposed = true;
        logger.Log(LogLevel.INFO, "AdaptiveMixer disposed");
      }
    }
  }
}
