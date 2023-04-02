using HomeOfficeOnAirNotifierService.HardwareChecker;
using HomeOfficeOnAirNotifierService.Publisher;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{
    internal class RegistryCapabilityAccessChecker : HardwareUsageChecker
    {
        private static string LOG_TAG = "RegistryCapabilityAccessChecker";

        private string hardware2Check;
        private string registryKey;
        private State lastKnownState = State.Unknown;

        public RegistryCapabilityAccessChecker(String hardware2Check) 
        {
            this.hardware2Check = hardware2Check;
            this.registryKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{hardware2Check}\NonPackaged";
        }

        public override void CheckHardwareForUsage()
        {
            if (CheckIfHardwareIsInUse())
            {
                PublishHardwareState(State.Active);
            } 
            else
            {
                PublishHardwareState(State.Inactive);

            }
        }

        private void PublishHardwareState(State state)
        {
            if (lastKnownState != state)
            {
                lastKnownState = state;
                LogInfo(LOG_TAG, $"{this.hardware2Check} - State changed to {state}");

                if (hardware2Check == "microphone")
                {
                    StatePublisher.PublishMicrophoneState(state);
                }
                else if (hardware2Check == "webcam")
                {
                    StatePublisher.PublishCameraState(state);
                }
                else
                    throw new Exception("Unknown hardware: " + hardware2Check);
            }
        }

        private Boolean CheckIfHardwareIsInUse()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(registryKey))
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using (var subKey = key.OpenSubKey(subKeyName))
                    {
                        if (subKey.GetValueNames().Contains("LastUsedTimeStop"))
                        {
                            var endTime = subKey.GetValue("LastUsedTimeStop") is long ? (long)subKey.GetValue("LastUsedTimeStop") : -1;
                            if (endTime <= 0)
                                return true;
                        }
                    }

                }
            }
            return false;
        }
    }
}
