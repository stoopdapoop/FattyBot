using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;

namespace System.Net {
	#region Delegates
    public delegate void ChannelMessage(string ircUser, string message);
    public delegate void PrivateMessage(string ircUser, string message);
	public delegate void CommandReceived(string ircCommand);
	public delegate void TopicSet(string ircChannel, string ircTopic);
    public delegate void TopicOwner(string ircChannel, string ircUser, string topicDate);
	public delegate void NamesList(string userNames);
	public delegate void ServerMessage(string serverMessage);
    public delegate void Join(string ircChannel, string ircUser);
    public delegate void Part(string ircChannel, string ircUser);
    public delegate void Mode(string ircChannel, string ircUser, string userMode);
	public delegate void NickChange(string UserOldNick, string UserNewNick);
    public delegate void Kick(string ircChannel, string userKicker, string userKicked, string kickMessage);
	public delegate void Quit(string userQuit, string quitMessage);
    public delegate void Notice(string ircUser, string message);
	#endregion
	
	public class IRC {
		#region Events
        public event ChannelMessage eventChannelMessage;
        public event PrivateMessage eventPrivateMessage;
		public event CommandReceived eventReceiving;
		public event TopicSet eventTopicSet;
		public event TopicOwner eventTopicOwner;
		public event NamesList eventNamesList;
		public event ServerMessage eventServerMessage;
		public event Join eventJoin;
		public event Part eventPart;
		public event Mode eventMode;
		public event NickChange eventNickChange;
		public event Kick eventKick;
		public event Quit eventQuit;
        public event Notice eventNotice;
		#endregion
		
		#region Private Variables
        // aehuaeahu
		#endregion
		
		#region Properties
        public string IrcServer { get; set;  }
        public int IrcPort { get; set; }
        public string IrcNick { get; set; }
        public string IrcUserName { get; set; }
        public string IrcRealName { get; set; }
        public string IrcChannel { get; set; } 
        public string AuthPassword { get; set; }
        public bool IsInvisble { get; set; }

        public TcpClient IrcConnection { get; set; }
        public NetworkStream IrcStream { get; set; }
        public StreamWriter IrcWriter { get; set; }
        public StreamReader IrcReader { get; set; } 
		#endregion	
		
		#region Constructor
		public IRC(string ircNick, string ircChannel) {
			this.IrcNick = ircNick;
			this.IrcUserName = System.Environment.MachineName;
			this.IrcRealName = "FattyBot v1.0";
			this.IrcChannel = ircChannel;
			this.IsInvisble = false;
		} /* IRC */
		#endregion
		
		#region Public Methods
		public void Connect(string ircServer, int ircPort, string authPassword) {
			this.IrcServer = ircServer;
			this.IrcPort = ircPort;
            this.AuthPassword = authPassword;

			// Connect with the IRC server.
			this.IrcConnection = new TcpClient(this.IrcServer, this.IrcPort);
			this.IrcStream = this.IrcConnection.GetStream();
			this.IrcReader = new StreamReader(this.IrcStream);
			this.IrcWriter = new StreamWriter(this.IrcStream);

            Console.WriteLine("Connected to {0}", ircServer);


			// Authenticate our user
			string isInvisible = this.IsInvisble ? "8" : "0";
			this.IrcWriter.WriteLine(String.Format("USER {0} {1} * :{2}", this.IrcUserName, isInvisible, this.IrcRealName));
			this.IrcWriter.Flush();
			this.IrcWriter.WriteLine(String.Format("NICK {0}", this.IrcNick));
			this.IrcWriter.Flush();
            this.IrcWriter.WriteLine("PRIVMSG NickServ :IDENTIFY " + authPassword);
            this.IrcWriter.Flush();
            Thread.Sleep(400);
			this.IrcWriter.WriteLine(String.Format("JOIN {0}", this.IrcChannel));
			this.IrcWriter.Flush();

			// Listen for commands
			while (true) {
                ListenForCommands();			
			}
		}

