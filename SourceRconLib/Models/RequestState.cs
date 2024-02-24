using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceRconLib.Models
{
    internal class RequestState
    {
        internal RequestState()
        {
            PacketLength = -1;
            BytesSoFar = 0;
            IsPacketLength = false;
        }

        public int PacketCount { get; set; }
        public int PacketLength { get; set; }
        public int BytesSoFar { get; set; }
        public bool IsPacketLength { get; set; }
        public byte[] Data { get; set; }
    }
}

