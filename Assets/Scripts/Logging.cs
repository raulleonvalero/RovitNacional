using OVR.OpenVR;
using System;
using System.IO;


namespace RovitNacional
{
    public class Logging
    {
        private static string logPath = "INFO";
        private static string logFile;

        public static void createFile()
        {
            logFile = Path.Combine(logPath, DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("HH-mm-ss") + ".log");
            DirectoryInfo directory  = Directory.CreateDirectory(Path.GetDirectoryName(logFile));

        }
        public static string WriteLog(int actividad,int modo, string message)
        {
            string loglevel;
            string logActivity;
            string log;

            switch (modo)
            {
                case 0: loglevel = "TEA"; break;
                case 1: loglevel = "Sindrome de Down"; break;
                case 2: loglevel = "Altas Capacidades"; break;
                default: loglevel = "System"; break;
            }
            switch (actividad)
            {
                case 0: logActivity = "Building Tower"; break;
                case 1: logActivity = "Go Stop Go"; break;
                default: logActivity = "No Activity"; break;
            }

            //StreamWriter sw = new StreamWriter(logPath);
            using (StreamWriter sw = new StreamWriter(logFile, true))
            {
                // Add some text to the file.
                log = "[" + DateTime.Now.ToString("HH:mm:ss") + "][" + logActivity + "][" + loglevel + "] - " + message;
                Variables.logOutput += log + "\n";
                sw.WriteLine(log);
            }
            return log;
        }
    }
}


