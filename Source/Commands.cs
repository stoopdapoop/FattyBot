using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace FattyBot {
    partial class FattyBot {

        private void ListCommands(CommandInput info) {
            foreach (KeyValuePair<string, Tuple<CommandMethod, string>> mthd in Commands) {
                string ThisMessage = CommandSymbol + mthd.Key;
                ThisMessage += " - " + mthd.Value.Item2;
                SendNotice(info.Caller, ThisMessage);
            }
            
        }

        string[] cuteName = { "bb", "cutie", "babbycakes", "qt", "m'lady", "homeboy", "broseph" };
        private void Seen(CommandInput info) {
            // todo get rid of old alias and seenrecords and handle when this command is not called from channel
 
                var result = DatabaseInterface.GetLastLogMessageFromUser(info.Arguments, info.Source, IrcObject.IrcServer);
                if (result == null) {
                    SendMessage(info.Source, String.Format("Never seen {0} in this channel", info.Arguments));
                }
                else {
                    var callerID = DatabaseInterface.GetUserID(info.Caller);
                    if (callerID == result.Item4) {
                        Random rand = new Random();
                        SendMessage(info.Source,String.Format("I see you right now, {0} :]", cuteName[rand.Next(cuteName.Length)]));
                    }
                    else {
                        TimeSpan lastSeenSpan = DateTime.Now - result.Item3;
                        string prettyTime = GetPrettyTime(lastSeenSpan);
                        SendMessage(info.Source, String.Format("Last seen {0} on {1} in {2}. {3} ago. \"{4}\"", info.Arguments, result.Item3, result.Item2, prettyTime, result.Item1));
                    }
                }
            
        }

        private void Tell(CommandInput info) {
            var parts = info.Arguments.Split(' ');
            var recipients = parts[0].Split(',');
            string msg = String.Join(" ", parts, 1, parts.Length - 1);
            foreach (var recip in recipients) {
                if (recip == IrcObject.IrcNick) {
                    SendMessage(info.Source, "xD");
                    continue;
                }
                else {
                    FattyTellManager.AddTellForUser(recip.ToLower(), info.Caller, msg);
                }
            }
            SendMessage(info.Source, String.Format("Will tell that to {0} when they are round", parts[0]));
        }

        #region EightBall
        readonly string[] EightBallResponses = { "It is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", 
                             "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy, try again", "Try again later", "Better not tell you now", 
                             "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
        private void EightBall(CommandInput info) {
            Random rand = new Random();

            SendMessage(info.Source, String.Format("{0}: {1}", info.Caller, EightBallResponses[rand.Next(EightBallResponses.Length)]));
        }
        #endregion

        DateTime GagTime = DateTime.Now - new TimeSpan(5, 5, 5);
        string Gagger;
        private void Shutup(CommandInput info) {
            Gagger = info.Caller;
            GagTime = DateTime.Now;
            SendMessage(info.Source, String.Format("Alright {0}, I'll be quiet for you for the next 5 minutes.  I know how you hate fun and all.", info.Caller));
        }

        public static string GetPrettyTime(TimeSpan ts) {
            string timeLastSeen = "";
            int fieldCount = 0;
            if (ts.Days > 0) {
                timeLastSeen += String.Format("{0} day(s)", ts.Days);
                ++fieldCount;
            }
            if (ts.Hours > 0) {
                timeLastSeen += (fieldCount > 0 ? ", " : "");
                timeLastSeen += String.Format("{0} hour(s)", ts.Hours);
                ++fieldCount;
            }
            if (ts.Minutes > 0) {
                timeLastSeen += (fieldCount > 0 ? ", and " : "");
                timeLastSeen += String.Format("{0} minute(s)", ts.Minutes);
                ++fieldCount;
            }
            if (fieldCount == 0) {
                timeLastSeen = String.Format("{0} second(s)", ts.Seconds);
            }

            return timeLastSeen;
        }

        static public int GetMessageOverhead(string source) {
            return String.Format("PRIVMSG {0} :", source).Length+2;
        }

        public static bool TryAppend(StringBuilder sb, string message, string source, int reserve = 0) {
            if (sb.Length + message.Length + reserve + GetMessageOverhead(source) >= 480)
                return false;
            sb.Append(message);
            return true;
        }

    }
}
