﻿using System;
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

        const string DictionaryKey = "e38f6db8-e792-44a6-b3ab-9acc75e9edec";
        const string ThesaurusKey = "3ce55c4e-5f26-4cce-af61-1dff08836aa7";

        const string Stands4UserID = "3492";
        const string Stands4TokenID = "lVM1lpRT2RHxUFRT";

        UserAliasesRegistry FattyUserAliases = new UserAliasesRegistry();

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

        static int maxResultsToDisplay = 10;
        private void Acronym(string caller, string args, string source) {
            string searchURL = "http://www.stands4.com/services/v2/abbr.php?uid=" + Stands4UserID + "&tokenid=" + Stands4TokenID;
            searchURL += "&term=" + args;
            // this is for exact lookup, so I don't have to display results in an intelligent manner
            searchURL += "&searchtype=e";

            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);

            var errs = xmlDoc.GetElementsByTagName("error");
            if (errs.Count > 0) {
                string errorText = errs[0].InnerText;
                for (int i = 1; i < errs.Count; ++i)
                    errorText += " | " + errorText;
                SendMessage(source, String.Format("Errors returned: {0}", errorText));
                return;
            }

            StringBuilder messageAccumulator = new StringBuilder();

            var res = xmlDoc.GetElementsByTagName("result");
            int outputCount = 0;
            foreach (XmlNode elem in res) {
                var childRes = elem.ChildNodes;
                foreach (XmlNode child in childRes) {
                    if (child.Name != "definition")
                        continue;
                    string def = child.InnerText;
                    //iterate through rest of entries, even though there's no point, saves code.
                    if (outputCount >= maxResultsToDisplay || !TryAppend(messageAccumulator, def + " | ", source))
                        break;
                    ++outputCount;
                }
            }

            if (res.Count < 1)
                messageAccumulator.Append("No results for: " + args);
            SendMessage(source, messageAccumulator.ToString());
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

        private void Alias(string caller, string args, string source) {
            args = args.Trim();
            var argParts = args.Split(' ');

            if (argParts.Length == 3)
                PerformAliasOperation(argParts, source);
            else if (argParts.Length == 1)
                DisplayUserAliases(argParts[0], source, args);
            else
                SendMessage(source, "Not the right number of inputs");
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

        private void Dictionary(string caller, string args, string source) {
            string searchURL = "http://www.dictionaryapi.com/api/v1/references/collegiate/xml/" + args + "?key=" + DictionaryKey;
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            StringBuilder messageAccumulator = new StringBuilder();

            var res = xmlDoc.GetElementsByTagName("entry");
            if (res.Count == 1) {
                messageAccumulator.Append("1 result for ");
                string candidateMessage = res[0].Attributes["id"].Value;
                var chldrn = res[0].ChildNodes;
                foreach (XmlNode chld in chldrn) {
                    if (chld.Name == "def") {
                        var defnodes = chld.ChildNodes;
                        foreach (XmlNode def in defnodes) {
                            if (def.Name == "dt") {
                                candidateMessage += def.InnerText;
                                if (!TryAppend(messageAccumulator, candidateMessage, source)) {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else if (res.Count > 1) {
                messageAccumulator.Append(res.Count + " results. Too many to list here, so here's a link: http://www.merriam-webster.com/dictionary/" + args);
            }
            else {
                res = xmlDoc.GetElementsByTagName("suggestion");

                messageAccumulator.Append("No results found. Did you mean: ");
                foreach (XmlNode nd in res) {
                    if (!TryAppend(messageAccumulator, nd.InnerText + "|", source))
                        break;
                }
                messageAccumulator.Remove(messageAccumulator.Length - 2, 1);
                messageAccumulator.Append("?");
            }

            SendMessage(source, messageAccumulator.ToString());
        }

        DateTime GagTime = DateTime.Now - new TimeSpan(5, 5, 5);
        string Gagger;
        private void Shutup(String caller, String args, string source) {
            Gagger = caller;
            GagTime = DateTime.Now;
            SendMessage(source, String.Format("Alright {0}, I'll be quiet for you for the next 5 minutes.  I know how you hate fun and all.", caller));
        }

        private void DisplayUserAliases(string alias, string source, string args) {
            StringBuilder sb = new StringBuilder();
            UserAliasGroup ag;
            if (FattyUserAliases.GetAliasGroup(alias, out ag)) {
                var names = ag.GetUserAliases();
                foreach (string name in names) {
                    sb.Append(name + " ");
                }
                SendMessage(source, sb.ToString());
            }
            else {
                SendMessage(source, String.Format("No results found for {0}", args));
            }
        }

        private void PerformAliasOperation(string[] argParts, string source) {
            if (argParts.Length < 3)
                SendMessage(source, "error parsing arguments");
            string operation = argParts[0];
            string firstName = argParts[1];
            string secondName = argParts[2];
            switch (operation) {
                case "add":
                    SendMessage(source, FattyUserAliases.AddAlias(firstName, secondName));
                    break;
                case "remove":
                    SendMessage(source, FattyUserAliases.RemoveAlias(firstName, secondName));
                    break;
                default:
                    SendMessage(source, String.Format("{0} is an unknown operation, try 'add' or 'remove'", operation));
                    break;
            }
        }

        private string GetPrettyTime(TimeSpan ts) {
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

        private bool TryAppend(StringBuilder sb, string message, string source) {
            if (sb.Length + message.Length + GetMessageOverhead(source) >= 480)
                return false;
            sb.Append(message);
            return true;
        }

    }
}
