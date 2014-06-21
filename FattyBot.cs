using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

namespace FattyBot {
	partial class FattyBot {
		private IRC IrcObject;
        private const char CommandSymbol = ';';
        private Dictionary<string, CommandMethod> Commands = new Dictionary<string, CommandMethod>();
        private Dictionary<string, Tuple<DateTime, String>> SeenList = new Dictionary<string, Tuple<DateTime, String>>();
        private Dictionary<string, List<Tuple<String, DateTime, String>>> TellList = new Dictionary<string, List<Tuple<String, DateTime, String>>>();
        private delegate void CommandMethod(string caller, string args);
		
		static void Main(string[] args) {
            
            //Console.Write("Server: ");
            //string IrcServer = Console.ReadLine();
            //Console.Write("Port: ");
            //int IrcPort = Convert.ToInt32(Console.ReadLine());
            //Console.Write("User: ");
            //string IrcUser = Console.ReadLine();
            //Console.Write("Chan: ");
            //string IrcChan = Console.ReadLine();


            string IrcServer = "irc.rizon.us";
            int IrcPort = 6667;
            string IrcUser = "fatty";
            string IrcChan = "#cuties";
            try
            {
                FattyBot IrcApp = new FattyBot(IrcServer, IrcPort, IrcUser, IrcChan);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + e.StackTrace);
            }
		} /* Main */
		
		private FattyBot(string IrcServer, int IrcPort, string IrcUser, string IrcChan) {
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

            Commands.Add("seen", new CommandMethod(Seen));
            Commands.Add("tell", new CommandMethod(Tell));
            Commands.Add("g", new CommandMethod(Google));
            Commands.Add("gis", new CommandMethod(GoogleImageSearch));
            Commands.Add("alias", new CommandMethod(Alias));
            Commands.Add("help", new CommandMethod(Help));
			
			// Connect to server
			IrcObject.Connect(IrcServer, IrcPort);
		} /* cIRC */
		
		private void IrcCommandReceived(string IrcCommand) {
            var parts = IrcCommand.Split(' ');
            
            if (parts[1] == "PRIVMSG") {
                string user = parts[0].Substring(1, parts[0].IndexOf('!')-1);
                string channel = parts[2];
                string message = parts[3].Substring(1);
                for (int i = 4; i < parts.Length; ++i)
                    message += " " + parts[i];

                //Console.ForegroundColor = ConsoleColor.DarkGray;
                //Console.WriteLine("{0} says \"{1}\" in {2}", user, message, channel);
                //Console.ResetColor();
               

                List<Tuple<String, DateTime, string>> waitingTells;
                bool hasMessagesWaiting = TellList.TryGetValue(user, out waitingTells);
                if (hasMessagesWaiting)
                {
                    foreach (var waitingMessage in waitingTells)
                    {
                        string fromUser = waitingMessage.Item1;
                        DateTime dateSent = waitingMessage.Item2;
                        string prettyTime = GetPrettyTime(DateTime.Now - dateSent);
                        string messageSent = waitingMessage.Item3;
                        SendToChannel(String.Format("{0}:<{1}>{2} - sent {3} ago.", user, fromUser, messageSent, prettyTime));
                    }
                    TellList.Remove(user);
                }

                if (message[0] == CommandSymbol)
                {
                    string command = message.Substring(1);
                    int separatorPosition = command.IndexOf(' ');
                    string commandName;
                    string commandArgs;
                    if (separatorPosition > -1)
                    {
                        commandName = command.Substring(0, separatorPosition);
                        commandArgs = command.Substring(separatorPosition + 1);
                        
                    }
                    else
                    {
                        commandName = command;
                        commandArgs = "";
                    }
                                        
                    RunCommand(user, commandName, commandArgs);
                }
                SeenList[user] = new Tuple<DateTime, String>(DateTime.Now, message);
                
            }
            else {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(DateTime.Now + IrcCommand);
                Console.ResetColor();
            }
           
			
		} /* IrcCommandReceived */
		
		private void IrcTopicSet(string IrcChan, string IrcTopic) {
			Console.WriteLine(String.Format("Topic of {0} is: {1}", IrcChan, IrcTopic));
		} /* IrcTopicSet */
		
		private void IrcTopicOwner(string IrcChan, string IrcUser, string TopicDate) {
			Console.WriteLine(String.Format("Topic of {0} set by {1} on {2} (unixtime)", IrcChan, IrcUser, TopicDate));
		} /* IrcTopicSet */
		
		private void IrcNamesList(string UserNames) {
			Console.WriteLine(String.Format("Names List: {0}", UserNames));
		} /* IrcNamesList */
		
		private void IrcServerMessage(string ServerMessage) {
			Console.WriteLine(String.Format("Server Message: {0}", ServerMessage));
		} /* IrcNamesList */
		
		private void IrcJoin(string IrcChan, string IrcUser) {
			//Console.WriteLine(String.Format("{0} joins {1}", IrcUser, IrcChan));
			//IrcObject.IrcWriter.WriteLine(String.Format("NOTICE {0} :Hello {0}, welcome to {1}!", IrcUser, IrcChan));
			//IrcObject.IrcWriter.Flush ();	
		} /* IrcJoin */
		
		private void IrcPart(string IrcChan, string IrcUser) {
			//Console.WriteLine(String.Format("{0} parts {1}", IrcUser, IrcChan));
		} /* IrcPart */
		
		private void IrcMode(string IrcChan, string IrcUser, string UserMode) {
			if (IrcUser != IrcChan) {
				Console.WriteLine(String.Format("{0} sets {1} in {2}", IrcUser, UserMode, IrcChan));
			}
		} /* IrcMode */
		
		private void IrcNickChange(string UserOldNick, string UserNewNick) {
			Console.WriteLine(String.Format("{0} changes nick to {1}", UserOldNick, UserNewNick));
		} /* IrcNickChange */
		
		private void IrcKick(string IrcChannel, string UserKicker, string UserKicked, string KickMessage) {
			Console.WriteLine(String.Format("{0} kicks {1} out {2} ({3})", UserKicker, UserKicked, IrcChannel, KickMessage));
		} /* IrcKick */
		
		private void IrcQuit(string UserQuit, string QuitMessage) {
			//Console.WriteLine(String.Format("{0} has quit IRC ({1})", UserQuit, QuitMessage));
		} /* IrcQuit */

        private void RunCommand(string caller,string command, string args)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("{0} used command called \"{1}\" with arguments \"{2}\"", caller, command, args);
            CommandMethod meth;
            if (Commands.TryGetValue(command, out meth))
                meth.Invoke(caller, args);
            else
                SendToChannel(String.Format("{0} is not a valid command", command));
            Console.ResetColor();
        }

        private void SendToChannel(string message)
        {
            //Encoding enc = new UTF8Encoding(false, false);

            string outputMessage = String.Format("PRIVMSG {0} :{1}", IrcObject.IrcChannel, message);
            //byte[] rawBytes = enc.GetBytes(outputMessage);
            //char[] rawChars = enc.GetChars(rawBytes);
            //IrcObject.IrcWriter.WriteLine(rawChars);
            IrcObject.IrcWriter.WriteLine(outputMessage);
            IrcObject.IrcWriter.Flush();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();

        }

	} /* cIRC */
} /* cIRC */