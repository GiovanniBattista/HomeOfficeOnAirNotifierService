using HomeOfficeOnAirNotifierService.HardwareChecker;
using HomeOfficeOnAirNotifierService.Publisher;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{
    internal class RegistryCapabilityAccessChecker : HardwareUsageChecker
    {
        private const string LOG_TAG = "RegistryCapabilityAccessChecker";

        private ManagementEventWatcher watcher;

        private string hardware2Check;
        private string registryKey;
        private State lastKnownState = State.Unknown;

        private string loggedOnSAMUser;
        private string loggedOnUserSID;

        public RegistryCapabilityAccessChecker(String hardware2Check) 
        {
            this.hardware2Check = hardware2Check;

            this.loggedOnSAMUser = ConfigurationManager.AppSettings.Get("LoggedOnSAMUser");
            this.loggedOnUserSID = ConfigurationManager.AppSettings.Get("LoggedOnUserSID");
        }

        public override bool InitializeChecker(IOnAirStatePublisher statePublisher, ILogger logger)
        {
            base.InitializeChecker(statePublisher, logger);

            if (string.IsNullOrEmpty(this.loggedOnSAMUser) && string.IsNullOrEmpty(this.loggedOnUserSID))
            {
                getLogger().LogInfo(LOG_TAG, "Provide either LoggedOnUserSID or LoggedOnUserSID (preferred) in Config!\n" +
                    "Config file: " + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile + "\n" +
                    "Both can be found in Registry under HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\SessionData\n"
                    );

                return false;
            }

            bool successfullyDeterminedUserSID = SetCurrentlyLoggedOnSecurityID();

            if (successfullyDeterminedUserSID)
            {
                //HKEY_USERS\S - 1 - 5 - 21 - 1437491012 - 3787555785 - 1699929658 - 1001\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore
                this.registryKey = $@"{loggedOnUserSID}\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\{hardware2Check}\\NonPackaged";

                string query = @"SELECT * FROM RegistryTreeChangeEvent " +
                    "WHERE Hive='HKEY_USERS' " +
                    "AND RootPath='" + registryKey + "'";

                this.watcher = new ManagementEventWatcher(query);
                this.watcher.EventArrived += new EventArrivedEventHandler(OnRegistryTreeChanged);

                getLogger().LogInfo(LOG_TAG, "Start watching for RegistryTreeChangeEvent for " + hardware2Check);
                this.watcher.Start();
            } else
            {
                getLogger().LogInfo(LOG_TAG, "Could not determine currently logged on user SID. RegistryCapabilityAccessChecker will not be available.");
            }

            return successfullyDeterminedUserSID;
        }

        private void OnRegistryTreeChanged(object sender, EventArrivedEventArgs e)
        {
            getLogger().LogInfo(LOG_TAG, "RegistryTreeChangeEvent occurred!");
            CheckHardwareForUsage();
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
                getLogger().LogInfo(LOG_TAG, $"{this.hardware2Check} - State changed to {state}");

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
            using (var key = Registry.Users.OpenSubKey(registryKey))
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

        private bool SetCurrentlyLoggedOnSecurityID()
        {
            if (!string.IsNullOrEmpty(this.loggedOnUserSID))
                return true;

            getLogger().LogInfo(LOG_TAG, "LoggedOnUserSID was not set in appSettings. Trying to determine SID by currently logged on user now!");

            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\SessionData
            //HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\SessionData
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {
                using (RegistryKey regKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\SessionData", false))
                {
                    if (regKey != null)
                    {
                        string[] sessionKeyNames = regKey.GetSubKeyNames();
                        foreach (string sessionKey in sessionKeyNames)
                        {
                            using (RegistryKey key = regKey.OpenSubKey(sessionKey, false))
                            {
                                string[] names = key.GetValueNames();
                                foreach (string name in names)
                                {
                                    if (name == "LoggedOnSAMUser")
                                    {
                                        if (key.GetValue(name).ToString() == this.loggedOnSAMUser)
                                        {
                                            this.loggedOnUserSID = key.GetValue("LoggedOnUserSID").ToString();
                                            getLogger().LogInfo(LOG_TAG, "Got LoggedOnUserSID: " + this.loggedOnUserSID);
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            getLogger().LogInfo(LOG_TAG, "Cannot determine logged on user! RegistryCapabilityAccessChecker will not be available.");
            return false;
        }
    }
}
