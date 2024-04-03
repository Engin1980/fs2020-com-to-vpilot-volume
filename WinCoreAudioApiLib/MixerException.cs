using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.WinCoreAudioApiLib
{
  internal class MixerException : Exception
  {
    public MixerException(string? message) : base(message)
    {
    }

    public MixerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
  }
}
