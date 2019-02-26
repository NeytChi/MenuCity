using System;
using MySql.Data.MySqlClient;

namespace Menu.NDatabase
{
    public class Storage
    {
        public object locker;
        public MySqlConnection connection;
        public string table_name;
        public string table;

        public void SetTableName(string table_name)
        {
            this.table_name = table_name;
        }
        public void SetTable(string table)
        {
            this.table = table;
        }
        public string GetTableName()
        {
            return table_name;
        }
        public string GetTable()
        {
            return table;
        }
    }
}
