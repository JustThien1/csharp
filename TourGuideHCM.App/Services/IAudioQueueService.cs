using System.Collections.ObjectModel;
using TourGuideHCM.App.Models;

namespace TourGuideHCM.App.Services;

/// <summary>
/// Service quản lý queue phát audio cho audio tour.
/// Khi user đi qua vùng có nhiều POI, thay vì phát chồng tiếng nhau,
/// service sẽ xếp hàng (priority theo khoảng cách gần nhất) và phát lần lượt.
/// </summary>
public interface IAudioQueueService
{
    // ====================== STATE ======================

    /// <summary>POI đang phát hiện tại (null nếu queue rỗng).</summary>
    QueuedPoi? Current { get; }

    /// <summary>Danh sách POI đang chờ phát — UI có thể bind trực tiếp.</summary>
    ReadOnlyObservableCollection<QueuedPoi> Queue { get; }

    // ====================== EVENTS ======================

    /// <summary>
    /// Bắn khi queue hoặc POI đang phát thay đổi (thêm, bớt, skip, finish).
    /// MapViewModel subscribe để OnPropertyChanged các binding.
    /// </summary>
    event EventHandler? QueueChanged;

    // ====================== ACTIONS ======================

    /// <summary>
    /// Cập nhật danh sách POI trong vùng phát hiện. Service sẽ:
    /// - Thêm POI mới vào queue (nếu chưa phát recently)
    /// - Bỏ POI không còn trong vùng
    /// - Tự động phát POI đầu queue nếu chưa có gì đang phát
    /// </summary>
    /// <param name="poisInRange">Tuple (POI, khoảng cách mét) đã filter radius.</param>
    /// <param name="language">Ngôn ngữ narration ("vi"/"en").</param>
    void UpdateInRangePois(List<(POI poi, double distance)> poisInRange, string language);

    /// <summary>Bỏ qua POI đang phát, chuyển sang POI kế tiếp trong queue.</summary>
    void Skip();

    /// <summary>Dừng toàn bộ — clear queue + stop audio đang phát.</summary>
    void StopAll();
}