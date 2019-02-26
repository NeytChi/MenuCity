using System;
using Menu.Logger;
using Menu.NDatabase;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Menu.NDatabase.UserData;

namespace Menu.Functional.UserF
{
    /// <summary>
    /// User functional for general movement. This class will be generate functional for user ability.
    /// </summary>
    public class UserF
    {
        private Database database;
        private LogProgram logger;
        private Worker worker;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Menu.Functional.UserF.UserF"/> class.
        /// </summary>
        /// <param name="worker">Worker with logger and database references. Else it will be exception on the null object</param>
        public UserF(Worker worker)
        {
            this.logger = worker.logger;
            this.database = worker.database;
            this.worker = worker;
        }
        /// <summary>
        /// Registration user account on the specified request and remoteSocket.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void Registration(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                string user_email = worker.CheckRequiredJsonField(json, "user_email", JTokenType.String, remoteSocket);
                string user_login = worker.CheckRequiredJsonField(json, "user_login", JTokenType.String, remoteSocket);
                string user_password = worker.CheckRequiredJsonField(json, "user_password", JTokenType.String, remoteSocket);
                if (!string.IsNullOrEmpty(user_email) && !string.IsNullOrEmpty(user_login) && !string.IsNullOrEmpty(user_password))
                {
                    if (worker.validator.ValidateEmail(user_email))
                    {
                        string message = string.Empty;
                        if (worker.validator.ValidatePassword(user_password, ref message))
                        {
                            UserCache user = new UserCache();
                            user.user_email = user_email;
                            user.user_login = user_login;
                            user.user_password = worker.validator.HashPassword(user_password);
                            user.user_hash = worker.validator.GenerateHash(100);
                            user.created_at = (int)(DateTime.Now - worker.unixed).TotalSeconds;
                            user.activate = 0;
                            user = database.user.AddUser(user);
                            worker.mail.SendEmail(user.user_email, "Activate account", "Activate account url: http://" + worker.domen + "/user/activate/?hash=" + user.user_hash);
                            logger.WriteLog("Registrate new user, user_id=" + user.user_id, LogLevel.User);
                            worker.JsonRequest(worker.JsonData(user), remoteSocket);
                        }
                        else
                        {
                            worker.ErrorJsonRequest(worker.JsonAnswer(false, message), remoteSocket);
                        }
                    }
                    else
                    {
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "Validating email=" + user_email + " false"), remoteSocket);
                    }
                }
            }
        }
        /// <summary>
        /// Login user account on the specified request and remoteSocket.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void Login(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                string user_email = worker.CheckRequiredJsonField(json, "user_email", JTokenType.String, remoteSocket);
                string user_password = worker.CheckRequiredJsonField(json, "user_password", JTokenType.String, remoteSocket);
                if (!string.IsNullOrEmpty(user_email) && !string.IsNullOrEmpty(user_password))
                {
                    UserCache user = database.user.SelectUserByEmail(user_email);
                    if (user != null)
                    {
                        if (worker.validator.VerifyHashedPassword(user.user_password, user_password))
                        {
                            if (user.activate == 1)
                            {
                                user.user_password = null;
                                logger.WriteLog("Login user, user_id=" + user.user_id, LogLevel.User);
                                worker.JsonRequest(worker.JsonData(user), remoteSocket);
                            }
                            else
                            {
                                logger.WriteLog("User account is not activate.", LogLevel.Error);
                                worker.ErrorJsonRequest(worker.JsonAnswer(false, "User account is not activate."), remoteSocket);
                            }
                        }
                        else
                        {
                            logger.WriteLog("Wrong password.", LogLevel.Error);
                            worker.ErrorJsonRequest(worker.JsonAnswer(false, "Wrong password."), remoteSocket);
                        }
                    }
                    else
                    {
                        logger.WriteLog("No user with such email.", LogLevel.Error);
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "No user with such email."), remoteSocket);
                    }
                }
            }
        }
        /// <summary>
        /// Recovery user's password.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void RecoveryPassword(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                string user_email = worker.CheckRequiredJsonField(json, "user_email", JTokenType.String, remoteSocket);
                if (!string.IsNullOrEmpty(user_email))
                {
                    UserCache user = database.user.SelectUserByEmail(user_email);
                    if (user != null)
                    {
                        user.user_password = worker.validator.GenerateHash(12);
                        worker.mail.SendEmail(user_email, "Recovery password", "Recovery password =" + user.user_password);
                        user.user_password = worker.validator.HashPassword(user.user_password);
                        database.user.UpdateUserPassword(user.user_id, user.user_password);
                        logger.WriteLog("Recovery password, user_id=" + user.user_id, LogLevel.User);
                        worker.JsonRequest(worker.JsonAnswer(true, "Recovery password. Send message with password to email=" + user_email), remoteSocket);
                    }
                    else
                    {
                        logger.WriteLog("No user with such email.", LogLevel.Error);
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "No user with such email."), remoteSocket);
                    }
                }
            }
        }
        /// <summary>
        /// Changes user's password.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void ChangePassword(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                int? user_id = worker.CheckRequiredJsonField(json, "user_id", JTokenType.Integer, remoteSocket);
                string user_new_password = worker.CheckRequiredJsonField(json, "user_password", JTokenType.String, remoteSocket);
                string user_password = worker.CheckRequiredJsonField(json, "user_password", JTokenType.String, remoteSocket);
                string user_confirm_password = worker.CheckRequiredJsonField(json, "user_confirm_password", JTokenType.String, remoteSocket);
                if (user_id != null && !string.IsNullOrEmpty(user_password) && !string.IsNullOrEmpty(user_confirm_password))
                {
                    if (worker.validator.EqualsPasswords(user_password, user_confirm_password))
                    {
                        UserCache user = database.user.SelectUserById(user_id);
                        if (user != null)
                        {
                            if (worker.validator.VerifyHashedPassword(user.user_password, user_password))
                            {
                                database.user.UpdateUserPassword(user.user_id, user_new_password);
                                logger.WriteLog("Change user password, user_id=" + user.user_id, LogLevel.User);
                                worker.JsonRequest(worker.JsonAnswer(true, "Change user password, user_id=" + user.user_id), remoteSocket);
                            }
                            else
                            {
                                logger.WriteLog("Wrong password.", LogLevel.Error);
                                worker.ErrorJsonRequest(worker.JsonAnswer(false, "Wrong password."), remoteSocket);
                            }
                        }
                        else
                        {
                            logger.WriteLog("No user with such user_id.", LogLevel.Error);
                            worker.ErrorJsonRequest(worker.JsonAnswer(false, "No user with such user_id."), remoteSocket);
                        }
                    }
                    else
                    {
                        logger.WriteLog("Password does not match the confirm-password.", LogLevel.Error);
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "Password does not match the confirm-password."), remoteSocket);
                    }
                }
            }
        }
        /// <summary>
        /// Delete user account on the specified request and remoteSocket.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void Delete(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                int? user_id = worker.CheckRequiredJsonField(json, "user_id", JTokenType.Integer, remoteSocket);
                UserCache user = database.user.SelectUserById(user_id);
                if (user != null)
                {
                    database.user.DeleteUser(user_id);
                    logger.WriteLog("Delete user with user_id=" + user_id, LogLevel.User);
                    worker.JsonRequest(worker.JsonAnswer(true, "Delete user with user_id=" + user_id), remoteSocket);
                }
                else
                {
                    logger.WriteLog("No user with such user_id.", LogLevel.Error);
                    worker.ErrorJsonRequest(worker.JsonAnswer(false, "No user with such user_id."), remoteSocket);
                }
            }
        }
        /// <summary>
        /// Activate user account on the specified request and remoteSocket.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void Activate(string request, Socket remoteSocket)
        {
            string hash = worker.FindParamFromRequest(request, "hash");
            if (hash != null)
            {
                database.user.UpdateActivateUser(hash);
                worker.JsonRequest(worker.JsonAnswer(true, "Active account is done"), remoteSocket);
            }
            else
            {
                logger.WriteLog("Can not get hash from url parameters", LogLevel.Error);
                worker.JsonRequest(worker.JsonAnswer(false, "Can not get hash from url parameters"), remoteSocket);
            }
        }
    }
}
