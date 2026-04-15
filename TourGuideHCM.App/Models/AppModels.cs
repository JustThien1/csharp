using SQLite;

// KHÔNG dùng System.ComponentModel.DataAnnotations (gây conflict với SQLite attributes)

namespace TourGuideHCM.App.Models;

[Table("PlaybackHistory")]
public class PlaybackHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    public string TriggerType { get; set; } = string.Empty;
    public bool WasAudio { get; set; }
}

[Table("GeofenceEvents")]
public class GeofenceEvent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int PoiId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public double DistanceAtTrigger { get; set; }
}

public class LocationUpdate
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Accuracy { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class NarrationRequest
{
    public POI Poi { get; set; } = null!;
    public string Language { get; set; } = "vi";
    public string TriggerType { get; set; } = "manual";
    public bool PreferAudioFile { get; set; } = true;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public class GeofenceTriggerRequest
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}

public class GeofenceTriggerResponse
{
    public bool Triggered { get; set; }
    public int PoiId { get; set; }
    public string PoiName { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? NarrationText { get; set; }
}

public class PlaybackLogRequest
{
    public int UserId { get; set; }
    public int POIId { get; set; }
    public int? DurationSeconds { get; set; }
    public string TriggerType { get; set; } = "manual";
}
