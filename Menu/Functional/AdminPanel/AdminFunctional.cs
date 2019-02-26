using System;
using YonderMedia.DB;
using Newtonsoft.Json;
using Yonder.EventModels;
using Newtonsoft.Json.Linq;
using Yonder.Functional.Mail;
using Yonder.Functional.Pass;
using Yonder.Functional.Tasker;
using System.Collections.Generic;
using Yonder.Functional.FileWork;
using YonderMedia.Functional.FileWork;
using Yonder.Functional.TwitterFuctional;

namespace Yonder.Functional.AdminPanel
{
    public class AdminFunctional : AdminSkeleton
    {
        public TweetModule tweetModule;
        private DatabaseYonder database;
        private LoaderFile LoaderFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Yonder.Functional.AdminPanel.AdminFunctional"/> class.
        /// </summary>
        /// <param name="logProgram">Log program.</param>
        /// <param name="data_base">Data base.</param>
        /// <param name="taskManager">Task manager.</param>
        public AdminFunctional(LogProgram logProgram, DatabaseYonder data_base, TaskManager taskManager): base (logProgram,data_base,taskManager)
        {
            this.logger = logProgram;
            this.database = data_base;
            this.validator = new Validator(logger);
            this.Tasker = taskManager;
            this.Mail = new MailF(data_base, logProgram);
            this.tweetModule = new TweetModule(data_base, logProgram);
            this.LoaderFile = new LoaderFile(database, logger);
            ControlPassword();
            taskManager.ControlTokens(this);
            taskManager.HandleTweets(tweetModule);
        }
        /// <summary>
        /// Creates the event.
        /// </summary>
        /// <returns>The event.</returns>
        /// <param name="request">Request.</param>
        public string CreateEvent(string request)
        {
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                if (json.ContainsKey("event_name") && json.ContainsKey("event_happens"))
                {
                    return HandleCreateEvent(json);
                }
                else
                {
                    return JsonWithMessage("Json doesn't have required fields.", false);
                }
            }
        }
        /// <summary>
        /// Handles the create event.
        /// </summary>
        /// <returns>The create event.</returns>
        /// <param name="json">Json.</param>
        public string HandleCreateEvent(JObject json)
        {
            Event_Y event_Y = new Event_Y();
            event_Y.event_name = json.GetValue("event_name").ToString();
            event_Y.event_happens = (int)json.GetValue("event_happens");
            event_Y.created_at = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            if (json.ContainsKey("event_location_x") && json.ContainsKey("event_location_y"))
            {
                event_Y.event_location_x = (double)json.GetValue("event_location_x");
                event_Y.event_location_y = (double)json.GetValue("event_location_y");
            }
            if (json.ContainsKey("event_about"))
            {
                event_Y.event_about = json.GetValue("event_about").ToString();
            }
            database.AddEvent(event_Y);
            event_Y = database.SelectEventByCreatedAt(event_Y.created_at);
            logger.WriteLog("Add new event, event_name=" + event_Y.event_name, LogLevel.AdminPanel);
            return JsonObjectAnswer(true, "Event is successfull add to server", "event", JsonConvert.SerializeObject(event_Y));
        }
        /// <summary>
        /// Updates the events.
        /// </summary>
        /// <returns>The events.</returns>
        /// <param name="request">Request.</param>
        public string UpdateEvents(string request)
        {
            int event_id;
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                if(!json.ContainsKey("event_id"))
                {
                    logger.WriteLog("Json does not have event id, access to update - denied", LogLevel.AdminPanel);
                    return JsonWithMessage("Json does not have event id, access to update - denied", false);
                }
                else
                {
                    event_id = ConvertSaveString(json.GetValue("event_id").ToString());
                }
                if (json.ContainsKey("event_name"))
                {
                    database.UpdateEventName(event_id, json.GetValue("event_name").ToString());
                    logger.WriteLog("Update event name, event_id=" + event_id, LogLevel.AdminPanel);
                }
                if (json.ContainsKey("event_happens"))
                {
                    database.UpdateEventHappens(event_id,ConvertSaveString(json.GetValue("event_happens").ToString()));
                    logger.WriteLog("Update event happens, event_id=" + event_id, LogLevel.AdminPanel);
                }
                if (json.ContainsKey("event_location_x") && json.ContainsKey("event_location_y"))
                {
                    database.UpdateEventLocation(event_id, (double)json.GetValue("event_location_x"), (double)json.GetValue("event_location_y"));
                    logger.WriteLog("Update event location, event_id=" + event_id, LogLevel.AdminPanel);
                }
                if (json.ContainsKey("event_about"))
                {
                    database.UpdateEventAbout(event_id, json.GetValue("event_about").ToString());
                    logger.WriteLog("Update event about, event_id=" + event_id, LogLevel.AdminPanel);
                }
                Event_Y event_Y = database.SelectEventById(event_id);
                event_Y.eventImage = database.SelectImagesByEventId(event_id);
                return JsonObjectAnswer(true,"Successful update fields of event", "event", JsonConvert.SerializeObject(event_Y));
            }
        }
        /// <summary>
        /// Deletes the event.
        /// </summary>
        /// <returns>The event.</returns>
        /// <param name="request">Request.</param>
        public string DeleteEvent(string request)
        {
            int event_id;
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                if (!json.ContainsKey("event_id"))
                {
                    logger.WriteLog("Json does not have event id, access to update - denied", LogLevel.AdminPanel);
                    return JsonWithMessage("Json does not have event id, access to update - denied", false);
                }
                else
                {
                    event_id = ConvertSaveString(json.GetValue("event_id").ToString());
                }
                database.DeleteEvent(event_id);
                return JsonWithMessage("Event, event_id=" + event_id + ", successful delete", true);
            }
        }
        /// <summary>
        /// Sets the twitter account. Add screen name.
        /// </summary>
        /// <returns>The twitter account.</returns>
        /// <param name="request">Request.</param>
        public string SetTwitterAccount(string request)
        {
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                if (!json.ContainsKey("screen_name") || json.GetValue("screen_name").ToString() == "")
                {
                    logger.WriteLog("Json does not have screen_name, access to set twitter account - denied", LogLevel.Error);
                    return JsonWithMessage("Json does not have screen_name, access to set twitter account - denied", false);
                }
                else
                {
                    if (tweetModule.AddTwitterScreenNames(json.GetValue("screen_name").ToString()))
                    {
                        return JsonObjectAnswer(true, "Screen name complitle added. After a while, tweets will be show on Recent Tweets timeline", "screen_names", JsonConvert.SerializeObject(tweetModule.ScreenNames));
                    }
                    else
                    {
                        return JsonObjectAnswer(false, "Server contains the same screen name, and his will be upload to Recent Tweets timeline", "screen_names", JsonConvert.SerializeObject(tweetModule.ScreenNames));
                    }
                }
            }
        }
        /// <summary>
        /// Selects the screen names.
        /// </summary>
        /// <returns>The screen names.</returns>
        /// <param name="request">Request.</param>
        public string SelectScreenNames(string request)
        {
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                return JsonObjectAnswer(true, "After a while, another tweets will be show on Recent Tweets timeline", "screen_names", JsonConvert.SerializeObject(tweetModule.ScreenNames));
            }
        }
        /// <summary>
        /// Deletes the name of the screen.
        /// </summary>
        /// <returns>The screen name.</returns>
        /// <param name="request">Request.</param>
        public string DeleteScreenName(string request)
        {
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                if (!json.ContainsKey("screen_name") || json.GetValue("screen_name").ToString() == "")
                {
                    logger.WriteLog("Json does not have screen_name, access to set twitter account - denied", LogLevel.Error);
                    return JsonWithMessage("Json does not have screen_name, access to set twitter account - denied", false);
                }
                else
                {
                    if (tweetModule.DeleteScreenName(json.GetValue("screen_name").ToString()))
                    {
                        return JsonObjectAnswer(true, "Screen name=" + json.GetValue("screen_name") + " complitle deleted.", "screen_names", JsonConvert.SerializeObject(tweetModule.ScreenNames));
                    }
                    else
                    {
                        return JsonObjectAnswer(false, "Can not delete this screen name=" + json.GetValue("screen_name") + ". This screen name not exist", "screen_names", JsonConvert.SerializeObject(tweetModule.ScreenNames));
                    }
                }
            }
        }
        /// <summary>
        /// Set mains screen of website.
        /// </summary>
        /// <returns>The picture.</returns>
        /// <param name="request">Request.</param>
        /// <param name="buffer">Buffer.</param>
        /// <param name="bytes">Bytes.</param>
        public string MainPicture(string request, byte[] buffer, int bytes)
        {
            string token = FindValueContentDisposition(request, "token");
            if (token == "")
            {
                logger.WriteLog("Can not define token from request", LogLevel.Error);
                return JsonWithMessage("Can not define token from request", false);
            }
            if (!TokenTimes.ContainsKey(token))
            {
                logger.WriteLog("Token is not valid", LogLevel.Error);
                return JsonWithMessage("Token is not valid", false);
            }
            if (LoaderFile.AddMainScreen(request, buffer, bytes) != null)
            {
                List<MainScreen> mainScreens = database.SelectMainScreen();
                logger.WriteLog("File successfully sent", LogLevel.Worker);
                return JsonObjectAnswer(true, "File successfully sent", "main_screens", JsonConvert.SerializeObject(mainScreens));
            }
            else
            {
                return JsonWithMessage("Can not get file from request", false);
            }
        }
        public string DeleteMainPicture(string request)
        {
            string answer = "";
            JObject json = null;
            if (!CheckAuthorization(request, ref answer, ref json))
            {
                logger.WriteLog("False authorization, token is out of time", LogLevel.AdminPanel);
                return answer;
            }
            else
            {
                if (json.ContainsKey("mainscreen_id") && json.GetValue("mainscreen_id") != null)
                {
                    List<MainScreen> mainScreens = database.SelectMainScreen();
                    foreach(MainScreen mainScreen in mainScreens)
                    {
                        if (mainScreen.MainScreen_id == (int)json.GetValue("mainscreen_id"))
                        {
                            LoaderFile.DeleteFile(mainScreen.MainScreenPathServer);
                        }
                    }
                    database.DeleteMainScreen((int)json.GetValue("mainscreen_id"));
                    logger.WriteLog("Delete main screen with id=" + json.GetValue("mainscreen_id"), LogLevel.AdminPanel);
                    return JsonObjectAnswer(true,"Main screen was complitle delete from server", "main_screens", JsonConvert.SerializeObject(mainScreens));
                }
                else
                {
                    logger.WriteLog("Can not get mainscreen_id from request.", LogLevel.Error);
                    return JsonWithMessage("Can not get mainscreen_id from request.", false);
                }
            }
        }
        public string AddEventImage(string request, byte[] buffer, int bytes)
        {
            string token = FindValueContentDisposition(request, "admin_token");
            string event_id = FindValueContentDisposition(request, "event_id");
            if (!TokenTimes.ContainsKey(token))
            {
                logger.WriteLog("Token is not valid", LogLevel.Error);
                return JsonWithMessage("Token is not valid", false);
            }
            if (token == "")
            {
                logger.WriteLog("Can not define token from request", LogLevel.Error);
                return JsonWithMessage("Can not define token from request", false);
            }
            if (LoaderFile.AddEventImage(request, buffer, bytes, ConvertSaveString(event_id)) != null)
            {
                EventImage eventImage = database.SelectImagesByEventId(ConvertSaveString(event_id));
                Event_Y event_Y = database.SelectEventById(ConvertSaveString(event_id));
                event_Y.eventImage = eventImage;
                logger.WriteLog("File successfully sent", LogLevel.Worker);
                return JsonObjectAnswer(true, "File successfully sent", "event", JsonConvert.SerializeObject(event_Y));
            }
            else
            {
                return JsonWithMessage("Can not get file from request", false);
            }
        }
        private string FindValueContentDisposition(string request, string key)
        {
            string findKey = "Content-Disposition: form-data; name=\"" + key + "\"";
            string boundary = "\r\n--" + LoaderFile.GetBoundary(request);
            if (request.Contains(findKey))
            {
                int searchKey = request.IndexOf(findKey, StringComparison.Ordinal) + findKey.Length + "\r\n\r\n".Length;
                if (searchKey == -1) { return ""; }
                int transfer = request.IndexOf(boundary, searchKey, StringComparison.Ordinal);
                if (transfer == -1) { return ""; }
                return request.Substring(searchKey, transfer - searchKey);
            }
            else return "";
        }
        public string JsonObjectAnswer(bool success, string message, string jsonObjectName , string jsonObject)
        {
            string successTrue = "{\r\n" +
                                 "\"success\":" + success.ToString().ToLower() + ",\r\n" +
                                 "\"message\":\"" + message + "\",\r\n" +
                                 "\"" + jsonObjectName + "\":" + jsonObject + "\r\n" +
                                 "\r\n}";
            return successTrue;
        }
    }
}
