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
        private static string LOG_TAG = "MicrophoneUsageChecker";

        private string microphoneInQuestion;
        private MMDevice microphone;
        private IOnAirStatePublisher statePublisher;

        public MicrophoneUsageChecker() 
        {
            this.microphoneInQuestion = ConfigurationManager.AppSettings.Get("MicrophoneInQuestion");    
        }

        public override void InitializeChecker(Publisher.IOnAirStatePublisher statePublisher, ILogger logger)
        {
            base.InitializeChecker(statePublisher, logger);

            this.statePublisher = statePublisher;
            this.microphone = GetMicrophoneDevice();

            this.microphone.AudioSessionManager.OnSessionCreated += OnAudioSessionCreated;
        }

        public override void CheckHardwareForUsage()
        {
            AudioSessionManager sessionManager = this.microphone.AudioSessionManager;

            SessionCollection sessions = sessionManager.Sessions;
            int sessionCount = sessions.Count;

            LogInfo(LOG_TAG, "Currently active sessions: " + sessionCount);
            for (int i = 0; i < sessionCount; i++)
            {
                string sessionIdentifier = sessions[i].GetSessionIdentifier;
                sessions[i].RegisterEventClient(new AudioSessionCreatedListener(sessionIdentifier, statePublisher, Logger));
            }
        }

        private void OnAudioSessionCreated(object sender, IAudioSessionControl newSession)
        {
            IAudioSessionControl2 newSession2 = (IAudioSessionControl2)newSession;
            newSession2.GetSessionIdentifier(out string sessionIdentifier);

            newSession.RegisterAudioSessionNotification(new AudioSessionCreatedListener(sessionIdentifier, statePublisher, Logger));
        }

        private MMDevice GetMicrophoneDevice()
        {
            // Create an instance of the MMDeviceEnumerator
            MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();

            // Enumerate through all the audio endpoint devices
            foreach (MMDevice device in deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                if (device.DeviceFriendlyName.Contains(microphoneInQuestion))
                {
                    return device;
                }
            }

            return null;
        }
    }


    internal class AudioSessionCreatedListener : IAudioSessionEvents, IAudioSessionEventsHandler
    {
        private static string LOG_TAG = "AudioSessionCreatedListener";

        private Regex processNameRegex = new Regex(@"([a-zA-Z]+\.exe)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
            statePublisher.updateMicrophoneState(newState);
        }

        int IAudioSessionEvents.OnStateChanged(AudioSessionState state)
        {
            this.logger.LogInfo(LOG_TAG, this.processName + " - Audio session state changed to : " + state);

            State newState = convertAudioSessionState(state);
            statePublisher.updateMicrophoneState(newState);

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
