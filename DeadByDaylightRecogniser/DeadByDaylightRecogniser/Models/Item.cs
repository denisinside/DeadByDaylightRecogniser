using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser.Models
{
    internal struct Item
    {
        public string Name { get; set; }
        public string Addon1 { get; set; }
        public string Addon2 { get; set; }

        public Item(string name, string addon1, string addon2)
        {
            Name = name;
            Addon1 = addon1;
            Addon2 = addon2;
        }
    }
}
