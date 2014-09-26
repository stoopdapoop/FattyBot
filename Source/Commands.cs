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

        private void ListCommands(string caller, string args, string source) {
            StringBuilder availableMethodNames = new StringBuilder();
            foreach (KeyValuePair<string, Tuple<CommandMethod, string>> mthd in Commands) {
                availableMethodNames.Append(CommandSymbol + mthd.Key);
                availableMethodNames.Append(":" + mthd.Value.Item2 + ", ");
            }
            availableMethodNames.Remove(availableMethodNames.Length - 2, 1);
            SendMessage(source, availableMethodNames.ToString());
        }

        private void Seen(string caller, string args, string source) {
            Tuple<DateTime, String> lastSeentEntry;

            bool userSeen = SeenList.TryGetValue(args, out lastSeentEntry);

            if (userSeen) {
                DateTime lastSeentTime = lastSeentEntry.Item1;
                TimeSpan lastSeenSpan = DateTime.Now - lastSeentTime;
                string prettyTime = GetPrettyTime(lastSeenSpan);

                if (caller == args)
                    SendMessage(source, String.Format("You were last seen (before this command) {0} on {1}. {2} ago. \"{3}\"", args, lastSeentTime, prettyTime, lastSeentEntry.Item2));
                else
                    SendMessage(source, String.Format("Last seen {0} on {1}. {2} ago. \"{3}\"", args, lastSeentTime, prettyTime, lastSeentEntry.Item2));
            }
            else {
                SendMessage(source, String.Format("Haven't ever seen \"{0}\" around", args));
            }
        }

        private void Tell(string caller, string args, string source) {
            var parts = args.Split(' ');
            var recipients = parts[0].Split(',');
            string msg = String.Join(" ", parts, 1, parts.Length - 1);
            foreach (var recip in recipients) {
                if (recip == IrcObject.IrcNick) {
                    SendMessage(source, "xD");
                    continue;
                }
                else {
                    FattyTellManager.AddTellForUser(recip, caller, msg);
                }
            }
            SendMessage(source, String.Format("Will tell that to {0} when they are round", parts[0]));
        }

        #region EightBall
        readonly string[] EightBallResponses = { "It is certain", "It is decidedly so", "Without a doubt", "Yes definitely", "You may rely on it", "As I see it, yes", 
                             "Most likely", "Outlook good", "Yes", "Signs point to yes", "Reply hazy, try again", "Try again later", "Better not tell you now", 
                             "Cannot predict now", "Concentrate and ask again", "Don't count on it", "My reply is no", "My sources say no", "Outlook not so good", "Very doubtful" };
        private void EightBall(string caller, string args, string source) {
            Random rand = new Random();

            SendMessage(source, String.Format("{0}: {1}", caller, EightBallResponses[rand.Next(EightBallResponses.Length)]));
        }
        #endregion

        DateTime GagTime = DateTime.Now - new TimeSpan(5, 5, 5);
        string Gagger;
        private void Shutup(String caller, String args, string source) {
            Gagger = caller;
            GagTime = DateTime.Now;
            SendMessage(source, String.Format("Alright {0}, I'll be quiet for you for the next 5 minutes.  I know how you hate fun and all.", caller));
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
            return String.Format("PRIVMSG {0} :", source).Length;
        }

        public static bool TryAppend(StringBuilder sb, string message, string source, int reserve = 0) {
            if (sb.Length + message.Length + reserve + GetMessageOverhead(source) >= 480)
                return false;
            sb.Append(message);
            return true;
        }

    }
}
