using System.Net.Http.Json;
using TourGuideHCM.Saler.Models;

namespace TourGuideHCM.Saler.Services;

public class AudioService
{
    private readonly HttpClient _http;

    public AudioService(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Lấy danh sách audio của POI cụ thể.</summary>
    public async Task<List<AudioDto>> GetByPoiAsync(int poiId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AudioDto>>($"api/audio/poi/{poiId}") ?? new();
        }
        catch { return new(); }
    }

    /// <summary>Lấy tất cả audio của saler hiện tại.</summary>
    public async Task<List<AudioDto>> GetMineAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AudioDto>>("api/audio/mine") ?? new();
        }
        catch { return new(); }
    }

    /// <summary>Tạo audio bằng cách chuyển text → MP3 qua Google TTS.</summary>
    public async Task<(bool ok, string message, string? audioUrl)> ConvertTtsAsync(TtsConvertRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/audio/convert", req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err, null);
            }

            var body = await res.Content.ReadFromJsonAsync<ConvertResponse>();
            return (true, body?.Message ?? "Đã tạo audio", body?.AudioUrl);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    /// <summary>Upload file MP3 đã có sẵn.</summary>
    public async Task<(bool ok, string message, string? audioUrl)> UploadFileAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
            content.Add(streamContent, "file", fileName);

            var res = await _http.PostAsync("api/audio/upload", content);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err, null);
            }

            var audioUrl = await res.Content.ReadAsStringAsync();
            audioUrl = audioUrl.Trim('"');
            return (true, "Upload thành công", audioUrl);
        }
        catch (Exception ex) { return (false, ex.Message, null); }
    }

    public async Task<(bool ok, string message)> CreateAsync(AudioDto dto)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/audio", dto);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err);
            }
            return (true, "Đã lưu audio");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool ok, string message)> DeleteAsync(int id)
    {
        try
        {
            var res = await _http.DeleteAsync($"api/audio/{id}");
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                return (false, err);
            }
            return (true, "Đã xoá audio");
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    private class ConvertResponse
    {
        public int Id { get; set; }
        public string? AudioUrl { get; set; }
        public int Duration { get; set; }
        public string? Message { get; set; }
    }
}
