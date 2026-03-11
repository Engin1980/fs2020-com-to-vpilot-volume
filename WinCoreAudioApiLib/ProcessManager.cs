using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.WinCoreAudioApiLib
{
  public record ProcessNamePid(int PID, string Name);
  public interface IProcessManager
  {
    List<ProcessNamePid> GetRunningProcesses();
  }
  public class ProcessManager : IProcessManager
  {
    /// <summary>
    /// Returns a list of currently running processes as (PID, Name) records.
    /// The method handles transient access exceptions and disposes Process objects.
    /// </summary>
    public List<ProcessNamePid> GetRunningProcesses()
    {
      var result = new List<ProcessNamePid>();

      try
      {
        var processes = Process.GetProcesses();
        foreach (var p in processes)
        {
          try
          {
            // Accessing ProcessName or Id can throw for system processes; skip on error
            result.Add(new ProcessNamePid(p.Id, p.ProcessName));
          }
          catch
          {
            // ignore processes we cannot query
          }
          finally
          {
            // Ensure native handles are released
            try { p.Dispose(); } catch { }
          }
        }

        // Order results for stable output
        return result.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(r => r.PID)
                     .ToList();
      }
      catch
      {
        // On unexpected failures return empty list rather than throwing
        return new List<ProcessNamePid>();
      }
    }
  }
}
