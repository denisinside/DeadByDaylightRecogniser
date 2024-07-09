using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser.Models
{
    internal struct DBDElement
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string? Parent { get; set; }
        public byte[] Descriptors { get; set; }
    }

}
