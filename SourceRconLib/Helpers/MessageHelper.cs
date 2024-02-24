using SourceRcon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceRconLib.Helpers
{
    public class MessageHelper
    {


        public static string ConnectionClosed = "Connection closed by remote host";
        public static string ConnectionSuccessString = "Connection Succeeded!";
        public static string ConnectionFailedString = "Connection Failed!";
        public static string UnknownResponseType = "Unknown response";
        public static string GotJunkPacket = "Had junk packet. This is normal.";

        public delegate void StringOutput(string output);
        public delegate void BoolInfo(bool info);

        internal static void OnServerOutput(string output, StringOutput ServerOutput)
        {
            if (ServerOutput != null)
            {
                ServerOutput(output);
            }
        }

        internal static void OnError(string error, StringOutput Errors)
        {
            if (Errors != null)
            {
                Errors(error);
            }
        }

        internal static void OnConnectionSuccess(bool info, BoolInfo ConnectionSuccess)
        {
            if (ConnectionSuccess != null)
            {
                ConnectionSuccess(info);
            }
        }
    }
}
