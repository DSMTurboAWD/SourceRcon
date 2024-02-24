using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using SourceRconLib.Models;
using Microsoft.VisualBasic;
using static SourceRconLib.Models.RconPacket;
using SourceRconLib.Helpers;
using static SourceRconLib.Helpers.MessageHelper;

namespace SourceRcon
{
	/// <summary>
	/// Summary description for SourceRcon.
	/// </summary>
	public class SourceRcon
	{
        public SourceRcon(StringOutput serverOutput, StringOutput errors, BoolInfo connectionSuccess)
		{
			Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			PacketCount = 0;
			Connected = false;
			_serverOutput= serverOutput;
			_errors = errors;
			_connectionSuccess = connectionSuccess;

		}

        public event StringOutput _serverOutput;
        public event StringOutput _errors;
        public event BoolInfo _connectionSuccess;
		public int PacketCount { get; set; }
		public Socket Sock { get; set; }
		public bool Connected { get; set; }

        public bool Connect(IPEndPoint Server, string password)
		{
			try
			{
				Sock.Connect(Server);
			}
			catch(SocketException)
			{
				OnError(ConnectionFailedString, _serverOutput);
				OnConnectionSuccess(false, _connectionSuccess);
				return false;
			}

			var packet = new RconPacket();
			packet.RequestId = 1;
			packet.String1 = password;
			packet.ServerDataSent = SERVERDATA_sent.SERVERDATA_AUTH;

			SendRCONPacket(packet);
			
			// This is the first time we've sent, so we can start listening now!
			StartGetNewPacket();

			return true;
		}

		public void ServerCommand(string command, string command2 = null)
		{
			if (Connected)
			{
				var PacketToSend = new RconPacket
				{
					RequestId = 2,
					ServerDataSent = SERVERDATA_sent.SERVERDATA_EXECCOMMAND,
					String1 = command
				};

				SendRCONPacket(PacketToSend);
			}
		}
	
		void SendRCONPacket(RconPacket packet)
		{
			var Packet = EncodingHelper.OutputAsBytes(packet);
			Sock.BeginSend(Packet,0,Packet.Length,SocketFlags.None,new AsyncCallback(SendCallback),this);
		}

		private void SendCallback(IAsyncResult result)
		{
			Sock.EndSend(result);
		}

		void StartGetNewPacket()
		{
			var state = new RequestState
			{
                IsPacketLength = true,
				Data = new byte[4],
				PacketCount = PacketCount
			};
		
			PacketCount++;
			Sock.BeginReceive(state.Data,0,4,SocketFlags.None,new AsyncCallback(ReceiveCallback),state);
		}

		void ReceiveCallback(IAsyncResult result)
		{
			bool requestSuccess = false;
			RequestState state = null;

			try
			{
				int	bytesgotten = Sock.EndReceive(result);
				state = (RequestState)result.AsyncState;
				state.BytesSoFar += bytesgotten;
				requestSuccess = true;

				DisplayPacketMessage(state);

			}
			catch(SocketException)
			{
				OnError(ConnectionClosed, _errors);
			}

			if (requestSuccess)
			{
				if (state.BytesSoFar > 0) { ProcessIncomingData(state); }
				return;
			}
		}

		void ProcessIncomingData(RequestState state)
		{
			if(state.IsPacketLength)
			{
				// First 4 bytes of a new packet.
				state.PacketLength = BitConverter.ToInt32(state.Data,0);

				state.IsPacketLength = false;
				state.BytesSoFar = 0;
				state.Data = new byte[state.PacketLength];
				Sock.BeginReceive(state.Data,0,state.PacketLength,SocketFlags.None,new AsyncCallback(ReceiveCallback),state);
			}
			else
			{
				// Do something with data...
				if(state.BytesSoFar < state.PacketLength)
				{
					// Missing data.
					Sock.BeginReceive(state.Data,state.BytesSoFar,state.PacketLength - state.BytesSoFar,SocketFlags.None,new AsyncCallback(ReceiveCallback),state);
				}
				else
				{
					// Process data.
					if (state.Data.Length < 1) return;

					var RetPack = new RconPacket();
					EncodingHelper.ParseFromBytes(state.Data,RetPack);

					ProcessResponse(RetPack);

					// Wait for new packet.
					StartGetNewPacket();
				}
			}
		}

		void ProcessResponse(RconPacket packet)
		{
			switch(packet.ServerDataReceived)
			{
				case SERVERDATA_rec.SERVERDATA_AUTH_RESPONSE:
					if(packet.RequestId != -1)
					{
						// Connected.
						Connected = true;
						Console.WriteLine();
						Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(ConnectionSuccessString);
						Console.ResetColor();
                        Console.WriteLine("========================= Welcome ========================");
						Console.WriteLine();						
						OnConnectionSuccess(true, _connectionSuccess);
					}
					else
					{
						// Failed!
						OnError(ConnectionFailedString, _errors);
						OnConnectionSuccess(false, _connectionSuccess);
					}
					break;
				case SERVERDATA_rec.SERVERDATA_RESPONSE_VALUE:
					if (!string.IsNullOrEmpty(packet.String1))
					{
						// Real packet!
						OnServerOutput(packet.String1, _serverOutput);
						if (!string.IsNullOrEmpty(packet.String2)) { OnServerOutput(packet.String2, _serverOutput); }
					}
					else
					{
						OnError(GotJunkPacket, _errors);
					}
					break;
				default:
					OnError(UnknownResponseType, _errors);
					break;
			}
		}

	}
}
