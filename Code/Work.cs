using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Aras.IOM;
using Aras.Common;
using System.IO;
using System.Reflection;

namespace ArasScheduleMethodBuildApp.Code
{
    public class Work
    {
        public string _log { set; get; }
        public string Method(string arg)
        {
            ShowMessage(arg);
            return _log;
        }
        public void ShowMessage(string msg)
        {
            Console.WriteLine(msg);
            Debug("ShowMessage", msg);
        }
        public void Debug(string method, string msg)
        {
            string datestring = DateTime.Now.ToString("yyyy-MM-dd");
            string datefullstring = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string content = "[" + datefullstring + "]" + method + ":" + msg + "\r\n";
            _log += content;
        }
    }
    
   
    
}
