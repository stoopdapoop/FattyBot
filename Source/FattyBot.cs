using System;
using System.Collections.Generic;
using System.Threading;

namespace FattyBot {
    partial class FattyBot {
        private static IRC IrcObject;
        private const char CommandSymbol = '.';
        private Dictionary<string, Tuple<CommandMethod, string>> Commands = new Dictionary<string, Tuple<CommandMethod, string>>();
        private TellManager FattyTellManager;
        
        private Dictionary<string, Tuple<DateTime, String>> SeenList = new Dictionary<string, Tuple<DateTime, String>>();
        static DateTime TimeOfLastSentMessage = DateTime.Now;
        private delegate void CommandMethod(CommandInput chat);

        static ConfigReader Config;

        private AliasAPI AliasInterface;
        private GoogleAPI GoogleInterface;
        private MerriamWebsterAPI MerriamWebsterInterface;
        private Stands4Api Stands4Interface;
        private WolframAPI WolframInterface;
        private DatabaseManager DatabaseInterface;
        private BitBucketAPI BitBucketInterface;

        private Timer BitBuckerTimer;

        private static TimeSpan FiveMins = new TimeSpan(0, 5, 0);
        bool IsGagged { 
            get { return (DateTime.Now - GagTime) < FiveMins; } 
            set { IsGagged = value; } 
        }

        static void Main(string[] args) {

            FattyBot.Config = new ConfigReader();
            Config.AddConfig("Connection.cfg");

            string ircServer = Config.GetValue("ServerName");
            int ircPort;
            int.TryParse(Config.GetValue("Port"), out ircPort);
            string ircUser = Config.GetValue("Nick");
            string[] ircChan = Config.GetValueArray("Channel");
            string ircPassword = FattyBot.Config.GetValue("Password");
            try {
                FattyBot IrcApp = new FattyBot(ircServer, ircPort, ircUser, ircChan, ircPassword); 
                    // Connect to server
                FattyBot.IrcObject.Connect();
                
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString() + e.StackTrace);
            }
        }

        private FattyBot(string ircServer, int ircPort, string ircUser, string[] ircChan,string ircPassword) {         
            
            FattyBot.IrcObject = new IRC(ircUser, ircServer, ircChan, ircPort, ircPassword);
            // Assign events
            AssignEvents();
            CreateAPIInterfaces();
            RegisterCommands();
        }

        private void CreateAPIInterfaces() {
            FattyBot.Config.AddConfig("GoogleAPI.cfg");
            string googleAPIKey = Config.GetValue("GoogleAPIKey");
            string googleCustomSearch = Config.GetValue("GoogleCustomSearchID");
            this.GoogleInterface = new GoogleAPI(googleAPIKey, googleCustomSearch);

            FattyBot.Config.AddConfig("WolframAlphaAPI.cfg");
            string wolframKey = Config.GetValue("WolframKey");
            string maxWolframString = Config.GetValue("WolframMaxCallsPerHour");
            int maxWolframCallsPerHour = int.Parse(maxWolframString);
            this.WolframInterface = new WolframAPI(wolframKey, maxWolframCallsPerHour);

            FattyBot.Config.AddConfig("MerriamWebsterAPI.cfg");
            string websterDictionaryKey = Config.GetValue("MerriamWebsterDictionaryAPIKey");
            string websterThesaurusKey = Config.GetValue("MerriamWebsterThesaurusAPIKey");
            this.MerriamWebsterInterface = new MerriamWebsterAPI(websterDictionaryKey, websterThesaurusKey, this.GoogleInterface);

            FattyBot.Config.AddConfig("Stands4API.cfg");
            string stands4UserID = Config.GetValue("Stands4UserID");
            string stands4TokenID = Config.GetValue("Stands4TokenID");
            int stands4MaxDisplay = int.Parse(Config.GetValue("Stands4MaxResults"));
            this.Stands4Interface = new Stands4Api(stands4UserID, stands4TokenID, stands4MaxDisplay);
            
            FattyBot.Config.AddConfig("Database.cfg");
            string databaseServerAddress = Config.GetValue("DatabaseServerAddress");
            string databaseUserID = Config.GetValue("DatabaseUserID");
            string databasePassword = Config.GetValue("DatabasePassword");
            string databaseDatabase = Config.GetValue("DatabaseDatabase");
            this.DatabaseInterface = new DatabaseManager(databaseServerAddress, databaseUserID, databasePassword, databaseDatabase);

            FattyBot.Config.AddConfig("BitBucket.cfg");
            string[] bitBucketSubscriptions = FattyBot.Config.GetValueArray("BitBucketEvents");
            string[] bitBucketRepoAliases = FattyBot.Config.GetValueArray("BitBucketRepoAliases");
            string bitBucketLogin = Config.GetValue("BitBucketLogin");
            string bitBucketPassword = Config.GetValue("BitBucketPassword");
            BitBucketInterface = new BitBucketAPI(bitBucketSubscriptions, bitBucketRepoAliases, bitBucketLogin, bitBucketPassword);

            BitBuckerTimer = new Timer(BitBucketInterface.CheckRepos, null, 1000*30, 1000 * 60);

            this.AliasInterface = new AliasAPI(DatabaseInterface);
            this.FattyTellManager = new TellManager(DatabaseInterface);            
        }

