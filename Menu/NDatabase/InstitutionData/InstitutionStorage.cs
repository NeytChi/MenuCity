using System;
using Menu.NDatabase;
using MySql.Data.MySqlClient;

namespace Menu.NDatabase.InstitutionData
{
    public class InstitutionStorage : Storage
    {
        public string table_name = "menu_institutions";

        public string table = "CREATE TABLE IF NOT EXISTS menu_institutions" +
        "(" +
            "institution_id int NOT NULL AUTO_INCREMENT," +
            "user_owner_id int," +
            "institution_name varchar(30)," +
            "institution_type varchar(20)," +
            "institution_about text(500)," +
            "institution_location_x double," +
            "institution_location_y double," +
            "PRIMARY KEY (institution_id)," +
            "FOREIGN KEY (user_owner_id) REFERENCES menu_users(user_id)" +
        ");";
        private string insertInstitution = "INSERT INTO menu_institutions( user_owner_id, institution_name, institution_type, institution_about, institution_location_x, institution_location_y)" +
            "VALUES( @user_owner_id, @institution_name, @institution_type, @institution_about, @institution_location_x, @institution_location_y);";

        private string selectByUserId = "SELECT * FROM menu_institutions WHERE user_owner_id=@user_owner_id;";

        private string deleteByUserId = "DELETE FROM menu_institutions WHERE institution_id=@institution_id;";

        public InstitutionStorage(MySqlConnection mainconnection, object block)
        {
            this.connection = mainconnection;
            this.locker = block;
            SetTable(table);
            SetTableName(table_name);
        }
        public Institution AddInstitution(Institution institution)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(insertInstitution, connection))
                {
                    commandSQL.Parameters.AddWithValue("@user_owner_id", institution.user_owner_id);
                    commandSQL.Parameters.AddWithValue("@institution_name", institution.institution_name);
                    commandSQL.Parameters.AddWithValue("@institution_type", institution.institution_type);
                    commandSQL.Parameters.AddWithValue("@institution_about", institution.institution_about);
                    commandSQL.Parameters.AddWithValue("@institution_location_x", institution.institution_location_x);
                    commandSQL.Parameters.AddWithValue("@institution_location_y", institution.institution_location_y);
                    commandSQL.ExecuteNonQuery();
                    institution.institution_id = (int)commandSQL.LastInsertedId;
                    commandSQL.Dispose();
                }
            }
            return institution;
        }
        public Institution SelectInstitution(string user_id)
        {
            using (MySqlCommand commandSQL = new MySqlCommand(selectByUserId, connection))
            {
                commandSQL.Parameters.AddWithValue("@user_owner_id", user_id);
                lock (locker)
                {
                    using (MySqlDataReader readerMassive = commandSQL.ExecuteReader())
                    {
                        while (readerMassive.Read())
                        {
                            Institution institution = new Institution();
                            institution.institution_id = readerMassive.GetInt32("institution_id");
                            institution.user_owner_id = readerMassive.GetInt32("user_owner_id");
                            institution.institution_name = readerMassive.GetString("institution_name");
                            institution.institution_type = readerMassive.GetString("institution_type");
                            institution.institution_about = readerMassive.GetString("institution_about");
                            institution.institution_location_x = readerMassive.GetDouble("institution_location_x");
                            institution.institution_location_y = readerMassive.GetDouble("institution_location_y");
                            return institution;
                        }
                        return null;
                    }
                }
            }
        }
        public void DeleteInstitution(int institution_id)
        {
            lock (locker)
            {
                using (MySqlCommand commandSQL = new MySqlCommand(deleteByUserId, connection))
                {
                    commandSQL.Parameters.AddWithValue("@institution_id", institution_id);
                    commandSQL.ExecuteNonQuery();
                    commandSQL.Dispose();
                }
            }
        }
    }
}
