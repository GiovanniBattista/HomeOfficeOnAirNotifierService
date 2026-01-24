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

        private readonly IHardwareUsageChecker microphoneChecker;
        private readonly IHardwareUsageChecker cameraChecker;
        private readonly IHardwareUsageChecker microphoneRegistryChecker;
        private readonly IHardwareUsageChecker cameraRegistryChecker;

        private IOnAirStatePublisher statePublisher;
        private ILogger logger;

        // TODO: Those two vars are currently unused => remove them
        private bool micRegistryCheckerInitSuccessfully;
        private bool cameraRegistryCheckerInitSuccessfully;

        private readonly List<IAppConfigValidator> configValidators = new List<IAppConfigValidator>();

        // reload configuration and retry
        private readonly object initLock = new object();
        private System.Timers.Timer retryTimer;
        private bool monitoringRunning;

        public NotifierService()
        {
            InitializeComponent();

            this.logger = new FileLogger();
            this.logger.InitializeLogger();

            this.microphoneChecker = new MicrophoneUsageChecker(logger);
            this.cameraChecker = new CameraUsageChecker(logger);

            this.microphoneRegistryChecker = new RegistryCapabilityAccessChecker(logger, "microphone");
            this.cameraRegistryChecker = new RegistryCapabilityAccessChecker(logger, "webcam");

            this.statePublisher = new OpenhabOnAirStatePublisher(logger);

            this.configValidators.Add((IAppConfigValidator) this.statePublisher);
            this.configValidators.Add((IAppConfigValidator) this.microphoneChecker);
            this.configValidators.Add((IAppConfigValidator) this.cameraChecker);
            this.configValidators.Add((IAppConfigValidator) this.microphoneRegistryChecker);
            this.configValidators.Add((IAppConfigValidator) this.cameraRegistryChecker);
        }

        protected override void OnStart(string[] args)
        {
            this.logger.LogInfo(LOG_TAG, "Service startup");

            StartRetryLoop(30_000);
        }

        public void InitializingServiceComponents()
        {
            this.statePublisher.InitializePublisher();

            this.microphoneChecker.InitializeChecker(statePublisher);
            this.microphoneRegistryChecker.InitializeChecker(statePublisher);
            this.cameraRegistryChecker.InitializeChecker(statePublisher);

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

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
        }

        private void StartRetryLoop(double intervalMs)
        {
            retryTimer = new System.Timers.Timer(intervalMs);
            retryTimer.AutoReset = true;
            retryTimer.Elapsed += (_, __) => TryInitializeAndStartMonitoring();
            retryTimer.Start();

            // gleich beim Start einmal versuchen
            TryInitializeAndStartMonitoring();
        }

        private void TryInitializeAndStartMonitoring()
        {
            lock (initLock)
            {
                if (monitoringRunning) 
                    return;

                AppConfig.Reload();
                foreach (IAppConfigValidator validator in configValidators)
                {
                    validator.UpdateBoundProperties();
                    if (!validator.IsConfigValid())
                    {
                        logger.LogInfo(LOG_TAG, "Service not yet ready (config/device). Will retry.");
                        return;
                    }
                }

                InitializingServiceComponents(); // dein periodisches CheckHardwareForUsage etc.
                monitoringRunning = true;

                logger.LogInfo(LOG_TAG, "Monitoring started. Stopping retry loop.");
                retryTimer?.Stop();
            }
        }
    }
}