        private void RegisterCommands() {
            this.Commands.Add("help", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "Just calls 'commands'"));
            this.Commands.Add("c", new Tuple<CommandMethod, string>(new CommandMethod(this.Stands4Interface.Calculate), "Performs conversions and calculations"));
            this.Commands.Add("a", new Tuple<CommandMethod, string>(new CommandMethod(this.Stands4Interface.Acronym), "Defines given acronym"));
            this.Commands.Add("seen", new Tuple<CommandMethod, string>(new CommandMethod(Seen), "When was user last seen"));
            this.Commands.Add("tell", new Tuple<CommandMethod, string>(new CommandMethod(Tell), "Gives message to user when seen"));
            this.Commands.Add("g", new Tuple<CommandMethod, string>(new CommandMethod(this.GoogleInterface.Google), "Google search"));
            this.Commands.Add("gis", new Tuple<CommandMethod, string>(new CommandMethod(this.GoogleInterface.GoogleImageSearch), "Google image search"));
            this.Commands.Add("alias", new Tuple<CommandMethod, string>(new CommandMethod(this.AliasInterface.Alias), "Assigns nicknames to people"));
            this.Commands.Add("commands", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "aeahueahu"));
            this.Commands.Add("quote", new Tuple<CommandMethod, string>(new CommandMethod(Stands4Interface.Quotes), "Quote search"));
            this.Commands.Add("wolfram", new Tuple<CommandMethod, string>(new CommandMethod(this.WolframInterface.Math), "Wolfram alpha"));
            this.Commands.Add("wolflimiter", new Tuple<CommandMethod, string>(new CommandMethod(WolframInterface.MathLimit), "Remaining wolfram calls this hour"));
            this.Commands.Add("8ball", new Tuple<CommandMethod, string>(new CommandMethod(EightBall), "Magic 8 Ball"));
            this.Commands.Add("d", new Tuple<CommandMethod, string>(new CommandMethod(this.MerriamWebsterInterface.Dictionary), "dictionary definitions"));
            this.Commands.Add("shorten", new Tuple<CommandMethod, string>(new CommandMethod(this.GoogleInterface.URLShortener), "Shortens URL"));
            this.Commands.Add("shutup", new Tuple<CommandMethod, string>(new CommandMethod(Shutup), "Gags me for 5 minutes"));
            this.Commands.Add("repo", new Tuple<CommandMethod, string>(new CommandMethod(this.BitBucketInterface.RepoLink), "Returns links to every repo being tracked in this channel"));
        }

        private void AssignEvents() {
            FattyBot.IrcObject.eventReceiving += new CommandReceived(IrcCommandReceived);
            FattyBot.IrcObject.eventTopicSet += new TopicSet(IrcTopicSet);
            FattyBot.IrcObject.eventTopicOwner += new TopicOwner(IrcTopicOwner);
            FattyBot.IrcObject.eventNamesList += new NamesList(IrcNamesList);
            FattyBot.IrcObject.eventServerMessage += new ServerMessage(IrcServerMessage);
            FattyBot.IrcObject.eventJoin += new Join(IrcJoin);
            FattyBot.IrcObject.eventPart += new Part(IrcPart);
            FattyBot.IrcObject.eventMode += new Mode(IrcMode);
            FattyBot.IrcObject.eventNickChange += new NickChange(IrcNickChange);
            FattyBot.IrcObject.eventKick += new Kick(IrcKick);
            FattyBot.IrcObject.eventQuit += new Quit(IrcQuit);
            FattyBot.IrcObject.eventChannelMessage += new ChannelMessage(IrcChannelMessage);
            FattyBot.IrcObject.eventPrivateMessage += new PrivateMessage(IrcPrivateMessage);
            FattyBot.IrcObject.eventNotice += new Notice(IrcNotice);
        }
        private void IrcCommandReceived(string ircCommand) {
        }

        private void IrcTopicSet(string ircChan, string ircTopic) {
            Console.WriteLine(String.Format("Topic of {0} is: {1}", ircChan, ircTopic));
        }

        private void IrcTopicOwner(string ircChan, string ircUser, string topicDate) {
            Console.WriteLine(String.Format("Topic of {0} set by {1} on {2} (unixtime)", ircChan, ircUser, topicDate));
        }

        private void IrcNamesList(string userNames) {
            Console.WriteLine(String.Format("Names List: {0}", userNames));
        }

        private void IrcServerMessage(string serverMessage) {
            Console.WriteLine(String.Format("Server Message: {0}", serverMessage));
        }

        private void IrcJoin(string ircChan, string ircUser) {
            bool hasMessagesWaiting = this.FattyTellManager.CheckTellsForUser(ircUser);
            if (!hasMessagesWaiting)
                return;

            Thread.Sleep(4000);
            SendNotice(ircUser, "You have tells waiting, Speak in a channel with " + IrcObject.IrcNick + " present, or pm me to receive it");
        }

        private void IrcPart(string ircChan, string ircUser) {
        }

        private void IrcMode(string ircChan, string ircUser, string userMode) {
        }

        private void IrcNickChange(string userOldNick, string userNewNick) {
        }

        private void IrcKick(string ircChannel, string userKicker, string userKicked, string kickMessage) {
            Console.WriteLine(String.Format("{0} kicks {1} out {2} ({3})", userKicker, userKicked, ircChannel, kickMessage));
        }

        private void IrcQuit(string userQuit, string quitMessage) {
        }

        private void IrcChannelMessage(string ircUser, string ircChannel, string message) {
            MonitorChat(ircUser, message, ircChannel, SourceType.Channel);
            this.SeenList[ircUser.ToLower()] = new Tuple<DateTime, String>(DateTime.Now, message);
        }

        private void IrcPrivateMessage(string ircUser, string message) {
            MonitorChat(ircUser, message, ircUser, SourceType.PrivateMessage);

            HandleBotProtocol(message, ircUser);          
        }

        private void IrcNotice(string ircUser, string message) {
            Console.WriteLine(String.Format("!NOTICE {0}:{1}", ircUser, message));
            string command = message;
            int separatorPosition = command.IndexOf(' ');
            string commandName;
            string commandArgs;
            if (separatorPosition > -1) {
                commandName = command.Substring(0, separatorPosition).ToLower();
                commandArgs = command.Substring(separatorPosition + 1);
            }
            else {
                commandName = command.ToLower();
                commandArgs = "";
            }
            switch (commandName) {
                case "say":
                    int spacePos = commandArgs.IndexOf(" ");
                    spacePos = Math.Max(0, spacePos);
                    string channel = commandArgs.Substring(0, spacePos);
                    string sayMessage = commandArgs.Substring(spacePos + 1, commandArgs.Length - (spacePos + 1));
                    SendMessage(channel, sayMessage);
                    break;
                case "join":
                    SendNotice(ircUser, "See you there");
                    IrcObject.JoinChannel(commandArgs);
                    break;
                case "leave":
                    SendMessage(commandArgs, "peacin'");
                    IrcObject.LeaveChannel(commandArgs);
                    break;
                case "quit":
                    IrcObject.Quit(commandArgs);
                    Environment.Exit(0);
                    break;
            }
        }

        string[] thanksReplies = { "np", "don't mention it", "yee", "yep", "yip", ":]", "I'm the best" };

        private void MonitorChat(string ircUser, string message, string messageSource, SourceType type) {
            if (type == SourceType.Channel)
                DatabaseInterface.SetChannelLog(ircUser, messageSource, IrcObject.IrcServer, message);
            
            DeliverTells(ircUser, messageSource);

            ExecuteCommands(message, ircUser, messageSource, type);
            if (message.ToLower() == String.Format("hi {0}", FattyBot.IrcObject.IrcNick).ToLower())
                SendMessage(messageSource, String.Format("hi {0} :]", ircUser));

            if (message.ToLower() == String.Format("thanks {0}", FattyBot.IrcObject.IrcNick).ToLower()) {
                Random rand = new Random();
                SendMessage(messageSource, String.Format("{0}, {1}", thanksReplies[rand.Next(thanksReplies.Length)], ircUser));
            }

        }

        private void ExecuteCommands(string message, string ircUser, string messageSource, SourceType messageSourceType) {
            if (String.IsNullOrWhiteSpace(message))
                return;

            try {
                if (messageSource != "#hanayuka" && message[0] != CommandSymbol || messageSource == "#hanayuka" && message[0] != ';')
                    return;

                string command = message.Substring(1);
                int separatorPosition = command.IndexOf(' ');
                string commandName;
                string commandArgs;
                if (separatorPosition > -1) {
                    commandName = command.Substring(0, separatorPosition);
                    commandArgs = command.Substring(separatorPosition + 1);
                }
                else {
                    commandName = command;
                    commandArgs = "";
                }

                CommandInput info = new CommandInput(ircUser, commandArgs, messageSource, commandName, messageSourceType);
                RunCommand(info);
            }
            catch (Exception ex) {
                SendMessage(messageSource, ex.ToString() + "with command: " + message + ": " + ex.TargetSite);
            }
        }

        private void DeliverTells(string ircUser, string messageSource) {
            List<Tuple<String, DateTime, string>> waitingTells;
            bool hasMessagesWaiting = this.FattyTellManager.GetTellsForUser(ircUser.ToLower(), out waitingTells);
            if (!hasMessagesWaiting)
                return;

            foreach (var waitingMessage in waitingTells) {
                string fromUser = waitingMessage.Item1;
                DateTime dateSent = waitingMessage.Item2;
                string prettyTime = GetPrettyTime(DateTime.Now - dateSent);
                string messageSent = waitingMessage.Item3;
                SendMessage(messageSource, String.Format("{0}:<{1}>{2} - sent {3} ago.", ircUser, fromUser, messageSent, prettyTime));
            }
        }

        private void RunCommand(CommandInput info) {
            Tuple<CommandMethod, string> meth;

            bool realCommand = this.Commands.TryGetValue(info.CommandName, out meth);
            if (info.Source[0] == '#' && IsGagged && realCommand) {
                TimeSpan timeLeft = (FiveMins - (DateTime.Now - GagTime));
                SendNotice(info.Caller, String.Format("Sorry, {0} is a spoilsport and has me gagged for the next {1} minutes and {2} seconds. These responses are only visible to you", Gagger, timeLeft.Minutes, timeLeft.Seconds));
            }
            else if (realCommand) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("{0} used command called \"{1}\" from {2} with arguments \"{3}\"", info.Caller, info.Source, info.CommandName, info.Arguments);
                Console.ResetColor();
                meth.Item1.Invoke(info);
            }
        }

        public static void SendMessage(string sendTo, string message) {
            message = message.Trim('\n', '\r');
            string outputMessage = String.Format("PRIVMSG {0} :{1}\r\n", sendTo, message);
            InternalSend(outputMessage);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void SendNotice(string sendTo, string message) {
            string outputMessage = String.Format("NOTICE {0} :{1}\r\n", sendTo, message);
            InternalSend(outputMessage);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void InternalSend(string formattedMessage) {
            TimeSpan SendInterval = new TimeSpan(0, 0, 0, 0, 500);
            //todo: loop here and retry
            TimeSpan TimeSinceLastMessageSent = DateTime.Now - FattyBot.TimeOfLastSentMessage;
            if (TimeSinceLastMessageSent < SendInterval)
                Thread.Sleep((SendInterval - TimeSinceLastMessageSent).Milliseconds);

            FattyBot.IrcObject.SendServerMessage(formattedMessage);
            FattyBot.TimeOfLastSentMessage = DateTime.Now;

        }

        private void HandleBotProtocol(string message, string ircUser) {
            if (message == "SYN")
                SendMessage(ircUser, "ACK");
        }
    }
}