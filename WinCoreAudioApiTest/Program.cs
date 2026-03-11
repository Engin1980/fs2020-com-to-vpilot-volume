/* Adaptive Mixer */

// See https://aka.ms/new-console-template for more information
using Eng.WinCoreAudioApiLib;

Console.WriteLine("Hello, World!");

var processManager = new ProcessManager();

var processes = processManager.GetRunningProcesses();
processes.ForEach(p => Console.WriteLine($"{p.PID} - {p.Name}"));

Console.WriteLine(" * * * ");

var edges = processes.Where(q => q.Name.ToLower().Contains("vlc")).ToList();

Dictionary<ProcessNamePid, AdaptiveMixer> dct = [];

foreach (var process in edges)
{
  var am = new AdaptiveMixer(process.PID);
  dct[process] = am;
}

AudioLeveler al = new();
al.BaseVolume = 0.2;

while (true)
{
  Console.WriteLine("* * * * *");
  foreach (var entry in dct)
  {
    string res;
    if (entry.Value.IsSessionAvailable)
    {
      res = $"{entry.Value.GetCurrentPeak().ToString("N3")} - {entry.Value.GetWindowsMixerVolume()}";
    }
    else
      res = "N/A";

    Console.WriteLine($"{entry.Key.PID} - {entry.Key.Name} - {res}");
  }

  var key = dct.Keys.FirstOrDefault(q => q.Name.ToLower().Contains("vlc"));
  if (key != null)
  {
    var entry = dct[key];

    var currentLevel = entry.GetCurrentPeak();
    al.AdjustSuggestedVolume(currentLevel);
    var currentVolume = entry.GetWindowsMixerVolume();
    var newVolume = al.SuggestedVolume;

    if (Math.Abs(currentVolume - newVolume) > 0.01)
    {
      //Console.WriteLine($"Adjusted volume for {key.Name} (PID {key.PID}): Level={currentLevel:N3}, CurrentVolume={currentVolume:N3}, NewVolume={newVolume:N3}");
      entry.SetWindowsMixerVolume(newVolume);
    }
    else
    {
      //Console.WriteLine($"No adjustment needed for {key.Name} (PID {key.PID}): Level={currentLevel:N3}, CurrentVolume={currentVolume:N3}");
    }
  }
  else
  {
    Console.WriteLine("No pid");
  }

  Thread.Sleep(500);
}

class AudioLeveler
{
  // Thresholds and parameters -- tweak to taste
  private const double QUIET_THRESHOLD = 0.01;           // treat near-silence as idle
  private const int LONG_WINDOW_LENGTH = 20;
  private const double MAX_VOLUME_INCREASE_PER_STEP = 0.01;
  private const double TARGET_LEVEL = 0.5;
  private const double LONG_RELATIVE_DETECTION_THRESHOLD = 0.1;
  private const double AGGRESIVE_LEVEL_CHANGE_RANGE = 0.1;
  private readonly Queue<double> longQueue = new(LONG_WINDOW_LENGTH);
  private readonly object sync = new();

  private double ratio = 1.0;
  public double BaseVolume { get; set; } = 0.2;
  public double SuggestedVolume => BaseVolume * ratio;

