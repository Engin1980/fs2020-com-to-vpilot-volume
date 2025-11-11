using Eng.Com2vPilotVolume.Types;
using ESystem.Asserting;
using ESystem.WPF.KeyHooking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.Com2vPilotVolume.Services
{
  public class KeyHookService : BaseService
  {
    public delegate void VolumeChangeRequestedHandler(double changeAmount, bool isRelative);
    public event VolumeChangeRequestedHandler? VolumeChangeRequested;

    private record KeyHookAction(KeyboardMappingEntry Entry, object KeyBlock, Action Action);

    private readonly KeyboardMappingsConfig settings;
    private readonly KeyHook keyHook;
    private readonly List<KeyHookAction> keyHookActions = [];

    public KeyHookService(KeyboardMappingsConfig settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      this.settings = settings;
      this.keyHook = new KeyHook();
    }

    private void InitHooks()
    {
      logger.Info("Analysing key shortcuts...");
      keyHookActions.Clear();
      foreach (var entry in settings)
      {
        object? keyBlock = DecodeKeyBlock(entry.Keys);
        Action? action = DecodeAction(entry);
        if (keyBlock == null || action == null) continue;

        keyHookActions.Add(new(entry, keyBlock, action));
      }
    }

    private Action? DecodeAction(KeyboardMappingEntry entry)
    {
      Action? ret;
      if (entry.Set.HasValue)
        ret = () => VolumeChangeRequested?.Invoke(entry.Set.Value, false);
      else if (entry.Adjust.HasValue)
        ret = () => VolumeChangeRequested?.Invoke(entry.Adjust.Value, true);
      else
      {
        logger.Error($"Keyboard mapping entry '{entry.Keys}' has neither Set nor Adjust value. Action will be skipped.");
        ret = null;
      }
      return ret;
    }

    private object? DecodeKeyBlock(string keys)
    {
      object? ret = null;

      logger.Debug($"Decoding key block from string '{keys}'");
      try
      {
        ret = KeyShortcut.Parse(keys);
        logger.Debug($"Decoded key shortcut: {ret}");
      }
      catch (Exception)
      {
        // intentionally blank
      }
      if (ret == null)
      {
        try
        {
          ret = KeyChord.Parse(keys);
          logger.Debug($"Decoded key chord: {ret}");
        }
        catch (Exception)
        {
          // intentionally blank
        }
      }

      if (ret == null)
      {
        logger.Error($"Could not decode key block from string '{keys}' Key mapping will be skipped.");
      }

      return ret;
    }

    protected async override Task StartInternalAsync()
    {
      Task t = Task.Run(() =>
      {
        InitHooks();

        foreach (var kha in keyHookActions)
        {
          logger.Info($"Registering key hook for '{kha.Entry.Keys}'");
          if (kha.KeyBlock is KeyChord kc)
            keyHook.RegisterKeyChord(kc, kha.Action);
          else if (kha.KeyBlock is KeyShortcut ks)
            keyHook.RegisterKeyShortcut(ks, kha.Action);
          else
            throw new NotImplementedException("Unknown key block type registered in key hook.");
        }
      });
      await t;
    }

    protected async override Task StopInternalAsync()
    {
      Task t = Task.Run(() => this.keyHook.UnregisterAll());
      await t;
    }
  }
}
