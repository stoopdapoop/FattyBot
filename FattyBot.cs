using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FattyBot {
    partial class FattyBot {
        private static IRC IrcObject;
        private const char CommandSymbol = '.';
        private Dictionary<string, Tuple<CommandMethod, string>> Commands = new Dictionary<string, Tuple<CommandMethod, string>>();
        private TellManager FattyTellManager;
        private GoogleAPI GoogleInterface = new GoogleAPI();
        private Dictionary<string, Tuple<DateTime, String>> SeenList = new Dictionary<string, Tuple<DateTime, String>>();
        static DateTime TimeOfLastSentMessage = DateTime.Now;
        private delegate void CommandMethod(string caller, string args, string source);

        static void Main(string[] args) {


            string IrcServer = args[0];
            int IrcPort;
            int.TryParse(args[1], out IrcPort);
            string IrcUser = args[2];
            string IrcChan = args[3];
            try {
                FattyBot IrcApp = new FattyBot(IrcServer, IrcPort, IrcUser, IrcChan);
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString() + e.StackTrace);
            }
        }

        private FattyBot(string IrcServer, int IrcPort, string IrcUser, string IrcChan) {
            FattyTellManager = new TellManager(FattyUserAliases);

            IrcObject = new IRC(IrcUser, IrcChan);

            // Assign events
            IrcObject.eventReceiving += new CommandReceived(IrcCommandReceived);
            IrcObject.eventTopicSet += new TopicSet(IrcTopicSet);
            IrcObject.eventTopicOwner += new TopicOwner(IrcTopicOwner);
            IrcObject.eventNamesList += new NamesList(IrcNamesList);
            IrcObject.eventServerMessage += new ServerMessage(IrcServerMessage);
            IrcObject.eventJoin += new Join(IrcJoin);
            IrcObject.eventPart += new Part(IrcPart);
            IrcObject.eventMode += new Mode(IrcMode);
            IrcObject.eventNickChange += new NickChange(IrcNickChange);
            IrcObject.eventKick += new Kick(IrcKick);
            IrcObject.eventQuit += new Quit(IrcQuit);
            IrcObject.eventChannelMessage += new ChannelMessage(IrcChannelMessage);
            IrcObject.eventPrivateMessage += new PrivateMessage(IrcPrivateMessage);
            IrcObject.eventNotice += new Notice(IrcNotice);

            Commands.Add("help", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "Just calls 'commands'"));
            Commands.Add("a", new Tuple<CommandMethod, string>(new CommandMethod(Acronym), "Defines given acronym"));
            Commands.Add("seen", new Tuple<CommandMethod, string>(new CommandMethod(Seen), "When was user last seen"));
            Commands.Add("tell", new Tuple<CommandMethod, string>(new CommandMethod(Tell), "Gives message to user when seen"));
            Commands.Add("g", new Tuple<CommandMethod, string>(new CommandMethod(GoogleInterface.Google), "Google search"));
            Commands.Add("gis", new Tuple<CommandMethod, string>(new CommandMethod(GoogleInterface.GoogleImageSearch), "Google image search"));
            Commands.Add("alias", new Tuple<CommandMethod, string>(new CommandMethod(Alias), "Assigns nicknames to people"));
            Commands.Add("commands", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "aeahueahu"));
            Commands.Add("wolfram", new Tuple<CommandMethod, string>(new CommandMethod(Math), "Wolfram alpha"));
            Commands.Add("8ball", new Tuple<CommandMethod, string>(new CommandMethod(EightBall), "Magic 8 Ball"));
            Commands.Add("d", new Tuple<CommandMethod, string>(new CommandMethod(Dictionary), "dictionary definitions"));
            Commands.Add("shorten", new Tuple<CommandMethod, string>(new CommandMethod(GoogleInterface.GetShortURL), "Shortens URL"));
            Commands.Add("wolflimiter", new Tuple<CommandMethod, string>(new CommandMethod(MathLimit), "Remaining wolfram calls this hour"));
            Commands.Add("shutup", new Tuple<CommandMethod, string>(new CommandMethod(Shutup), "Gags me for 5 minutes"));

            // Connect to server
            IrcObject.Connect(IrcServer, IrcPort, "poopie");
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
            MonitorChat(ircUser, message, IrcObject.IrcChannel);
            SeenList[ircUser] = new Tuple<DateTime, String>(DateTime.Now, message);
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
                    SendMessage(IrcObject.IrcChannel, commandArgs);
            }
        }

        private void MonitorChat(string ircUser, string message, string messageSource) {
            DeliverTells(ircUser, messageSource);

            ExecuteCommands(message, ircUser, messageSource);
            if (message.ToLower() == String.Format("hi {0}", IrcObject.IrcNick).ToLower())
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
            bool hasMessagesWaiting = FattyTellManager.GetTellsForUser(ircUser, out waitingTells);
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

            TimeSpan FiveMins = new TimeSpan(0, 5, 0);

            bool realCommand = Commands.TryGetValue(command, out meth);
            if (source == IrcObject.IrcChannel && (DateTime.Now - GagTime) < FiveMins && realCommand) {
                TimeSpan timeLeft = (FiveMins - (DateTime.Now - GagTime));
                SendMessage(source, String.Format("Sorry, {0} is a spoilsport and has me gagged for the next {1} minutes and {2} seconds. I can still respond to PM's though", Gagger, timeLeft.Minutes, timeLeft.Seconds));
            }
            else if (realCommand) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("{0} used command called \"{1}\" with arguments \"{2}\"", caller, command, args);
                Console.ResetColor();
                meth.Item1.Invoke(caller, args, source);
            }
        }

        public static void SendMessage(string sendTo, string message) {
            string outputMessage = String.Format("PRIVMSG {0} :{1}", sendTo, message);
            TimeSpan SendInterval = new TimeSpan(0, 0, 0, 0, 500);
            TimeSpan TimeSinceLastMessageSent = DateTime.Now - TimeOfLastSentMessage;
            if (TimeSinceLastMessageSent < SendInterval)
                Thread.Sleep((SendInterval - TimeSinceLastMessageSent).Milliseconds);
            IrcObject.IrcWriter.WriteLine(outputMessage);
            IrcObject.IrcWriter.Flush();
            TimeOfLastSentMessage = DateTime.Now;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}