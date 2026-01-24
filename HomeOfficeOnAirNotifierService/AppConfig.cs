using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{
    internal interface IAppConfigValidator
    {
        /**
         * Updates the bound properties from the configuration source.
         */
        void UpdateBoundProperties();

        /**
         * Validates whether the current configuration is valid.
         */
        bool IsConfigValid();
    }

    internal static class AppConfig
    {
        public static void Reload() => ConfigurationManager.RefreshSection("appSettings");

        public static string LoggedOnSAMUser => ConfigurationManager.AppSettings.Get("LoggedOnSAMUser");
        public static string LoggedOnUserSID => ConfigurationManager.AppSettings.Get("LoggedOnUserSID");

        public static string MicrophoneIDInQuestion => ConfigurationManager.AppSettings["MicrophoneIDInQuestion"];

        public static string BaseEndpointUrl => ConfigurationManager.AppSettings["BaseEndpointUrl"];
        public static string MicrophoneEndpointPath => ConfigurationManager.AppSettings["MicrophoneEndpointPath"];
        public static string CameraEndpointPath => ConfigurationManager.AppSettings["CameraEndpointPath"];
        public static string BearerHeaderValue => ConfigurationManager.AppSettings["BearerHeaderValue"];
    }
}
