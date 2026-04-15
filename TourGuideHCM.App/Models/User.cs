using SQLite;

namespace TourGuideHCM.App.Models;

[Table("LocalUser")]
public class User
{
    [PrimaryKey]
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PreferredLanguage { get; set; } = "vi";

    public bool TtsEnabled { get; set; } = true;

    public bool IsLoggedIn { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}
