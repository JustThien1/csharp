using System.Net.Http.Json;
using TourGuideHCM.Admin.Models;

namespace TourGuideHCM.Admin.Services
{
    public class UserService
    {
        private readonly HttpClient _http;

        public UserService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<UserDto>> GetAllAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<UserDto>>("api/users")
                       ?? new List<UserDto>();
            }
            catch
            {
                return new List<UserDto>();
            }
        }

        public async Task<bool> CreateAsync(UserDto user)
        {
            var res = await _http.PostAsJsonAsync("api/users", user);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateAsync(UserDto user)
        {
            var res = await _http.PutAsJsonAsync($"api/users/{user.Id}", user);
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var res = await _http.DeleteAsync($"api/users/{id}");
            return res.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var res = await _http.PutAsync($"api/users/{id}/toggle-active", null);
            return res.IsSuccessStatusCode;
        }
    }
}