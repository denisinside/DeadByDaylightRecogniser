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
                files = files.Concat(Directory.EnumerateFiles(additionalDataPath, "*.png", SearchOption.AllDirectories));
            }

            foreach (var filePath in files)
            {
                if (filePath.EndsWith("empty.png") || filePath.EndsWith("Missing.png"))
                    continue;


                string name = Path.GetFileNameWithoutExtension(filePath);
                if(name.Contains("anniversary"))
                    name = name[(name.IndexOf('_') + 1)..];
                else
                    name = name[(name.LastIndexOf('_') + 1)..];

                string id = null;
                var role = Role.Unknown;
                string? parent = null;
                string? itemType = null;

                foreach (var element in data)
                {
                    if (element.Value.TryGetProperty("image", out JsonElement property)
                        && property.GetString() != null
                        && property.GetString().ToLower().EndsWith($"{name}.png".ToLower()))
                    {
                        if(!(element.Value.TryGetProperty("type", out JsonElement type)
                           && type.GetString().Equals("none")))
                        {
                            if (type.ValueKind != JsonValueKind.Undefined 
                                && (type.GetString().Equals("item") || type.GetString().Equals("itemaddon")))
                            {
                                if (element.Value.TryGetProperty("item_type", out JsonElement i)
                                    && i.GetString() != null
                                    && !i.GetString().Equals("firecracker"))
                                    itemType = i.GetString();
                                else
                                    break;
                            }

                            if (type.ValueKind != JsonValueKind.Undefined
                                && type.GetString().Equals("power"))
                                role = Role.Killer;
                            else
                                role = RoleExtensions.FromFriendlyString(element.Value.GetProperty("role").GetString());
                            name = element.Value.GetProperty("name").GetString();
                            if (name.Contains("Anniversary"))
                                break;
                            id = element.Key;
                            if (element.Value.TryGetProperty("parents", out JsonElement p)
                                && p.GetArrayLength() != 0)
                                parent = p[0].GetString();
                            break;
                        }    
                        else
                            break;
                    }
                }
                if (id != null)
                {
                    var element = new DBDElement
                    {
                        ID = id,
                        Name = name,
                        Role = role,
                        Parent = parent,
                        ItemType = itemType,
                        Descriptors = GetDescriptors(filePath)
                    };
                    elements.Add(element);
                }
            }
            string json = JsonSerializer.Serialize(elements);
            File.WriteAllText(jsonPath, json);
            return elements;
        }

        public static List<DBDElement> GetStatuses()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string imgDirectory = Path.Combine(baseDirectory, @"..\..\..\img\");

            var files = Directory.EnumerateFiles(imgDirectory, "*.png", SearchOption.AllDirectories);
            List<DBDElement> statuses = new List<DBDElement>();
            foreach (var file in files)
            {
                var element = new DBDElement()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Role = Role.Unknown,
                    Descriptors = GetDescriptors(file)
                };
                statuses.Add(element);
            }
            return statuses;
        }

        private static byte[] GetDescriptors(string filePath)
        {
            using var t = new ResourcesTracker();
            var orb = ORB.Create();
            Mat img = t.T(ResultProcessing.ReadPNG(filePath));
            Cv2.CvtColor(img, img, ColorConversionCodes.BGR2GRAY);
            Cv2.Resize(img, img, new Size(ResultProcessing.ElementResizeSize, ResultProcessing.ElementResizeSize));
            //Cv2.ImShow(filePath + "1 ", img);
            //Cv2.Threshold(img, img, 128, 300, ThresholdTypes.Otsu);
            //Cv2.ImShow(filePath, img);
            //Cv2.WaitKey();
            Mat descriptors = t.NewMat();
            orb.DetectAndCompute(img, null, out _, descriptors);
            return descriptors.ToBytes();
        }
    }

}
