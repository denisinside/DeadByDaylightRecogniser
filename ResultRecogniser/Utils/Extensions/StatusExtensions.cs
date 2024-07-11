using DeadByDaylightRecogniser.Utils.Enums;
using ResultRecogniser.Utils.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser.Utils.Extensions
{
    internal static class StatusExtensions
    {
        public static string ToFriendlyString(this Status role)
        {
            return role switch
            {
                Status.Disconnected => "Disconnected",
                Status.Sacrificed => "Sacrificed",
                Status.Moried => "Moried",
                Status.Escaped => "Escaped",
                Status.Killer => "Killer",
                Status.Playing => "Playing",
                _ => throw new NotImplementedException()
            };
        }

        public static Status FromFriendlyString(string role)
        {
            return role switch
            {
                "Disconnected" => Status.Disconnected,
                "Sacrificed" => Status.Sacrificed,
                "Moried" => Status.Moried,
                "Escaped" => Status.Escaped,
                "Playing" => Status.Playing,
                _ => Status.Killer,
            };
        }
    }
}
