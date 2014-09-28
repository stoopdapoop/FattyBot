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

        private void ListCommands(CommandInfo info) {
            foreach (KeyValuePair<string, Tuple<CommandMethod, string>> mthd in Commands) {
                string ThisMessage = CommandSymbol + mthd.Key;
                ThisMessage += " - " + mthd.Value.Item2;
                SendNotice(info.Caller, ThisMessage);
            }
            
        }

        private void Seen(CommandInfo info) {
            Tuple<DateTime, String> lastSeentEntry;

            bool userSeen = SeenList.TryGetValue(info.Arguments, out lastSeentEntry);

            if (userSeen) {
                DateTime lastSeentTime = lastSeentEntry.Item1;
                TimeSpan lastSeenSpan = DateTime.Now - lastSeentTime;
                string prettyTime = GetPrettyTime(lastSeenSpan);

                if (info.Caller == info.Arguments)
                    SendMessage(info.Source, String.Format("You were last seen (before this command) {0} on {1}. {2} ago. \"{3}\"", info.Arguments, lastSeentTime, prettyTime, lastSeentEntry.Item2));
                else
                    SendMessage(info.Source, String.Format("Last seen {0} on {1}. {2} ago. \"{3}\"", info.Arguments, lastSeentTime, prettyTime, lastSeentEntry.Item2));
            }
            else {
                SendMessage(info.Source, String.Format("Haven't ever seen \"{0}\" around", info.Arguments));
            }
        }

        private void Tell(CommandInfo info) {
            var parts = info.Arguments.Split(' ');
            var recipients = parts[0].Split(',');
            string msg = String.Join(" ", parts, 1, parts.Length - 1);
            foreach (var recip in recipients) {
                if (recip == IrcObject.IrcNick) {
                    SendMessage(info.Source, "xD");
                    continue;
                }
                else {
                    FattyTellManager.AddTellForUser(recip, info.Caller, msg);
                }
            }
            SendMessage(info.Source, String.Format("Will tell that to {0} when they are round", parts[0]));
        }

        #region EightBall
        readonly string[] EightBallResponses = { "It is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", 
                             "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy, try again", "Try again later", "Better not tell you now", 
                             "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
        private void EightBall(CommandInfo info) {
            Random rand = new Random();

            SendMessage(info.Source, String.Format("{0}: {1}", info.Caller, EightBallResponses[rand.Next(EightBallResponses.Length)]));
        }
        #endregion

        DateTime GagTime = DateTime.Now - new TimeSpan(5, 5, 5);
        string Gagger;
        private void Shutup(CommandInfo info) {
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
