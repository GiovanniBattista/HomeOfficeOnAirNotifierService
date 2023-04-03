using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HomeOfficeOnAirNotifierService
{
    internal interface ILogger
    {
        void InitializeLogger();

        void LogInfo(string tag, string message);
    }

    internal enum Severity
    {
        INFO, WARNING, ERROR
    }

    internal class FileLogger : ILogger
    {
        private string path;

        public FileLogger()
        {
            this.path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\service.log";
        }

        public void InitializeLogger()
        {
            File.Delete(this.path);
        }

        public void LogInfo(string tag, string logMessage)
        {
            try
            {
                using (StreamWriter txtWriter = File.AppendText(path))
                {
                    Log(txtWriter, Severity.INFO, tag, logMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void Log(TextWriter txtWriter, Severity severity, string tag, string logMessage)
        {
            try
            {
                txtWriter.WriteLine("{0} {1} {2}  [{3}] - {4}", 
                    DateTime.Now.ToShortDateString(),
                    DateTime.Now.ToLongTimeString(), 
                    severity,
                    tag,
                    logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
