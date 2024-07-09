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

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            HttpResponseMessage response = await httpClient.GetAsync("https://dbd.tricky.lol/api/perks");
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            Dictionary<string, JsonElement> perkData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);

            TemplateDownloader.DownloadPerks(perkData, "perks.json");
            var rp = new ResultProcessing("img\\example2.png");
            rp.Process();
        }
    }
}