  public void AdjustSuggestedVolume(double currentLevel)
  {
    double prevRatio = this.ratio;
    double adjustedLevel = currentLevel * prevRatio;
    double prevSuggestedVolume = this.SuggestedVolume;
    bool isHandled = false;
    string updMsg = "???";
    lock (sync)
    {
      // sanitize input
      if (double.IsNaN(currentLevel) || double.IsInfinity(currentLevel))
      {
        updMsg = "No data, no adjustment";
        ratio = 1.0;
        isHandled = true;
      }

      // Quiet reset: if channel is silent, clear history
      if (!isHandled && currentLevel < QUIET_THRESHOLD)
      {
        if (longQueue.Count != 0)
        {
          updMsg = "Becoming quiet, no adjustment";
          longQueue.Clear();
        }
        else
        {
          updMsg = "Keeping quiet";
        }
        ratio = 1.0;
        isHandled = true;
      }
      else if (!isHandled)
        EnqueueLimited(longQueue, adjustedLevel, LONG_WINDOW_LENGTH);

      double absDiff = adjustedLevel - TARGET_LEVEL;
      if (!isHandled && absDiff > AGGRESIVE_LEVEL_CHANGE_RANGE)
      {
        double relDiff = 1 / (adjustedLevel / TARGET_LEVEL);
        ratio = ratio * relDiff;
        updMsg = $"Too loud (diff={absDiff:N3})";
        isHandled = true;
      }

      if (!isHandled && longQueue.Count > 5)
      {
        double avg = longQueue.Average();

        double avgDiff = TARGET_LEVEL - avg;
        if (avgDiff > LONG_RELATIVE_DETECTION_THRESHOLD && longQueue.All(q=>q < TARGET_LEVEL))
        {
          if (SuggestedVolume < BaseVolume)
          {
            ratio += Math.Min(1, MAX_VOLUME_INCREASE_PER_STEP);
            updMsg = $"Increasing back as avg={avg:N3}, diff={avgDiff:N3}";
            isHandled = true;
          }
        }
      }

      if (!isHandled)
        updMsg = "No adjustment";

      Console.WriteLine($"BaseV={BaseVolume}, Old/New SugV={prevSuggestedVolume:N3}/{SuggestedVolume:N3}, old/new R:{prevRatio:N3}/{ratio:N3}, cur/adj={currentLevel:N3}/{adjustedLevel:N3}, msg: {updMsg}");
    } // lock
  }

  // --- helpers ---
  private static void EnqueueLimited(Queue<double> q, double value, int capacity)
  {
    while (q.Count >= capacity)
      q.Dequeue();
    q.Enqueue(value);
  }
}



//// Get all process IDs with audio sessions
//var pids = AdaptiveMixer.GetProcessIds().ToList();

//Console.WriteLine($"Found {pids.Count} processes with audio sessions:");

//// Get process names for display
//var pidNames = System.Diagnostics.Process.GetProcesses()
//    .Where(p => pids.Contains(p.Id))
//    .ToDictionary(p => p.Id, p => p.ProcessName);

//// Monitor peak levels for each process
//foreach (var pid in pids)
//{
//  using var mixer = new AdaptiveMixer(pid);

//  if (mixer.IsSessionAvailable)
//  {
//    float peak = mixer.GetCurrentPeak();
//    string name = pidNames.ContainsKey(pid) ? pidNames[pid] : "Unknown";
//    Console.WriteLine($"PID: {pid}, Name: {name}, Peak Level: {peak:F3}");
//  }
//  else
//  {
//    string name = pidNames.ContainsKey(pid) ? pidNames[pid] : "Unknown";
//    Console.WriteLine($"PID: {pid}, Name: {name}, Session not available");
//  }
//}



/* Classic mixer: */

//// See https://aka.ms/new-console-template for more information
//using Eng.WinCoreAudioApiLib;

//Console.WriteLine("Hello, World!");

//Mixer m = new Mixer();

//var pids = m.GetProcessIds();

//var vols = pids.Select(pid => new { pid, vol = m.GetVolume(pid) });

//// get project title/name from pid
//var pidNames = System.Diagnostics.Process.GetProcesses()
//    .Where(p => vols.Any(v => v.pid == p.Id))
//    .ToDictionary(p => p.Id, p => p.ProcessName);

//Console.WriteLine("Master volume: " + m.GetMasterVolume());
//m.SetMasterVolume(m.GetMasterVolume() * 0.5);

//m.SetVolume(12860, 0.2);

//foreach (var v in vols)
//{
//    Console.WriteLine($"PID: {v.pid}, Name: {pidNames[v.pid]}, Volume: {v.vol}");
//}

