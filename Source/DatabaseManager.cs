using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace FattyBot {
    class DatabaseManager {

        private readonly string QueryInsertUser = "INSERT INTO users (user_nick) VALUES (@nick)";
        private readonly string QuerySelectUser = "SELECT user_id FROM users WHERE user_nick = @nick";
        private readonly string QueryInsertChannel = "INSERT INTO channels (channel_server, channel_name) VALUES (@server, @channel)";
        private readonly string QuerySelectChannel = "SELECT channel_id FROM channels WHERE channel_name = @channel AND channel_server = @server";
        private readonly string QueryInsertChannelLog = "INSERT INTO channel_logs (user_id, channel_id, channel_log_message) VALUES (@user, @channel, @message)";
        private readonly string QuerySelectRecentChannelLogForUser = @"SELECT channel_log_message, channel_name, channel_log_time FROM channel_logs natural join 
            channels natural join users where user_id = @user_id AND channel_id = @channel_id ORDER BY channel_log_time DESC LIMIT 1";

        private readonly string ConnectionString;

        public DatabaseManager(string serverAddress, string userId, string pwd, string database) {
            try {
                ConnectionString = String.Format("server={0}; uid={1}; pwd={2}; database={3};", serverAddress, userId, pwd, database);
                MySqlConnection con = new MySqlConnection(ConnectionString);
                con.Open();
                Console.WriteLine("Connected to " + serverAddress + " database successfully");
                con.Close();
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!!!!!!!!!!!FAILED TO SET UP DATABASE!!!!!!!!!!!!!");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        public int GetUserID(string nick) {

            var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectUser, new MySqlParameter("@nick", nick));
            if (reader.Read()) {
                int uID = reader.GetInt32("user_id");
                reader.Close();
                return uID;
            }
            else {
                MySqlHelper.ExecuteNonQuery(ConnectionString, QueryInsertUser, new MySqlParameter("@nick", nick));
                reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectUser, new MySqlParameter("@nick", nick));
                if (reader.Read()) {
                    int uID = reader.GetInt32("user_id");
                    reader.Close();
                    return uID;
                }
            }
            throw new Exception("failed to get user ID from database");
        }

        public int GetChannelID(string channel, string server) {
            MySqlParameter[] sqlParams = {new MySqlParameter("@channel", channel), new MySqlParameter("@server", server)};
            var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectChannel, sqlParams);
            if (reader.Read()) {
                int cID = reader.GetInt32("channel_id");
                reader.Close();
                return cID;
            }
            else {
                MySqlHelper.ExecuteNonQuery(ConnectionString, QueryInsertChannel, sqlParams);
                reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectChannel, sqlParams);
                if (reader.Read()) {
                    int cID = reader.GetInt32("channel_id");
                    reader.Close();
                    return cID;
                }
            }
            throw new Exception("failed to get channel ID from database");  
        }

        public void SetChannelLog(string nick, string channel, string server, string message) {
            int userID = GetUserID(nick);
            int channelID = GetChannelID(channel, server);
            MySqlParameter[] sqlParams = { new MySqlParameter("@user", userID), new MySqlParameter("@channel", channelID), new MySqlParameter("@message", message) };
            if (MySqlHelper.ExecuteNonQuery(ConnectionString, QueryInsertChannelLog, sqlParams) < 1)
                throw new Exception("failed to update channel log");
        }

        public Tuple<string, string, DateTime> GetLastLogMessageFromUser(string nick, string channel, string server) {
            int userID = GetUserID(nick);
            int channelID = GetChannelID(channel, server);
            MySqlParameter[] sqlParams = { new MySqlParameter("@user_id", userID), new MySqlParameter("@channel_id", channelID) };
            var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectRecentChannelLogForUser, sqlParams);
            if (reader.Read()) {
                string logMessage = reader.GetString("channel_log_message");
                string channelName = reader.GetString("channel_name");
                DateTime messageTime = reader.GetDateTime("channel_log_time");
                return new Tuple<string, string, DateTime>(logMessage, channelName, messageTime);
            }
            else
                return null;
        }
    }
}
