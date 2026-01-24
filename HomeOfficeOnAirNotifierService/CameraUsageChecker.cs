using HomeOfficeOnAirNotifierService.Publisher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService.HardwareChecker
{
    internal class CameraUsageChecker : HardwareUsageChecker, IAppConfigValidator
    {
        private const string LOG_TAG = "CameraUsageChecker";

        public CameraUsageChecker(ILogger logger) : base(logger)
        {

        }

        void IAppConfigValidator.UpdateBoundProperties()
        {
            // nothing to do here
        }

        bool IAppConfigValidator.IsConfigValid()
        {
            return true;
        }



        public override bool InitializeChecker(IOnAirStatePublisher statePublisher)
        {
            base.InitializeChecker(statePublisher);

            /*
            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery(
            "SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%Camera%'"
            //"SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 AND TargetInstance ISA 'Win32_DeviceChangeEvent ' AND TargetInstance.Name LIKE '%WebCam%'"
            //"SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_PerfRawData_Counters_EventTracingforWindowsSession' AND TargetInstance.Name LIKE '%WebCam%'"
            );
            watcher.Query = query;
            watcher.Start();

            watcher.EventArrived += new EventArrivedEventHandler(CameraEventArrived);
            */

            return true;
        }

        public override void CheckHardwareForUsage()
        {
            // nothing to do here for the moment
        }

        static void CameraEventArrived(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("Camera device change event detected.");
        }


    }
}
