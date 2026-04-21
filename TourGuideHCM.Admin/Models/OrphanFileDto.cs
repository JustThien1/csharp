namespace TourGuideHCM.Admin.Models;

public class OrphanFileDto
{
    public string FileName { get; set; } = "";
    public string Url { get; set; } = "";
    public int SizeKB { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string GuessedLanguage { get; set; } = "vi";
}
