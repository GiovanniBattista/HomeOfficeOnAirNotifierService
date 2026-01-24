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
        bool InitializeChecker(Publisher.IOnAirStatePublisher statePublisher);

        void CheckHardwareForUsage();
    }

    internal abstract class HardwareUsageChecker : IHardwareUsageChecker
    {

        public HardwareUsageChecker(ILogger logger)
        {
            this.Logger = logger;
        }

        protected ILogger Logger
        {
            get; set;
        }

        protected IOnAirStatePublisher StatePublisher
        {
            get; set;
        }

        public virtual bool InitializeChecker(IOnAirStatePublisher statePublisher)
        {
            this.StatePublisher = statePublisher;

            return true;
        }

        public abstract void CheckHardwareForUsage();
    }
}
