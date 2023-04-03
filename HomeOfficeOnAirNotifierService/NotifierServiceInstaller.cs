using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{

    [RunInstaller(true)]
    public partial class NotifierServiceInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller notifierServiceInstaller;

        public NotifierServiceInstaller()
        {
            InitializeComponent();

            // Instantiate installers for process and services.
            processInstaller = new ServiceProcessInstaller();
            notifierServiceInstaller = new ServiceInstaller();

            // The services run under the system account.
            processInstaller.Account = ServiceAccount.LocalSystem;

            // The services are started automatically.
            notifierServiceInstaller.StartType = ServiceStartMode.Automatic;

            // ServiceName must equal those on ServiceBase derived classes.
            notifierServiceInstaller.ServiceName = "Home Office On Air Notifier Service";

            // Add installers to collection. Order is not important.
            Installers.Add(notifierServiceInstaller);
            Installers.Add(processInstaller);
        }
    }
}
