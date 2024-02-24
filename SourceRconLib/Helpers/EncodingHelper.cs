using SourceRconLib.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SourceRconLib.Models.RconPacket;

namespace SourceRconLib.Helpers
{
    public class EncodingHelper
    {
        public static byte[] OutputAsBytes(RconPacket packet)
        {

            var utf = new UTF8Encoding();

            var byteString1 = utf.GetBytes(packet.String1);
            var byteString2 = utf.GetBytes(packet.String2);

            var serverdata = BitConverter.GetBytes((int)packet.ServerDataSent);
            var reqid = BitConverter.GetBytes(packet.RequestId);

            // Compose into one packet.
            var FinalPacket = new byte[4 + 4 + 4 + byteString1.Length + 1 + byteString2.Length + 1];
            var packetsize = BitConverter.GetBytes(FinalPacket.Length - 4) ?? Array.Empty<byte>();

            var bytePointer = 0;
            packetsize.CopyTo(FinalPacket, bytePointer);
            bytePointer += 4;

            reqid.CopyTo(FinalPacket, bytePointer);
            bytePointer += 4;

            serverdata.CopyTo(FinalPacket, bytePointer);
            bytePointer += 4;

            byteString1.CopyTo(FinalPacket, bytePointer);
            bytePointer += byteString1.Length;

            FinalPacket[bytePointer] = (byte)0;
            bytePointer++;

            byteString2.CopyTo(FinalPacket, bytePointer);
            bytePointer += byteString2.Length;

            FinalPacket[bytePointer] = (byte)0;
            bytePointer++;

            return FinalPacket;
        }

        public static void ParseFromBytes(byte[] inputBytes, RconPacket packet)
        {
            var bytePointer = 0;

            var utf = new UTF8Encoding();

            // First 4 bytes are ReqId.
            packet.RequestId = BitConverter.ToInt32(inputBytes, bytePointer);
            bytePointer += 4;
            // Next 4 are server data.
            packet.ServerDataReceived = (SERVERDATA_rec)BitConverter.ToInt32(inputBytes, bytePointer);
            bytePointer += 4;
            // string1 till /0
            var stringcache = new ArrayList();
            while (inputBytes[bytePointer] != 0)
            {
                stringcache.Add(inputBytes[bytePointer]);
                bytePointer++;
            }
            packet.String1 = utf.GetString((byte[])stringcache.ToArray(typeof(byte)));
            bytePointer++;

            // string2 till /0

            stringcache = new ArrayList();
            while (inputBytes[bytePointer] != 0)
            {
                stringcache.Add(inputBytes[bytePointer]);
                bytePointer++;
            }
            packet.String2 = utf.GetString((byte[])stringcache.ToArray(typeof(byte)));
            bytePointer++;

            // Repeat if there's more data?
            if (bytePointer != inputBytes.Length)
            {
                MessageHelper.OnError("Urk, extra data!");
            }
        }
    }
}
