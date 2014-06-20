using System;
using System.Net;

namespace cIRC {
	class cIRC {
		private IRC IrcObject;
		
		static void Main(string[] args) {
			Console.Write("Server: ");
			string IrcServer = Console.ReadLine();
			Console.Write("Port: ");
			int IrcPort = Convert.ToInt32(Console.ReadLine());
			Console.Write("User: ");
			string IrcUser = Console.ReadLine();
			Console.Write("Chan: ");
			string IrcChan = Console.ReadLine();
			
			cIRC IrcApp = new cIRC(IrcServer, IrcPort, IrcUser, IrcChan);
		} /* Main */
		
		private cIRC(string IrcServer, int IrcPort, string IrcUser, string IrcChan) {
			//IrcObject = new IRC("CumpsD", "#mypreciousss");
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
			
			// Connect to server
			//IrcObject.Connect("efnet.xs4all.nl", 6667);	
			IrcObject.Connect(IrcServer, IrcPort);	
		} /* cIRC */
		
		private void IrcCommandReceived(string IrcCommand) {
			//Console.WriteLine(IrcCommand);
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
			//Console.WriteLine(String.Format("Server Message: {0}", ServerMessage));
		} /* IrcNamesList */
		
		private void IrcJoin(string IrcChan, string IrcUser) {
			Console.WriteLine(String.Format("{0} joins {1}", IrcUser, IrcChan));
			IrcObject.IrcWriter.WriteLine(String.Format("NOTICE {0} :Hello {0}, welcome to {1}!", IrcUser, IrcChan));
			IrcObject.IrcWriter.Flush ();	
		} /* IrcJoin */
		
		private void IrcPart(string IrcChan, string IrcUser) {
			Console.WriteLine(String.Format("{0} parts {1}", IrcUser, IrcChan));
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
			Console.WriteLine(String.Format("{0} has quit IRC ({1})", UserQuit, QuitMessage));
		} /* IrcQuit */
	} /* cIRC */
} /* cIRC */