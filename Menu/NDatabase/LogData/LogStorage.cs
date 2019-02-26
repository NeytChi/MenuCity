using Menu.Logger;
using Menu.NDatabase;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace Menu.NDatabase.LogData
{
    public class LogStorage : Storage
    {
        public MySqlConnection connection;
        public object locker;

        private string logsT = "CREATE TABLE IF NOT EXISTS menu_logs" +
        "(" +
            "log_id long AUTO_INCREMENT," +
            "log varchar(255)," +
            "user_computer text(2000)," +
            "seconds varchar(10)," +
            "minutes varchar(10)," +
            "hours varchar(10)," +
            "day varchar(10)," +
            "month varchar(10)," +
            "year varchar(10)," +
            "level varchar(100)" +
        ");";
        private string logs_name = "menu_logs";

        public string insertLog = "INSERT INTO logs( log, user_computer, seconds, minutes, hours, day, month, year, type) " +
            "VALUES( '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}');";

        public LogStorage(MySqlConnection connection, object locker)
        {
            this.connection = connection;
            this.locker = locker;
            SetTable(logsT);
            SetTableName(logs_name);
        }
        public void AddLogs(Log loger)
        {
            string command = string.Format(insertLog,
            loger.log, loger.user_computer, loger.seconds, loger.minutes, loger.hours, loger.day, loger.month, loger.year, loger.level);
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(command, connection))
                {
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public List<Log> SelectLogs()
        {
            List<Log> logsMass = new List<Log>();
            using (MySqlCommand commandSQL = new MySqlCommand("SELECT * FROM logs;", connection))
            {
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        while (readerMassive.Read())
                        {
                            Log log = new Log
                            {
                                log = readerMassive.GetString("log"),
                                user_computer = readerMassive.GetString("user_computer"),
                                seconds = readerMassive.GetString("seconds"),
                                minutes = readerMassive.GetString("minutes"),
                                hours = readerMassive.GetString("hours"),
                                day = readerMassive.GetString("day"),
                                month = readerMassive.GetString("month"),
                                year = readerMassive.GetString("year"),
                                level = readerMassive.GetString("level")
                            };
                            logsMass.Add(log);
                        }
                        if (logsMass.Count == 0) return null;
                        commandSQL.Dispose();
                        return logsMass;
                    }
                }
            }
        }
    }
}
