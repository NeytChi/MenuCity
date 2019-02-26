using System;
using Menu.NDatabase;
using MySql.Data.MySqlClient;

namespace Menu.NDatabase.UserData
{
    public class UserStorage : Storage
    {
        public string table_name = "menu_users";

        public string table = "CREATE TABLE IF NOT EXISTS menu_users" +
        "(" +
            "user_id int NOT NULL AUTO_INCREMENT," +
            "user_email varchar(256) UNIQUE," +
            "user_login varchar(256)," +
            "user_password varchar(256)," +
            "created_at int," +
            "user_hash varchar(120)," +
            "activate tinyint DEFAULT '0'," +
            "PRIMARY KEY (user_id)" +
        ");";
        private string insert = "INSERT INTO menu_users (user_email, user_login, user_password, created_at, user_hash, activate) " +
            "VALUES (@user_email, @user_login, @user_password, @created_at, @user_hash, @activate);";

        private string selectById = "SELECT user_id, user_email, user_login, created_at, activate FROM menu_users WHERE user_id=@user_id;";

        private string selectByEmail = "SELECT user_id, user_email, user_login, created_at, activate FROM menu_users WHERE user_email=@user_email;";

        private string updateActive = "UPDATE menu_users SET user_activate='1' WHERE user_hash=@user_hash;";

        private string updateEmail = "UPDATE menu_users SET user_email=@user_email WHERE user_id=@user_id;";

        private string updatePassById = "UPDATE menu_users SET user_password=@user_password WHERE user_id=@user_id;";

        private string deleteById = "DELETE FROM menu_users WHERE user_id=@user_id;";

        public UserStorage(MySqlConnection mainconnection, object block)
        {
            this.connection = mainconnection;
            this.locker = block;
            SetTable(table);
            SetTableName(table_name);
        }
        public UserCache AddUser(UserCache user)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(insert, connection))
                {
                    commandSQL.Parameters.AddWithValue("@user_email", user.user_email);
                    commandSQL.Parameters.AddWithValue("@user_login", user.user_login);
                    commandSQL.Parameters.AddWithValue("@user_password", user.user_password);
                    commandSQL.Parameters.AddWithValue("@created_at", user.created_at);
                    commandSQL.Parameters.AddWithValue("@user_hash", user.user_hash);
                    commandSQL.Parameters.AddWithValue("@activate", user.activate);
                    commandSQL.ExecuteNonQuery();
                    user.user_id = (int)commandSQL.LastInsertedId;
                    user.user_password = null;
                    commandSQL.Dispose();
                }
            }
            return user;
        }
        public UserCache SelectUserById(int? user_id)
        {
            if (user_id == null)
            {
                return null;
            }
            using (MySqlCommand commandSQL = new MySqlCommand(selectById, connection))
            {
                commandSQL.Parameters.AddWithValue("@user_id", user_id);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            UserCache user = new UserCache();
                            user.user_id = readerMassive.GetInt32("user_id");
                            user.user_email = readerMassive.GetString("user_email");
                            user.user_login = readerMassive.GetString("user_login");
                            user.user_password = readerMassive.GetString("user_password");
                            user.created_at = readerMassive.GetInt32("created_at");
                            user.user_hash = readerMassive.GetString("user_hash");
                            user.activate = readerMassive.GetInt16("activate");
                            return user;
                        }
                        return null;
                    }
                }
            }
        }
        public UserCache SelectUserByEmail(string user_email)
        {
            using (MySqlCommand commandSQL = new MySqlCommand(selectById, connection))
            {
                commandSQL.Parameters.AddWithValue("@user_email", user_email);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        while (readerMassive.Read())
                        {
                            UserCache user = new UserCache();
                            user.user_id = readerMassive.GetInt32("user_id");
                            user.user_email = readerMassive.GetString("user_email");
                            user.user_login = readerMassive.GetString("user_login");
                            user.user_password = readerMassive.GetString("user_password");
                            user.created_at = readerMassive.GetInt32("created_at");
                            user.user_hash = readerMassive.GetString("user_hash");
                            user.activate = readerMassive.GetInt16("activate");
                            return user;
                        }
                        return null;
                    }
                }
            }
        }
        public void UpdateUserPassword(int user_id, string user_password)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(updatePassById, connection))
                {
                    commandSQL.Parameters.AddWithValue("@user_id", user_id);
                    commandSQL.Parameters.AddWithValue("@user_password", user_password);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public void UpdateActivateUser(string user_hash)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(updateActive, connection))
                {
                    commandSQL.Parameters.AddWithValue("@user_hash", user_hash);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public void UpdateEmail(int user_id, string user_email)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(updateEmail, connection))
                {
                    commandSQL.Parameters.AddWithValue("@user_id", user_id);
                    commandSQL.Parameters.AddWithValue("@user_email", user_email);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public void DeleteUser(int? user_id)
        {
            if (user_id == null)
            {
                return;
            }
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(deleteById, connection))
                {
                    commandSQL.Parameters.AddWithValue("@user_id", user_id);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
    }
}
