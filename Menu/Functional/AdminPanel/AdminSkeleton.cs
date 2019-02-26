using System;
using YonderMedia.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Yonder.Functional.Mail;
using Yonder.Functional.Pass;
using Yonder.Functional.Tasker;
using System.Collections.Generic;

namespace Yonder.Functional.AdminPanel
{
    public class AdminSkeleton
    {
        public LogProgram logger;
        private Database database;
        public Validator validator;
        public TaskManager Tasker;
        public MailF Mail;
        public Dictionary<string, int> TokenTimes = new Dictionary<string, int>();
        public Dictionary<string, string> TokenRight = new Dictionary<string, string>();
        public Dictionary<string, string> EmailPassword = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Yonder.Functional.AdminPanel.AdminFunctional"/> class.
        /// </summary>
        /// <param name="logProgram">Log program.</param>
        /// <param name="data_base">Data base.</param>
        /// <param name="taskManager">Task manager.</param>
        public AdminSkeleton(LogProgram logProgram, Database data_base, TaskManager taskManager)
        {
            this.logger = logProgram;
            this.database = data_base;
            this.validator = new Validator(logger);
            this.Tasker = taskManager;
            this.Mail = new MailF(data_base, logProgram);
            taskManager.ControlTokens(this);
        }
        public void ControlPassword()
        {
            Admin admin = database.SelectAdmin("neyton61@gmail.com");
            if (admin == null)
            {
                admin = new Admin();
                admin.admin_email = "neyton61@gmail.com";
                admin.admin_password = validator.HashPassword("Pass1234");
                admin.create_at = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                database.AddAdmin(admin);
                logger.WriteLog("Add new admin to server base", LogLevel.AdminPanel);
            }
            EmailPassword.Add(admin.admin_email, admin.admin_password);
        }
        /// <summary>
        /// Authorization admin from specified request.
        /// </summary>
        /// <returns>The authorization.</returns>
        /// <param name="request">Request.</param>
        public string Authorization(string request)
        {
            JObject json = GetJsonFromRequest(request);
            if (json == null)
            {
                return JsonWithMessage("Can not get json from request", false);
            }
            else
            {
                if (json.ContainsKey("email") && json.ContainsKey("password"))  // && json.ContainsKey("access_right")
                {
                    return HandleAuthorization(json.GetValue("email").ToString(),
                    json.GetValue("password").ToString());                      // json.GetValue("access_right").ToString()
                }
                else
                {
                    return JsonWithMessage("Json doesn't have required fields.", false);
                }
            }
        }
        /// <summary>
        /// Handles the authorization.
        /// </summary>
        /// <returns>The authorization.</returns>
        /// <param name="email">Email.</param>
        /// <param name="password">Password.</param>
        private string HandleAuthorization(string email, string password)       //string access_right
        {
            string hash = validator.GenerateHash(100);
            if (!EmailPassword.ContainsKey(email))
            {
                return JsonWithMessage("Validation email - false. Does not has this email. Ask about email server creator.", false);
            }
            if (!validator.VerifyHashedPassword(EmailPassword[email], password))
            {
                return JsonWithMessage("Validation password - false. Password does not corresponds with email", false);
            }
            TokenTimes.Add(hash, (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
            logger.WriteLog("Verification accout - true", LogLevel.AdminPanel);
            return SuccessTrueToken("Verification admin account - true", hash);
        }
        /// <summary>
        /// Recovery the specified request.
        /// </summary>
        /// <returns>The recovery.</returns>
        /// <param name="request">Request.</param>
        public string Recovery(string request)
        {
            string NewPassword = validator.GenerateHash(12);
            string HashNewPassword = validator.HashPassword(NewPassword);
            JObject json = GetJsonFromRequest(request);
            if (json == null)
            {
                logger.WriteLog("Can not get json from request", LogLevel.AdminPanel);
                return JsonWithMessage("Can not get json from request", false);
            }
            if (json.ContainsKey("email"))
            {
                if (EmailPassword.ContainsKey(json.GetValue("email").ToString()))
                {
                    EmailPassword.Remove(json.GetValue("email").ToString());
                    EmailPassword.Add(json.GetValue("email").ToString(), HashNewPassword);
                    Mail.SendEmailAsync(json.GetValue("email").ToString(), "Recovery password", "Recovery password. New password:" + NewPassword);
                    database.UpdateAdminPassword(HashNewPassword, json.GetValue("email").ToString());
                    logger.WriteLog("Send message to recovery password", LogLevel.AdminPanel);
                    return JsonWithMessage("New password sent to your email", true);
                }
                else
                {
                    logger.WriteLog("Recovery password, unknow email-address", LogLevel.AdminPanel);
                    return JsonWithMessage("Unknow email-address", false);
                }
            }
            else
            {
                logger.WriteLog("Json does not have required fields.", LogLevel.AdminPanel);
                return JsonWithMessage("Json doesn't have required fields.", false);
            }
        }
        private dynamic GetJsonFromRequest(string request)
        {
            try
            {
                string json = "";
                int searchIndex = request.IndexOf("application/json", StringComparison.Ordinal);
                if (searchIndex == -1) { return null; }
                int indexFirstChar = request.IndexOf("{", searchIndex, StringComparison.Ordinal);     
                if (indexFirstChar == -1) { return null; }
                int indexLastChar = request.LastIndexOf("}", StringComparison.Ordinal); 
                if (indexLastChar == -1) { return null; }
                json = request.Substring(indexFirstChar, indexLastChar - indexFirstChar + 1);
                logger.WriteLog("Get json from request", LogLevel.AdminPanel);
                return JsonConvert.DeserializeObject<dynamic>(json);
            }
            catch (Exception)
            {
                logger.WriteLog("Can not get json from request", LogLevel.Error);
                return null;
            }
        }
        public bool CheckAuthorization(string request, ref string answer, ref JObject json)
        {
            json = GetJsonFromRequest(request);
            if (json == null)
            {
                answer = JsonWithMessage("Can not get json from request",false);
                return false;
            }
            if (!json.ContainsKey("token"))
            {
                answer = JsonWithMessage("Json doesn't have required fields.", false);
                return false;
            }
            if (!TokenTimes.ContainsKey(json.GetValue("token").ToString()))
            {
                answer = JsonWithMessage("Token is not valid", false);
                return false;
            }
            else
            { 
                return true; 
            }
        }
        public void ControlTokens()
        {
            int timer = 0;
            DateTime Hour1Left = DateTime.Now.AddHours(-1);
            timer = (int)(Hour1Left.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
            foreach (KeyValuePair<string, int> token_times in TokenTimes)
            {
                if (token_times.Value < timer)
                {
                    TokenTimes.Remove(token_times.Key);
                }
            }
            logger.WriteLog("Remove old tokens", LogLevel.AdminPanel);
        }
        public string JsonWithMessage(string message, bool success)
        {
            return "{\r\n \"success\":" + success.ToString().ToLower() + ",\r\n\"message\": \"" + message + "\"}";
        }
        public string SuccessTrueToken(string message, string token)
        {
            string successTrue = "{" +
        	                     "\r\n\"success\":true," +
        	                     "\r\n\"token\":\"" + token + "\"," +
                                 "\r\n\"message\":\"" + message + "\"" +
                                 "\r\n}";
            return successTrue;
        }
        public string SuccessTrueMessageAndJson(string message, string json)
        {
            string successTrue = "{\r\n" +
                                 "\"success\":true,\r\n" +
                                 "\"message\":\"" + message + "\",\r\n" +
                                 json + 
                                 "\r\n}";
            return successTrue;
        }
        public int ConvertSaveString(string resouce)
        {
            if (resouce == "") { return -1; }
            try
            {
                return Convert.ToInt32(resouce);
            }
            catch
            {
                return -1;
            }
        }
    }
}
