using HomeOfficeOnAirNotifierService.HardwareChecker;
using HomeOfficeOnAirNotifierService.Publisher;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Timers;

namespace HomeOfficeOnAirNotifierService
{
    internal class RegistryCapabilityAccessChecker : HardwareUsageChecker, IAppConfigValidator, IDisposable
    {
        private const string LOG_TAG = "RegistryCapabilityAccessChecker";

        private readonly string hardware2Check; // "microphone" oder "webcam"
        private readonly ConcurrentDictionary<string, ManagementEventWatcher> watchers = new ConcurrentDictionary<string, ManagementEventWatcher>();

        private Timer refreshTimer;

        private readonly object sidLock = new object();
        private HashSet<string> cachedLoadedSids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private State lastKnownState = State.Unknown;

        public RegistryCapabilityAccessChecker(ILogger logger, string hardware2Check) : base(logger)
        {
            this.hardware2Check = hardware2Check;
        }

        public override bool InitializeChecker(IOnAirStatePublisher statePublisher)
        {
            base.InitializeChecker(statePublisher);

            // initial watch + cache
            RefreshWatchers();

            // periodisch nachziehen (neue Logins / Hives)
            refreshTimer = new Timer(15_000);
            refreshTimer.AutoReset = true;
            refreshTimer.Elapsed += (_, __) => RefreshWatchers();
            refreshTimer.Start();

            Logger.LogInfo(LOG_TAG, $"Watching all loaded user hives (cached) for '{hardware2Check}'.");
            return true;
        }

        void IAppConfigValidator.UpdateBoundProperties()
        {
            // nothing to do here
        }

        bool IAppConfigValidator.IsConfigValid()
        {
            return true;
        }

        private void RefreshWatchers()
        {
            try
            {
                var loadedSidsNow = GetLoadedUserSidsFromRegistry();

                // Cache aktualisieren
                lock (sidLock)
                {
                    cachedLoadedSids = new HashSet<string>(loadedSidsNow, StringComparer.OrdinalIgnoreCase);
                }

                // Neue Watcher hinzufügen
                foreach (var sid in loadedSidsNow)
                {
                    EnsureWatcherForSid(sid);
                }

                // Alte Watcher entfernen (Hive nicht mehr geladen)
                foreach (var key in watchers.Keys)
                {
                    var sid = key.Split('|')[0];
                    if (!cachedLoadedSids.Contains(sid))
                    {
                        RemoveWatcher(key, "Hive unloaded");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LOG_TAG, "RefreshWatchers failed: " + ex);
            }
        }

        private void EnsureWatcherForSid(string sid)
        {
            string watcherKey = $"{sid}|{hardware2Check}";
            if (watchers.ContainsKey(watcherKey))
                return;

            // Für WMI RootPath müssen Backslashes escaped sein -> \\ in der Query
            string registryKeyWmi =
                $@"{sid}\\Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\{hardware2Check}\\NonPackaged";

            string query =
                "SELECT * FROM RegistryTreeChangeEvent " +
                "WHERE Hive='HKEY_USERS' " +
                $"AND RootPath='{registryKeyWmi}'";

            try
            {
                var watcher = new ManagementEventWatcher(query);
                watcher.EventArrived += (_, __) =>
                {
                    Logger.LogInfo(LOG_TAG, $"RegistryTreeChangeEvent: SID={sid}, hardware={hardware2Check}");
                    CheckHardwareForUsage(); // nutzt Cache
                };

                if (watchers.TryAdd(watcherKey, watcher))
                {
                    watcher.Start();
                    Logger.LogInfo(LOG_TAG, $"Started watcher: {watcherKey}");

                    bool hiveLoaded = Registry.Users.OpenSubKey($@"{sid}\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager") != null;
                    Logger.LogInfo(LOG_TAG, $"HKEY_USERS\\{sid} hive loaded: {hiveLoaded}");
                }
                else
                {
                    watcher.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LOG_TAG, $"Failed to start watcher {watcherKey}: {ex}");
            }
        }

        private void RemoveWatcher(string watcherKey, string reason)
        {
            if (!watchers.TryRemove(watcherKey, out var watcher))
                return;

            try { watcher.Stop(); } catch { }
            try { watcher.Dispose(); } catch { }

            Logger.LogInfo(LOG_TAG, $"Removed watcher: {watcherKey} ({reason})");
        }

        public override void CheckHardwareForUsage()
        {
            bool inUse = CheckIfHardwareIsInUseAcrossCachedSids();
            PublishHardwareState(inUse ? State.Active : State.Inactive);
        }

        private void PublishHardwareState(State state) 
        { 
            if (lastKnownState != state) 
            { 
                lastKnownState = state; 
                Logger.LogInfo(LOG_TAG, $"{this.hardware2Check} - State changed to {state}"); 
                
                if (hardware2Check == "microphone") { 
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

        private bool CheckIfHardwareIsInUseAcrossCachedSids()
        {
            string[] sidsSnapshot;
            lock (sidLock)
            {
                sidsSnapshot = cachedLoadedSids.ToArray();
            }

            foreach (var sid in sidsSnapshot)
            {
                if (CheckIfHardwareIsInUseForSid(sid))
                    return true;
            }
            return false;
        }

        private bool CheckIfHardwareIsInUseForSid(string sid)
        {
            // Für Registry-Zugriff: normale Backslashes
            string registryKeyReg =
                $@"{sid}\Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\{hardware2Check}\NonPackaged";

            try
            {
                using (var key = Registry.Users.OpenSubKey(registryKeyReg))
                {
                    if (key == null) return false;

                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using (var subKey = key.OpenSubKey(subKeyName))
                        {
                            if (subKey == null) continue;

                            // LastUsedTimeStop <= 0 -> in use
                            object v = subKey.GetValue("LastUsedTimeStop");
                            if (v is long endTime && endTime <= 0)
                                return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo(LOG_TAG, $"CheckIfHardwareIsInUseForSid failed for {sid}: {ex.Message}");
            }

            return false;
        }

        private static List<string> GetLoadedUserSidsFromRegistry()
        {
            return Registry.Users
                .GetSubKeyNames()
                .Where(n => n.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public override void Dispose() 
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            refreshTimer = null;

            foreach (var key in watchers.Keys.ToList())
                RemoveWatcher(key, "Dispose");
        }
    }
}
