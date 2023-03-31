using HomeOfficeOnAirNotifierService.HardwareChecker;
using HomeOfficeOnAirNotifierService.Publisher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{
    public partial class NotifierService : ServiceBase
    {
        private static string LOG_TAG = "Service";

        private IHardwareUsageChecker microphoneChecker;
        private IHardwareUsageChecker cameraChecker;

        private IOnAirStatePublisher statePublisher;
        private ILogger logger;

        public NotifierService()
        {
            InitializeComponent();
            this.logger = new FileLogger();

            this.logger.LogInfo(LOG_TAG, "Service startup");

            this.microphoneChecker = new MicrophoneUsageChecker();
            this.cameraChecker = new CameraUsageChecker();

            this.statePublisher = new OpenhabOnAirStatePublisher();
        }

        protected override void OnStart(string[] args)
        {
            this.microphoneChecker.InitializeChecker(statePublisher, logger);
            //this.cameraChecker.InitializeChecker(statePublisher);

            this.microphoneChecker.CheckHardwareForUsage();
            //this.cameraChecker.CheckHardwareForSessions();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
        }
    }
}
