using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FattyBot {
	partial class FattyBot {
		private IRC IrcObject;
        private const char CommandSymbol = '.';
        private Dictionary<string, Tuple<CommandMethod, string>> Commands = new Dictionary<string, Tuple<CommandMethod, string>>();
        private TellManager FattyTellManager;
        private Dictionary<string, Tuple<DateTime, String>> SeenList = new Dictionary<string, Tuple<DateTime, String>>();
        static DateTime TimeOfLastSentMessage = DateTime.Now;
        private delegate void CommandMethod(string caller, string args, string source);
		
		static void Main(string[] args) {

                      
            string IrcServer = args[0];
            int IrcPort;
            int.TryParse(args[1], out IrcPort);
            string IrcUser = args[2];
            string IrcChan = args[3];
            try
            {
                FattyBot IrcApp = new FattyBot(IrcServer, IrcPort, IrcUser, IrcChan);
            }
            catch (Exception e)
            {
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

            Commands.Add("seen", new Tuple<CommandMethod, string>(new CommandMethod(Seen), "When was user last seen"));
            Commands.Add("tell", new Tuple<CommandMethod, string>(new CommandMethod(Tell), "Gives message to user when seen"));
            Commands.Add("g", new Tuple<CommandMethod, string>(new CommandMethod(Google), "Google search"));
            Commands.Add("gis", new Tuple<CommandMethod, string>(new CommandMethod(GoogleImageSearch), "Google image search"));
            Commands.Add("alias", new Tuple<CommandMethod, string>(new CommandMethod(Alias), "Assigns nicknames to people"));
            Commands.Add("commands", new Tuple<CommandMethod, string>(new CommandMethod(ListCommands), "aeahueahu"));
            Commands.Add("wolfram", new Tuple<CommandMethod, string>(new CommandMethod(Math), "queries Wolfram alpha"));
            Commands.Add("8ball", new Tuple<CommandMethod, string>(new CommandMethod(EightBall), "Magic 8 Ball"));
			
			// Connect to server
			IrcObject.Connect(IrcServer, IrcPort, "poopie");
		} 
		
		private void IrcCommandReceived(string IrcCommand) {
		}
		
		private void IrcTopicSet(string IrcChan, string IrcTopic) {
			Console.WriteLine(String.Format("Topic of {0} is: {1}", IrcChan, IrcTopic));
		}
		
		private void IrcTopicOwner(string IrcChan, string IrcUser, string TopicDate) {
			Console.WriteLine(String.Format("Topic of {0} set by {1} on {2} (unixtime)", IrcChan, IrcUser, TopicDate));
		} 
		
		private void IrcNamesList(string UserNames) {
			Console.WriteLine(String.Format("Names List: {0}", UserNames));
		} 
		
		private void IrcServerMessage(string ServerMessage) {
			Console.WriteLine(String.Format("Server Message: {0}", ServerMessage));
		} 
		
		private void IrcJoin(string IrcChan, string IrcUser) {
		} 
		
		private void IrcPart(string IrcChan, string IrcUser) {			
		} 
		
		private void IrcMode(string IrcChan, string IrcUser, string UserMode) {
            //if (IrcUser != IrcChan) {
            //    Console.WriteLine(String.Format("{0} sets {1} in {2}", IrcUser, UserMode, IrcChan));
            //}
		} 
		
		private void IrcNickChange(string UserOldNick, string UserNewNick) {
			//Console.WriteLine(String.Format("{0} changes nick to {1}", UserOldNick, UserNewNick));
		} 
		
		private void IrcKick(string IrcChannel, string UserKicker, string UserKicked, string KickMessage) {
			Console.WriteLine(String.Format("{0} kicks {1} out {2} ({3})", UserKicker, UserKicked, IrcChannel, KickMessage));
		} 
		
		private void IrcQuit(string UserQuit, string QuitMessage) {
		} 

        private void IrcChannelMessage(string IrcUser, string Message) {
            MonitorChat(IrcUser, Message, IrcObject.IrcChannel);
            SeenList[IrcUser] = new Tuple<DateTime, String>(DateTime.Now, Message);
        }

        private void IrcPrivateMessage(string IrcUser, string Message) {
            MonitorChat(IrcUser, Message, IrcUser);
        }

        private void IrcNotice(string IrcUser, string Message) {
            Console.WriteLine(String.Format("!NOTICE {0}:{1}", IrcUser, Message));
        }

        private void MonitorChat(string IrcUser, string Message, string MessageSource) {
            DeliverTells(IrcUser, MessageSource);
            ExecuteCommands(Message, IrcUser, MessageSource);
        }

        private void ExecuteCommands(string Message, string IrcUser, string MessageSource) {
            if (Message[0] == CommandSymbol) {
                string command = Message.Substring(1);
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
                try
                {
                    RunCommand(IrcUser, MessageSource, commandName, commandArgs);
                }
                catch (Exception ex)
                {
                    SendMessage(MessageSource, ex.ToString());
                }
                
            }
        }

        private void DeliverTells(string IrcUser, string MessageSource) {
            List<Tuple<String, DateTime, string>> waitingTells;
            bool hasMessagesWaiting = FattyTellManager.GetTellsForUser(IrcUser, out waitingTells);
            if (hasMessagesWaiting) {
                foreach (var waitingMessage in waitingTells) {
                    string fromUser = waitingMessage.Item1;
                    DateTime dateSent = waitingMessage.Item2;
                    string prettyTime = GetPrettyTime(DateTime.Now - dateSent);
                    string messageSent = waitingMessage.Item3;
                    SendMessage(MessageSource, String.Format("{0}:<{1}>{2} - sent {3} ago.", IrcUser, fromUser, messageSent, prettyTime));
                }
            }
        }

        private void RunCommand(string caller, string source, string command, string args)
        {           
            Tuple<CommandMethod, string> meth;
            if (Commands.TryGetValue(command, out meth))
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("{0} used command called \"{1}\" with arguments \"{2}\"", caller, command, args);
                Console.ResetColor();
                meth.Item1.Invoke(caller, args, source);
            }
            
        }

        private void SendMessage(string sendTo, string message)
        {
            string outputMessage = String.Format("PRIVMSG {0} :{1}", sendTo, message);
            TimeSpan SendInterval = new TimeSpan(0, 0, 0, 0 ,800);
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