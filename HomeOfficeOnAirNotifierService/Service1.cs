using HomeOfficeOnAirNotifierService.HardwareChecker;
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
    public partial class Service1 : ServiceBase
    {
        private IHardwareChecker microphoneChecker;

        public Service1()
        {
            InitializeComponent();

            this.microphoneChecker = new MicrophoneChecker();
        }

        protected override void OnStart(string[] args)
        {
            this.microphoneChecker.InitializeChecker();

            this.microphoneChecker.CheckHardwareForSessions();
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
