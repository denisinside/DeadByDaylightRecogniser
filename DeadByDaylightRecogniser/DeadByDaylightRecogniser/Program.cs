using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var rp = new ResultProcessing("img\\example.png");
            rp.Process();
        }
    }
}
