using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService.Publisher
{
    internal enum State { Active, Inactive, Unknown }

    internal interface IOnAirStatePublisher
    {
        void InitializePublisher();

        void PublishMicrophoneState(State newState);

        void PublishCameraState(State newState);
    }

    internal class OpenhabOnAirStatePublisher : IOnAirStatePublisher, IAppConfigValidator
    {
        private const string LOG_TAG = "OpenhabOnAirStatePublisher";

        static HttpClient client = new HttpClient();

        private ILogger logger;

        private string baseEndpointUrl;
        private string microphoneEndpointPath;
        private string cameraEndpointPath;
        private string bearerHeaderValue;

        public OpenhabOnAirStatePublisher(ILogger logger)
        {
            this.logger = logger;
        }

        void IAppConfigValidator.UpdateBoundProperties()
        {
            this.baseEndpointUrl = AppConfig.BaseEndpointUrl;
            this.microphoneEndpointPath = AppConfig.MicrophoneEndpointPath;
            this.cameraEndpointPath = AppConfig.CameraEndpointPath;
            this.bearerHeaderValue = AppConfig.BearerHeaderValue;
        }

        bool IAppConfigValidator.IsConfigValid()
        {
            if (string.IsNullOrEmpty(this.baseEndpointUrl))
            {
                logger.LogInfo(LOG_TAG, "Missing BaseEndpointUrl in config!");
                return false; 
            }

            if (string.IsNullOrEmpty(this.microphoneEndpointPath))
            {
                logger.LogInfo(LOG_TAG, "Missing MicrophoneEndpointPath in config!");
                return false;
            }

            if (string.IsNullOrEmpty(this.cameraEndpointPath))
            {
                logger.LogInfo(LOG_TAG, "Missing CameraEndpointPath in config!");
                return false;
            }
                
            if (string.IsNullOrEmpty(this.bearerHeaderValue))
            {
                logger.LogInfo(LOG_TAG, "Missing BearerHeaderValue in config!");
                return false;
            }

            return true;
        }

        public void InitializePublisher()
        {
            client.BaseAddress = new Uri(this.baseEndpointUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.bearerHeaderValue);
        }

        public void PublishMicrophoneState(State newState)
        {
            this.logger.LogInfo(LOG_TAG, "Updating state of microphone to " + newState);
            putAsync(newState, this.microphoneEndpointPath);
        }

        public void PublishCameraState(State newState)
        {
            this.logger.LogInfo(LOG_TAG, "Updating state of camera to " + newState);
            putAsync(newState, this.cameraEndpointPath);
        }

        private async void putAsync(State newState, string requestUri)
        {
            string stateAsString = convertStateToString(newState);
            StringContent content = new StringContent(stateAsString, Encoding.UTF8, "text/plain");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUri);
            request.Content = content;

            HttpResponseMessage response = await putAsync(request);
        }

        private async Task<HttpResponseMessage> putAsync(HttpRequestMessage request)
        {
            //Console.WriteLine("Request:");
            //Console.WriteLine(request.ToString());

            HttpResponseMessage response = await client.SendAsync(request);

            //Console.WriteLine("Response:");
            //Console.WriteLine(response.ToString());

            return response.EnsureSuccessStatusCode();
        }

        private string convertStateToString(State state)
        {
            switch (state)
            {
                case State.Active:
                    return "ON";
                case State.Inactive:
                    return "OFF";
            }
            throw new Exception("Unknown state: " + state);
        }
    }
}
