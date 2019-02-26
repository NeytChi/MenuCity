using System;
using Menu.Logger;
using System.Diagnostics;
using Menu.NDatabase;

namespace Menu
{
	public class Run 
	{
        public static int port;
        public static string ip;
        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "-f": Server server1 = new Server(port, ip);
                        server1.InitListenSocket();
                        break;
                    case "-u": Server server2 = new Server(port, ip);
                        server2.InitListenSocket();
                        break;
                    case "-r": LaunchReadOnly();
                        break;
                    case "-d": Server server3 = new Server();
                        server3.InitListenSocket();
                        break;
                    case "-c": Database database = new Database();
                        database.DropTables();
                        break;
                    case "-h": case "-help": Helper();
                        break;
                    default: Console.WriteLine("Turn first parameter for initialize server. You can turned keys: -h or -help - to see instruction of start servers modes.");
                        break;
                }
            }
            else
            {
                Config config = new Config();
                Server server = new Server(config.Port, config.IP);
                server.InitListenSocket();
            }
        }
        public static void Helper()
        {
            string[] commands = { "-f [time_in_minutes]", "-r", "-u", "-d", "-c", "-h or -help" };
            string[] description =
            {
                "Start server in full working cycle. After first key, second key set time to cycle for upper program. By default, it's set 5 minutes.",
                "Start reading logs from server." ,
                "Start server in non-full working cycle. Init server without upper program.",
                "Start server in default configuration settings.",
                "Start the database cleanup mode." ,
                "Helps contains 5 modes of the server that cound be used."
            };
            Console.WriteLine();
            for (int i = 0; i < commands.Length; i++) { Console.WriteLine(commands[i] + "\t - " + description[i]); }
        }
        public static void LaunchReadOnly()
        {
            LogProgram log = new LogProgram();
            log.ReadConsoleLogsDatabase();
        }  
	}
}
