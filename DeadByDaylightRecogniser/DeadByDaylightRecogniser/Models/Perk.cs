using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser.Models
{
    internal struct Perk
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public byte[] Descriptors { get; set; }
        public Perk(string name, string role, byte[] descriptor)
        {
            Name = name;
            Role = role;
            Descriptors = descriptor;
        }
    }

}
