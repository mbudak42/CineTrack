using CineTrack.Shared.DTOs;

namespace CineTrack.MvcUI.Models;

public class UserProfileViewModel
{
	// Kullanıcı Bilgileri
	public UserResponseDto User { get; set; }

	// Sahiplik ve Takip Durumu
	public bool IsOwner { get; set; }
	public bool IsFollowing { get; set; }

	// İstatistikler
	public int FollowersCount { get; set; }
	public int FollowingCount { get; set; }

	// Listeler
	public List<ContentDto> WatchedMovies { get; set; } = new();
	public int WatchedListId { get; set; } // YENİ

	public List<ContentDto> WatchlistMovies { get; set; } = new();
	public int WatchlistListId { get; set; } // YENİ

	public List<ContentDto> ReadBooks { get; set; } = new();
	public int ReadListId { get; set; } // YENİ

	public List<ContentDto> ReadingListBooks { get; set; } = new();
	public int ReadingListId { get; set; } // YENİ

	// Özel listeler
	public List<UserListDto> CustomLists { get; set; } = new();

	// Son Aktiviteler
	public List<ActivityDto> RecentActivities { get; set; } = new();
}