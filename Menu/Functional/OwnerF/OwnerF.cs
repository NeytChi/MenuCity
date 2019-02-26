using System;
using Menu.Logger;
using Menu.NDatabase;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using Menu.NDatabase.OwnerData;

namespace Menu.Functional.OwnerF
{
    /// <summary>
    /// Owner functional for general movement. This class will be generate functional for owner ability.
    /// </summary>
    public class OwnerF
    {
        private Database database;
        private LogProgram logger;
        private Worker worker;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Menu.Functional.OwnerF.OwnerF"/> class.
        /// </summary>
        /// <param name="worker">Worker with logger and database references. Else it will be exception on the null object</param>
        public OwnerF(Worker worker)
        {
            this.logger = worker.logger;
            this.database = worker.database;
            this.worker = worker;
        }
        /// <summary>
        /// Registration owner account on the specified request and remoteSocket.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="remoteSocket">Remote socket.</param>
        public void Registration(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                string owner_email = worker.CheckRequiredJsonField(json, "owner_email", JTokenType.String, remoteSocket);
                string owner_password = worker.CheckRequiredJsonField(json, "owner_password", JTokenType.String, remoteSocket);
                if (!string.IsNullOrEmpty(owner_email) && !string.IsNullOrEmpty(owner_password))
                {
                    if (worker.validator.ValidateEmail(owner_email))
                    {
                        string message = string.Empty;
                        if (worker.validator.ValidatePassword(owner_password, ref message))
                        {
                            Owner owner = new Owner();
                            owner.owner_email = owner_email;
                            owner.owner_password = worker.validator.HashPassword(owner_password);
                            owner.owner_hash = worker.validator.GenerateHash(100);
                            owner.created_at = (int)(DateTime.Now - worker.unixed).TotalSeconds;
                            owner.activate = 0;
                            owner = database.owner.AddOwner(owner);
                            worker.mail.SendEmail(owner.owner_email, "Activate account", "Activate account url: http://" + worker.domen + "/owner/activate/?hash=" + owner.owner_hash);
                            logger.WriteLog("Registrate new owner, owner_id=" + owner.owner_id, LogLevel.Owner);
                            worker.JsonRequest(worker.JsonData(owner), remoteSocket);
                        }
                        else
                        {
                            worker.ErrorJsonRequest(worker.JsonAnswer(false, message), remoteSocket);
                        }
                    }
                    else
                    {
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "Validating email=" + owner_email + " false"), remoteSocket);
                    }
                }
            }
        }
        public void Login(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                string owner_email = worker.CheckRequiredJsonField(json, "owner_email", JTokenType.String, remoteSocket);
                string owner_password = worker.CheckRequiredJsonField(json, "owner_password", JTokenType.String, remoteSocket);
                if (!string.IsNullOrEmpty(owner_email) && !string.IsNullOrEmpty(owner_password))
                {
                    Owner owner = database.owner.SelectOwnerByEmail(owner_email);
                    if (owner != null)
                    {
                        if (worker.validator.VerifyHashedPassword(owner.owner_password, owner_password))
                        {
                            if (owner.activate == 1)
                            {
                                owner.owner_password = null;
                                logger.WriteLog("Login owner, owner_id=" + owner.owner_id, LogLevel.User);
                                worker.JsonRequest(worker.JsonData(owner), remoteSocket);
                            }
                            else
                            {
                                logger.WriteLog("Owner account is not activate.", LogLevel.Error);
                                worker.ErrorJsonRequest(worker.JsonAnswer(false, "Owner account is not activate."), remoteSocket);
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
                        logger.WriteLog("No such owner with this email.", LogLevel.Error);
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "No such owner with this email."), remoteSocket);
                    }
                }
            }
        }
        public void RecoveryPassword(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                string owner_email = worker.CheckRequiredJsonField(json, "owner_email", JTokenType.String, remoteSocket);
                if (!string.IsNullOrEmpty(owner_email))
                {
                    Owner owner = database.owner.SelectOwnerByEmail(owner_email);
                    if (owner != null)
                    {
                        owner.owner_password = worker.validator.GenerateHash(12);
                        worker.mail.SendEmail(owner_email, "Recovery password", "Recovery password =" + owner.owner_password);
                        owner.owner_password = worker.validator.HashPassword(owner.owner_password);
                        database.owner.UpdateOwnerPassword(owner.owner_id, owner.owner_password);
                        logger.WriteLog("Recovery password, owner_id=" + owner.owner_id, LogLevel.Owner);
                        worker.JsonRequest(worker.JsonAnswer(true, "Recovery password. Send message with password to email=" + owner_email), remoteSocket);
                    }
                    else
                    {
                        logger.WriteLog("No such owner with this email.", LogLevel.Error);
                        worker.ErrorJsonRequest(worker.JsonAnswer(false, "No such user with this email."), remoteSocket);
                    }
                }
            }
        }
        public void ChangePassword(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                int? owner_id = worker.CheckRequiredJsonField(json, "owner_id", JTokenType.Integer, remoteSocket);
                string owner_password = worker.CheckRequiredJsonField(json, "owner_password", JTokenType.String, remoteSocket);
                string owner_confirm_password = worker.CheckRequiredJsonField(json, "owner_confirm_password", JTokenType.String, remoteSocket);
                string owner_new_password = worker.CheckRequiredJsonField(json, "owner_new_password", JTokenType.String, remoteSocket);
                if (owner_id != null && !string.IsNullOrEmpty(owner_password) && !string.IsNullOrEmpty(owner_confirm_password))
                {
                    if (worker.validator.EqualsPasswords(owner_password, owner_confirm_password))
                    {
                        Owner owner = database.owner.SelectOnwerById(owner_id);
                        if (owner != null)
                        {
                            if (worker.validator.VerifyHashedPassword(owner.owner_password, owner_password))
                            {
                                database.owner.UpdateOwnerPassword(owner.owner_id, owner_new_password);
                                logger.WriteLog("Change owner password, owner_id=" + owner.owner_id, LogLevel.Owner);
                                worker.JsonRequest(worker.JsonAnswer(true, "Change owner password, owner_id=" + owner.owner_id), remoteSocket);
                            }
                            else
                            {
                                logger.WriteLog("Wrong password.", LogLevel.Error);
                                worker.ErrorJsonRequest(worker.JsonAnswer(false, "Wrong password."), remoteSocket);
                            }
                        }
                        else
                        {
                            logger.WriteLog("No such owner with this owner_id.", LogLevel.Error);
                            worker.ErrorJsonRequest(worker.JsonAnswer(false, "No such owner with this owner_id"), remoteSocket);
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
        public void Delete(string request, Socket remoteSocket)
        {
            JObject json = worker.DefineJsonRequest(request, remoteSocket);
            if (json == null)
            {
                int? owner_id = worker.CheckRequiredJsonField(json, "owner_id", JTokenType.Integer, remoteSocket);
                Owner owner = database.owner.SelectOnwerById(owner_id);
                if (owner != null)
                {
                    database.owner.DeleteOwner(owner_id);
                    logger.WriteLog("Delete owner with owner_id=" + owner_id, LogLevel.User);
                    worker.JsonRequest(worker.JsonAnswer(true, "Delete owner with owner_id=" + owner_id), remoteSocket);
                }
                else
                {
                    logger.WriteLog("No such user with this owner_id.", LogLevel.Error);
                    worker.ErrorJsonRequest(worker.JsonAnswer(false, "No such user with this owner_id"), remoteSocket);
                }
            }
        }
        public void Activate(string request, Socket remoteSocket)
        {
            string hash = worker.FindParamFromRequest(request, "hash");
            if (hash != null)
            {
                database.owner.UpdateActivateOwner(hash);
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

