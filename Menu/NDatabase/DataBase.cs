using System;
using System.Data;
using Menu.NDatabase.LogData;
using MySql.Data.MySqlClient;
using Menu.NDatabase.UserData;
using Menu.NDatabase.FileData;
using Menu.NDatabase.OwnerData;
using System.Collections.Generic;
using Menu.NDatabase.InstitutionData;

namespace Menu.NDatabase
{
    public class Database
    {
        public object locker = new object();
        public string defaultNameDB = "menu_city";
        public MySqlConnectionStringBuilder connectionstring = new MySqlConnectionStringBuilder();
        public MySqlConnection connection;

        public UserStorage user;
        public InstitutionStorage institution;
        public LogStorage log;
        public FileStorage file;
        public OwnerStorage owner;

        public List<Storage> storages = new List<Storage>();

        public Database()
        {
            Console.WriteLine("MySQL connection...");
            GetJsonConfig();
            connection = new MySqlConnection(connectionstring.ToString());
            connection.Open();
            SetMainStorages();
            CheckingAllTables();
            Console.WriteLine("MySQL connected.");
        }
        private void SetMainStorages()
        {
            user = new UserStorage(connection, locker);
            institution = new InstitutionStorage(connection, locker);
            log = new LogStorage(connection, locker);
            file = new FileStorage(connection, locker);
            owner = new OwnerStorage(connection, locker);
            storages.Add(user);
            storages.Add(institution);
            storages.Add(log);
            storages.Add(file);
            storages.Add(owner);
        }
        public bool GetJsonConfig()
        {
            string Json = GetConfigDatabase();
            connectionstring.Pooling = true;
            connectionstring.SslMode = MySqlSslMode.None;
            connectionstring.ConnectionReset = false;
            if (Json == "")
            {
                connectionstring.Server = "localhost";
                connectionstring.Database = "databasename";
                connectionstring.UserID = "root";
                connectionstring.Password = "root";
                defaultNameDB = "databasename";
                return false;
            }
            else
            {
                var configJson = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(Json);
                connectionstring.Server = configJson["Server"].ToString();
                connectionstring.Database = configJson["Database"].ToString();
                connectionstring.UserID = configJson["User ID"].ToString();
                connectionstring.Password = configJson["Password"].ToString();
                defaultNameDB = configJson["Database"].ToString();
                return true;
            }
        }
        private static string GetConfigDatabase()
        {
            if (System.IO.File.Exists("database.conf"))
            {
                using (var fstream = System.IO.File.OpenRead("database.conf"))
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
                Console.WriteLine("Function getConfigInfoDB() doesn't get database configuration information. Server DB starting with default configuration.");
                return string.Empty;
            }
        }
        public bool CheckingAllTables()
        {
            bool checking = true;
            CheckDatabaseExists(defaultNameDB);
            foreach(Storage storage in storages)
            {
                if(!CheckTableExists(storage.table))
                {
                    checking = false;
                    Console.WriteLine("The table=" + storage.table + " didn't create.");
                }
            }
            Console.WriteLine("The specified tables created.");
            return checking;
        }
        private bool CheckTableExists(string sqlCreateCommand)
        {
            try
            {
                using (MySqlCommand command = new MySqlCommand(sqlCreateCommand, connection))
                {
                    command.ExecuteNonQuery();
                    command.Dispose();
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\nError function CheckTableExists().\r\n{1}\r\nMessage:\r\n{0}\r\n", e.Message, sqlCreateCommand);
                return false;
            }
        }
        public bool DropTables()
        {
            string command = "DROP TABLE {0};";
            foreach(Storage storage in storages)
            { 
                command = string.Format(command, storage.table_name);
                using (MySqlCommand commandSQL = new MySqlCommand(command, connection))
                {
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
            return true;
        }
        public bool CheckDatabaseExists(string databaseName)
        {
            bool check = false;
            if (connection.State == ConnectionState.Open)
            {
                using (MySqlCommand command = new MySqlCommand($"SELECT ('{databaseName}');", connection))
                {
                    check = command.ExecuteScalar() != DBNull.Value;
                }
            }
            else
            {
                Console.WriteLine("Error connection...");
                check = true;
            }
            if (check == false)
            {
                string creatingDB = "CREATE DATABASE IF NOT EXISTS " + defaultNameDB + ";";
                MySqlCommand command = new MySqlCommand(creatingDB, connection);
                command.ExecuteNonQuery();
                command.Dispose();
                return true;
            }
            else return true;
        }
        public Storage AddStorage(Storage storage)
        {
            storage.locker = locker;
            storage.connection = connection;
            if (!CheckTableExists(storage.table))
            {
                Console.WriteLine("The table=" + storage.table + " didn't create.");
                return null;
            }
            return storage;
        }
    }
    public interface IDatabaseLogs
    {
        bool InitConnection();
        void AddLogs(Log log);
        List<Log> SelectLogs();
    }
}
