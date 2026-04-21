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

        /// <summary>
        /// Trả về bool đơn giản để tương thích ngược.
        /// Muốn lấy thông tin duplicate thì gọi CreateWithResult.
        /// </summary>
        public async Task<bool> Create(PoiDto poi)
        {
            var result = await CreateWithResult(poi);
            return result.Success;
        }

        public async Task<bool> Update(PoiDto poi)
        {
            var result = await UpdateWithResult(poi);
            return result.Success;
        }

        /// <summary>
        /// MỚI: Create POI và trả về thông tin duplicate nếu có.
        /// Dùng để PoiDialog hiện cảnh báo "POI này có vẻ trùng với X".
        /// </summary>
        public async Task<PoiSaveResult> CreateWithResult(PoiDto poi)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/poi", poi);
                if (!res.IsSuccessStatusCode)
                    return new PoiSaveResult { Success = false };

                var body = await res.Content.ReadFromJsonAsync<PoiCreateResponse>();
                return new PoiSaveResult
                {
                    Success = true,
                    HasDuplicateWarning = body?.HasDuplicateWarning ?? false,
                    Duplicates = body?.Duplicates ?? new()
                };
            }
            catch
            {
                return new PoiSaveResult { Success = false };
            }
        }

        public async Task<PoiSaveResult> UpdateWithResult(PoiDto poi)
        {
            try
            {
                var res = await _http.PutAsJsonAsync($"api/poi/{poi.Id}", poi);
                if (!res.IsSuccessStatusCode)
                    return new PoiSaveResult { Success = false };

                var body = await res.Content.ReadFromJsonAsync<PoiCreateResponse>();
                return new PoiSaveResult
                {
                    Success = true,
                    HasDuplicateWarning = body?.HasDuplicateWarning ?? false,
                    Duplicates = body?.Duplicates ?? new()
                };
            }
            catch
            {
                return new PoiSaveResult { Success = false };
            }
        }

        public async Task<bool> Delete(int id)
        {
            var res = await _http.DeleteAsync($"api/poi/{id}");
            return res.IsSuccessStatusCode;
        }

        // ====================== DTOs ======================
        public class PoiCreateResponse
        {
            public object? Poi { get; set; }
            public bool HasDuplicateWarning { get; set; }
            public List<DuplicateInfo> Duplicates { get; set; } = new();
        }

        public class DuplicateInfo
        {
            public int ExistingId { get; set; }
            public string ExistingName { get; set; } = "";
            public string Level { get; set; } = "";
            public double Similarity { get; set; }
            public double Distance { get; set; }
        }

        public class PoiSaveResult
        {
            public bool Success { get; set; }
            public bool HasDuplicateWarning { get; set; }
            public List<DuplicateInfo> Duplicates { get; set; } = new();
        }
    }
}
