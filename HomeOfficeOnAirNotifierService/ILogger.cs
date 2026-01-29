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
        private readonly object sync = new object();

        // wie viele Tage Logfiles behalten
        private const int RetentionDays = 14;

        // Ordner (für Services empfehle ich ProgramData)
        private readonly string logDir;

        private readonly string filePath;

        // Dateiname-Präfix
        private const string Prefix = "service";

        public FileLogger()
        {
            logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "HomeOfficeOnAirNotifierService");
        }

        public void InitializeLogger()
        {
            Directory.CreateDirectory(logDir);
            CleanupOldLogs();
        }

        public void LogInfo(string tag, string logMessage)
        {
            WriteLine(Severity.INFO, tag, logMessage);
        }

        private void WriteLine(Severity severity, string tag, string message)
        {
            try
            {
                lock (sync)
                {
                    Directory.CreateDirectory(logDir);

                    // Daily rotation: pro Tag ein File
                    string filePath = GetDailyLogPath(DateTime.Now);

                    string line =
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {severity} [{tag}] - {message}{Environment.NewLine}";

                    File.AppendAllText(filePath, line, Encoding.UTF8);

                    // optional: nicht bei jedem Log aufräumen (kann IO kosten)
                    // -> wir machen es nur 1x pro Tag (siehe unten)
                    CleanupOldLogsOncePerDay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.ToString());
            }
        }

        private string GetDailyLogPath(DateTime now)
        {
            // service-2026-01-24.log
            return Path.Combine(logDir, $"{Prefix}-{now:yyyy-MM-dd}.log");
        }

        // --- Cleanup (Retention) ---

        private DateTime lastCleanupDate = DateTime.MinValue.Date;

        private void CleanupOldLogsOncePerDay()
        {
            var today = DateTime.Today;
            if (lastCleanupDate == today) return;

            CleanupOldLogs();
            lastCleanupDate = today;
        }

        private void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(logDir)) return;

                DateTime cutoff = DateTime.Today.AddDays(-RetentionDays);

                foreach (var file in Directory.EnumerateFiles(logDir, $"{Prefix}-*.log"))
                {
                    var fi = new FileInfo(file);

                    // schnelle Variante: LastWriteTime als Basis
                    if (fi.LastWriteTime.Date < cutoff)
                    {
                        try { fi.Delete(); } catch { /* ignore */ }
                    }
                }
            }
            catch
            {
                // Cleanup darf Logging nicht killen
            }
        }
    }

}