        private void ListenForCommands()
        {
            try
            {
                //todo: function this, yo
                if(!this.IrcConnection.Connected)
                {
                    Thread.Sleep(5000);
                    this.IrcConnection = new TcpClient(this.IrcServer, this.IrcPort);
			        this.IrcStream = this.IrcConnection.GetStream();
			        this.IrcReader = new StreamReader(this.IrcStream);
                    this.IrcWriter = new StreamWriter(this.IrcStream);

                    Console.WriteLine("Connected to {0}", IrcServer);

                    // Authenticate our user
                    string isInvisible = this.IsInvisble ? "8" : "0";
                    this.IrcWriter.WriteLine(String.Format("USER {0} {1} * :{2}", this.IrcUserName, isInvisible, this.IrcRealName));
                    this.IrcWriter.Flush();
                    this.IrcWriter.WriteLine(String.Format("NICK {0}", this.IrcNick));
                    this.IrcWriter.Flush();
                    this.IrcWriter.WriteLine("PRIVMSG NickServ :IDENTIFY " + AuthPassword);
                    this.IrcWriter.Flush();
                    Thread.Sleep(400);
                    this.IrcWriter.WriteLine(String.Format("JOIN {0}", this.IrcChannel));
                    this.IrcWriter.Flush();
                }

                string ircCommand;
                while ((ircCommand = this.IrcReader.ReadLine()) != null)
                {
                    if (eventReceiving != null) { this.eventReceiving(ircCommand); }

                    string[] commandParts = new string[ircCommand.Split(' ').Length];
                    commandParts = ircCommand.Split(' ');
                    if (commandParts[0].Substring(0, 1) == ":")
                    {
                        commandParts[0] = commandParts[0].Remove(0, 1);
                    }

                    if (commandParts[0] == this.IrcServer)
                    {
                        // Server message
                        switch (commandParts[1])
                        {
                            case "332": this.IrcTopic(commandParts); break;
                            case "333": this.IrcTopicOwner(commandParts); break;
                            case "353": this.IrcNamesList(commandParts); break;
                            case "366": /*this.IrcEndNamesList(commandParts);*/ break;
                            case "372": /*this.IrcMOTD(commandParts);*/ break;
                            case "376": /*this.IrcEndMOTD(commandParts);*/ break;
                            case "433": 
                                Random rand = new Random();
                                this.IrcWriter.WriteLine(String.Format("NICK {0}", this.IrcNick+rand.Next(9999)));
			                    this.IrcWriter.Flush();
                                Thread.Sleep(400);
                                this.IrcWriter.WriteLine(String.Format("JOIN {0}", this.IrcChannel));
			                    this.IrcWriter.Flush();
                                break;
                            default: this.IrcServerMessage(commandParts); break;

                        }
                    }
                    else if (commandParts[0] == "PING")
                    {
                        // Server PING, send PONG back
                        this.IrcPing(commandParts);
                    }
                    else
                    {
                        // Normal message
                        string commandAction = commandParts[1];
                        switch (commandAction)
                        {
                            case "JOIN": this.IrcJoin(commandParts); break;
                            case "PART": this.IrcPart(commandParts); break;
                            case "MODE": this.IrcMode(commandParts); break;
                            case "NICK": this.IrcNickChange(commandParts); break;
                            case "KICK": this.IrcKick(commandParts); break;
                            case "QUIT": this.IrcQuit(commandParts); break;
                            case "PRIVMSG": this.IrcPrivateMessage(commandParts); break;
                            case "NOTICE": this.IrcNotice(commandParts); break;
                                //todo: implement topic changed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if(this.IrcConnection.Connected)
                    throw;
                
            }
            
        }
		#endregion
		
		#region Private Methods
		#region Server Messages
		private void IrcTopic(string[] ircCommand) {
			string ircChannel = ircCommand[3];
			string ircTopic = "";
            ircTopic = RejoinMessage(ircCommand, 4);
			if (eventTopicSet != null) { this.eventTopicSet(ircChannel, ircTopic.Remove(0, 1).Trim()); }
		} 
		
		private void IrcTopicOwner(string[] ircCommand) {
			string ircChannel = ircCommand[3];
			string ircUser = ircCommand[4].Split('!')[0];
			string topicDate = ircCommand[5];
			if (eventTopicOwner != null) { this.eventTopicOwner(ircChannel, ircUser, topicDate); }
		}
		
		private void IrcNamesList(string[] ircCommand) {
		  string userNames = "";
          userNames = RejoinMessage(ircCommand, 5);
			if (eventNamesList != null) { this.eventNamesList(userNames.Remove(0, 1).Trim()); }
		} 
		
		private void IrcServerMessage(string[] ircCommand) {
			string serverMessage = "";
            serverMessage = RejoinMessage(ircCommand, 1);
			if (eventServerMessage != null) { this.eventServerMessage(serverMessage.Trim()); }
		} 
		#endregion
		
		#region Ping
		private void IrcPing(string[] ircCommand) {
			string pingHash = "";
			for (int intI = 1; intI < ircCommand.Length; intI++) {
				pingHash += ircCommand[intI] + " ";
			}
			this.IrcWriter.WriteLine("PONG " + pingHash);
			this.IrcWriter.Flush();
		}
		#endregion
		
		#region User Messages
		private void IrcJoin(string[] ircCommand) {
			string ircChannel = ircCommand[2];
			string ircUser = ircCommand[0].Split('!')[0];
			if (eventJoin != null) { this.eventJoin(ircChannel.Remove(0, 1), ircUser); }
		}
		
		private void IrcPart(string[] ircCommand) {
			string ircChannel = ircCommand[2];
			string ircUser = ircCommand[0].Split('!')[0];
			if (eventPart != null) { this.eventPart(ircChannel, ircUser); }
		} 
		
		private void IrcMode(string[] ircCommand) {
			string ircChannel = ircCommand[2];
			string ircUser = ircCommand[0].Split('!')[0];
			string userMode = "";
            userMode = RejoinMessage(ircCommand, 3);
			if (userMode.Substring(0, 1) == ":") {
				userMode = userMode.Remove(0, 1);
			}
			if (eventMode != null) { this.eventMode(ircChannel, ircUser, userMode.Trim()); }
		}
		
		private void IrcNickChange(string[] ircCommand) {
			string userOldNick = ircCommand[0].Split('!')[0];
			string userNewNick = ircCommand[2].Remove(0, 1);
			if (eventNickChange != null) { this.eventNickChange(userOldNick, userNewNick); }
		}
		
		private void IrcKick(string[] ircCommand) {
			string userKicker = ircCommand[0].Split('!')[0];
			string userKicked = ircCommand[3];
			string ircChannel = ircCommand[2];
			string kickMessage = "";

            kickMessage = RejoinMessage(ircCommand, 4);
			if (eventKick != null) { this.eventKick(ircChannel, userKicker, userKicked, kickMessage.Remove(0, 1).Trim()); }
		} /* IrcKick */
		
		private void IrcQuit(string[] ircCommand) {
			string userQuit = ircCommand[0].Split('!')[0];
			string quitMessage = "";

            quitMessage = RejoinMessage(ircCommand, 2);
			if (eventQuit != null) { this.eventQuit(userQuit, quitMessage.Remove(0, 1).Trim()); }
		} /* IrcQuit */

        void IrcPrivateMessage(string[] ircCommand)
        {
            string userSender = ircCommand[0].Split('!')[0];
            string messageTo = ircCommand[2];
            string message = "";
            message = RejoinMessage(ircCommand, 3);
            message = message.Remove(0, 1).Trim();
            if (messageTo == this.IrcChannel) {
                if (eventChannelMessage != null) { this.eventChannelMessage(userSender, message); }
            }
            else if (messageTo == this.IrcNick) {
                if (eventPrivateMessage != null) { this.eventPrivateMessage(userSender, message); }
            }         

        }

        void IrcNotice(string[] ircCommand)
        {
            string UserSender = ircCommand[0].Split('!')[0];
            string message = "";
            message = RejoinMessage(ircCommand, 3);
            message = message.Remove(0, 1).Trim();
            if (eventNotice != null) { this.eventNotice(UserSender, message); }
        }

		#endregion 

        #region Utility Methods

        string RejoinMessage(string[] messageParts, int startPos) {
            return String.Join(" ", messageParts, startPos, messageParts.Length - startPos);
        }

        #endregion
        #endregion
    }
}