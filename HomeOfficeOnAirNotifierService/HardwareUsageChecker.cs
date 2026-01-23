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
        bool InitializeChecker(Publisher.IOnAirStatePublisher statePublisher, ILogger logger);

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

        public virtual bool InitializeChecker(IOnAirStatePublisher statePublisher, ILogger logger)
        {
            this.Logger = logger;
            this.StatePublisher = statePublisher;

            return true;
        }

        public abstract void CheckHardwareForUsage();

        protected ILogger getLogger()
        {
            return this.Logger;
        }
    }
}
