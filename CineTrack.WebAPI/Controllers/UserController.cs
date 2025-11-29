using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineTrack.WebAPI.Services;

namespace CineTrack.WebAPI.Controllers;

// 1. DTO Tanımı (Sınıfın dışına veya içine ekleyebilirsiniz)
public class UserUpdateDto
{
	public string? Bio { get; set; }
	public string? AvatarUrl { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
	private readonly UserService _userService;

	public UserController(UserService userService)
	{
		_userService = userService;
	}

	// ... (GetProfile metodu aynı kalacak) ...
	[HttpGet("{id}")]
	public async Task<IActionResult> GetProfile(int id)
	{
		var user = await _userService.GetProfileAsync(id);
		if (user == null)
			return NotFound(new { message = "Kullanici bulunamadi." });

		return Ok(user);
	}

	// 2. Profil Güncelleme (GÜNCELLENDİ)
	[Authorize]
	[HttpPut("update")]
	public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto body)
	{
		// Artık body.Bio ve body.AvatarUrl tip güvenli
		var updated = await _userService.UpdateProfileAsync(body.Bio, body.AvatarUrl);

		if (updated == null)
			return NotFound(new { message = "Kullanici bulunamadi." });

		return Ok(updated);
	}

	// 3) Takip et
	[Authorize]
	[HttpPost("follow/{id}")]
	public async Task<IActionResult> Follow(int id)
	{
		bool result = await _userService.FollowAsync(id);
		if (!result)
			return BadRequest(new { message = "Takip islemi basarisiz." });

		return Ok(new { message = "Kullanici takip edildi." });
	}

	// 4) Takipten cik
	[Authorize]
	[HttpPost("unfollow/{id}")]
	public async Task<IActionResult> Unfollow(int id)
	{
		bool result = await _userService.UnfollowAsync(id);
		if (!result)
			return BadRequest(new { message = "Takipten cikma islemi basarisiz." });

		return Ok(new { message = "Kullanici takipten cikarildi." });
	}

	// 5) Takipcileri getir
	[HttpGet("{id}/followers")]
	public async Task<IActionResult> GetFollowers(int id)
	{
		var followers = await _userService.GetFollowersAsync(id);
		return Ok(followers);
	}

	// 6) Takip ettikleri getir
	[HttpGet("{id}/following")]
	public async Task<IActionResult> GetFollowing(int id)
	{
		var following = await _userService.GetFollowingAsync(id);
		return Ok(following);
	}
}
