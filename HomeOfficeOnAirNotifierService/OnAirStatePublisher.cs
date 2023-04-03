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
        void PublishMicrophoneState(State newState);

        void PublishCameraState(State newState);
    }

    internal class OpenhabOnAirStatePublisher : IOnAirStatePublisher
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

            this.baseEndpointUrl = ConfigurationManager.AppSettings.Get("BaseEndpointUrl");
            this.microphoneEndpointPath = ConfigurationManager.AppSettings.Get("MicrophoneEndpointPath");
            this.cameraEndpointPath = ConfigurationManager.AppSettings.Get("CameraEndpointPath");
            this.bearerHeaderValue = ConfigurationManager.AppSettings.Get("BearerHeaderValue");

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
