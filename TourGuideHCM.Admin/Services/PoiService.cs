using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services
{
    public class PoiService
    {
        private readonly HttpClient _http;

        public PoiService(HttpClient http)
        {
            _http = http;
        }

        // ✅ GET ALL
        public async Task<List<PoiDto>> GetAll()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<PoiDto>>("api/poi")
                       ?? new List<PoiDto>();
            }
            catch
            {
                return new List<PoiDto>();
            }
        }

        // ✅ CREATE
        public async Task<bool> Create(PoiDto poi)
        {
            var res = await _http.PostAsJsonAsync("api/poi", poi);
            return res.IsSuccessStatusCode;
        }

        // ✅ UPDATE
        public async Task<bool> Update(PoiDto poi)
        {
            var res = await _http.PutAsJsonAsync($"api/poi/{poi.Id}", poi);
            return res.IsSuccessStatusCode;
        }

        // ✅ DELETE
        public async Task<bool> Delete(int id)
        {
            var res = await _http.DeleteAsync($"api/poi/{id}");
            return res.IsSuccessStatusCode;
        }
    }
}