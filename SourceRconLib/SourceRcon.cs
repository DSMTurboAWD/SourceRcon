using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace SourceRcon
{
	/// <summary>
	/// Summary description for SourceRcon.
	/// </summary>
	public class SourceRcon
	{
		public SourceRcon()
		{
			Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			PacketCount = 0;

		}

		public bool Connect(IPEndPoint Server, string password)
		{
			try
			{
				Sock.Connect(Server);
			}
			catch(SocketException)
			{
				OnError(ConnectionFailedString);
				OnConnectionSuccess(false);
				return false;
			}

			var packet = new RCONPacket();
			packet.RequestId = 1;
			packet.String1 = password;
			packet.ServerDataSent = RCONPacket.SERVERDATA_sent.SERVERDATA_AUTH;

			SendRCONPacket(packet);
			
			// This is the first time we've sent, so we can start listening now!
			StartGetNewPacket();

			return true;
		}

		public void ServerCommand(string command, string? command2 = null)
		{
			if (connected)
			{
				var PacketToSend = new RCONPacket
				{
					RequestId = 2,
					ServerDataSent = RCONPacket.SERVERDATA_sent.SERVERDATA_EXECCOMMAND,
					String1 = command
				};

				SendRCONPacket(PacketToSend);
			}
		}
	
		void SendRCONPacket(RCONPacket packet)
		{
			var Packet = packet.OutputAsBytes();
			Sock.BeginSend(Packet,0,Packet.Length,SocketFlags.None,new AsyncCallback(SendCallback),this);
		}

		bool connected;
		public bool Connected
		{
			get { return connected; }
		}

		private void SendCallback(IAsyncResult result)
		{
			Sock.EndSend(result);
		}

		int PacketCount;

		void StartGetNewPacket()
		{
			RequestState state = new RequestState();
			state.IsPacketLength = true;
			state.Data = new byte[4];
			state.PacketCount = PacketCount;
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

				Console.WriteLine($"Receive Callback. Packet: {state.PacketCount} First packet: {state.IsPacketLength}, Bytes so far: {state.BytesSoFar}");


			}
			catch(SocketException)
			{
				OnError(ConnectionClosed);
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
#if DEBUG
					Console.WriteLine("Complete packet.");
#endif
					if (state.Data.Length < 1) return;

					RCONPacket RetPack = new RCONPacket();
					RetPack.ParseFromBytes(state.Data,this);

					ProcessResponse(RetPack);

					// Wait for new packet.
					StartGetNewPacket();
				}
			}
		}

		void ProcessResponse(RCONPacket packet)
		{
			switch(packet.ServerDataReceived)
			{
				case RCONPacket.SERVERDATA_rec.SERVERDATA_AUTH_RESPONSE:
					if(packet.RequestId != -1)
					{
						// Connected.
						connected = true;
						Console.WriteLine();
						Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(ConnectionSuccessString);
						Console.ResetColor();
                        Console.WriteLine("========================= Welcome ========================");
						Console.WriteLine();						
						OnConnectionSuccess(true);
					}
					else
					{
						// Failed!
						OnError(ConnectionFailedString);
						OnConnectionSuccess(false);
					}
					break;
				case RCONPacket.SERVERDATA_rec.SERVERDATA_RESPONSE_VALUE:
					if (!string.IsNullOrEmpty(packet.String1))
					{
						// Real packet!
						OnServerOutput(packet.String1);
						if (!string.IsNullOrEmpty(packet.String2)) { OnServerOutput(packet.String2); }
					}
					else
					{
						OnError(GotJunkPacket);
					}
					break;
				default:
					OnError(UnknownResponseType);
					break;
			}
		}

		internal void OnServerOutput(string output)
		{
			if(ServerOutput != null)
			{
				ServerOutput(output);
			}
		}

		internal void OnError(string error)
		{
			if(Errors != null)
			{
				Errors(error);
			}
		}

		internal void OnConnectionSuccess(bool info)
		{
			if(ConnectionSuccess != null)
			{
				ConnectionSuccess(info);
			}
		}

		public event StringOutput ServerOutput;
		public event StringOutput Errors;
		public event BoolInfo ConnectionSuccess;

		public static string ConnectionClosed = "Connection closed by remote host";
		public static string ConnectionSuccessString = "Connection Succeeded!";
		public static string ConnectionFailedString = "Connection Failed!";
		public static string UnknownResponseType = "Unknown response";
		public static string GotJunkPacket = "Had junk packet. This is normal.";

		Socket Sock;
	}

	public delegate void StringOutput(string output);
	public delegate void BoolInfo(bool info);

	internal class RequestState
	{
		internal RequestState()
		{
			PacketLength = -1;
			BytesSoFar = 0;
			IsPacketLength = false;
		}

		public int PacketCount;
		public int PacketLength;
		public int BytesSoFar;
		public bool IsPacketLength;
		public byte[] Data;
	}

	

	internal class RCONPacket
	{
		internal RCONPacket()
		{
			RequestId = 0;
			String1 = "Test String";
			String2 = string.Empty;
			ServerDataSent = SERVERDATA_sent.None;
			ServerDataReceived = SERVERDATA_rec.None;
		}

		internal byte[] OutputAsBytes()
		{

            var utf = new UTF8Encoding();
			
			var byteString1 = utf.GetBytes(String1);
			var byteString2 = utf.GetBytes(String2);

			var serverdata = BitConverter.GetBytes((int)ServerDataSent);
			var reqid = BitConverter.GetBytes(RequestId);

			// Compose into one packet.
			var FinalPacket = new byte[4 + 4 + 4 + byteString1.Length + 1 + byteString2.Length + 1];
			var packetsize = BitConverter.GetBytes(FinalPacket.Length - 4) ?? Array.Empty<byte>();

			var bytePointer = 0;
			packetsize.CopyTo(FinalPacket,bytePointer);
			bytePointer += 4;

			reqid.CopyTo(FinalPacket,bytePointer);
			bytePointer += 4;

			serverdata.CopyTo(FinalPacket,bytePointer);
			bytePointer += 4;

			byteString1.CopyTo(FinalPacket,bytePointer);
			bytePointer += byteString1.Length;

			FinalPacket[bytePointer] = (byte)0;
			bytePointer++;

			byteString2.CopyTo(FinalPacket,bytePointer);
			bytePointer += byteString2.Length;

			FinalPacket[bytePointer] = (byte)0;
			bytePointer++;

			return FinalPacket;
		}

		internal void ParseFromBytes(byte[] inputBytes, SourceRcon parent)
		{
			var bytePointer = 0;

            var utf = new UTF8Encoding();

			// First 4 bytes are ReqId.
			RequestId = BitConverter.ToInt32(inputBytes,bytePointer);
			bytePointer += 4;
			// Next 4 are server data.
			ServerDataReceived = (SERVERDATA_rec)BitConverter.ToInt32(inputBytes,bytePointer);
			bytePointer += 4;
			// string1 till /0
			var stringcache = new ArrayList();
			while(inputBytes[bytePointer] != 0)
			{
				stringcache.Add(inputBytes[bytePointer]);
				bytePointer++;
			}
			String1 = utf.GetString((byte[])stringcache.ToArray(typeof(byte)));
			bytePointer++;

			// string2 till /0

			stringcache = new ArrayList();
			while(inputBytes[bytePointer] != 0)
			{
				stringcache.Add(inputBytes[bytePointer]);
				bytePointer++;
			}
			String2 = utf.GetString((byte[])stringcache.ToArray(typeof(byte)));
			bytePointer++;

			// Repeat if there's more data?
			if(bytePointer != inputBytes.Length)
			{
				parent.OnError("Urk, extra data!");
			}
		}

		internal enum SERVERDATA_sent
		{
			SERVERDATA_AUTH = 3,
			SERVERDATA_EXECCOMMAND = 2,
			None = 255
		}

		internal enum SERVERDATA_rec
		{
			SERVERDATA_RESPONSE_VALUE = 0,
			SERVERDATA_AUTH_RESPONSE = 2,
			None = 255
		}

		internal int RequestId {get; set;}
		internal string String1 { get; set;}
		internal string String2 { get; set; }
		internal RCONPacket.SERVERDATA_sent ServerDataSent { get; set;}
		internal RCONPacket.SERVERDATA_rec ServerDataReceived { get; set;}
	}
}
