using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace FattyBot {
    class DatabaseManager {

        public DatabaseManager(string serverAddress, string userId, string pwd, string database) {
            try {
                MySqlConnection con = new MySqlConnection(String.Format("server={0}; uid={1}; pwd={2}; database={3};", serverAddress, userId, pwd, database));
                con.Open();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
