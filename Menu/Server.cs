using System;
using System.Net;
using Menu.Logger;
using System.Text;
using Menu.NDatabase;
using System.Threading;
using System.Net.Sockets;
using Menu.Functional.Tasker;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Menu
{
    public class Server
    {
        private static Database database = new Database();
        private static LogProgram logger = new LogProgram();

        private int port = 8000;
        private string ip = "127.0.0.1";
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private Worker worker = new Worker(database, logger);
        private IPEndPoint iPEndPoint;
        private TaskManager tasker = new TaskManager(logger);
        private Regex contentlength = new Regex("ength: [0-9]*\r\n", RegexOptions.Compiled);
        private readonly string[] methods =
        {
            "GET",
            "POST",
            "OPTIONS"
        };
        public Server()
        {
            iPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            logger.stateLogging = false;
            logger.WriteLog("Server run. Host_Port=" + ip + ":" + port, LogLevel.Server);
        }
        public Server(int Port, string IP, string Domen)
        {
            ip = IP;
            port = Port;
            worker.domen = Domen;
            iPEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
            logger.WriteLog("Server run. Host_Port=" + ip + ":" + port, LogLevel.Server);
        }
        public void InitListenSocket()
        {
            socket.Bind(iPEndPoint);
            socket.Listen(1000);
            while (true)
            {
                Socket handleSocket = socket.Accept();
                Thread thread = new Thread(() => ReceivedSocketData(handleSocket))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }
        private void ReceivedSocketData(Socket handleSocket)
        {
            byte[] buffer = new byte[1096];
            int bytes = 0;
            string request = "";
            int ContentLength = 0;
            for (; ; )
            {
                if (buffer.Length < bytes + 300)
                {
                    buffer = ChangeAppropriateBuffer(buffer.Length + 2000, buffer, bytes);
                }
                else
                {
                    bytes += handleSocket.Receive(buffer, bytes, 60, SocketFlags.None);
                }
                if (bytes > 500 && bytes < 800)
                {
                    request = Encoding.ASCII.GetString(buffer, 0, bytes);
                    if (request.Contains("content-length:") || request.Contains("Content-Length:"))
                    {
                        ContentLength = GetRequestContentLenght(request);
                        if (ContentLength > 0 && ContentLength < 2147483647 && buffer.Length == 1096)
                        {
                            buffer = ChangeAppropriateBuffer(ContentLength, buffer, bytes);
                        }
                    }
                }
                if (handleSocket.Available == 0 && bytes >= ContentLength) { break; }
                if (handleSocket.Available == 0 && bytes < ContentLength)
                {
                    if ((handleSocket.Poll(10000, SelectMode.SelectRead) && (handleSocket.Available == 0)) || !handleSocket.Connected)
                    {
                        handleSocket.Close();
                        logger.WriteLog("Remote socket was disconnected.", LogLevel.Server);
                        break;
                    }
                }
            }
            if (handleSocket.Connected)
            {
                request = Encoding.ASCII.GetString(buffer, 0, bytes);
                IdentifyRequest(request, handleSocket, buffer, bytes);
            }
            if (handleSocket.Connected) { handleSocket.Close(); }
        }
        private void IdentifyRequest(string request, Socket handleSocket, byte[] buffer, int bytes)
        {
            Debug.WriteLine("Request:");
            Debug.WriteLine(request);
            switch (GetMethodRequest(request))
            {
                case "GET":
                    HandleGetRequest(request, handleSocket);
                    break;
                case "POST":
                    HandlePostRequest(request, handleSocket, buffer, bytes);
                    break;
                case "OPTIONS":
                    HttpOptions(handleSocket);
                    break;
                default:
                    HttpErrorUrl(handleSocket);
                    break;
            }
        }
        public void HandlePostRequest(string request, Socket handleSocket, byte[] buffer, int bytes)
        {
            switch (FindURLRequest(request, "POST"))
            {
                case "user/registration": worker.user.Registration(request, handleSocket);
                    break;
                default:
                    HttpErrorUrl(handleSocket);
                    break;
            }
        }
        public void HandleGetRequest(string request, Socket handleSocket)
        {
            switch (FindURLRequest(request, "GET"))
            {
                case "logs":
                    HttpLogs(handleSocket);
                    break;
                case "sendfile":
                    HttpSend(handleSocket);
                    break;
                default:
                    HttpErrorUrl(handleSocket);
                    break;
            }
        }
        /// <summary>
        /// Finds the URL in request.
        /// </summary>
        /// <returns>The URLR equest.</returns>
        /// <param name="request">Request.</param>
        /// <param name="method">Method.</param>
        private string FindURLRequest(string request, string method)
        {
            string url = GetBetween(request, method + " ", " HTTP/1.1");
            int questionUrl = url.IndexOf('?', 1);
            if (questionUrl == -1)
            {
                url = url.Substring(1);
                if (url[url.Length - 1] != '/')
                {
                    return url.ToLower();                                       // handle this pattern url -> /Log || /Log/Level
                }
                else
                {
                    return url.Remove(url.Length - 1).ToLower();                // handle this pattern url -> /Log/ || /Log/Level/
                }
            }
            else
            {
                if (url[questionUrl - 1] == '/')                                // handle this pattern url -> /LogInfo/Account/?id=1111 -> /LogInfo/Account/
                {
                    return url.Substring(1, questionUrl - 2).ToLower();         // handle this pattern url -> Log/Account - return
                }
                else
                {
                    logger.WriteLog("Can not define pattern of url, function FindURLRequest()", LogLevel.Error);
                    return "";                                                  // Don't handle this pattern url -> /LogInfo?id=1111 and /LogInfo/Account?id=9999 
                }
            }
        }
        public string GetMethodRequest(string request)
        {
            if (string.IsNullOrEmpty(request))
            {
                if (request.Length < 20)
                {
                    logger.WriteLog("Can not define method of request, input request have not enough characters, function GetMethodRequest()", LogLevel.Error);
                    return "";
                }
                string requestMethod = request.Substring(0, 20);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (requestMethod.Contains(methods[i]))
                    {
                        int start = request.IndexOf(methods[i], 0, StringComparison.Ordinal);
                        return request.Substring(start, methods[i].Length);
                    }
                }
                logger.WriteLog("Can not define method of request, function GetMethodRequest()", LogLevel.Error);
                return "";
            }
            else
            {
                logger.WriteLog("Input request is null or empty, function GetMethodRequest", LogLevel.Error);
                return "";
            }
        }
        public string GetBetween(string source, string start, string end)
        {
            if (string.IsNullOrEmpty(source))
            {
                if (source.Contains(start) && source.Contains(end))
                {
                    int Start = source.IndexOf(start, 0, StringComparison.Ordinal) + start.Length;
                    if (Start == -1)
                    {
                        logger.WriteLog("Can not find start of source, function GetBetween()", LogLevel.Error);
                        return "";
                    }
                    int End = source.IndexOf(end, Start, StringComparison.Ordinal);
                    if (End == -1)
                    {
                        logger.WriteLog("Can not find end of source, function GetBetween()", LogLevel.Error);
                        return "";
                    }
                    return source.Substring(Start, End - Start);
                }
                else
                {
                    logger.WriteLog("Source does not contains search values, function GetBetween()", LogLevel.Error);
                    return "";
                }
            }
            else
            {
                logger.WriteLog("Source is null or empty, function GetBetween()", LogLevel.Error);
                return "";
            }
        }
        private void HttpOptions(Socket remoteSocket)
        {
            string response = "HTTP/1.1 200 OK\r\n" +
                              "Access-Control-Allow-Methods: POST, GET, OPTIONS\r\n" +
                              "Access-Control-Allow-Headers: *\r\n" +
                              "Access-Control-Allow-Origin: *\r\n" +
                              "Vary: Accept-Encoding, Origin\r\n" +
                              "Content-Encoding: gzip\r\n" +
                              "Content-Length: 0\r\n" +
                              "Keep-Alive: timeout=300\r\n" +
                              "Connection: Keep-Alive\r\n" +
                              "Content-Type: multipart/form-data\r\n\r\n";
            remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            logger.WriteLog("HTTP Response " + ip + ":" + port.ToString() + " - OPTIONS", LogLevel.Server);
        }
        private void HttpLogs(Socket remoteSocket)
        {
            string response = "";
            byte[] logs = logger.ReadMassiveLogs();
            string start = "<HTML><BODY>Logs massive information:<br><hr>";
            string end = "</BODY></HTML>";
            response = "HTTP/1.1 200 OK\r\n" +
                       "Version: HTTP/1.1\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       "Content-Length: " + (start.Length + end.Length + logs.Length) +
                       "\r\n\r\n" +
                        start;
            byte[] answerstart = Encoding.ASCII.GetBytes(response);
            byte[] answerend = Encoding.ASCII.GetBytes(end);
            byte[] requestbyte = new byte[logs.Length + answerstart.Length + answerend.Length];
            answerstart.CopyTo(requestbyte, 0);
            logs.CopyTo(requestbyte, answerstart.Length);
            answerend.CopyTo(requestbyte, answerstart.Length + logs.Length);
            remoteSocket.Send(requestbyte);
            GC.Collect();
            logger.WriteLog("HTTP Response 127.0.0.1:8000/LogInfo/", LogLevel.Server);
        }
        private void HttpSend(Socket remoteSocket)
        {
            string response = "";
            string responseBody = "";
            responseBody = string.Format("<HTML>" + "<BODY>My web page.<br>" +
                                         "<form name = \"form1\" method=\"post\"enctype = \"multipart/form-data\" action = \"api/upload\">" +
                                         "<div>" +
                                         "<label for= \"image1\" > Image File </ label >" +
                                         "<input name = \"image1\" type = \"file\" />" +
                                         "</div>" +
                                         "<div>" +
                                         "<input type = \"submit\" value = \"Submit\" />" +
                                         "</div>" +
                                         "</form ></BODY></HTML > ");
            response = "HTTP/1.1 200 OK\r\n" +
                       "Version: HTTP/1.1\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       "Content-Length: " + responseBody.Length +
                       "\r\n\r\n" +
                       responseBody;
            remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            logger.WriteLog("HTTP Response 127.0.0.1:8000/SendFile/", LogLevel.Server);
        }
        private void HttpErrorUrl(Socket remoteSocket)
        {
            string response = "";
            string responseBody = "";
            responseBody = string.Format("<HTML>" + "<BODY>" +
                                         "<h1>error url...</h1>" +
                                         "</BODY>" + "</HTML>");
            response = "HTTP/1.1 400 \r\n" +
                        "Version: HTTP/1.1\r\n" +
                        "Content-Type: text/html; charset=utf-8\r\n" +
                        "Content-Length: " + (response.Length + responseBody.Length) +
                        "\r\n\r\n" +
                        responseBody;
            if (remoteSocket.Connected)
            {
                remoteSocket.Send(Encoding.ASCII.GetBytes(response));
            }
            logger.WriteLog("HTTP 400 Error link response", LogLevel.Error);
        }
        /// <summary>
        /// Gets value "content lenght" from request.
        /// </summary>
        /// <returns>The request content lenght.</returns>
        /// <param name="request">Picie of request.</param>
        public int GetRequestContentLenght(string request)
        {
            try
            {
                Match resultContentLength = contentlength.Match(request);
                if (resultContentLength.Success)
                {
                    return Convert.ToInt32(resultContentLength.Value.Substring("ength: ".Length)) + resultContentLength.Index + resultContentLength.Length;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                logger.WriteLog("Error function GetRequestContentLenght(), exception with converting to int value", LogLevel.Error);
                return 0;
            }
        }
        /// <summary>
        /// Changes appropriate buffer. Changes small buffer for this request, to new buffer for donwloding file and t.e.. 
        /// </summary>
        /// <returns>The appropriate buffer.</returns>
        /// <param name="size">Size. Size to new buffer. == Content-Length</param>
        /// <param name="currentbuffer">Currentbuffer. Copy info from last to new buffer</param>
        /// <param name="bytes">Bytes. Need to copy last buffer to new buffer.</param>
        private byte[] ChangeAppropriateBuffer(int size, byte[] currentbuffer, int bytes)
        {
            byte[] new_buffer = new byte[size + bytes];
            Array.Copy(currentbuffer, new_buffer, bytes);
            logger.WriteLog("Change dynamic buffer for request. From buffer.length=" + currentbuffer.Length + " to buffer.length=" + new_buffer.Length, LogLevel.Server);
            return new_buffer;
        }
    }
}
