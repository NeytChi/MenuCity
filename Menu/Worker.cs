using System;
using System.Text;
using Menu.Logger;
using Menu.NDatabase;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Menu.Functional.Pass;
using Menu.Functional.Mail;
using Menu.Functional.UserF;
using System.Text.RegularExpressions;

namespace Menu
{
    public class Worker
    {
        public string domen = "";
        public MailF mail;
        public UserF user;
        public Database database;
        public LogProgram logger;
        public Validator validator;

        public DateTime unixed = new DateTime(1970, 1, 1, 1, 1, 1, 1);

        public Worker() 
        {
            this.database = new Database(); 
            this.logger = new LogProgram();
            validator = new Validator(logger);
            this.user = new UserF(this);
            this.mail = new MailF(database, logger);
        }      
        public Worker(Database dataBase, LogProgram logprogram) 
        {
            this.database = dataBase;
            this.logger = logprogram;
            this.validator = new Validator(logger);
            this.user = new UserF(this);
            this.mail = new MailF(dataBase, logprogram);
        }
        public void JsonRequest(string json, Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                string response = "HTTP/1.1 200\r\n" +
                                  "Version: HTTP/1.1\r\n" +
                                  "Content-Type: application/json\r\n" +
                                  "Access-Control-Allow-Headers: *\r\n" +
                                  "Access-Control-Allow-Origin: *\r\n" +
                                  "Content-Length: " + (json.Length).ToString() +
                                  "\r\n\r\n" +
                                  json;
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
                Debug.Write(response);
                logger.WriteLog("Return http 200 JSON response", LogLevel.Worker);
            }
        }
        public void ErrorJsonRequest(string json, Socket remoteSocket)
        {
            if (string.IsNullOrEmpty(json))
            {
                string response = "HTTP/1.1 500\r\n" +
                                  "Version: HTTP/1.1\r\n" +
                                  "Content-Type: application/json\r\n" +
                                  "Access-Control-Allow-Headers: *\r\n" +
                                  "Access-Control-Allow-Origin: *\r\n" +
                                  "Content-Length: " + json.Length.ToString() +
                                  "\r\n\r\n" +
                                  json;
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
                Debug.WriteLine(response);
                logger.WriteLog("Return http 500 responce with JSON data.", LogLevel.Worker);
            }
        }
        public dynamic CheckRequiredJsonField(JObject json, string field_name, JTokenType field_type, Socket remoteSocket)
        {
            if (json != null)
            {
                logger.WriteLog("Insert json is null, function CheckRequiredJsonField", LogLevel.Error);
                return null;
            }
            if (json.ContainsKey(field_name))
            {
                JToken token = json.GetValue(field_name);
                if (token.Type == field_type)
                {
                    return token;
                }
                else
                {
                    logger.WriteLog("Required field is not in correct format, field_name=" + field_name + " field_type=" + field_type, LogLevel.Error);
                    string answer = JsonAnswer(false, "Required field is not in correct format, field_name=" + field_name + " field_type=" + field_type);
                    ErrorJsonRequest(answer, remoteSocket);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Json does not contain required field, field_name=" + field_name, LogLevel.Error);
                string answer = JsonAnswer(false, "Json does not contain required field, field_name=" + field_name);
                ErrorJsonRequest(answer, remoteSocket);
                return null;
            }
        }
        public dynamic DefineJsonRequest(string request, Socket remoteSocket)
        {
            JObject json = GetJsonFromRequest(request);
            if (json != null)
            {
                return json;
            }
            else
            {
                string errorJson = JsonAnswer(false, "Server can't define json object from request.");
                ErrorJsonRequest(errorJson, remoteSocket);
                logger.WriteLog("Server can't define json object from request.", LogLevel.Error);
                return null;
            }
        }
        private dynamic GetJsonFromRequest(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                string json = "";
                int searchIndex = request.IndexOf("application/json", StringComparison.Ordinal);
                if (searchIndex == -1)
                {
                    logger.WriteLog("Can not find \"application/json\" in request.", LogLevel.Error);
                    return null;
                }
                int indexFirstChar = request.IndexOf("{", searchIndex, StringComparison.Ordinal);
                if (indexFirstChar == -1)
                {
                    logger.WriteLog("Can not find start json in request.", LogLevel.Error);
                    return null;
                }
                int indexLastChar = request.LastIndexOf("}", StringComparison.Ordinal);
                if (indexLastChar == -1)
                {
                    logger.WriteLog("Can not find end json in request.", LogLevel.Error);
                    return null;
                }
                if (indexLastChar > indexFirstChar)
                {
                    json = request.Substring(indexFirstChar, indexLastChar - indexFirstChar + 1);
                    return JsonConvert.DeserializeObject<dynamic>(json);
                }
                else
                {
                    logger.WriteLog("Can not define json object in request.", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Insert request is null or empty, function GetJsonFromRequest", LogLevel.Error);
                return null;
            }
        }
        public string FindValueContentDisposition(string request, string key)
        {
            string findKey = "Content-Disposition: form-data; name=\"" + key + "\"";
            string boundary = GetBoundaryRequest(request);
            if (string.IsNullOrEmpty(boundary))
            {
                logger.WriteLog("Can not get boundary from request, function FindValueContentDisposition", LogLevel.Error);
                return null;
            }
            boundary = "\r\n--" + boundary;
            if (request.Contains(findKey))
            {
                int searchKey = request.IndexOf(findKey, StringComparison.Ordinal) + findKey.Length + "\r\n\r\n".Length;
                if (searchKey == -1)
                {
                    logger.WriteLog("Can not find content-disposition key from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
                int transfer = request.IndexOf(boundary, searchKey, StringComparison.Ordinal);
                if (transfer == -1)
                {
                    logger.WriteLog("Can not end boundary from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
                if (transfer > searchKey)
                {
                    return request.Substring(searchKey, transfer - searchKey);
                }
                else
                {
                    logger.WriteLog("Can not define key value from request, function FindValueContentDisposition", LogLevel.Error);
                    return null;
                }
            }
            else
            {
                logger.WriteLog("Request does not contain find key, function FindValueContentDisposition", LogLevel.Error);
                return null;
            }
        }
        public dynamic CheckFormDataField(string request, Socket remoteSocket, string field_name, string field_type)
        {
            string field_value = FindValueContentDisposition(request, field_name);
            if (field_value != null)
            {
                switch(field_type)
                {
                    case "int": 
                        int? field_int_value = ConvertSaveString(field_value, field_type);
                        if (field_int_value != null)
                        {
                            return field_int_value;
                        }
                        else
                        {
                            JsonRequest(JsonAnswer(false, "Can not define field_type of value"), remoteSocket);
                            return null;
                        }
                    case "string": return field_value;
                    default:
                        logger.WriteLog("Can not define field_type of value, function CheckFormDataField", LogLevel.Error);
                        JsonRequest(JsonAnswer(false, "Can not define field_type of value"), remoteSocket);
                        return null;
                }
            }
            else
            {
                logger.WriteLog("Can not define form-data value from request", LogLevel.Error);
                JsonRequest(JsonAnswer(false, "Can not define form-data value from request"), remoteSocket);
                return null;
            }
        }
        public string FindParamFromRequest(string request, string key)
        {
            Regex urlParams = new Regex(@"[\?&](" + key + @"=([^&=#\s]*))", RegexOptions.Multiline);
            Match match = urlParams.Match(request);
            if (match.Success)
            {
                string value = match.Value;
                return value.Substring(key.Length + 2);
            }
            else
            {
                logger.WriteLog("Can not define url parameter from request, function FindParamFromRequest", LogLevel.Error);
                return null;
            }
        }
        private void HttpIternalServerError(Socket remoteSocket)
        {
            string response = "";
            string responseBody = "<HTML>" +
                                 "<BODY>" +
                                 "<h1> 500 Internal Server Error...</h1>" +
                                 "</BODY></HTML>";
            response = "HTTP/1.1 500 \r\n" +
                       "Version: HTTP/1.1\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       "Content-Length: " + (response.Length + responseBody.Length) +
                       "\r\n\r\n" +
                       responseBody;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            logger.WriteLog("HTTP 500 Error link response", LogLevel.Error);
        }
        private dynamic ConvertSaveString(string value, string value_type)
        {
            if (string.IsNullOrEmpty(value))
            {
                logger.WriteLog("Value is null or empty, function ConvertSaveString", LogLevel.Error);
                return null;
            }
            try
            {
                switch (value_type)
                {
                    case "int": return Convert.ToInt32(value);
                    case "double": return Convert.ToDouble(value);
                    default: return null;
                }
            }
            catch
            {
                logger.WriteLog("Can not convert current value to type->" + value_type + ", function ConvertSaveString", LogLevel.Error);
                return null;
            }
        }
        public string GetBoundaryRequest(string request)
        {
            int i = 0;
            bool exist = false;
            string boundary = "";
            string subRequest = "";
            int first = request.IndexOf("boundary=", StringComparison.Ordinal);
            if (first == -1)
            {
                logger.WriteLog("Can not search boundary from request", LogLevel.Error);
                return "";
            }
            first += 9;                                     // boundary=.Length
            if (request.Length > 2500 + first)
            {
                subRequest = request.Substring(first, 2000);
            }
            else
            {
                subRequest = request.Substring(first);
            }
            while (!exist)
            {
                if (subRequest[i] == '\r')
                {
                    exist = true;
                }
                else
                {
                    boundary += subRequest[i];
                    i++;
                }
                if (i > 2000)
                {
                    logger.WriteLog("Can not define end of boundary request", LogLevel.Error);
                    return "";
                }
            }
            return boundary;
        }
        public string JsonData(dynamic data)
        {
            string jsonAnswer = "{\r\n" +
            	"\"success\":true,\r\n" +
                "\"data\":" + JsonConvert.SerializeObject(data) + "\r\n" +
                "}";
            return jsonAnswer;
        }
        public string JsonAnswer(bool success, string message)
        {
            string jsonAnswer = "{\r\n \"success\":" + success.ToString().ToLower() + ",\r\n" +
                " \"message\":\"" + message + "\"\r\n" +
                "}";
            return jsonAnswer;
        }
    }
}