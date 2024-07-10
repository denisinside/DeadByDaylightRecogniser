using DeadByDaylightRecogniser.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser.Utils.Extensions
{
    internal static class RoleExtensions
    {
        public static string ToFriendlyString(this Role role)
        {
            return role switch
            {
                Role.Survivor => "survivor",
                Role.Killer => "killer",
                Role.Unknown => "null",
                _ => throw new NotImplementedException()
            };
        }

        public static Role FromFriendlyString(string role)
        {
            return role switch
            {
                "survivor" => Role.Survivor,
                "killer" => Role.Killer,
                _ => Role.Unknown
            };
        }
    }
}
