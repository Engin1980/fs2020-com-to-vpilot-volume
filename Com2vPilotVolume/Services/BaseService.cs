using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eng.Com2vPilotVolume.Services
{
  public abstract class BaseService
  {
    protected readonly ESystem.Logging.Logger logger;

    public BaseService()
    {
      this.logger = ESystem.Logging.Logger.Create(this, this.GetType().Name);
    }

    public async Task StartAsync()
    {
      logger.Info("Starting...");
      await StartInternalAsync();
      logger.Info("Started.");
    }

    protected abstract Task StartInternalAsync();

    protected async Task StopAsync()
    {
      logger.Info("Stopping...");
      await StopInternalAsync();
      logger.Info("Stopped.");
    }

    protected abstract Task StopInternalAsync();
  }
}
