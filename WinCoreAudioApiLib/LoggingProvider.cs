using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.WinCoreAudioApiLib
{
  public static class LoggingProvider
  {
    private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(q =>
    {
      q.AddFilter(x => true).AddConsole();
    });

    public static ILogger CreateLogger<T>() => loggerFactory.CreateLogger<T>();
  }
}
