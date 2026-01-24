using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{

    [RunInstaller(true)]
    public partial class NotifierServiceInstaller : System.Configuration.Install.Installer
    {
        public NotifierServiceInstaller()
        {
            InitializeComponent();

            // Instantiate installers for process and services.
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalSystem
            };

            var serviceInstaller = new ServiceInstaller
            {
                StartType = ServiceStartMode.Automatic,

                // Wichtig: interner ServiceName besser ohne Leerzeichen
                ServiceName = "HomeOfficeOnAirNotifierService",
                DisplayName = "Home Office On Air Notifier Service",
                Description = "Notifies about microphone/camera usage in home office."
            };

            var eventLogInstaller = new EventLogInstaller
            {
                Log = "Application",
                Source = "HomeOfficeOnAirNotifierService"
            };

            // Add installers to collection. Order is not important.
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
            Installers.Add(eventLogInstaller);
        }
    }
}
