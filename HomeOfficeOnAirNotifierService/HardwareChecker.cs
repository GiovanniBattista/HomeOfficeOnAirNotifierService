using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService.HardwareChecker
{
    internal interface IHardwareChecker
    {
        void InitializeChecker(Publisher.IOnAirStatePublisher statePublisher);

        void CheckHardwareForSessions();
    }
}
