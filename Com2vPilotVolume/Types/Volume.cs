using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace eng.com2vPilotVolume.Types
{
  public struct Volume
  {
    private readonly double value;

    public Volume(double newValue)
    {
      this.value = Math.Max(0, Math.Min(1, newValue));
    }

    public static implicit operator double(Volume value) => value.value;
    public static implicit operator Volume(double value) => new Volume(value);

    public override string ToString() => $"{value * 100:0.00} %";
  }
}
