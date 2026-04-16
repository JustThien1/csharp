using Plugin.Maui.Audio;
using System.Net.Http.Json;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class NarrationService : INarrationService, IDisposable
{
    private readonly IDatabaseService _db;
    private readonly IApiService _api;
    private readonly IAudioManager _audioManager;
    private CancellationTokenSource? _cts;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsSpeaking { get; private set; }
    public bool IsPlayingAudio { get; private set; }

    public event EventHandler? NarrationStarted;
    public event EventHandler? NarrationCompleted;

    public NarrationService(IDatabaseService db, IApiService api, IAudioManager audioManager)
    {
        _db = db;
        _api = api;
        _audioManager = audioManager;
    }

    public async Task PlayAsync(NarrationRequest request)
    {
        if (!await _lock.WaitAsync(5000)) return;

        try
        {
            await StopCoreAsync();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            NarrationStarted?.Invoke(this, EventArgs.Empty);

            var poi = request.Poi;
            // Lấy audio URL từ bảng Audios (do TTS tạo ra) thay vì POI.AudioUrl cũ
            var audioUrl = await _api.GetAudioUrlAsync(poi.Id, request.Language);
            if (string.IsNullOrEmpty(audioUrl))
            {
                // Fallback về POI.AudioUrl nếu chưa có TTS
                var rawUrl = poi.AudioUrl;
#if ANDROID
                if (!string.IsNullOrEmpty(rawUrl))
                    rawUrl = rawUrl.Replace("localhost", "10.0.2.2")
                                   .Replace("127.0.0.1", "10.0.2.2");
#endif
                audioUrl = _api.ResolveAudioUrl(rawUrl);
            }

            // Debug log
            System.Diagnostics.Debug.WriteLine($"[Narration] POI: {poi.Name}");
            System.Diagnostics.Debug.WriteLine($"[Narration] AudioUrl raw: {poi.AudioUrl}");
            System.Diagnostics.Debug.WriteLine($"[Narration] AudioUrl resolved: {audioUrl}");
            System.Diagnostics.Debug.WriteLine($"[Narration] NarrationText: {poi.NarrationText}");
            System.Diagnostics.Debug.WriteLine($"[Narration] PreferAudioFile: {request.PreferAudioFile}");

            bool audioSuccess = false;

            if (request.PreferAudioFile && !string.IsNullOrEmpty(audioUrl))
            {
                audioSuccess = await PlayAudioUrlAsync(audioUrl, token);
                System.Diagnostics.Debug.WriteLine($"[Narration] Audio result: {audioSuccess}");
            }

            // Fallback TTS nếu không có audio hoặc audio thất bại
            if (!audioSuccess)
            {
                var text = !string.IsNullOrEmpty(poi.NarrationText)
                    ? poi.NarrationText
                    : poi.Description;

                System.Diagnostics.Debug.WriteLine($"[Narration] TTS fallback, text: {text?.Substring(0, Math.Min(50, text?.Length ?? 0))}");

                if (!string.IsNullOrEmpty(text))
                    await PlayTtsAsync(text, request.Language, token);
            }

            await _db.AddPlaybackHistoryAsync(new PlaybackHistory
            {
                PoiId = poi.Id,
                PoiName = poi.Name,
                TriggerType = request.TriggerType,
                WasAudio = audioSuccess
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Narration] PlayAsync error: {ex.Message}");
        }
        finally
        {
            IsSpeaking = false;
            IsPlayingAudio = false;
            _lock.Release();
            NarrationCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Trả về true nếu phát thành công</summary>
    private async Task<bool> PlayAudioUrlAsync(string url, CancellationToken token)
    {
        IAudioPlayer? player = null;
        try
        {
            System.Diagnostics.Debug.WriteLine($"[Narration] Fetching audio: {url}");
            IsPlayingAudio = true;

            using var http = new System.Net.Http.HttpClient
            { Timeout = TimeSpan.FromSeconds(15) };
            var bytes = await Task.Run(() => http.GetByteArrayAsync(url, token).Result, token);
            var stream = new System.IO.MemoryStream(bytes);

            System.Diagnostics.Debug.WriteLine("[Narration] Stream OK, creating player...");
            player = _audioManager.CreatePlayer(stream);

            var tcs = new TaskCompletionSource<bool>();
            player.PlaybackEnded += (_, _) =>
            {
                System.Diagnostics.Debug.WriteLine("[Narration] PlaybackEnded");
                tcs.TrySetResult(true);
            };

            player.Play();
            System.Diagnostics.Debug.WriteLine("[Narration] player.Play() called");

            using var reg = token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
            await Task.Delay(100);

            return true;
        }
        catch (OperationCanceledException)
        {
            player?.Stop();
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Narration] Audio error: {ex.Message}");
            return false;
        }
        finally
        {
            player?.Dispose();
            IsPlayingAudio = false;
        }
    }

    private async Task PlayTtsAsync(string text, string language, CancellationToken token)
    {
        try
        {
            IsSpeaking = true;
            System.Diagnostics.Debug.WriteLine($"[Narration] TTS speaking: {language}");

            var locales = await TextToSpeech.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(language, StringComparison.OrdinalIgnoreCase));

            System.Diagnostics.Debug.WriteLine($"[Narration] TTS locale: {locale?.Language ?? "null"}");

            await TextToSpeech.Default.SpeakAsync(text,
                new SpeechOptions { Locale = locale, Volume = 1f, Pitch = 1f },
                token);

            System.Diagnostics.Debug.WriteLine("[Narration] TTS done");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Narration] TTS error: {ex.Message}");
        }
        finally { IsSpeaking = false; }
    }

    public async Task StopAsync() => await StopCoreAsync();

    private async Task StopCoreAsync()
    {
        _cts?.Cancel();
        await Task.Delay(80);
        IsSpeaking = IsPlayingAudio = false;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _lock.Dispose();
    }
}