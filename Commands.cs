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
        private class GoogleSearchItem
        {
            public string kind { get; set; }
            public string title { get; set; }
            public string link { get; set; }
            public string displayLink { get; set; }
            // and so on... add more properties here if you want
            // to deserialize them
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
            // and so on... add more properties here if you want to
            // deserialize them
        }
        #endregion

        List<HashSet<String>> AliasList = new List<HashSet<String>>();

        private void Help(String caller, String args)
        {
            StringBuilder availableMethodNames = new StringBuilder();
            foreach (KeyValuePair<string, CommandMethod> mthd in Commands)
            {
                availableMethodNames.Append(mthd.Key);
                availableMethodNames.Append(", ");
            }
            availableMethodNames.Remove(availableMethodNames.Length - 2, 1);
            SendToChannel(availableMethodNames.ToString());
        }

        private void Seen(String caller, String args)
        {
            Tuple<DateTime, String> lastSeentEntry;
            
            bool userSeen = SeenList.TryGetValue(args, out lastSeentEntry);
            
            if (userSeen)
            {
                DateTime lastSeentTime = lastSeentEntry.Item1;
                TimeSpan lastSeenSpan = DateTime.Now - lastSeentTime;
                string prettyTime = GetPrettyTime(lastSeenSpan);
                
                if (caller == args)
                    SendToChannel(String.Format("You were last seen (before this command) {0} on {1}. {2} ago. \"{3}\"", args, lastSeentTime, prettyTime, lastSeentEntry.Item2));
                else
                    SendToChannel(String.Format("Last seen {0} on {1}. {2} ago. \"{3}\"", args, lastSeentTime, prettyTime, lastSeentEntry.Item2));

            }
            else
            {
                SendToChannel(String.Format("Haven't ever seen \"{0}\" around", args));
            }
        }

        private void Tell(String caller, String args)
        {
            var parts = args.Split(' ');
            var recipients = parts[0].Split(',');
            string msg = String.Join(" ", parts, 1, parts.Length-1);
            foreach(var recip in recipients) {
                if (recip == IrcObject.IrcNick) {
                    SendToChannel("xD");
                    continue;
                }
                List<Tuple<String, DateTime, String>> thisTell;
                bool isPresent = TellList.TryGetValue(recip, out thisTell);
                if (isPresent)
                    thisTell.Add(new Tuple<String, DateTime, String>(caller, DateTime.Now, msg));
                else
                    TellList[recip] = new List<Tuple<String, DateTime, string>>(new Tuple<String, DateTime, string>[] { new Tuple<String, DateTime, string>(caller, DateTime.Now, msg) });
            }
            SendToChannel(String.Format("Will tell that to {0} when they are round", parts[0]));
        }

        private void Alias(String caller, String args)
        {
            args = args.Trim();
            var argParts = args.Split(' ');

            if(argParts.Length == 3)
            {
                string operation = argParts[0];
                string firstName = argParts[1];
                string secondName = argParts[2];
                switch (operation)
                {
                    case "add":
                        HashSet<string> firstNamePresent = null;
                        HashSet<string> secondNamePresent = null;
                        foreach (HashSet<string> hs in AliasList)
                        {
                            if(firstNamePresent != null && secondNamePresent != null)
                                break;
                            if(firstNamePresent == null) {
                                if (hs.Contains(firstName)) {
                                    firstNamePresent = hs;
                                }
                            }
                            if (secondNamePresent == null) {
                                if (hs.Contains(secondName)) {
                                    secondNamePresent = hs;
                                }
                            }
                        }
                        if (firstNamePresent != null && secondNamePresent != null)
                            SendToChannel("Both nicks are already assigned");
                        else if (firstNamePresent == null && secondNamePresent != null) {
                            secondNamePresent.Add(firstName);
                            SendToChannel(String.Format("{0} assigned to alias group containing {1}", firstName, secondName));
                        }
                        else if (firstNamePresent != null && secondNamePresent == null) {
                            firstNamePresent.Add(secondName);
                            SendToChannel(String.Format("{0} assigned to alias group containing {1}", secondName, firstName));
                        }
                        else if (firstNamePresent == null && secondNamePresent == null) {
                            HashSet<string> newAliasSet = new HashSet<string>();
                            newAliasSet.Add(firstName);
                            newAliasSet.Add(secondName);
                            AliasList.Add(newAliasSet);
                            SendToChannel(String.Format("New alias group created for {0} and {1}", firstName, secondName));
                        }
                            break;
                    case "remove":
                        break;
                    default:
                        SendToChannel(String.Format("{0} is an unknown operation, try 'add' or 'remove'", operation));
                        break;
                }
            }
            else if (argParts.Length == 1) {
                StringBuilder sb = new StringBuilder();
                foreach (HashSet<string> hs in AliasList) {
                    if (hs.Contains(args)) {
                        foreach (string userAlias in hs) {
                            sb.Append(userAlias);
                            sb.Append(", ");
                        }
                    }     
                }
                if (sb.Length > 0) {
                    sb.Remove(sb.Length - 2, 1);
                    SendToChannel(sb.ToString());
                }
                else {
                    SendToChannel(String.Format("No results found for {0}", args));
                }
            }
            else
            {
                SendToChannel("What are you doing?");
            }
            
        }

        private void Google(String caller, String args)
        {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=AIzaSyBly4r53qyXHbwbwr4j3Te0OazoBEhWsHY&cx=016968405084681006944:ksw5ydltpt0&q=";
            searchURL += args;
            GoogleAPIPrinter(searchURL);
        }

        private void GoogleImageSearch(String caller, String args)
        {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=AIzaSyBly4r53qyXHbwbwr4j3Te0OazoBEhWsHY&cx=016968405084681006944:ksw5ydltpt0&searchType=image&q=";
            searchURL += args;
            GoogleAPIPrinter(searchURL);
        }

        private void GoogleAPIPrinter(string searchURL)
        {
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            String data = reader.ReadToEnd();

            GoogleSearchResults results = JsonConvert.DeserializeObject<GoogleSearchResults>(data);
            StringBuilder messageAccumulator = new StringBuilder();
            int i = 0;
            int messageOverhead = GetMessageOverhead();
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
                SendToChannel("No Results Found");
            else
                SendToChannel(messageAccumulator.ToString());
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

        private int GetMessageOverhead()
        {
            return  String.Format("PRIVMSG {0} :", IrcObject.IrcChannel).Length;
        }

    }
}
