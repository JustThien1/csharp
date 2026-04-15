using SQLite;
using TourGuideHCM.App.Models;
using TourGuideHCM.App.Services.Interfaces;

namespace TourGuideHCM.App.Services;

public class DatabaseService : IDatabaseService
{
    private SQLiteAsyncConnection? _db;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private static string DbPath => Path.Combine(
        FileSystem.AppDataDirectory, "tourguide_local.db3");

    public async Task InitAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_db is not null) return;
            _db = new SQLiteAsyncConnection(DbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
            await _db.CreateTableAsync<POI>();
            await _db.CreateTableAsync<User>();
            await _db.CreateTableAsync<PlaybackHistory>();
            await _db.CreateTableAsync<GeofenceEvent>();
        }
        finally { _initLock.Release(); }
    }

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is null) await InitAsync();
        return _db!;
    }

    public async Task<List<POI>> GetCachedPoisAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<POI>().Where(p => p.IsActive).ToListAsync();
    }

    public async Task UpsertPoisAsync(IEnumerable<POI> pois)
    {
        var db = await GetDbAsync();
        foreach (var p in pois)
            await db.InsertOrReplaceAsync(p);
    }

    public async Task<POI?> GetPoiByIdAsync(int id)
    {
        var db = await GetDbAsync();
        return await db.Table<POI>().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task SaveUserAsync(User user)
    {
        var db = await GetDbAsync();
        await db.InsertOrReplaceAsync(user);
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<User>().FirstOrDefaultAsync();
    }

    public async Task ClearUserAsync()
    {
        var db = await GetDbAsync();
        await db.DeleteAllAsync<User>();
    }

    public async Task AddPlaybackHistoryAsync(PlaybackHistory h)
    {
        var db = await GetDbAsync();
        await db.InsertAsync(h);
    }

    public async Task<List<PlaybackHistory>> GetPlaybackHistoryAsync(int limit = 50)
    {
        var db = await GetDbAsync();
        return await db.Table<PlaybackHistory>()
            .OrderByDescending(h => h.PlayedAt).Take(limit).ToListAsync();
    }

    public async Task AddGeofenceEventAsync(GeofenceEvent evt)
    {
        var db = await GetDbAsync();
        await db.InsertAsync(evt);
    }
}
