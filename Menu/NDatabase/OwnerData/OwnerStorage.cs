using System;
using MySql.Data.MySqlClient;

namespace Menu.NDatabase.OwnerData
{
    public class OwnerStorage : Storage
    {
        public string table_name = "menu_owners";

        public string table = "CREATE TABLE IF NOT EXISTS menu_owners" +
        "(" +
            "owner_id int NOT NULL AUTO_INCREMENT," +
            "owner_first_name varchar(256)," +
            "owner_last_name varchar(256)," +
            "owner_email varchar(256)," +
            "owner_password varchar(256)," +
            "owner_phone_number varchar(256)," +
            "owner_hash varchar(100)," +
            "activate tinyint DEFAULT '0'," +
            "created_at int," +
            "PRIMARY KEY (owner_id)" +
        ");";
        private string insert = "INSERT INTO menu_owners " +
        "(" +
            "owner_first_name," +
            "owner_last_name," +
            "owner_email," +
            "owner_password," +
            "owner_phone_number," +
            "owner_hash," +
            "activate," +
            "created_at" +
        ")" +
            "VALUES " +
        "(" +
            "@owner_first_name," +
            "@owner_last_name," +
            "@owner_email," +
            "@owner_password," +
            "@owner_phone_number," +
            "@owner_hash," +
            "@activate," +
            "@created_at" +
        ");";

        private string selectById = "SELECT * FROM menu_owners WHERE owner_id=@owner_id;";

        private string selectByEmail = "SELECT * FROM menu_owners WHERE owner_email=@owner_email;";

        private string updateActive = "UPDATE menu_owners SET activate='1' WHERE owner_hash=@owner_hash;";

        private string updatePasswordByID = "UPDATE menu_owners SET owner_password=@owner_password WHERE owner_id=@owner_id;";

        private string deleteByID = "DELETE FROM menu_owners WHERE owner_id=@owner_id;";

        public OwnerStorage(MySqlConnection mainconnection, object block)
        {
            this.connection = mainconnection;
            this.locker = block;
            SetTable(table);
            SetTableName(table_name);
        }
        public Owner AddOwner(Owner owner)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(insert, connection))
                {
                    commandSQL.Parameters.AddWithValue("@owner_first_name,", owner.owner_first_name);
                    commandSQL.Parameters.AddWithValue("@owner_last_name,", owner.owner_last_name);
                    commandSQL.Parameters.AddWithValue("@owner_email", owner.owner_email);
                    commandSQL.Parameters.AddWithValue("@owner_password", owner.owner_password);
                    commandSQL.Parameters.AddWithValue("@owner_phone_number", owner.owner_phone_number);
                    commandSQL.Parameters.AddWithValue("@owner_hash", owner.owner_hash);
                    commandSQL.Parameters.AddWithValue("@activate", owner.activate);
                    commandSQL.Parameters.AddWithValue("@created_at", owner.created_at);
                    commandSQL.ExecuteNonQuery();
                    owner.owner_id = (int)commandSQL.LastInsertedId;
                    owner.owner_password = null;
                    commandSQL.Dispose();
                }
            }
            return owner;
        }
        public Owner SelectOnwerById(int? owner_id)
        {
            if (owner_id == null)
            {
                return null;
            }
            using (MySqlCommand commandSQL = new MySqlCommand(selectById, connection))
            {
                commandSQL.Parameters.AddWithValue("@owner_id", owner_id);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            Owner owner = new Owner();
                            owner.owner_id = readerMassive.GetInt32(0);
                            owner.owner_first_name = readerMassive.IsDBNull(1) ? "" : readerMassive.GetString(1);
                            owner.owner_last_name = readerMassive.IsDBNull(2) ? "" : readerMassive.GetString(2);
                            owner.owner_email = readerMassive.GetString(3);
                            owner.owner_password = readerMassive.GetString(4);
                            owner.owner_phone_number = readerMassive.IsDBNull(5) ? "" : readerMassive.GetString(5);
                            owner.owner_hash = readerMassive.GetString(6);
                            owner.activate = readerMassive.GetInt16(7);
                            owner.created_at = readerMassive.GetInt32(8);
                            return owner;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public Owner SelectOwnerByEmail(string owner_email)
        {
            using (MySqlCommand commandSQL = new MySqlCommand(selectById, connection))
            {
                commandSQL.Parameters.AddWithValue("@owner_email", owner_email);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        if (readerMassive.Read())
                        {
                            Owner owner = new Owner();
                            owner.owner_id = readerMassive.GetInt32(0);
                            owner.owner_first_name = readerMassive.IsDBNull(1) ? "" : readerMassive.GetString(1);
                            owner.owner_last_name = readerMassive.IsDBNull(2) ? "" : readerMassive.GetString(2);
                            owner.owner_email = readerMassive.GetString(3);
                            owner.owner_password = readerMassive.GetString(4);
                            owner.owner_phone_number = readerMassive.IsDBNull(5) ? "" : readerMassive.GetString(5);
                            owner.owner_hash = readerMassive.GetString(6);
                            owner.activate = readerMassive.GetInt16(7);
                            owner.created_at = readerMassive.GetInt32(8);
                            return owner;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        public void UpdateOwnerPassword(int owner_id, string owner_password)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(updatePasswordByID, connection))
                {
                    commandSQL.Parameters.AddWithValue("@owner_id", owner_id);
                    commandSQL.Parameters.AddWithValue("@owner_password", owner_password);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public void UpdateActivateOwner(string owner_hash)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(updateActive, connection))
                {
                    commandSQL.Parameters.AddWithValue("@owner_hash", owner_hash);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
        public void DeleteOwner(int? owner_id)
        {
            if (owner_id == null)
            {
                return;
            }
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(deleteByID, connection))
                {
                    commandSQL.Parameters.AddWithValue("@owner_id", owner_id);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
    }
}

