using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeadByDaylightRecogniser.Utils.Enums;

namespace DeadByDaylightRecogniser.Models
{
    internal struct DBDElement
    {
        public string Name { get; set; }
        public Role Role { get; set; }
        public string? Parent { get; set; }
        public byte[] Descriptors { get; set; }
    }

}
