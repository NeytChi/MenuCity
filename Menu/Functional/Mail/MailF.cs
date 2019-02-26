using Menu;
using System;
using MimeKit;
using Menu.Logger;
using Menu.NDatabase;
using MailKit.Net.Smtp;
using System.Collections.Generic;

namespace Menu.Functional.Mail
{
    public class MailF
    {
        private MailboxAddress hostMail;
        private string ip = "127.0.0.1";
        private Database database;
        private LogProgram logger;
        private List<string> listEmails = new List<string>();
        private string mailAddress = "";
        private string mailPassword = "";
        private readonly string GmailServer = "smtp.gmail.com";
        private readonly int GmailPort = 587;

        public MailF(Database callDB, LogProgram logProgram)
        {
            this.database = callDB;
            this.logger = logProgram;
            Config config = new Config();
            this.ip = config.GetConfigValue("ip", "string");
            this.mailAddress = config.GetConfigValue("mail_address", "string");
            this.mailPassword = config.GetConfigValue("mail_password", "string");
            hostMail = new MailboxAddress(ip, mailAddress);
            config = null;
        }
        public void SendEmail(string emailAddress, string subject, string message)
        {
            MimeMessage emailMessage = new MimeMessage();
            emailMessage.From.Add(hostMail);
            emailMessage.To.Add(new MailboxAddress(emailAddress));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };
            try
            {
                using (SmtpClient client = new SmtpClient())
                {
                    client.Connect(GmailServer, GmailPort, false);
                    client.Authenticate(hostMail.Address, mailPassword);
                    client.Send(emailMessage);
                    client.DisconnectAsync(true);
                    logger.WriteLog("Send message to " + emailAddress, LogLevel.Mail);
                }
            }
            catch (Exception e)
            {
                logger.WriteLog("Error SendEmailAsync, Message:" + e.Message, LogLevel.Error);
            }
        }
    }
}

