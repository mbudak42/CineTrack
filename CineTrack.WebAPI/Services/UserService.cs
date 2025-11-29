using CineTrack.DataAccess;
using CineTrack.DataAccess.Entities;
using CineTrack.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CineTrack.WebAPI.Services;

public class UserService
{
	private readonly CineTrackDbContext _context;
	private readonly IHttpContextAccessor _httpContextAccessor;

	public UserService(CineTrackDbContext context, IHttpContextAccessor httpContextAccessor)
	{
		_context = context;
		_httpContextAccessor = httpContextAccessor;
	}

	// 1) Aktif kullanıcı ID’sini JWT’den çek
	private int GetCurrentUserId()
	{
		var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return userId != null ? int.Parse(userId) : 0;
	}

	// 2) Kullanıcı profilini getir
	public async Task<UserResponseDto?> GetProfileAsync(int userId)
	{
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return null;

		return new UserResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			Email = user.Email,
			AvatarUrl = user.AvatarUrl,
			Bio = user.Bio,
			CreatedAt = user.CreatedAt
		};
	}

	// 3) Profil güncelle
	public async Task<UserResponseDto?> UpdateProfileAsync(string? bio, string? avatarUrl)
	{
		var userId = GetCurrentUserId();
		var user = await _context.Users.FindAsync(userId);
		if (user == null) return null;

		user.Bio = bio ?? user.Bio;
		user.AvatarUrl = avatarUrl ?? user.AvatarUrl;

		await _context.SaveChangesAsync();

		return new UserResponseDto
		{
			Id = user.Id,
			Username = user.Username,
			Email = user.Email,
			AvatarUrl = user.AvatarUrl,
			Bio = user.Bio,
			CreatedAt = user.CreatedAt
		};
	}

	// 4) Takip et
	public async Task<bool> FollowAsync(int targetId)
	{
		var currentUserId = GetCurrentUserId();
		if (currentUserId == targetId) return false;

		var alreadyFollowing = await _context.Follows
			.AnyAsync(f => f.FollowerId == currentUserId && f.FollowedId == targetId);

		if (alreadyFollowing) return false;

		_context.Follows.Add(new Follow
		{
			FollowerId = currentUserId,
			FollowedId = targetId
		});
		await _context.SaveChangesAsync();
		return true;
	}

	// 5) Takipten çık
	public async Task<bool> UnfollowAsync(int targetId)
	{
		var currentUserId = GetCurrentUserId();

		var follow = await _context.Follows
			.FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowedId == targetId);

		if (follow == null) return false;

		_context.Follows.Remove(follow);
		await _context.SaveChangesAsync();
		return true;
	}

	// 6) Takipçileri getir
	public async Task<List<UserResponseDto>> GetFollowersAsync(int userId)
	{
		var followers = await _context.Follows
			.Where(f => f.FollowedId == userId)
			.Include(f => f.Follower)
			.Select(f => new UserResponseDto
			{
				Id = f.Follower.Id,
				Username = f.Follower.Username,
				Email = f.Follower.Email,
				AvatarUrl = f.Follower.AvatarUrl,
				Bio = f.Follower.Bio,
				CreatedAt = f.Follower.CreatedAt
			}).ToListAsync();

		return followers;
	}

	//> takip ettiklerigetir
	public async Task<List<UserResponseDto>> GetFollowingAsync(int userId)
	{
		var following = await _context.Follows
			.Where(f => f.FollowerId == userId)
			.Include(f => f.Followed)
			.Select(f => new UserResponseDto
			{
				Id = f.Followed.Id,
				Username = f.Followed.Username,
				Email = f.Followed.Email,
				AvatarUrl = f.Followed.AvatarUrl,
				Bio = f.Followed.Bio,
				CreatedAt = f.Followed.CreatedAt
			}).ToListAsync();

		return following;
	}
}
