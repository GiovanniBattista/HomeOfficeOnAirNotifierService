using HomeOfficeOnAirNotifierService.HardwareChecker;
using HomeOfficeOnAirNotifierService.Publisher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
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
        private ILogger Logger;

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
            this.CanHandleSessionChangeEvent = true;

            this.Logger = new FileLogger();
            this.Logger.InitializeLogger();

            var wi = WindowsIdentity.GetCurrent();
            this.Logger.LogInfo(LOG_TAG, $"Running as: {wi.Name} / SID={wi.User?.Value}");
            this.Logger.LogInfo(LOG_TAG, $"Config: {AppDomain.CurrentDomain.SetupInformation.ConfigurationFile}");
            this.Logger.LogInfo(LOG_TAG, $"BaseDir: {AppDomain.CurrentDomain.BaseDirectory}");
            this.Logger.LogInfo(LOG_TAG, $"CurrentDir: {Environment.CurrentDirectory}");

            this.microphoneChecker = new MicrophoneUsageChecker(Logger);
            this.cameraChecker = new CameraUsageChecker(Logger);

            this.microphoneRegistryChecker = new RegistryCapabilityAccessChecker(Logger, "microphone");
            this.cameraRegistryChecker = new RegistryCapabilityAccessChecker(Logger, "webcam");

            this.statePublisher = new OpenhabOnAirStatePublisher(Logger);

            this.configValidators.Add((IAppConfigValidator) this.statePublisher);
            this.configValidators.Add((IAppConfigValidator) this.microphoneChecker);
            this.configValidators.Add((IAppConfigValidator) this.cameraChecker);
            this.configValidators.Add((IAppConfigValidator) this.microphoneRegistryChecker);
            this.configValidators.Add((IAppConfigValidator) this.cameraRegistryChecker);
        }

        protected override void OnStart(string[] args)
        {
            this.Logger.LogInfo(LOG_TAG, "Service startup");

            StartRetryLoop(30_000);
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
            this.cameraChecker?.Dispose();
            this.microphoneChecker?.Dispose();
            this.cameraRegistryChecker?.Dispose();
            this.microphoneRegistryChecker?.Dispose();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);

            Logger.LogInfo(LOG_TAG, $"Session change: {changeDescription.Reason}");

            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
                case SessionChangeReason.SessionUnlock:
                case SessionChangeReason.ConsoleConnect:
                case SessionChangeReason.RemoteConnect:
                    this.Logger.LogInfo(LOG_TAG, "User session changed!");
                    break;
            }
        }

        public void InitializingServiceComponents()
        {
            this.statePublisher.InitializePublisher();

            this.microphoneChecker.InitializeChecker(statePublisher);
            this.microphoneRegistryChecker.InitializeChecker(statePublisher);
            this.cameraRegistryChecker.InitializeChecker(statePublisher);

            this.microphoneChecker.CheckHardwareForUsage();
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
                        Logger.LogInfo(LOG_TAG, "Service not yet ready (config/device). Will retry.");
                        return;
                    }
                }

                InitializingServiceComponents(); // dein periodisches CheckHardwareForUsage etc.
                monitoringRunning = true;

                Logger.LogInfo(LOG_TAG, "Monitoring started. Stopping retry loop.");
                retryTimer?.Stop();
            }
        }
    }
}
