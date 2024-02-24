using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SourceRconLib.Helpers.MessageHelper;

namespace SourceRconLib.Models
{
    internal class MessageStates
    {
        public event StringOutput ServerOutput { get; set; }
        public event StringOutput _errors;
        public event BoolInfo _connectionSuccess;
    }
}
