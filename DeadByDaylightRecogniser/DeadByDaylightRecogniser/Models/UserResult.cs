using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser.Models
{
    internal struct UserResult
    {
        public int Prestige { get; set; }
        public string Status { get; set; }
        public string Character { get; set; }
        public int Score { get; set; }
        public string Perk1 { get; set; }
        public string Perk2 { get; set; }
        public string Perk3 { get; set; }
        public string Perk4 { get; set; }
        public string Offering { get; set; }
        public Item Item { get; set; }

        public UserResult(int prestige, string status, string character, int score, string perk1, string perk2, string perk3, string perk4, string offering, Item item)
        {
            Prestige = prestige;
            Status = status;
            Character = character;
            Score = score;
            Perk1 = perk1;
            Perk2 = perk2;
            Perk3 = perk3;
            Perk4 = perk4;
            Offering = offering;
            Item = item;
        }
    }

}
