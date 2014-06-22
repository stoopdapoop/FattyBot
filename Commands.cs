using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace FattyBot
{
    partial class FattyBot
    {

        #region GoogleStructs
        const string GoogleAPIKey = "AIzaSyDniQGV3voKW5ZSrqWfiXgnWz-2AX6xNBo";
        private class GoogleSearchItem
        {
            public string kind { get; set; }
            public string title { get; set; }
            public string link { get; set; }
            public string displayLink { get; set; }
        }

        private class SourceUrl
        {
            public string type { get; set; }
            public string template { get; set; }
        }

        private class GoogleSearchResults
        {
            public string kind { get; set; }
            public SourceUrl url { get; set; }
            public GoogleSearchItem[] items { get; set; }
        }
        #endregion


        UserAliasesRegistry UserAliases = new UserAliasesRegistry(); 

        private void Help(string caller, string args, string source)
        {
            StringBuilder availableMethodNames = new StringBuilder();
            foreach (KeyValuePair<string, CommandMethod> mthd in Commands)
            {
                availableMethodNames.Append(mthd.Key);
                availableMethodNames.Append(", ");
            }
            availableMethodNames.Remove(availableMethodNames.Length - 2, 1);
            SendMessage(source, availableMethodNames.ToString());
        }

        private void Seen(string caller, string args, string source)
        {
            Tuple<DateTime, String> lastSeentEntry;
            
            bool userSeen = SeenList.TryGetValue(args, out lastSeentEntry);
            
            if (userSeen)
            {
                DateTime lastSeentTime = lastSeentEntry.Item1;
                TimeSpan lastSeenSpan = DateTime.Now - lastSeentTime;
                string prettyTime = GetPrettyTime(lastSeenSpan);
                
                if (caller == args)
                    SendMessage(source, String.Format("You were last seen (before this command) {0} on {1}. {2} ago. \"{3}\"", args, lastSeentTime, prettyTime, lastSeentEntry.Item2));
                else
                    SendMessage(source, String.Format("Last seen {0} on {1}. {2} ago. \"{3}\"", args, lastSeentTime, prettyTime, lastSeentEntry.Item2));

            }
            else
            {
                SendMessage(source, String.Format("Haven't ever seen \"{0}\" around", args));
            }
        }

        private void Tell(string caller, string args, string source)
        {
            var parts = args.Split(' ');
            var recipients = parts[0].Split(',');
            string msg = String.Join(" ", parts, 1, parts.Length-1);
            foreach(var recip in recipients) {
                if (recip == IrcObject.IrcNick) {
                    SendMessage(source, "xD");
                    continue;
                }
                List<Tuple<String, DateTime, String>> thisTell;
                bool isPresent = TellList.TryGetValue(recip, out thisTell);
                if (isPresent)
                    thisTell.Add(new Tuple<String, DateTime, String>(caller, DateTime.Now, msg));
                else
                    TellList[recip] = new List<Tuple<String, DateTime, string>>(new Tuple<String, DateTime, string>[] { new Tuple<String, DateTime, string>(caller, DateTime.Now, msg) });
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

        private void DisplayUserAliases(string alias, string source, string args) {
            StringBuilder sb = new StringBuilder();
            UserAliasGroup ag;
            if (UserAliases.GetAliasGroup(alias, out ag)) {
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
                    SendMessage(source, UserAliases.AddAlias(firstName, secondName));
                    break;
                case "remove":
                    break;
                default:
                    SendMessage(source, String.Format("{0} is an unknown operation, try 'add' or 'remove'", operation));
                    break;
            }
        }
        private void Google(string caller, string args, string source)
        {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=" + GoogleAPIKey + "&cx=016968405084681006944:ksw5ydltpt0&q=";
            searchURL += args;
            GoogleAPIPrinter(searchURL, source);
        }

        private void GoogleImageSearch(String caller, String args, string source)
        {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=" + GoogleAPIKey + "&cx=016968405084681006944:ksw5ydltpt0&searchType=image&q=";
            searchURL += args;
            GoogleAPIPrinter(searchURL, source);
        }

        private void GoogleAPIPrinter(string searchURL, string source)
        {
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            String data = reader.ReadToEnd();

            GoogleSearchResults results = JsonConvert.DeserializeObject<GoogleSearchResults>(data);
            StringBuilder messageAccumulator = new StringBuilder();
            int i = 0;
            int messageOverhead = GetMessageOverhead(source);
            while (i < 10)
            {
                if (results.items != null && results.items.Length >= i)
                {
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
                else
                {
                    break;
                }
            }
            if (messageAccumulator.Length == 0)
                SendMessage(source, "No Results Found");
            else
                SendMessage(source, messageAccumulator.ToString());
        }
                
        private string GetPrettyTime(TimeSpan ts)
        {
            string timeLastSeen = "";
            int fieldCount = 0;
            if (ts.Days > 0)
            {
                timeLastSeen += String.Format("{0} day(s)", ts.Days);
                ++fieldCount;
            }
            if (ts.Hours > 0)
            {
                timeLastSeen += (fieldCount > 0 ? ", " : "");
                timeLastSeen += String.Format("{0} hour(s)", ts.Hours);
                ++fieldCount;
            }
            if (ts.Minutes > 0)
            {
                timeLastSeen += (fieldCount > 0 ? ", and " : "");
                timeLastSeen += String.Format("{0} minute(s)", ts.Minutes);
                ++fieldCount;
            }
            if(fieldCount == 0)
            {
                timeLastSeen = String.Format("{0} second(s)", ts.Seconds);
            }

            return timeLastSeen;
        }

        private int GetMessageOverhead( string source)
        {
            return  String.Format("PRIVMSG {0} :", source).Length;
        }

    }
}
