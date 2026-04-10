using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services
{
    public class AudioService
    {
        private readonly HttpClient _http;

        public AudioService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<AudioDto>> GetAllAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AudioDto>>("api/audio")
                       ?? new List<AudioDto>();
            }
            catch
            {
                return new List<AudioDto>();
            }
        }

        public async Task<List<AudioDto>> GetByPoiIdAsync(int poiId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<AudioDto>>($"api/audio/poi/{poiId}")
                       ?? new List<AudioDto>();
            }
            catch
            {
                return new List<AudioDto>();
            }
        }

        // Upload file audio thật
        public async Task<(bool Success, string? AudioUrl)> UploadAudioAsync(MultipartFormDataContent content)
        {
            try
            {
                var response = await _http.PostAsync("api/audio/upload", content);
                if (response.IsSuccessStatusCode)
                {
                    var url = await response.Content.ReadAsStringAsync();
                    return (true, url.Trim('"'));
                }
                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }

        public async Task<bool> CreateAsync(AudioDto audio)
        {
            var res = await _http.PostAsJsonAsync("api/audio", audio);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(AudioDto audio)
        {
            var res = await _http.PutAsJsonAsync($"api/audio/{audio.Id}", audio);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var res = await _http.DeleteAsync($"api/audio/{id}");
            return res.IsSuccessStatusCode;
        }
    }
}