using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeadByDaylightRecogniser
{
    internal class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        static async Task Main(string[] args)
        {
            //await UploadData();
            var rp = new ResultProcessing("img\\example.png");
            rp.Process();
        }

        private static async Task UploadData()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            HttpResponseMessage response = await httpClient.GetAsync("https://dbd.tricky.lol/api/perks");
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            Dictionary<string, JsonElement> perkData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);

            TemplateDownloader.DownloadTemplates("perks.json", perkData,
                @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Dead by Daylight\\DeadByDaylight\\Content\\UI\\Icons\\Perks\");

            response = await httpClient.GetAsync("https://dbd.tricky.lol/api/offerings");
            response.EnsureSuccessStatusCode();
            jsonResponse = await response.Content.ReadAsStringAsync();
            Dictionary<string, JsonElement> offeringData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);

            TemplateDownloader.DownloadTemplates("offerings.json", offeringData,
                @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Dead by Daylight\\DeadByDaylight\\Content\\UI\\Icons\\Favors\");

            response = await httpClient.GetAsync("https://dbd.tricky.lol/api/items");
            response.EnsureSuccessStatusCode();
            jsonResponse = await response.Content.ReadAsStringAsync();
            Dictionary<string, JsonElement> itemsData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);


            foreach (var item in itemsData)
            {
                if (item.Value.GetProperty("type").GetString().Equals("none"))
                    itemsData.Remove(item.Key);
            }

            TemplateDownloader.DownloadTemplates("items.json", itemsData,
                @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Dead by Daylight\\DeadByDaylight\\Content\\UI\\Icons\\Items\",
                @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Dead by Daylight\\DeadByDaylight\\Content\\UI\\Icons\\Powers\");

            response = await httpClient.GetAsync("https://dbd.tricky.lol/api/addons");
            response.EnsureSuccessStatusCode();
            jsonResponse = await response.Content.ReadAsStringAsync();
            Dictionary<string, JsonElement> addonData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);

            TemplateDownloader.DownloadTemplates("addons.json", addonData,
                @"C:\\Program Files (x86)\\Steam\\steamapps\\common\\Dead by Daylight\\DeadByDaylight\\Content\\UI\\Icons\\ItemAddons\");

        }
    }
}
