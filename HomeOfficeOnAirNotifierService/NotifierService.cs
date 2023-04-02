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
using System.Timers;

namespace HomeOfficeOnAirNotifierService
{
    public partial class NotifierService : ServiceBase
    {
        private static string LOG_TAG = "Service";

        private IHardwareUsageChecker microphoneChecker;
        private IHardwareUsageChecker cameraChecker;
        private IHardwareUsageChecker microphoneRegistryChecker;
        private IHardwareUsageChecker cameraRegistryChecker;

        private IOnAirStatePublisher statePublisher;
        private ILogger logger;

        private System.Timers.Timer registryCheckerTimer;

        public NotifierService()
        {
            InitializeComponent();
            this.logger = new FileLogger();

            this.logger.LogInfo(LOG_TAG, "Service startup");

            this.microphoneChecker = new MicrophoneUsageChecker();
            this.cameraChecker = new CameraUsageChecker();

            this.microphoneRegistryChecker = new RegistryCapabilityAccessChecker("microphone");
            this.cameraRegistryChecker = new RegistryCapabilityAccessChecker("webcam");

            this.statePublisher = new OpenhabOnAirStatePublisher();
        }

        protected override void OnStart(string[] args)
        {
            this.microphoneChecker.InitializeChecker(statePublisher, logger);
            this.microphoneRegistryChecker.InitializeChecker(statePublisher, logger);
            this.cameraChecker.InitializeChecker(statePublisher, logger);
            this.cameraRegistryChecker.InitializeChecker(statePublisher, logger);

            this.microphoneChecker.CheckHardwareForUsage();
            this.cameraChecker.CheckHardwareForUsage();

            this.microphoneRegistryChecker.CheckHardwareForUsage();
            this.cameraRegistryChecker.CheckHardwareForUsage();


            this.registryCheckerTimer = new System.Timers.Timer(5 * 1000); // check every 5 seconds
            this.registryCheckerTimer.Elapsed += RegistryCheckerTimerElapsed;
            this.registryCheckerTimer.Start();
        }

        private void RegistryCheckerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.microphoneRegistryChecker.CheckHardwareForUsage();
            this.cameraRegistryChecker.CheckHardwareForUsage();
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
