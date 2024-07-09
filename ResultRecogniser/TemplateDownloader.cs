using DeadByDaylightRecogniser.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser
{
    internal class TemplateDownloader
    {

        public static List<DBDElement> DownloadPerks(Dictionary<string, JsonElement> perkData, string jsonPath)
        {
            var perks = new List<DBDElement>();

            foreach (var filePath in Directory.EnumerateFiles(@"C:\Program Files (x86)\Steam\steamapps\common\Dead by Daylight\DeadByDaylight\Content\UI\Icons\Perks", "*.png", SearchOption.AllDirectories))
            {
                if (filePath.EndsWith("empty.png") || filePath.EndsWith("Missing.png"))
                    continue;


                string name = Path.GetFileNameWithoutExtension(filePath);
                name = name.Substring(name.LastIndexOf("_") + 1);

                string role = null;
                foreach (var perkElement in perkData)
                {
                    if (perkElement.Value.GetProperty("image").GetString().ToLower().EndsWith($"{name}.png".ToLower()))
                    {
                        role = perkElement.Value.GetProperty("role").GetString();
                        name = perkElement.Value.GetProperty("name").GetString();
                        break;
                    }
                }
                if (role != null && name != null)
                {
                    DBDElement perk = new DBDElement
                    {
                        Name = name,
                        Descriptors = GetDescriptors(filePath),
                        Role = role
                    };
                    perks.Add(perk);
                }
            }
            string json = JsonSerializer.Serialize(perks);
            File.WriteAllText(jsonPath, json);
            return perks;
        }

        private static byte[] GetDescriptors(string filePath)
        {
            using(var t = new  ResourcesTracker())
            {
                var orb = ORB.Create();
                Mat img = t.T(Cv2.ImRead(filePath, ImreadModes.Grayscale));
                Cv2.Resize(img, img, new Size(ResultProcessing.ElementResizeSize, ResultProcessing.ElementResizeSize));
                Mat descriptors = t.NewMat();
                orb.DetectAndCompute(img, null, out _, descriptors);
                return descriptors.ToBytes();
            }
        }
    }

}
