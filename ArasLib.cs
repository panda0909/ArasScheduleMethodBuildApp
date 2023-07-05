using Aras.IOM;
using Aras.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ArasScheduleMethodBuildApp
{
    public class ArasLib
    {
        internal Innovator inn { set; get; }
        internal HttpServerConnection connection { set; get; }
        internal string domain { set; get; }
        internal string database { set; get; }
        internal string codeRootDir { set; get; } = "../../Code/";
        internal string codeDBDir { set; get; } = "";
        internal string codeIPDir { set; get; } = "";
        public ArasLib(string plm_url,string db,string ad,string pwd)
        {
            /*建立連線*/
            this.connection = IomFactory.CreateHttpServerConnection(plm_url, db, ad, pwd);
            Item loginItem = this.connection.Login();
            this.inn = IomFactory.CreateInnovator(this.connection);
            Console.WriteLine("====================== Aras 連線狀態 ======================");
            if (loginItem.isError())
            {
                this.inn = null;
                Console.WriteLine("Aras連線失敗!");
            }
            else
            {
                database = connection.GetDatabaseName();
                
                Uri uri = new Uri(plm_url);
                this.domain = uri.Host;
                this.domain = domain.Replace(".", "_");
                codeIPDir = codeRootDir + domain + "/";
                codeDBDir = codeIPDir + database + "/";
                CreateDir(codeDBDir);
                Console.WriteLine("Aras連線成功!");
            }
        }
        internal bool CheckLogin()
        {
            if (inn == null)
            {
                return false;
            }
            return true;
        }
        internal void DownloadMethod(string search,string folder,string mode)
        {
            string aml = @"<AML>
            <Item type='Method' action='get'>
            <name condition='like'>{0}</name>
            <method_type>C#</method_type>
            </Item>
            </AML>";
            aml = string.Format(aml, search);
            Item itmMethods = inn.applyAML(aml);
            if (!itmMethods.isError())
            {
                string currentDir = codeDBDir;
                string template = File.ReadAllText(folder + "/template.txt");
                string template_program = File.ReadAllText(folder + "/template_program.txt");
                for (int i=0;i< itmMethods.getItemCount(); i++)
                {
                    Item itmMethod = itmMethods.getItemByIndex(i);
                    string method_code = itmMethod.getProperty("method_code", "");
                    string method_name = itmMethod.getProperty("name", "");
                    string filepath = currentDir + method_name+"/"+ method_name + ".cs"; //建立Method檔案
                    string filepath_program = currentDir + method_name + "/" + method_name + "_program.cs"; //建立Method執行檔案
                    string export_code = template.Replace("{@Domain}",this.domain).Replace("{@Database}", database).Replace("{@ClassName}", method_name).Replace("{@code}", method_code);
                    string export_program = template_program.Replace("{@Domain}", this.domain).Replace("{@Database}", database).Replace("{@ClassName}", method_name);
                    if (!Directory.Exists(currentDir + method_name))
                    {
                        Directory.CreateDirectory(currentDir + method_name);
                    }
                    if (mode == "method_only")
                    {
                        File.WriteAllText(filepath, export_code);
                    }
                    else if (mode == "all")
                    {
                        File.WriteAllText(filepath, export_code);
                        File.WriteAllText(filepath_program, export_program);
                    }
                    
                }
            }
            else
            {
                Console.WriteLine("找不到Method");
            }
        }
        internal string ConvertAMLtoSetPropertyCode(string aml)
        {
            string result = "\r\n";
            string template = "innMethod.setProperty(\"{0}\",\"{1}\");";
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(aml);
                var childs = doc.ChildNodes;
                if (childs.Count > 0)
                {
                    var child = childs.Item(0);
                    var properties = child.ChildNodes;
                    foreach(XmlElement property in properties)
                    {
                        string p_name = string.IsNullOrEmpty(property.Name) ? "" : property.Name;
                        string p_text = string.IsNullOrEmpty(property.InnerText) ? "" : property.InnerText;
                        result += string.Format(template, p_name, p_text) +"\r\n";
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Logger.Debug("ConvertAMLtoSetPropertyCode", ex.Message);
            }

            return result;
        }
        internal void CreateDir(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
    }
}
