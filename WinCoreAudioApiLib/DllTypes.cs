using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WinCoreAudioApiLib
{

  [ComImport]
  [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
  internal class MMDeviceEnumerator
  {
  }

  internal enum EDataFlow
  {
    eRender,
    eCapture,
    eAll,
    EDataFlow_enum_count
  }

  internal enum ERole
  {
    eConsole,
    eMultimedia,
    eCommunications,
    ERole_enum_count
  }

  [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IMMDeviceEnumerator
  {
    int NotImpl1();

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

    // the rest is not implemented
  }

  [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IMMDevice
  {
    [PreserveSig]
    int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

    // the rest is not implemented
  }

  [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IAudioSessionManager2
  {
    int NotImpl1();
    int NotImpl2();

    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

    // the rest is not implemented
  }

  [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IAudioSessionEnumerator
  {
    [PreserveSig]
    int GetCount(out int SessionCount);

    [PreserveSig]
    int GetSession(int SessionCount, out IAudioSessionControl2 Session);
  }

  [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IAudioSessionControl
  {
    int NotImpl1();

    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    // the rest is not implemented
  }

  public enum AudioSessionState
  {
    Inactive = 0,
    Active = 1,
    Expired = 2
  }

  public enum AudioSessionDisconnectReason
  {
    DisconnectReasonDeviceRemoval = 0,
    DisconnectReasonServerShutdown = 1,
    DisconnectReasonFormatChanged = 2,
    DisconnectReasonSessionLogoff = 3,
    DisconnectReasonSessionDisconnected = 4,
    DisconnectReasonExclusiveModeOverride = 5
  }

  [Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IAudioSessionEvents
  {
    void OnDisplayNameChanged([MarshalAs(UnmanagedType.LPWStr)] string NewDisplayName, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    void OnIconPathChanged([MarshalAs(UnmanagedType.LPWStr)] string NewIconPath, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    void OnSimpleVolumeChanged(float NewVolume, bool NewMute, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    void OnChannelVolumeChanged(int ChannelCount, IntPtr NewChannelVolumeArray, int ChangedChannel, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    void OnGroupingParamChanged([MarshalAs(UnmanagedType.LPStruct)] Guid NewGroupingParam, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
    void OnStateChanged(AudioSessionState NewState);
    void OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason);
  }

  [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IAudioSessionControl2
  {
    // IAudioSessionControl
    [PreserveSig]
    int GetState(out AudioSessionState pRetVal);

    [PreserveSig]
    int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

    [PreserveSig]
    int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

    [PreserveSig]
    int GetGroupingParam(out Guid pRetVal);

    [PreserveSig]
    int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

    [PreserveSig]
    int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

    [PreserveSig]
    int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

    // IAudioSessionControl2
    [PreserveSig]
    int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

    [PreserveSig]
    int GetProcessId(out int pRetVal);

    [PreserveSig]
    int IsSystemSoundsSession();

    [PreserveSig]
    int SetDuckingPreference(bool optOut);
  }

  [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface ISimpleAudioVolume
  {
    [PreserveSig]
    int SetMasterVolume(float fLevel, ref Guid EventContext);

    [PreserveSig]
    int GetMasterVolume(out float pfLevel);

    [PreserveSig]
    int SetMute(bool bMute, ref Guid EventContext);

    [PreserveSig]
    int GetMute(out bool pbMute);
  }
}
