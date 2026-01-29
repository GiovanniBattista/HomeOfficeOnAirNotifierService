using HomeOfficeOnAirNotifierService;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

internal class LoggedOnUserSidResolver
{
    private static string LOG_TAG = "LoggedOnUserSidResolver";

    private ILogger Logger;

    public LoggedOnUserSidResolver(ILogger logger)
    {
        this.Logger = logger;
    }

    public bool TryGetActiveConsoleUserSid(out string sid)
    {
        sid = null;

        uint sessionId = WTSGetActiveConsoleSessionId();
        if (sessionId == 0xFFFFFFFF) return false;

        if (!WTSQueryUserToken(sessionId, out IntPtr token) || token == IntPtr.Zero)
            return false;

        WindowsIdentity identity = null;
        try
        {
            identity = new WindowsIdentity(token);
            sid = identity.User?.Value;
            return !string.IsNullOrWhiteSpace(sid);
        }
        finally
        {
            if (identity != null)
                identity.Dispose();
            CloseHandle(token);
        }
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}

