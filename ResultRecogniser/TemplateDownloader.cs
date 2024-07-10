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
using DeadByDaylightRecogniser.Utils.Extensions;
using DeadByDaylightRecogniser.Utils.Enums;
using System.Xml.Linq;

namespace DeadByDaylightRecogniser
{
    internal class TemplateDownloader
    {

        public static List<DBDElement> DownloadTemplates(string jsonPath, Dictionary<string, JsonElement> data, string dataPath, string additionalDataPath = null)
        {
            var elements = new List<DBDElement>();

            var files = Directory.EnumerateFiles(dataPath, "*.png", SearchOption.AllDirectories);
            if (additionalDataPath != null)
            {
                files.Concat(Directory.EnumerateFiles(additionalDataPath, "*.png", SearchOption.AllDirectories));
            }

            foreach (var filePath in files)
            {
                if (filePath.EndsWith("empty.png") || filePath.EndsWith("Missing.png"))
                    continue;


                string name = Path.GetFileNameWithoutExtension(filePath);
                name = name.Substring(name.LastIndexOf("_") + 1);

                Role role = Role.Unknown;
                foreach (var element in data)
                {
                    JsonElement property;
                    if (element.Value.TryGetProperty("image", out property) 
                        && property.GetString() != null
                        && property.GetString().ToLower().EndsWith($"{name}.png".ToLower()))
                    {
                        role = RoleExtensions.FromFriendlyString(element.Value.GetProperty("role").GetString());
                        name = element.Value.GetProperty("name").GetString();
                        break;
                    }
                }
                if (role != null)
                {
                    DBDElement perk = new DBDElement
                    {
                        Name = name,
                        Descriptors = GetDescriptors(filePath),
                        Role = role
                    };
                    elements.Add(perk);
                }
            }
            string json = JsonSerializer.Serialize(elements);
            File.WriteAllText(jsonPath, json);
            return elements;
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
