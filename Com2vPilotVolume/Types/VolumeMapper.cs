using ESystem.Logging;
using ESystem.Asserting;
using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ESimConnect.Definitions.SimEvents;

namespace Eng.Com2vPilotVolume.Types
{
  internal class VolumeMapper
  {
    public record Settings(double[][] Map, double MinimumThreshold);
    public readonly Logger logger;
    private record VolumeMap(double Input, double Output);
    private readonly List<VolumeMap> volumeMapping;
    private readonly double minimumThreshold;


    public VolumeMapper(VolumeMappingConfig settings)
    {
      EAssert.Argument.IsNotNull(settings, nameof(settings));
      EAssert.Argument.IsNotNull(settings.Map, nameof(settings) + "." + nameof(settings.Map));

      this.logger = Logger.Create(this, nameof(VolumeMapper));
      this.logger.Log(LogLevel.DEBUG, $"Object construction requested.");

      this.volumeMapping = CreateMapping(settings.Map);
      this.minimumThreshold = settings.MinimumThreshold;
    }

    private class RecordEqualityComparer : EqualityComparer<double[]>
    {
      public override bool Equals(double[]? x, double[]? y) => x![0] == y![0];

      public override int GetHashCode([DisallowNull] double[] obj) => obj.GetHashCode();
    }

    private List<VolumeMap> CreateMapping(double[][] volumeMapping)
    {
      EAssert.Argument.IsNotNull(volumeMapping, nameof(volumeMapping));
      EAssert.Argument.IsTrue(volumeMapping.All(q => q.Length == 2), nameof(volumeMapping), "All values of 'volumeMapping' must be numeric tuples");
      EAssert.Argument.IsTrue(volumeMapping.All(q => q[0] >= 0 && q[0] <= 100), nameof(volumeMapping), "All input values of 'volumeMapping' must be between 0 .. 100");
      EAssert.Argument.IsTrue(volumeMapping.All(q => q[1] >= 0 && q[1] <= 100), nameof(volumeMapping), "All output values of 'volumeMapping' must be between 0 .. 100");

      List<VolumeMap> ret = volumeMapping
        .DistinctBy(q => q[0])
        .Select(q => new VolumeMap(q[0], q[1]))
        .OrderBy(q => q.Input)
        .ToList();
      if (ret.Count == 0 || ret[0].Input != 0)
        ret.Insert(0, new VolumeMap(0, 0));
      if (ret.Last().Input != 100)
        ret.Add(new VolumeMap(100, 100));
      return ret;
    }

    internal Volume Map(Volume inputVolume)
    {
      Volume ret;

      this.logger.Log(LogLevel.DEBUG, $"Requested mapping for {inputVolume}.");
      double d = 100 * (double)inputVolume;

      VolumeMap lower = this.volumeMapping.Where(q => q.Input <= d).OrderBy(q => q.Input).Last();
      if (lower.Input == d)
      {
        d = lower.Output;
      }
      else
      {
        VolumeMap upper = this.volumeMapping.Where(q => q.Input >= d).OrderBy(q => q.Input).First();
        double dx = upper.Input - lower.Input;
        double dy = upper.Output - lower.Output;
        double pt = ((d - lower.Input) / dx);
        d = lower.Output + dy * pt;
      }

      this.logger.Log(LogLevel.DEBUG, $"Initially mapped {inputVolume} into {d}.");

      if (d < this.minimumThreshold)
      {
        this.logger.Log(LogLevel.DEBUG, $"Minimum thresholding applied on {d} < {this.minimumThreshold}.");
        d = 0;
      }


      ret = d / 100d;
      this.logger.Log(LogLevel.INFO, $"Mapped {inputVolume} into {ret}.");
      return ret;
    }
  }
}
