using Aras.IOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ArasScheduleMethodBuildApp.Code
{
    public class Temp_localhost_PLMCTA_In_meeting_register :IProgram
    {
        public void run()
        {
            string plm_url = ConfigurationManager.AppSettings["plm_url"];
            string plm_db = ConfigurationManager.AppSettings["plm_db"];
            string plm_ad = ConfigurationManager.AppSettings["plm_ad"];
            string plm_pwd = ConfigurationManager.AppSettings["plm_pwd"];
            ArasLib arasLib = new ArasLib(plm_url, plm_db, plm_ad, plm_pwd);
            Wrapper_localhost_PLMCTA_In_meeting_register _wrapperIn_meeting_register = new Wrapper_localhost_PLMCTA_In_meeting_register();
            var innMethod = _wrapperIn_meeting_register.init(arasLib.connection);
            //取得method執行結果

            Item result = innMethod.MethodCode0();
            XmlDocument document = new XmlDocument();
            document.Load(new StringReader(result.dom.InnerXml));
            StringBuilder builder = new StringBuilder();
            using (XmlTextWriter writer = new XmlTextWriter(new StringWriter(builder)))
            {
                writer.Formatting = Formatting.Indented;
                document.Save(writer);
            }
            Console.WriteLine("result = " + Environment.NewLine + builder.ToString());
        }
    }
}
