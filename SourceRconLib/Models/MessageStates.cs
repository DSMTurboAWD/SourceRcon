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
        public event StringOutput ServerOutput;
        public event StringOutput Errors;
        public event BoolInfo ConnectionSuccess;
    }
}
