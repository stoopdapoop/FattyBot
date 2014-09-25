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

        #region GoogleStructs
        const string GoogleAPIKey = "AIzaSyDniQGV3voKW5ZSrqWfiXgnWz-2AX6xNBo";
        private class GoogleSearchItem {
            public string kind { get; set; }
            public string title { get; set; }
            public string link { get; set; }
            public string displayLink { get; set; }
        }

        private class ShortURL {
            public string id { get; set; }
        }

        private class SourceUrl {
            public string type { get; set; }
            public string template { get; set; }
        }

        private class GoogleSearchResults {
            public string kind { get; set; }
            public SourceUrl url { get; set; }
            public GoogleSearchItem[] items { get; set; }
        }
        #endregion

        const string WolframAlphaKey = "95JE4A-XQLX9WPU99";
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

        private void GetShortURL(string caller, string args, string source) {
            string searchURL = "https://www.googleapis.com/urlshortener/v1/url?key=" + GoogleAPIKey;

            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;

            ASCIIEncoding encoder = new ASCIIEncoding();
            string pls = JsonConvert.SerializeObject(new { longUrl = args });
            byte[] data = encoder.GetBytes(pls);

            searchRequest.ContentType = "application/json";
            searchRequest.ContentLength = data.Length;
            searchRequest.Expect = "application/json";
            searchRequest.Method = "POST";
            searchRequest.GetRequestStream().Write(data, 0, data.Length);
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());

            ShortURL temp = JsonConvert.DeserializeObject<ShortURL>(reader.ReadToEnd());

            SendMessage(source, temp.id);
        }

        static List<DateTime> RecentMathInvocations = new List<DateTime>();

        private void MathLimit(string caller, string args, string source) {
            //cull old messages
            TimeSpan anHour = new TimeSpan(1, 0, 0);
            for (int i = RecentMathInvocations.Count - 1; i >= 0; --i) {
                if ((DateTime.Now - RecentMathInvocations[i]) > anHour)
                    RecentMathInvocations.RemoveAt(i);

            }

            SendMessage(source, String.Format("{0} wolfram invocations have been made in the past hour. {1} left.", RecentMathInvocations.Count, 30 - RecentMathInvocations.Count));
        }

        private void Math(string caller, string args, string source) {

            //cull old messages
            TimeSpan anHour = new TimeSpan(1, 0, 0);
            for (int i = RecentMathInvocations.Count - 1; i >= 0; --i) {
                if ((DateTime.Now - RecentMathInvocations[i]) > anHour)
                    RecentMathInvocations.RemoveAt(i);

            }

            if (RecentMathInvocations.Count > 30) {
                TimeSpan nextInvoke = anHour - (DateTime.Now - RecentMathInvocations[0]);
                SendMessage(source, String.Format("Sorry {0}, rate limit on this command exceeded, you can use it again in {2} minutes", caller, nextInvoke.Minutes));
                return;
            }

            args = args.Replace("+", "%2B");
            string searchURL = "http://api.wolframalpha.com/v2/query?input=" + args + "&appid=" + WolframAlphaKey;
            //Uri search = new Uri(searchURL);
            //searchURL = HttpUtility.UrlEncode(search.AbsoluteUri);
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            //string butts = searchRequest.RequestUri.IsWellFormedOriginalString();
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            StringBuilder messageAccumulator = new StringBuilder();
            int messageOverhead = GetMessageOverhead(source);


            XmlNodeList res = xmlDoc.GetElementsByTagName("queryresult");
            if (res[0].Attributes["success"].Value == "false") {
                messageAccumulator.Append("Query failed: ");
                res = xmlDoc.GetElementsByTagName("tip");
                for (int i = 0; i < res.Count; i++) {
                    string desc = res[i].InnerText;
                    if (desc.Length + messageOverhead + messageAccumulator.Length <= 480)
                        messageAccumulator.Append(desc + ". ");
                    else
                        break;
                }
                res = xmlDoc.GetElementsByTagName("didyoumean");

                for (int i = 0; i < res.Count; i++) {
                    string desc = "";
                    if (i == 0)
                        desc += "Did you mean: ";
                    desc += res[i].InnerText + " ? ";
                    if (desc.Length + messageOverhead + messageAccumulator.Length <= 480)
                        messageAccumulator.Append(desc);
                    else
                        break;
                }
                SendMessage(source, messageAccumulator.ToString());
            }
            else {
                res = xmlDoc.GetElementsByTagName("plaintext");

                for (int i = 0; i < res.Count; i++) {
                    string value = res[i].InnerText;
                    string description = res[i].ParentNode.ParentNode.Attributes["title"].Value;
                    if (description == "Number line")
                        continue;
                    description = description + ":" + value;
                    if (description.Length + messageOverhead + messageAccumulator.Length <= 480)
                        messageAccumulator.Append(description + " / ");
                    else
                        break;
                }
                messageAccumulator.Remove(messageAccumulator.Length - 2, 1);
                messageAccumulator.Replace("\n", " ");
                messageAccumulator.Replace("\r", " ");

                SendMessage(source, messageAccumulator.ToString());
            }

            RecentMathInvocations.Add(DateTime.Now);
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

        private void Google(string caller, string args, string source) {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=" + GoogleAPIKey + "&cx=016968405084681006944:ksw5ydltpt0&q=";
            searchURL += args;
            GoogleAPIPrinter(searchURL, source);
        }

        private void GoogleImageSearch(String caller, String args, string source) {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=" + GoogleAPIKey + "&cx=016968405084681006944:ksw5ydltpt0&searchType=image&q=";
            searchURL += args;
            GoogleAPIPrinter(searchURL, source);
        }

        private void GoogleAPIPrinter(string searchURL, string source) {
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            String data = reader.ReadToEnd();

            GoogleSearchResults results = JsonConvert.DeserializeObject<GoogleSearchResults>(data);
            StringBuilder messageAccumulator = new StringBuilder();
            int i = 0;
            int messageOverhead = GetMessageOverhead(source);
            while (i < 10 && results.items != null && i < results.items.Length) {
                if (results.items.Length >= i) {
                    StringBuilder resultAccumulator = new StringBuilder();
                    GoogleSearchItem resultIterator = results.items[i++];
                    resultAccumulator.Append(String.Format("\"{0}\"", resultIterator.title));
                    resultAccumulator.Append(" - ");
                    resultAccumulator.Append(String.Format("\x02{0}\x02", resultIterator.link));
                    resultAccumulator.Append(@" 4| ");
                    if (messageAccumulator.Length + resultAccumulator.Length + messageOverhead <= 480)
                        messageAccumulator.Append(resultAccumulator);
                    else
                        break;
                }
                else {
                    break;
                }
            }
            if (messageAccumulator.Length == 0)
                SendMessage(source, "No Results Found");
            else
                SendMessage(source, messageAccumulator.ToString());
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

        private int GetMessageOverhead(string source) {
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
