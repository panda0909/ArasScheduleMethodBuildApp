using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace ArasScheduleMethodBuildApp.Libarary.ConnectConfig
{
    public class ConnectConfigLib
    {
        public ConnectConfigLib()
        {
            if (!File.Exists("Connect.json"))
            {
                File.WriteAllText("Connect.json","{}");
            }
        }
        public void SaveConfig(string config_name,string url,string database,string username,string password)
        {
            ConnConfig conn = new ConnConfig();
            conn.name = config_name;
            conn.url = url;
            conn.database = database;
            conn.username = username;
            conn.password = password;
            
        }
    }
}
