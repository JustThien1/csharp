public class DashboardDto
{
    public int TotalPoi { get; set; }
    public string TopPoi { get; set; }
    public int AvgTime { get; set; }

    public List<TopPoi> TopPois { get; set; } = new();
    public int[] DailyViews { get; set; } = Array.Empty<int>();
}

public class TopPoi
{
    public string Name { get; set; }
    public int Count { get; set; }
}