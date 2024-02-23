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

#if DEBUG
			TempPackets = new ArrayList();
#endif
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

			RCONPacket SA = new RCONPacket();
			SA.RequestId = 1;
			SA.String1 = password;
			SA.ServerDataSent = RCONPacket.SERVERDATA_sent.SERVERDATA_AUTH;

			SendRCONPacket(SA);
			
			// This is the first time we've sent, so we can start listening now!
			StartGetNewPacket();

			return true;
		}

		public void ServerCommand(string command)
		{
			if(connected)
			{
				RCONPacket PacketToSend = new RCONPacket();
				PacketToSend.RequestId = 2;
				PacketToSend.ServerDataSent = RCONPacket.SERVERDATA_sent.SERVERDATA_EXECCOMMAND;
				PacketToSend.String1 = command;
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

		private async void SendCallback(IAsyncResult result)
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
#if DEBUG
			TempPackets.Add(state);
#endif
			Sock.BeginReceive(state.Data,0,4,SocketFlags.None,new AsyncCallback(ReceiveCallback),state);
		}

#if DEBUG
		public ArrayList TempPackets;
#endif

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

#if DEBUG
				Console.WriteLine("Receive Callback. Packet: {0} First packet: {1}, Bytes so far: {2}",state.PacketCount,state.IsPacketLength,state.BytesSoFar);
#endif

			}
			catch(SocketException)
			{
				OnError(ConnectionClosed);
			}

			if(requestSuccess)
			ProcessIncomingData(state);
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

					RCONPacket RetPack = new RCONPacket();
					RetPack.ParseFromBytes(state.Data,this);

					ProcessResponse(RetPack);

					// Wait for new packet.
					StartGetNewPacket();
				}
			}
		}

		void ProcessResponse(RCONPacket P)
		{
			switch(P.ServerDataReceived)
			{
				case RCONPacket.SERVERDATA_rec.SERVERDATA_AUTH_RESPONSE:
					if(P.RequestId != -1)
					{
						// Connected.
						connected = true;
						OnError(ConnectionSuccessString);
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
					if(hadjunkpacket)
					{
						// Real packet!
						OnServerOutput(P.String1);
					}
					else
					{
						hadjunkpacket = true;
						OnError(GotJunkPacket);
					}
					break;
				default:
					OnError(UnknownResponseType);
					break;
			}
		}

		bool hadjunkpacket;

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

		public enum SERVERDATA_sent : int
		{
			SERVERDATA_AUTH = 3,
			SERVERDATA_EXECCOMMAND = 2,
			None = 255
		}

		public enum SERVERDATA_rec : int
		{
			SERVERDATA_RESPONSE_VALUE = 0,
			SERVERDATA_AUTH_RESPONSE = 2,
			None = 255
		}

		public int RequestId {get; set;}
		public string String1 { get; set;}
		public string String2 { get; set; }
		public RCONPacket.SERVERDATA_sent ServerDataSent { get; set;}
		public RCONPacket.SERVERDATA_rec ServerDataReceived { get; set;}
	}
}
