using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.Com2vPilotVolume.Types
{
  internal class ConfigLoadFailedException : Exception
  {
    public ConfigLoadFailedException(string key)
    : base($"Failed to load config from configuration file - key '{key}'.") { }
  }
}
