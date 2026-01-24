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
        private const string LOG_TAG = "NotifierService";

        private IHardwareUsageChecker microphoneChecker;
        private IHardwareUsageChecker cameraChecker;
        private IHardwareUsageChecker microphoneRegistryChecker;
        private IHardwareUsageChecker cameraRegistryChecker;

        private IOnAirStatePublisher statePublisher;
        private ILogger logger;

        private bool micRegistryCheckerInitSuccessfully;
        private bool cameraRegistryCheckerInitSuccessfully;

        private System.Timers.Timer registryCheckerTimer;

        public NotifierService()
        {
            InitializeComponent();
            this.logger = new FileLogger();
            this.logger.InitializeLogger();

            this.logger.LogInfo(LOG_TAG, "Service startup");

            this.microphoneChecker = new MicrophoneUsageChecker();
            this.cameraChecker = new CameraUsageChecker();

            this.microphoneRegistryChecker = new RegistryCapabilityAccessChecker("microphone");
            this.cameraRegistryChecker = new RegistryCapabilityAccessChecker("webcam");

            this.statePublisher = new OpenhabOnAirStatePublisher(logger);
        }

        protected override void OnStart(string[] args)
        {
            if (!this.statePublisher.validateConfig())
            {
                this.logger.LogInfo(LOG_TAG, "Startup aborted due to invalid publisher configuration.");
                this.ExitCode = 1066;  // "The service has returned a service-specific error code."
                this.Stop();
                return;
            }

            var successfullyChecked = this.microphoneChecker.InitializeChecker(statePublisher, logger);
            if (!successfullyChecked)
            {
                this.logger.LogInfo(LOG_TAG, "Startup aborted due to microphone checker initialization failure.");
                this.ExitCode = 1066;  // "The service has returned a service-specific error code."
                this.Stop();
                return;
            }

            this.micRegistryCheckerInitSuccessfully = this.microphoneRegistryChecker.InitializeChecker(statePublisher, logger);
            this.cameraRegistryCheckerInitSuccessfully = this.cameraRegistryChecker.InitializeChecker(statePublisher, logger);

            this.microphoneChecker.CheckHardwareForUsage();

            //this.cameraChecker.InitializeChecker(statePublisher, logger);
            //this.cameraChecker.CheckHardwareForUsage();


            //if (this.micRegistryCheckerInitSuccessfully)
            //    this.microphoneRegistryChecker.CheckHardwareForUsage();
            //if (this.cameraRegistryCheckerInitSuccessfully)
            //    this.cameraRegistryChecker.CheckHardwareForUsage();

            /*
            if (this.micRegistryCheckerInitSuccessfully || this.cameraRegistryCheckerInitSuccessfully)
            {
                this.registryCheckerTimer = new System.Timers.Timer(5 * 1000); // check every 5 seconds
                this.registryCheckerTimer.Elapsed += RegistryCheckerTimerElapsed;
                this.registryCheckerTimer.Start();
            }
            */
        }

        private void RegistryCheckerTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (this.micRegistryCheckerInitSuccessfully)
                this.microphoneRegistryChecker.CheckHardwareForUsage();
            if (this.cameraRegistryCheckerInitSuccessfully)
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
