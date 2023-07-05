using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace ArasScheduleMethodBuildApp
{
    public class Logger
    {
        public static void Debug(string method,string msg)
        {
            string datestring = DateTime.Now.ToString("yyyy-MM-dd");
            string datefullstring = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string _appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filename = _appDir + "/Log/Log" + datestring + ".log";
            string content = "["+datefullstring +"]"+ method + ":" + msg + "\r\n";
            File.AppendAllText(filename, content);    
        }
        public static string ReadTodayLog()
        {
            string result = "";
            string datestring = DateTime.Now.ToString("yyyy-MM-dd");
            string _appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filename = _appDir + "/Log/Log" + datestring + ".log";
            if (File.Exists(filename))
            {
                result = File.ReadAllText(filename);
            }

            return result;
        }
        public static void ResetTodayLog()
        {
            string datestring = DateTime.Now.ToString("yyyy-MM-dd");
            string datefullstring = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string _appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filename = _appDir + "/Log/Log" + datestring + ".log";
            File.WriteAllText(filename, "");
        }
    }
}
