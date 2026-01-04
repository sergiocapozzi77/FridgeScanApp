using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace FridgeScan.Services
{
    internal class PexelService
    {
        private HttpClient http;

        public PexelService()
        {
            http = new HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", "EZsji98K66WnOjzlif5fImG9qnhtaJnwLjC3GppJbYKJdlOUNF7EgyY6");

        }

        public async Task<string?> GetFoodImageAsync(string query)
        {
            var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page=1";

            var result = await http.GetFromJsonAsync<PexelsSearchResponse>(url);

            if (result?.Photos?.Any() != true)
                return null;

            return result.Photos.First().Src.Original;
        }

    }
}
