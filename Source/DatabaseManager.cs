using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace FattyBot {
    public class DatabaseManager {

        private readonly string QueryInsertUser = "INSERT INTO users (user_nick) VALUES (@nick)";
        private readonly string QuerySelectUser = "SELECT user_id FROM users WHERE user_nick = @nick";

        private readonly string QueryInsertChannel = "INSERT INTO channels (channel_server, channel_name) VALUES (@server, @channel)";
        private readonly string QuerySelectChannel = "SELECT channel_id FROM channels WHERE channel_name = @channel AND channel_server = @server";

        private readonly string QueryInsertChannelLog = "INSERT INTO channel_logs (user_id, channel_id, channel_log_message) VALUES (@user, @channel, @message)";
        private readonly string QuerySelectRecentChannelLogForUser = @"SELECT channel_log_message, channel_name, channel_log_time FROM channel_logs natural join 
            channels natural join users where user_id = @user_id AND channel_id = @channel_id ORDER BY channel_log_time DESC LIMIT 1";

        //todo:use this
        private readonly string QueryUpdateChannelLogForUser = "UPDATE channel_logs SET user_id =@new_id WHERE user_id = @old_id";
        private readonly string QuerySelectAliasesForUserID = "SELECT user_nick FROM users where user_id = @id";


        private readonly string QueryInsertTell = "INSERT INTO tells (user_id, user_nick, recipient_id, tell_message) VALUES (@user_id, @user_nick, @recipient_id, @tell_message)";
        private readonly string QuerySelectTell = "SELECT tell_id, user_nick, tell_message, tell_time FROM tells natural join users where recipient_id = @recip_id;";
        private readonly string QueryRemoveTell = "DELETE FROM tells where tell_id = @tell_id;";

        private readonly string ConnectionString;

        public DatabaseManager(string serverAddress, string userId, string pwd, string database) {
            try {
                ConnectionString = String.Format("server={0}; uid={1}; pwd={2}; database={3}; ConnectionLifeTime={4}; maxpoolsize=1500;", serverAddress, userId, pwd, database, 5);
                using (MySqlConnection con = new MySqlConnection(ConnectionString)) {
                    con.Open();
                    Console.WriteLine("Connected to " + serverAddress + " database successfully");
                    con.Close();
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!!!!!!!!!!!FAILED TO SET UP DATABASE!!!!!!!!!!!!!");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        public int GetUserID(string nick) {

            bool found = false;
            using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectUser, new MySqlParameter("@nick", nick))) {
                if (reader.Read()) {
                    found = true;
                    int uID = reader.GetInt32("user_id");
                    reader.Close();
                    return uID;
                }
            }
            if (!found) {
                MySqlHelper.ExecuteNonQuery(ConnectionString, QueryInsertUser, new MySqlParameter("@nick", nick));
                using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectUser, new MySqlParameter("@nick", nick))) {
                    if (reader.Read()) {
                        int uID = reader.GetInt32("user_id");
                        reader.Close();
                        return uID;
                    }
                }
                return 0;
            }
            throw new Exception("failed to get user ID from database");
        }

        public int GetChannelID(string channel, string server) {
            MySqlParameter[] sqlParams = { new MySqlParameter("@channel", channel), new MySqlParameter("@server", server) };
            using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectChannel, sqlParams)) {
                if (reader.Read()) {
                    int cID = reader.GetInt32("channel_id");
                    reader.Close();
                    return cID;
                }
            }

            {
                MySqlHelper.ExecuteNonQuery(ConnectionString, QueryInsertChannel, sqlParams);
                using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectChannel, sqlParams)) {
                    if (reader.Read()) {
                        int cID = reader.GetInt32("channel_id");
                        reader.Close();
                        return cID;
                    }
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

        public Tuple<string, string, DateTime, int> GetLastLogMessageFromUser(string nick, string channel, string server) {
            int userID = GetUserID(nick);
            int channelID = GetChannelID(channel, server);
            MySqlParameter[] sqlParams = { new MySqlParameter("@user_id", userID), new MySqlParameter("@channel_id", channelID) };
            using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectRecentChannelLogForUser, sqlParams)) {
                if (reader.Read()) {
                    string logMessage = reader.GetString("channel_log_message");
                    string channelName = reader.GetString("channel_name");
                    DateTime messageTime = reader.GetDateTime("channel_log_time");
                    return new Tuple<string, string, DateTime, int>(logMessage, channelName, messageTime, userID);
                }
            }

            return null;
        }

        public void SetTell(string nick, string message, string recip) {
            //(@user_id, @user_nick, @channel_id, @tell_message)
            int userID = GetUserID(nick);
            int recipID = GetUserID(recip);
            MySqlParameter[] sqlParams = { new MySqlParameter("@user_id", userID), new MySqlParameter("@user_nick", nick), new MySqlParameter("@recipient_id", recipID), new MySqlParameter("@tell_message", message) };
            if (MySqlHelper.ExecuteNonQuery(ConnectionString, QueryInsertTell, sqlParams) < 1)
                throw new Exception("failed to store tell");
        }

        // from time message
        public bool GetTells(string nick, out List<Tuple<string, DateTime, string>> tells) {
            int userID = GetUserID(nick);
            MySqlParameter[] sqlParams = { new MySqlParameter("@recip_id", userID) };
            using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectTell, sqlParams)) {
                tells = new List<Tuple<string, DateTime, string>>();
                while (reader.Read()) {
                    int tellID = reader.GetInt32("tell_id");
                    string senderNick = reader.GetString("user_nick");
                    DateTime tellTime = reader.GetDateTime("tell_time");
                    string tellMessage = reader.GetString("tell_message");
                    Tuple<string, DateTime, string> messageGroup = new Tuple<string, DateTime, string>(senderNick, tellTime, tellMessage);
                    tells.Add(messageGroup);
                    MySqlParameter[] sqlDeleteParams = { new MySqlParameter("@tell_id", tellID) };
                    MySqlHelper.ExecuteNonQuery(ConnectionString, QueryRemoveTell, sqlDeleteParams);
                }
                reader.Close();
            }           
            return tells.Count > 0 ? true : false;
        }

        public bool CheckForTells(string nick) {
            int userID = GetUserID(nick);
            MySqlParameter[] sqlParams = { new MySqlParameter("@recip_id", userID) };
            using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectTell, sqlParams)) {
                if (reader.Read()) {
                    reader.Close();
                    return true;
                }
            }
            return false;
        }

        public List<string> GetAliases(string nick) {
            var nickList = new List<string>();
            int nickID = GetUserID(nick);
            MySqlParameter[] sqlParams = { new MySqlParameter("@id", nickID) };
            using (var reader = MySqlHelper.ExecuteReader(ConnectionString, QuerySelectAliasesForUserID, sqlParams)) {
                while (reader.Read()) {
                    nickList.Add(reader.GetString(0));
                }
            }
            return nickList;
        }
    }
}
