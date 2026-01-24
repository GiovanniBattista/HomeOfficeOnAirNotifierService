using HomeOfficeOnAirNotifierService.Publisher;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService.HardwareChecker
{
    internal class MicrophoneUsageChecker : HardwareUsageChecker
    {
        private const string LOG_TAG = "MicrophoneUsageChecker";

        private string microphoneInQuestion;
        private MMDevice microphone;

        public MicrophoneUsageChecker() 
        {
            this.microphoneInQuestion = ConfigurationManager.AppSettings.Get("MicrophoneIDInQuestion");    
        }

        public override bool InitializeChecker(Publisher.IOnAirStatePublisher statePublisher, ILogger logger)
        {
            base.InitializeChecker(statePublisher, logger);

            if (string.IsNullOrEmpty(this.microphoneInQuestion))
            {
                getLogger().LogInfo(LOG_TAG, "Missing or empty 'MicrophoneIDInQuestion' in Config!\n" +
                    "Config file: " + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile + "\n" + 
                    "Available capture devices:\n" + GetAllAudioDevices());

                return false;
            }

            this.microphone = GetMicrophoneDevice();
            if (this.microphone == null)
            {
                getLogger().LogInfo(LOG_TAG,
                    $"No active capture device matched '{this.microphoneInQuestion}'.\n" +
                    "Config file: " + AppDomain.CurrentDomain.SetupInformation.ConfigurationFile + "\n" +
                    "Available capture devices:\n" + GetAllAudioDevices());

                return false;
            }

            this.microphone.AudioSessionManager.OnSessionCreated += OnAudioSessionCreated;

            // Endpoint mute/volume changes (device-level)
            this.microphone.AudioEndpointVolume.OnVolumeNotification += OnEndpointVolumeNotification;

            return true;
        }

        public override void CheckHardwareForUsage()
        {
            AudioSessionManager sessionManager = this.microphone.AudioSessionManager;

            SessionCollection sessions = sessionManager.Sessions;
            int sessionCount = sessions.Count;

            getLogger().LogInfo(LOG_TAG, "Currently active sessions: " + sessionCount);
            for (int i = 0; i < sessionCount; i++)
            {
                string sessionIdentifier = sessions[i].GetSessionIdentifier;
                sessions[i].RegisterEventClient(new AudioSessionCreatedListener(sessionIdentifier, StatePublisher, Logger));
            }
        }

        private void OnAudioSessionCreated(object sender, IAudioSessionControl newSession)
        {
            IAudioSessionControl2 newSession2 = (IAudioSessionControl2)newSession;
            newSession2.GetSessionIdentifier(out string sessionIdentifier);

            newSession.RegisterAudioSessionNotification(new AudioSessionCreatedListener(sessionIdentifier, StatePublisher, Logger));
        }

        private void OnEndpointVolumeNotification(AudioVolumeNotificationData data)
        {
            // data.Muted -> global device mute
            Logger.LogInfo(LOG_TAG, $"Mic endpoint volume changed. Muted={data.Muted}, MasterVolume={data.MasterVolume}");

            // Optional: publish a separate item (MicMuted) OR incorporate into "OnAir"
            // z.B. wenn muted => OFF, sonst unverändert
        }

        private MMDevice GetMicrophoneDevice()
        {
            // Create an instance of the MMDeviceEnumerator
            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                // Enumerate through all the audio endpoint devices
                foreach (MMDevice device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    if (device.ID.Equals(microphoneInQuestion))
                    {
                        return device;
                    }
                }
            }

            return null;
        }

        private string GetAllAudioDevices()
        {
            var sb = new StringBuilder();

            using (var deviceEnumerator = new MMDeviceEnumerator())
            {
                foreach (MMDevice device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
                {
                    sb.Append("• ")
                      .Append(device.DeviceFriendlyName ?? "<no name>")
                      .Append(" | State=")
                      .Append(device.State)
                      .Append(" | ID=")
                      .Append(device.ID)
                      .AppendLine();
                }
            }

            return sb.Length == 0 ? "<no capture devices found>" : sb.ToString();
        }
    }


    internal class AudioSessionCreatedListener : IAudioSessionEvents, IAudioSessionEventsHandler
    {
        private const string LOG_TAG = "AudioSessionCreatedListener";

        //private Regex processNameRegex = new Regex(@"([a-zA-Z]+\.exe)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private Regex processNameRegex = new Regex(@"([^\\/:]+\.exe)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal string processName;
        private IOnAirStatePublisher statePublisher;
        private ILogger logger;

        public AudioSessionCreatedListener(string sessionIdentifier, IOnAirStatePublisher statePublisher, ILogger logger)
        {
            this.statePublisher = statePublisher;
            this.logger = logger;

            Match match = processNameRegex.Match(sessionIdentifier);
            this.processName = match.Value;

            this.logger.LogInfo(LOG_TAG, $"Created Listener for audio session of process '{processName}'");
        }

        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex)
        {

            this.logger.LogInfo(LOG_TAG, this.processName + ": OnChannelVolumeChanged(3)");
        }

        public int OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex, ref Guid eventContext)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnChannelVolumeChanged(4)");
            return HResult.S_OK;
        }

        public void OnDisplayNameChanged(string displayName)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnDisplayNameChanged(1)");
        }

        public int OnDisplayNameChanged(string displayName, ref Guid eventContext)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnDisplayNameChanged(2)");
            return HResult.S_OK;
        }

        public void OnGroupingParamChanged(ref Guid groupingId)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnGroupingParamChanged(1)");
        }

        public int OnGroupingParamChanged(ref Guid groupingId, ref Guid eventContext)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnGroupingParamChanged(2)");
            return HResult.S_OK;
        }

        public int OnIconPathChanged(string iconPath, ref Guid eventContext)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnIconPathChanged(2)");
            return HResult.S_OK;
        }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnVolumeChanged(3)");
        }

        public int OnSimpleVolumeChanged(float volume, bool isMuted, ref Guid eventContext)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnSimpleVolumeChanged(3)");
            return HResult.S_OK;
        }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnSessionDisconnected: " + disconnectReason);
        }

        int IAudioSessionEvents.OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnSessionDisconnected: " + disconnectReason);
            return HResult.S_OK;
        }

        public void OnStateChanged(AudioSessionState state)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + " - Audio session state changed to " + state);
            
            State newState = convertAudioSessionState(state);
            statePublisher.PublishMicrophoneState(newState);
        }

        int IAudioSessionEvents.OnStateChanged(AudioSessionState state)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + " - Audio session state changed to : " + state);

            State newState = convertAudioSessionState(state);
            statePublisher.PublishMicrophoneState(newState);

            return HResult.S_OK;
        }

        public void OnIconPathChanged(string iconPath)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + ": OnIconPathChanged(1)");
        }

        State convertAudioSessionState(AudioSessionState state)
        {
            switch (state)
            {
                case AudioSessionState.AudioSessionStateActive:
                    return State.Active;
                case AudioSessionState.AudioSessionStateInactive:
                    return State.Inactive;
                case AudioSessionState.AudioSessionStateExpired:
                    return State.Inactive;

            }
            throw new Exception("Unhandled state " + state);
        }

    }

    }
