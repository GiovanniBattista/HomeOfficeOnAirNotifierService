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
    internal class MicrophoneChecker : IHardwareChecker
    {
        private string microphoneInQuestion;
        private MMDevice microphone;

        public MicrophoneChecker() 
        {
            this.microphoneInQuestion = ConfigurationManager.AppSettings.Get("MicrophoneInQuestion");    
        }

        public void InitializeChecker()
        {
            this.microphone = GetMicrophoneDevice();

            this.microphone.AudioSessionManager.OnSessionCreated += OnAudioSessionCreated;
        }

        public void CheckHardwareForSessions()
        {
            AudioSessionManager sessionManager = this.microphone.AudioSessionManager;

            SessionCollection sessions = sessionManager.Sessions;
            int sessionCount = sessions.Count;

            Console.WriteLine("Currently active sessions: " + sessionCount);
            for (int i = 0; i < sessionCount; i++)
            {
                string sessionIdentifier = sessions[i].GetSessionIdentifier;
                sessions[i].RegisterEventClient(new AudioSessionCreatedListener(sessionIdentifier));
            }
        }

        private void OnAudioSessionCreated(object sender, IAudioSessionControl newSession)
        {
            IAudioSessionControl2 newSession2 = (IAudioSessionControl2)newSession;
            newSession2.GetSessionIdentifier(out string sessionIdentifier);

            newSession.RegisterAudioSessionNotification(new AudioSessionCreatedListener(sessionIdentifier));
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
        private Regex processNameRegex = new Regex(@"([a-zA-Z]+\.exe)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal string processName;

        public AudioSessionCreatedListener(string sessionIdentifier)
        {
            Match match = processNameRegex.Match(sessionIdentifier);
            this.processName = match.Value;

            Console.WriteLine($"Created Listener for audio session of process '{processName}'");
        }

        public void OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex)
        {
            Console.WriteLine(this.processName + ": OnChannelVolumeChanged(3)");
        }

        public int OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex, ref Guid eventContext)
        {
            Console.WriteLine(this.processName + ": OnChannelVolumeChanged(4)");
            return HResult.S_OK;
        }

        public void OnDisplayNameChanged(string displayName)
        {
            Console.WriteLine(this.processName + ": OnDisplayNameChanged(1)");
        }

        public int OnDisplayNameChanged(string displayName, ref Guid eventContext)
        {
            Console.WriteLine(this.processName + ": OnDisplayNameChanged(2)");
            return HResult.S_OK;
        }

        public void OnGroupingParamChanged(ref Guid groupingId)
        {
            Console.WriteLine(this.processName + ": OnGroupingParamChanged(1)");
        }

        public int OnGroupingParamChanged(ref Guid groupingId, ref Guid eventContext)
        {
            Console.WriteLine(this.processName + ": OnGroupingParamChanged(2)");
            return HResult.S_OK;
        }

        public int OnIconPathChanged(string iconPath, ref Guid eventContext)
        {
            Console.WriteLine(this.processName + ": OnIconPathChanged(2)");
            return HResult.S_OK;
        }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            Console.WriteLine(this.processName + ": OnVolumeChanged(3)");
        }

        public int OnSimpleVolumeChanged(float volume, bool isMuted, ref Guid eventContext)
        {
            Console.WriteLine(this.processName + ": OnSimpleVolumeChanged(3)");
            return HResult.S_OK;
        }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            Console.WriteLine(this.processName + ": OnSessionDisconnected: " + disconnectReason);
        }

        int IAudioSessionEvents.OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            Console.WriteLine(this.processName + ": OnSessionDisconnected: " + disconnectReason);
            return HResult.S_OK;
        }

        public void OnStateChanged(AudioSessionState state)
        {
            Console.WriteLine(this.processName + ": OnStateChanged: " + state);
        }

        int IAudioSessionEvents.OnStateChanged(AudioSessionState state)
        {
            Console.WriteLine(this.processName + ": OnStateChanged: " + state);

            return HResult.S_OK;
        }

        public void OnIconPathChanged(string iconPath)
        {
            Console.WriteLine(this.processName + ": OnIconPathChanged(1)");
        }

    }
}
