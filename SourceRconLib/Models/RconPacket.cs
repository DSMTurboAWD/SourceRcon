using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceRconLib.Models
{
    internal class RconPacket
    {
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

        internal int RequestId { get; set; }
        internal string String1 { get; set; }
        internal string String2 { get; set; }
        internal SERVERDATA_sent ServerDataSent { get; set; }
        internal SERVERDATA_rec ServerDataReceived { get; set; }
    }
}
