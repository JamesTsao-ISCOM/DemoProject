namespace Project01_movie_lease_system.Models;
public class LeaseStatistics
{
    public int PendingLeases { get; set; } // 待處理
    public int CompletedLeases { get; set; } // 已完成
    public int ActiveLeases { get; set; } // 租賃中
    public int CancelledLeases { get; set; } // 已取消
}