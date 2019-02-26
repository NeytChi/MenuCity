using System;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace Menu
{
    public class Config
    {
        public JObject JsonObject;
        public string fileName = "conf";
        public string IP = "127.0.0.1";
        public int Port = 8022;
        public string currentDirectory = Directory.GetCurrentDirectory();       // Return of the path occurs without the last '/' (pointer to the directory)

        public Config()
        {
            FileInfo fileExist = new FileInfo(currentDirectory + "/" + fileName);
            if (fileExist.Exists)
            {
                string infoJson = ReadConfigJsonData();
                JsonObject = GetConfigJson(infoJson);
                if (JsonObject != null)
                {
                    Port = GetConfigValue("port", "int");
                    IP = GetConfigValue("ip", "string");
                }
                else
                {
                    Debug.WriteLine("Start with default config setting.");
                }
            }
            else
            {
                Debug.WriteLine("Start with default config setting.");
            }
        }
        private JObject GetConfigJson(string info)
        {
            JObject json = JObject.Parse(info);
            if (json.ContainsKey("ip") && json.ContainsKey("port") && json.ContainsKey("domen"))
            {
                return json;
            }
            else
            {
                Console.WriteLine("Can not get JsonObject, json doens't have set values");
                return null;
            }
        }
        private string ReadConfigJsonData()
        {
            if (File.Exists(fileName))
            {
                using (var fstream = File.OpenRead(fileName))
                {
                    byte[] array = new byte[fstream.Length];
                    fstream.Read(array, 0, array.Length);
                    string textFromFile = System.Text.Encoding.Default.GetString(array);
                    fstream.Close();
                    return textFromFile;
                }
            }
            else
            {
                Console.WriteLine("Can not read file=" + fileName + " , function Config.ReadConfigJsonData()");
                return string.Empty;
            }
        }
        public dynamic GetConfigValue(string conf_name, string type_value)
        {
            if (JsonObject != null)
            {
                if (JsonObject.ContainsKey(conf_name))
                {
                    switch (type_value)
                    {
                        case "int":
                            conf_name = JsonObject.GetValue(conf_name).ToString();
                            return Convert.ToInt32(conf_name);
                        case "string":
                            return JsonObject.GetValue(conf_name).ToString();
                        default:
                            Console.WriteLine("Can not get value, type of value not define, function GetConfigValue");
                            return null;
                    }
                }
                else
                {
                    Console.WriteLine("Can not get value, json doesn't have this value, value=" + conf_name + ", function GetConfigValue");
                    return "";
                }
            }
            else
            {
                Console.WriteLine("Can not get value, Json Object did not create, function GetConfigValue");
                return "";
            }
        }
    }
}