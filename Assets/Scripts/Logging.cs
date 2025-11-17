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
        public static string WriteLog(int level, string message)
        {
            string loglevel;
            string log;

            switch (level)
            {
                case 0: loglevel = "INFO"; break;
                case 1: loglevel = "WARNING"; break;
                case 2: loglevel = "ERROR"; break;
                default: loglevel = "INFO"; break;
            }

            //StreamWriter sw = new StreamWriter(logPath);
            using (StreamWriter sw = new StreamWriter(logFile, true))
            {
                // Add some text to the file.
                log = "[" + DateTime.Now.ToString("HH:mm:ss") + "][" + loglevel + "] - " + message;
                sw.WriteLine(log);
            }
            return log;
        }
    }
}


