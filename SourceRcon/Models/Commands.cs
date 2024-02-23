using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SourceRcon.Models
{
    public class Commands
    {
        public string IpAddress { get; set; }
        public string Password { get; set; }
        public string Command { get; set; }
        public int Port { get; set; }
        public bool Interactive { get; set; }
    }
}
