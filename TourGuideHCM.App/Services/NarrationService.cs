using Plugin.Maui.Audio;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

// KHÔNG dùng "using Android.Media" – gây lỗi cross-platform

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
        if (!await _lock.WaitAsync(300)) return;

        try
        {
            await StopCoreAsync();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            NarrationStarted?.Invoke(this, EventArgs.Empty);

            var poi = request.Poi;
            var audioUrl = _api.ResolveAudioUrl(poi.AudioUrl);

            if (request.PreferAudioFile && !string.IsNullOrEmpty(audioUrl))
            {
                await PlayAudioUrlAsync(audioUrl, token);

                // Fallback TTS nếu audio thất bại
                if (!IsPlayingAudio && !string.IsNullOrEmpty(poi.NarrationText))
                    await PlayTtsAsync(poi.NarrationText, request.Language, token);
            }
            else
            {
                var text = poi.NarrationText ?? poi.Description;
                if (!string.IsNullOrEmpty(text))
                    await PlayTtsAsync(text, request.Language, token);
            }

            await _db.AddPlaybackHistoryAsync(new PlaybackHistory
            {
                PoiId = poi.Id,
                PoiName = poi.Name,
                TriggerType = request.TriggerType,
                WasAudio = IsPlayingAudio
            });
        }
        finally
        {
            IsSpeaking = false;
            IsPlayingAudio = false;
            _lock.Release();
            NarrationCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private async Task PlayAudioUrlAsync(string url, CancellationToken token)
    {
        IAudioPlayer? player = null;
        try
        {
            IsPlayingAudio = true;
            using var http = new System.Net.Http.HttpClient
            { Timeout = TimeSpan.FromSeconds(15) };
            var stream = await http.GetStreamAsync(url, token);

            player = _audioManager.CreatePlayer(stream);

            // Plugin.Maui.Audio: dùng PlaybackEnded event thay vì IsPlaying loop
            var tcs = new TaskCompletionSource<bool>();
            player.PlaybackEnded += (_, _) => tcs.TrySetResult(true);

            player.Play();

            using var reg = token.Register(() => tcs.TrySetCanceled());
            await tcs.Task;
        }
        catch (OperationCanceledException) { player?.Stop(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Narration] Audio: {ex.Message}");
            IsPlayingAudio = false;
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
            var locales = await TextToSpeech.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(language, StringComparison.OrdinalIgnoreCase));

            await TextToSpeech.Default.SpeakAsync(text,
                new SpeechOptions { Locale = locale, Volume = 1f, Pitch = 1f },
                token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Narration] TTS: {ex.Message}");
        }
        finally { IsSpeaking = false; }
    }

    public async Task StopAsync() => await StopCoreAsync();

    private async Task StopCoreAsync()
    {
        _cts?.Cancel();
        // TextToSpeech.Default.CancelAll() không tồn tại – dùng SpeakAsync với token cancel
        await Task.Delay(80);
        IsSpeaking = IsPlayingAudio = false;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _lock.Dispose();
    }
}
