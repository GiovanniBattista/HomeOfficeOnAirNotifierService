using HomeOfficeOnAirNotifierService.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService.HardwareChecker
{
    internal interface IHardwareUsageChecker
    {
        void InitializeChecker(Publisher.IOnAirStatePublisher statePublisher, ILogger logger);

        void CheckHardwareForUsage();
    }

    internal abstract class HardwareUsageChecker : IHardwareUsageChecker
    {

        protected ILogger Logger
        {
            get; set;
        }

        protected IOnAirStatePublisher StatePublisher
        {
            get; set;
        }

        public virtual void InitializeChecker(IOnAirStatePublisher statePublisher, ILogger logger)
        {
            this.Logger = logger;
            this.StatePublisher = statePublisher;
        }

        public abstract void CheckHardwareForUsage();

        protected void LogInfo(string tag, string message)
        {
            this.Logger.LogInfo(tag, message);
        }
    }
}
