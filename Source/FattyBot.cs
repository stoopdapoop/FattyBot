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
        private delegate void CommandMethod(string caller, string args, string source);

        static ConfigReader Config;

        private AliasAPI AliasInterface;
        private GoogleAPI GoogleInterface;
        private MerriamWebsterAPI MerriamWebsterInterface;
        private Stands4Api Stands4Interface;
        private WolframAPI WolframInterface;

        private static TimeSpan FiveMins = new TimeSpan(0, 5, 0);
        bool IsGagged { 
            get { return (DateTime.Now - GagTime) < FiveMins; } 
            set { IsGagged = value; } 
        }

        static void Main(string[] args) {

            FattyBot.Config = new ConfigReader();
            Config.AddConfig("connection.cfg");


            string ircServer = Config.GetValue("ServerName");
            int ircPort;
            int.TryParse(Config.GetValue("Port"), out ircPort);
            string ircUser = Config.GetValue("Nick");
            string ircChan = Config.GetValue("Channel");
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

        private FattyBot(string ircServer, int ircPort, string ircUser, string ircChan,string ircPassword) {


            Config.AddConfig("GoogleAPI.cfg");
            string googleAPIKey = Config.GetValue("GoogleAPIKey");
            string googleCustomSearch = Config.GetValue("GoogleCustomSearchID");
            this.GoogleInterface = new GoogleAPI(googleAPIKey, googleCustomSearch);
            this.WolframInterface = new WolframAPI();
            this.MerriamWebsterInterface = new MerriamWebsterAPI(this.GoogleInterface);
            this.AliasInterface = new AliasAPI();
            this.FattyTellManager = new TellManager(this.AliasInterface);
            this.Stands4Interface = new Stands4Api();



            FattyBot.IrcObject = new IRC(ircUser, ircChan, ircServer, ircPort, ircPassword);
            // Assign events
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

            this.Commands.Add("help", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "Just calls 'commands'"));
            this.Commands.Add("a", new Tuple<CommandMethod, string>(new CommandMethod(this.Stands4Interface.Acronym), "Defines given acronym"));
            this.Commands.Add("seen", new Tuple<CommandMethod, string>(new CommandMethod(Seen), "When was user last seen"));
            this.Commands.Add("tell", new Tuple<CommandMethod, string>(new CommandMethod(Tell), "Gives message to user when seen"));
            this.Commands.Add("g", new Tuple<CommandMethod, string>(new CommandMethod(this.GoogleInterface.Google), "Google search"));
            this.Commands.Add("gis", new Tuple<CommandMethod, string>(new CommandMethod(this.GoogleInterface.GoogleImageSearch), "Google image search"));
            this.Commands.Add("alias", new Tuple<CommandMethod, string>(new CommandMethod(this.AliasInterface.Alias), "Assigns nicknames to people"));
            this.Commands.Add("commands", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "aeahueahu"));
            this.Commands.Add("wolfram", new Tuple<CommandMethod, string>(new CommandMethod(this.WolframInterface.Math), "Wolfram alpha"));
            this.Commands.Add("wolflimiter", new Tuple<CommandMethod, string>(new CommandMethod(WolframInterface.MathLimit), "Remaining wolfram calls this hour"));
            this.Commands.Add("8ball", new Tuple<CommandMethod, string>(new CommandMethod(EightBall), "Magic 8 Ball"));
            this.Commands.Add("d", new Tuple<CommandMethod, string>(new CommandMethod(this.MerriamWebsterInterface.Dictionary), "dictionary definitions"));
            this.Commands.Add("shorten", new Tuple<CommandMethod, string>(new CommandMethod(this.GoogleInterface.URLShortener), "Shortens URL"));
            this.Commands.Add("shutup", new Tuple<CommandMethod, string>(new CommandMethod(Shutup), "Gags me for 5 minutes"));

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

        private void IrcChannelMessage(string ircUser, string message) {
            MonitorChat(ircUser, message, FattyBot.IrcObject.IrcChannel);
            this.SeenList[ircUser] = new Tuple<DateTime, String>(DateTime.Now, message);
        }

        private void IrcPrivateMessage(string ircUser, string message) {
            MonitorChat(ircUser, message, ircUser);
        }

        private void IrcNotice(string ircUser, string message) {
            Console.WriteLine(String.Format("!NOTICE {0}:{1}", ircUser, message));
            // get rid of this
            if (message[0] == CommandSymbol) {
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
                if (commandName == "say")
                    SendMessage(FattyBot.IrcObject.IrcChannel, commandArgs);
            }
        }

        private void MonitorChat(string ircUser, string message, string messageSource) {
            DeliverTells(ircUser, messageSource);

            ExecuteCommands(message, ircUser, messageSource);
            if (message.ToLower() == String.Format("hi {0}", FattyBot.IrcObject.IrcNick).ToLower())
                SendMessage(messageSource, String.Format("hi {0} :]", ircUser));

        }

        private void ExecuteCommands(string message, string ircUser, string messageSource) {
            if (message[0] == CommandSymbol) {
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
                try {
                    RunCommand(ircUser, messageSource, commandName, commandArgs);
                }
                catch (Exception ex) {
                    SendMessage(messageSource, ex.ToString());
                }

            }
        }

        private void DeliverTells(string ircUser, string messageSource) {
            List<Tuple<String, DateTime, string>> waitingTells;
            bool hasMessagesWaiting = this.FattyTellManager.GetTellsForUser(ircUser, out waitingTells);
            if (hasMessagesWaiting) {
                foreach (var waitingMessage in waitingTells) {
                    string fromUser = waitingMessage.Item1;
                    DateTime dateSent = waitingMessage.Item2;
                    string prettyTime = GetPrettyTime(DateTime.Now - dateSent);
                    string messageSent = waitingMessage.Item3;
                    SendMessage(messageSource, String.Format("{0}:<{1}>{2} - sent {3} ago.", ircUser, fromUser, messageSent, prettyTime));
                }
            }
        }

        private void RunCommand(string caller, string source, string command, string args) {
            Tuple<CommandMethod, string> meth;

            bool realCommand = this.Commands.TryGetValue(command, out meth);
            if (source == FattyBot.IrcObject.IrcChannel && IsGagged && realCommand) {
                TimeSpan timeLeft = (FiveMins - (DateTime.Now - GagTime));
                SendNotice(caller, String.Format("Sorry, {0} is a spoilsport and has me gagged for the next {1} minutes and {2} seconds. These responses are only visible to you", Gagger, timeLeft.Minutes, timeLeft.Seconds));
            }
            else if (realCommand) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("{0} used command called \"{1}\" with arguments \"{2}\"", caller, command, args);
                Console.ResetColor();
                meth.Item1.Invoke(caller, args, source);
            }
        }

        public static void SendMessage(string sendTo, string message) {
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
            TimeSpan SendInterval = new TimeSpan(0, 0, 0, 0, 400);
            TimeSpan TimeSinceLastMessageSent = DateTime.Now - FattyBot.TimeOfLastSentMessage;
            if (TimeSinceLastMessageSent < SendInterval)
                Thread.Sleep((SendInterval - TimeSinceLastMessageSent).Milliseconds);
            FattyBot.IrcObject.IrcWriter.WriteLine(formattedMessage);
            FattyBot.IrcObject.IrcWriter.Flush();
            FattyBot.TimeOfLastSentMessage = DateTime.Now;
        }
    }
}