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
    public partial class Service1 : ServiceBase
    {
        private IHardwareChecker microphoneChecker;
        private IHardwareChecker cameraChecker;

        private IOnAirStatePublisher statePublisher;

        public Service1()
        {
            InitializeComponent();

            this.microphoneChecker = new MicrophoneChecker();
            this.cameraChecker = new CameraChecker();

            this.statePublisher = new OpenhabOnAirStatePublisher();
        }

        protected override void OnStart(string[] args)
        {
            this.microphoneChecker.InitializeChecker(statePublisher);
            //this.cameraChecker.InitializeChecker(statePublisher);

            this.microphoneChecker.CheckHardwareForSessions();
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
