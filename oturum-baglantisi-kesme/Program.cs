using System;
using System.Runtime.InteropServices;

class Program
{
    // WTSEnumSessions fonksiyonunu ve ilgili sabitleri içeren Wtsapi32.dll'i yükleme
    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSLogoffSession(IntPtr hServer, int sessionId, bool bWait);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

    [DllImport("wtsapi32.dll")]
    static extern void WTSCloseServer(IntPtr hServer);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSFreeMemory(IntPtr pMemory);

    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }

    enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    // WTSFreeMemory için kullanılacak sabit
    const int WTS_CURRENT_SESSION = -1;

    static void Main()
    {
        IntPtr serverHandle = WTSOpenServer(null);

        if (serverHandle == IntPtr.Zero)
        {
            Console.WriteLine("WTSOpenServer failed. Error: " + Marshal.GetLastWin32Error());
            return;
        }

        try
        {
            IntPtr sessionInfoPtr = IntPtr.Zero;
            int sessionCount = 0;

            // WTSEnumSessions fonksiyonunu kullanarak oturum bilgilerini alın
            if (WTSEnumerateSessions(serverHandle, 0, 1, ref sessionInfoPtr, ref sessionCount))
            {
                try
                {
                    IntPtr currentSession = sessionInfoPtr;

                    for (int i = 0; i < sessionCount; i++)
                    {
                        WTS_SESSION_INFO sessionInfo = (WTS_SESSION_INFO)Marshal.PtrToStructure(currentSession, typeof(WTS_SESSION_INFO));

                        // Bağlantısı kesilmiş oturumları kontrol et
                        if (sessionInfo.State == WTS_CONNECTSTATE_CLASS.WTSDisconnected)
                        {
                            //Console.WriteLine("Kullanıcı ID: " + sessionInfo.SessionID + " bağlantısı kesilmiş. Oturum kapatılıyor...");
                            // Oturumu sonlandır
                            WTSLogoffSession(serverHandle, sessionInfo.SessionID, true);
                        }

                        currentSession += Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                    }
                }
                finally
                {
                    WTSFreeMemory(sessionInfoPtr);
                }
            }
            else
            {
                Console.WriteLine("WTSEnumerateSessions failed. Error: " + Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            WTSCloseServer(serverHandle);
        }
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        ref IntPtr ppSessionInfo,
        ref int pCount);
}
