using CineTrack.Shared.DTOs;

namespace CineTrack.MvcUI.Models;

public class UserProfileViewModel
{
    // Kullanıcı Bilgileri
    public UserResponseDto User { get; set; }
    
    // Sahiplik ve Takip Durumu
    public bool IsOwner { get; set; }       // Profili görüntüleyen kişi sahibi mi?
    public bool IsFollowing { get; set; }   // Ziyaretçi bu kişiyi takip ediyor mu?
    
    // İstatistikler
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }

    // Listeler (Sekmeler için gruplanmış)
    public List<ContentDto> WatchedMovies { get; set; } = new(); // İzlediklerim
    public List<ContentDto> WatchlistMovies { get; set; } = new(); // İzlenecekler
    public List<ContentDto> ReadBooks { get; set; } = new();     // Okuduklarım
    public List<ContentDto> ReadingListBooks { get; set; } = new(); // Okunacaklar
    
    // Kullanıcının oluşturduğu diğer özel listeler
    public List<UserListDto> CustomLists { get; set; } = new();

    // Son Aktiviteler
    public List<ActivityDto> RecentActivities { get; set; } = new();
}