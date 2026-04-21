using System.Net.Http.Json;
using TourGuideHCM.Saler.Models;

namespace TourGuideHCM.Saler.Services;

public class PoiService
{
    private readonly HttpClient _http;

    public PoiService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Lấy danh sách POI của saler hiện tại (backend filter theo JWT).</summary>
    public async Task<List<PoiDto>> GetMyPoisAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PoiDto>>("api/poi/mine") ?? new();
        }
        catch { return new(); }
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CategoryDto>>("api/category") ?? new();
        }
        catch { return new(); }
    }

    public async Task<(bool ok, string message, PoiDto? poi)> CreateAsync(PoiDto dto)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/poi", dto);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err, null);
            }

            // API trả về { poi, hasDuplicateWarning, duplicates }
            var body = await res.Content.ReadFromJsonAsync<CreatePoiResponse>();
            return (true, "Đã tạo POI thành công", body?.Poi);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool ok, string message)> UpdateAsync(PoiDto dto)
    {
        try
        {
            var res = await _http.PutAsJsonAsync($"api/poi/{dto.Id}", dto);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err);
            }
            return (true, "Đã cập nhật POI");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string message)> DeleteAsync(int id)
    {
        try
        {
            var res = await _http.DeleteAsync($"api/poi/{id}");
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err);
            }
            return (true, "Đã xoá POI");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private class CreatePoiResponse
    {
        public PoiDto? Poi { get; set; }
        public bool HasDuplicateWarning { get; set; }
    }
}
