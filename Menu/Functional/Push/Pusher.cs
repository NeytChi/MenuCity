using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using PushSharp.Apple;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PushSharp.Google;
using PushSharp.Core;

namespace Yonder.Functional.Push
{
    public class PushJson
    {
        public string alert = "testing";
        public int badge = 1;
        public string sound = "default";
        public string category = "test";
    }
    public class ApnsPush
    {
        private LogProgram logger;
        private string Current_Directory = "/home/neytchi/Projects/Skeleton/Skeleton/bin/Debug";
        List<string> MY_DEVICE_TOKENS = new List<string>();

        public ApnsPush()
        {

        }
        public ApnsPush(LogProgram logProgram)
        {
            this.logger = logProgram;
            Config config = new Config();
            Current_Directory = config.currentDirectory;
        }
        public void Test()
        {
            var config = new ApnsConfiguration(ApnsConfiguration.ApnsServerEnvironment.Sandbox, Current_Directory + "/" + "testCert.p12", "pass1234");
            var apnsBroker = new ApnsServiceBroker(config);
            apnsBroker.OnNotificationFailed += (notification, aggregateEx) => 
            {
                aggregateEx.Handle(ex => 
                {
                    if (ex is ApnsNotificationException notificationException)
                    {
                        var apnsNotification = notificationException.Notification;
                        var statusCode = notificationException.ErrorStatusCode;
                        Console.WriteLine($"Apple Notification Failed: ID={apnsNotification.Identifier}, Code={statusCode}");
                    }
                    else
                    {
                        Console.WriteLine($"Apple Notification Failed for some unknown reason : {ex.InnerException}");
                    }
                    return true;
                });
            };
            apnsBroker.OnNotificationSucceeded += (notification) => 
            {
                Console.WriteLine("Apple Notification Sent!");
            };
            apnsBroker.Start();
            foreach (var deviceToken in MY_DEVICE_TOKENS)
            {
                apnsBroker.QueueNotification(new ApnsNotification
                {
                    DeviceToken = deviceToken,
                    Payload = JObject.Parse("{\"aps\":{\"badge\":7}}")
                });
            }
            apnsBroker.Stop();
        }
    }
    public class GCMPush
    {
        List<string> MY_REGISTRATION_IDS = new List<string>();
        private string GCM_sender_id = "677385549022";
        private string Auth_Token = "AIzaSyBb8Y6-zLQ_yhSjgqTUiA2OKO3ij1SmYrg";

        public GCMPush()
        {
        }
        public void Test()
        {
            var config = new GcmConfiguration(GCM_sender_id, Auth_Token, null);
            var provider = "GCM";
            var gcmBroker = new GcmServiceBroker(config);
            gcmBroker.OnNotificationFailed += (notification, aggregateEx) => 
            {
                aggregateEx.Handle(ex => 
                {
                    if (ex is GcmNotificationException notificationException)
                    {
                        var gcmNotification = notificationException.Notification;
                        var description = notificationException.Description;
                        Console.WriteLine($"{provider} Notification Failed: ID={gcmNotification.MessageId}, Desc={description}");
                    }
                    else if (ex is GcmMulticastResultException multicastException)
                    {
                        foreach (var succeededNotification in multicastException.Succeeded)
                        {
                            Console.WriteLine($"{provider} Notification Succeeded: ID={succeededNotification.MessageId}");
                        }
                        foreach (var failedKvp in multicastException.Failed)
                        {
                            var n = failedKvp.Key;
                            var e = failedKvp.Value;

                            Console.WriteLine($"{provider} Notification Failed: ID={n.MessageId}");
                        }
                    }
                    else if (ex is DeviceSubscriptionExpiredException expiredException)
                    {
                        var oldId = expiredException.OldSubscriptionId;
                        var newId = expiredException.NewSubscriptionId;
                        Console.WriteLine($"Device RegistrationId Expired: {oldId}");
                        if (!string.IsNullOrWhiteSpace(newId))
                        {
                            Console.WriteLine($"Device RegistrationId Changed To: {newId}");
                        }
                    }
                    else if (ex is RetryAfterException retryException)
                    {
                        Console.WriteLine($"{provider} Rate Limited, don't send more until after {retryException.RetryAfterUtc}");
                    }
                    else
                    {
                        Console.WriteLine("{provider} Notification Failed for some unknown reason");
                    }
                    return true;
                });
            };
            gcmBroker.OnNotificationSucceeded += (notification) => {
                Console.WriteLine("{provider} Notification Sent!");
            };
            gcmBroker.Start();
            foreach (var regId in MY_REGISTRATION_IDS)
            {
                gcmBroker.QueueNotification(new GcmNotification
                {
                    RegistrationIds = new List<string> { regId },
                    Data = JObject.Parse("{ \"somekey\" : \"somevalue\" }")
                });
            }
            gcmBroker.Stop();
        }
    }
	public class PushNotificationPayload
    {
        public string deviceToken;
        public string message;
        public string sound;
        public int badge;

        public string PushPayload()
        {
            return "{\"aps\":{\"alert\":\"" + message + "\",\"badge\":" + badge + ",\"sound\":\"" + sound + "\"}}";
        }
    }
	public class ApplePusher
    {
		private LogProgram loger;
		int port = 2195;
        string hostname = "gateway.sandbox.push.apple.com";
        //string hostname = "gateway.push.apple.com";
        string certificatePath = "Skeleton/Skeleton/bin/Debug/testCert.p12";
        string pwd = "root";
            

		public ApplePusher(LogProgram logProgram) 
        {
			this.loger = logProgram;
			Config config = new Config();
			certificatePath = config.getValueFromJson("common_path") + certificatePath;
        }

        public void SendNotification(string deviceToken, string notification) 
        {
            X509Certificate2 clientCertificate = new X509Certificate2(File.ReadAllBytes(certificatePath), pwd);
            X509Certificate2Collection certificatesCollection = new X509Certificate2Collection(clientCertificate);
            TcpClient tcpClient = new TcpClient(hostname, port);
            SslStream sslStream = new SslStream(tcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
            try
            {
                sslStream.AuthenticateAsClient(hostname, certificatesCollection, SslProtocols.Tls, false);
                MemoryStream memoryStream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(memoryStream);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)32);
                writer.Write(HexStringToByteArray(deviceToken.ToUpper()));
                string payload = "{\"aps\":{\"alert\":\"" + notification + "\",\"badge\":1,\"sound\":\"default\"}}";
                writer.Write((byte)0);
                writer.Write((byte)payload.Length);
                byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
                writer.Write(payloadBytes);
                writer.Flush();
                byte[] memoryStreamAsBytes = memoryStream.ToArray();
                sslStream.Write(memoryStreamAsBytes);
                sslStream.Flush();
                tcpClient.Close();
            }         
            catch (Exception e) {
				Console.WriteLine("Apple SendNotification() func false working" + e.Message);
                tcpClient.Close();
            }
        }      
        private byte[] HexStringToByteArray(string hexString) {
            return Enumerable.Range(0, hexString.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                     .ToArray();
        }      
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            return false;
        }
    }
}