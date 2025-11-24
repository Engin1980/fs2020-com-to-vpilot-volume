// See https://aka.ms/new-console-template for more information
using Eng.WinCoreAudioApiLib;

Console.WriteLine("Hello, World!");

Mixer m = new Mixer();

var pids = m.GetProcessIds();

var vols = pids.Select(pid => new { pid, vol = m.GetVolume(pid) });

// get project title/name from pid
var pidNames = System.Diagnostics.Process.GetProcesses()
    .Where(p => vols.Any(v => v.pid == p.Id))
    .ToDictionary(p => p.Id, p => p.ProcessName);

Console.WriteLine("Master volume: " + m.GetMasterVolume());
m.SetMasterVolume(m.GetMasterVolume() * 0.5);

foreach (var v in vols)
{
    Console.WriteLine($"PID: {v.pid}, Name: {pidNames[v.pid]}, Volume: {v.vol}");
}

